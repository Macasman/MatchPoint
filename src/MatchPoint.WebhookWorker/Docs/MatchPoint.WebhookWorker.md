# MatchPoint — Webhook Worker (`PaymentsWebhook`)
_Gerado em 2025-08-31 01:29:28Z (UTC)_

## 1) Objetivo
Worker Service independente que **lê jobs** da tabela `integration.WebhookQueue` (Status = Pending), **dispara um HTTP POST** para uma URL configurável (`UrlToRequest`) e **marca o resultado** no banco com `Sent/Failed/DeadLetter`, aplicando **retry com backoff exponencial**.

## 2) Fluxo (alto nível)
```
API (MatchPoint)                Worker Service                      Destino (Webhook)
------------------             ----------------                     -----------------
Create Reservation  ─────┐
                         ├─▶ Cria PaymentIntent (Pending)
                         └─▶ Enfileira job em integration.WebhookQueue (Status=0 Pending)
                                   │
                                   ▼
                         Dequeue (marca Processing) ──▶ POST {payload} em UrlToRequest
                                   │                          (IHttpClientFactory)
                      AckSuccess (Sent)  ◀─────────── 2xx     ou AckFailure (Failed/DeadLetter)
                                   │
                                   ▼
                           Próximo ciclo conforme ServiceExecutionPaymentsWebhook
```

### Status da fila
- **0 Pending**: pronto para envio (e `NextAttemptUtc <= now`)
- **1 Processing**: selecionado no ciclo atual
- **2 Sent**: sucesso (o Worker incrementa `Attempts`)
- **3 Failed**: falha; re-tenta após `NextAttemptUtc` (backoff)
- **4 DeadLetter**: excedeu `MaxAttempts`

## 3) Banco de dados — DDL da fila
```sql
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'integration')
    EXEC('CREATE SCHEMA integration');

IF OBJECT_ID('integration.WebhookQueue', 'U') IS NULL
BEGIN
    CREATE TABLE integration.WebhookQueue
    (
        Id             BIGINT IDENTITY(1,1) PRIMARY KEY,
        AggregateType  VARCHAR(50)   NOT NULL,   -- ex.: 'PaymentIntent'
        AggregateId    BIGINT        NOT NULL,   -- ex.: PaymentIntentId
        Payload        NVARCHAR(MAX) NOT NULL,   -- JSON que será postado
        Status         TINYINT       NOT NULL,   -- 0=Pending,1=Processing,2=Sent,3=Failed,4=DeadLetter
        Attempts       INT           NOT NULL CONSTRAINT DF_WebhookQueue_Attempts DEFAULT(0),
        LastError      NVARCHAR(1000)    NULL,
        NextAttemptUtc DATETIME2     NOT NULL CONSTRAINT DF_WebhookQueue_NextAttempt DEFAULT(SYSUTCDATETIME()),
        CreationDate   DATETIME2     NOT NULL CONSTRAINT DF_WebhookQueue_Created DEFAULT(SYSUTCDATETIME()),
        UpdateDate     DATETIME2         NULL
    );

    CREATE INDEX IX_WebhookQueue_Status_NextAttempt
        ON integration.WebhookQueue(Status, NextAttemptUtc)
        INCLUDE (Id, CreationDate);

    CREATE INDEX IX_WebhookQueue_Aggregate
        ON integration.WebhookQueue(AggregateType, AggregateId);
END
```

### Exemplo de enfileiramento manual
```sql
INSERT INTO integration.WebhookQueue
  (AggregateType, AggregateId, Payload, Status, NextAttemptUtc)
VALUES
  ('PaymentIntent', 6, N'{"paymentIntentId":6,"event":"payment.captured","providerRef":"manual"}', 0, SYSUTCDATETIME());
```

## 4) `appsettings.json` (Worker)
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,11433;Database=MatchPointDB;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True"
  },
  "UrlToRequest": "http://localhost:5000/payments/provider/webhook",
  "ServiceExecutionPaymentsWebhook": "00:00:30",
  "WebhookWorker": {
    "WorkerName": "PaymentsWebhook",
    "BatchSize": 200,
    "MaxAttempts": 6,
    "HttpTimeoutSeconds": 10,
    "BackoffSecondsBase": 30
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft": "Warning", "System": "Warning" }
  }
}
```
> **ServiceExecution{WorkerName}**: intervalo (TimeSpan) entre varreduras, e.g. `00:05:00` para 5 minutos.  
> **UrlToRequest**: destino do POST. Para testes: webhook.site.

## 5) Dependências & Build
Projeto **Worker** (solution separada):
- Pacotes:
  - `Microsoft.Extensions.Http` (para `AddHttpClient`/`IHttpClientFactory`).
  - `Microsoft.Data.SqlClient` (provider ADO.NET do Worker).
- Registro de serviços (**Program.cs** — versão utilizada):
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatchPoint.WebhookWorker.Data;
using MatchPoint.WebhookWorker.Models;
using MatchPoint.WebhookWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<WebhookWorkerOptions>(builder.Configuration.GetSection("WebhookWorker"));

builder.Services.AddSingleton<SqlDbContext>();
builder.Services.AddSingleton<IWebhookQueueRepository, WebhookQueueRepository>(); // Singleton para compatibilizar com IHostedService

builder.Services.AddHttpClient(nameof(WebhookDispatcherService));

builder.Services.AddHostedService<WebhookDispatcherService>();

await builder.Build().RunAsync();
```
**Build/Run**
```bash
dotnet restore
dotnet build
dotnet run --project src/MatchPoint.WebhookWorker
```

## 6) Contratos & Payloads
- **Payload HTTP** que o Worker envia: o conteúdo exato do campo `Payload` da fila. Para o caso de pagamentos simulados:
```json
{
  "paymentIntentId": 6,
  "event": "payment.captured",
  "providerRef": "sim-auto"
}
```
- **Headers**: por padrão nenhum. Para usar API Key/Bearer, adicione no `WebhookDispatcherService` algo como:
```csharp
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
http.DefaultRequestHeaders.Add("X-Source", "MatchPoint.Worker");
```

## 7) Ciclo do `WebhookDispatcherService`
1. Calcula `Interval` lendo `ServiceExecution{WorkerName}`.
2. `DequeueBatchAsync(max)`: atualiza `Pending -> Processing` e retorna os itens.
3. Para cada item:
   - `POST UrlToRequest` com `Payload` como `application/json`.
   - Sucesso (2xx) → `AckSuccessAsync` (`Sent`, `Attempts++`).
   - Falha → `AckFailureAsync` (`Failed`, `Attempts++`, `NextAttemptUtc += backoff`). `DeadLetter` quando `Attempts >= MaxAttempts`.
4. Aguarda `Interval` e repete.

### Backoff
`nextSeconds = min(BackoffSecondsBase * 2^attempts, 3600)` (capped em 1h).

## 8) Repositório (`WebhookQueueRepository`)
- **DequeueBatchAsync**: `UPDATE ... OUTPUT` para garantir atomicidade e evitar concorrência (usa `ROWLOCK/READPAST/UPDLOCK`).
- **AckSuccessAsync**: `Status = Sent`, `Attempts++`, `LastError = NULL`, `UpdateDate = now`.
- **AckFailureAsync**: `Status = Failed` (ou `DeadLetter`), `Attempts++`, `LastError`, `NextAttemptUtc = now + backoff`.

## 9) Teste rápido (webhook.site)
1. **Worker**: configure `"UrlToRequest"` para a URL do webhook.site.
2. **Fila**: insira um job `Pending` (vide exemplo do §3).
3. **Observe**: a requisição aparece no webhook.site com o JSON do `Payload`.
4. **Banco**: verifique `Status`, `Attempts`, `LastError`:
```sql
SELECT TOP (10) Id, Status, Attempts, LastError, NextAttemptUtc, UpdateDate
FROM integration.WebhookQueue ORDER BY Id DESC;
```

## 10) Integração com a API (MatchPoint)
- A API, ao **criar `PaymentIntent`**, pode **enfileirar** automaticamente o evento de captura simulada:
  - Interface: `MatchPoint.Application/Interfaces/IWebhookQueueWriter.cs`
  - Implementação: `MatchPoint.Infrastructure/Messaging/WebhookQueueSqlWriter.cs` (usa `System.Data.SqlClient` na API)
  - Handler: `CreateReservationCommandHandler` chama `EnqueuePaymentEventAsync(..., "payment.captured", ...)`.
- Endpoint manual na API (opcional):
```http
POST /payments/intents/{id}/enqueue-capture  -> 202 Accepted { jobId, ... }
```

## 11) Troubleshooting
- **Erro de lifetime (Scoped em HostedService)**: no Worker, registre `IWebhookQueueRepository` como **Singleton/Transient** ou crie escopos com `IServiceScopeFactory`.
- **404 no destino**: valide `UrlToRequest` e se o endpoint existe/publica. Se for a própria API, confirme prefixos (`/api`).
- **Jobs não processados**: garanta `Status=0` e `NextAttemptUtc <= now`; confira `ServiceExecution{WorkerName}` e logs.
- **Timeout**: aumente `WebhookWorker.HttpTimeoutSeconds`.
- **DeadLetter**: aumente `MaxAttempts` e verifique `LastError`.
- **Payload muito grande**: `NVARCHAR(MAX)` está ok; valide no destino o limite do corpo.

## 12) Segurança & Observabilidade
- **Segurança**: para POST autenticado, armazene segredo no `appsettings`/KeyVault e adicione o header de auth no `HttpClient`.
- **Logs**: inclua `JobId`, `HTTP StatusCode`, `Attempts`, `ElapsedMs`.
- **Métricas** (futuro): contadores de `Sent/Failed/DeadLetter` por janela.

## 13) Roadmap curto
- Headers configuráveis via `appsettings` (Authorization, X-Correlation-Id).
- Suporte a múltiplos destinos (por `AggregateType`).
- Particionamento/concorrência configurável por fila.
- Telemetria (OpenTelemetry) e dashboards (Grafana/Prometheus).

---

### Anexos úteis
**SQL para reprocessar um job:**
```sql
UPDATE integration.WebhookQueue
SET Status = 0, Attempts = 0, LastError = NULL, NextAttemptUtc = SYSUTCDATETIME()
WHERE Id = @JobId;
```

**SQL para listar DeadLetters:**
```sql
SELECT * FROM integration.WebhookQueue WHERE Status = 4 ORDER BY UpdateDate DESC;
```

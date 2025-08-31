# Documentação geral
_Gerado em 2025-08-30 23:58:58Z (UTC)_


## 1) Visão geral
O projeto **MatchPoint** é um backend .NET (ASP.NET Core) organizado em camadas **Domain**, **Application**, **Infrastructure** e **API**. 
Ele provê endpoints REST para **usuários**, **recursos**, **reservas**, **pagamentos** e **KYC**, além de endpoints de saúde.
Persistimos dados em **SQL Server** (schemas `booking` e `payments`) e utilizamos **RabbitMQ**, **Redis** e **MongoDB** para funcionalidades auxiliares (mensageria, cache e auditoria/logs, respectivamente).

Principais decisões de modelagem:
- **Enums** são mapeados como `TINYINT` no SQL (cast explícito).
- Moeda padrão **BRL** e valores monetários em **centavos** (`int`).
- **Datas UTC**; atualizações usam `SYSUTCDATETIME()`.
- Regras de conflito de agendamento com checagem de **sobreposição** (locks `UPDLOCK, HOLDLOCK` sob `SERIALIZABLE`).

---

## 2) Arquitetura (alto nível)
```
┌─────────────────────┐
│      API (REST)     │  Controllers: Users, Resources, Reservations, Payments, Kyc, Health, Ping
└─────────┬───────────┘
          │ IMediator (MediatR)
┌─────────▼───────────┐
│    Application      │  Commands/Queries/Handlers (orquestração)
└─────────┬───────────┘
          │ Repositories (interfaces)
┌─────────▼───────────┐
│   Infrastructure    │  Repositórios SQL (ADO.NET), SqlDbContext, Mensageria, Auditoria
└─────────┬───────────┘
          │ ADO.NET (SqlConnection/SqlCommand)
┌─────────▼───────────┐
│       SQL Server    │  Schemas: booking, payments
└─────────────────────┘

Serviços auxiliares: MongoDB (logs/auditoria), Redis (cache), RabbitMQ (eventos), Metabase (BI).
```

---

## 3) Domínio
### 3.1 Enums
```csharp
public enum ReservationStatus : byte
{ Scheduled = 1, Completed = 2, CanceledByUser = 3, CanceledByAdmin = 4, NoShow = 5 }

public enum PaymentIntentStatus : byte
{ Pending = 1, Authorized = 2, Captured = 3, Failed = 4, Canceled = 5 }
```

### 3.2 Entidades
**Reservation**
- `ReservationId (long)`
- `UserId (long)`
- `ResourceId (long)`
- `StartTime (DateTime)`
- `EndTime (DateTime)`
- `Status (ReservationStatus)`
- `PriceCents (int)`
- `Currency (char[3])` _(padrão "BRL")_
- `Notes (string?)`
- `CreationDate (datetime2)`
- `UpdateDate (datetime2?)`

**PaymentIntent**
- `PaymentIntentId (long)`
- `ReservationId (long)`
- `AmountCents (int)`
- `Currency (char[3])`
- `Status (PaymentIntentStatus)`
- `Provider (nvarchar?)` _(ex.: "Simulado")_
- `ProviderRef (nvarchar?)`
- `CreationDate (datetime2)`
- `UpdateDate (datetime2?)`

> Observação: Entidades de **Resources**, **Users** e **KYC** existem no projeto (controladores e handlers) mas não foram coladas aqui; a documentação assume esses módulos ativos.

---

## 4) Persistência
### 4.1 `SqlDbContext`
Wrapper simples para criar `SqlConnection` a partir de `IConfiguration` (`ConnectionStrings:SqlServer`).

### 4.2 Repositórios (ADO.NET)
- **ReservationRepository**
  - `CreateAsync`/`GetByIdAsync`
  - `ListByUserAsync` e `ListByResourceAsync` com **paginação** e filtros (`from`, `to`, `status`).
  - `CancelAsync` (atualiza `Status` para `CanceledByUser`).
  - `CreateIfNoOverlapAsync` com transação `SERIALIZABLE` e locks `UPDLOCK, HOLDLOCK` para evitar **overbooking**:
    - Verifica conflito (`StartTime < @EndTime` e `EndTime > @StartTime`) para `Status IN (Scheduled, Completed)`.
    - Insere a reserva se não houver conflito.
- **PaymentIntentRepository**
  - `CreateAsync`, `GetByIdAsync`, `CaptureAsync` (altera para `Captured`), 
  - `CreateForReservationAsync` (cria PI **vinculado a ReservationId** com `Status=Pending`),
  - `CancelByReservationAsync` (cancela PIs `Pending/Authorized` de uma Reserva).

### 4.3 Esquema SQL (sugestão base)
> Ajuste conforme sua migração real; abaixo um baseline coerente com o código.

```sql
-- Schemas
CREATE SCHEMA booking;
CREATE SCHEMA payments;

-- booking.Reservations
CREATE TABLE booking.Reservations (
  ReservationId BIGINT IDENTITY(1,1) PRIMARY KEY,
  UserId BIGINT NOT NULL,
  ResourceId BIGINT NOT NULL,
  StartTime DATETIME2 NOT NULL,
  EndTime DATETIME2 NOT NULL,
  Status TINYINT NOT NULL,              -- ReservationStatus
  PriceCents INT NOT NULL,
  Currency CHAR(3) NOT NULL DEFAULT 'BRL',
  Notes NVARCHAR(MAX) NULL,
  CreationDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdateDate DATETIME2 NULL
);
CREATE INDEX IX_Reservations_Resource_Time ON booking.Reservations(ResourceId, StartTime, EndTime);
CREATE INDEX IX_Reservations_User_Time ON booking.Reservations(UserId, StartTime);

-- payments.PaymentIntents
CREATE TABLE payments.PaymentIntents (
  PaymentIntentId BIGINT IDENTITY(1,1) PRIMARY KEY,
  ReservationId BIGINT NOT NULL,
  AmountCents INT NOT NULL,
  Currency CHAR(3) NOT NULL DEFAULT 'BRL',
  Status TINYINT NOT NULL,              -- PaymentIntentStatus
  Provider NVARCHAR(100) NULL,
  ProviderRef NVARCHAR(200) NULL,
  CreationDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdateDate DATETIME2 NULL
);
CREATE INDEX IX_PaymentIntents_Reservation ON payments.PaymentIntents(ReservationId);
```

> Nota: leituras usam `WITH (NOLOCK)`; considere trocar por SNAPSHOT se precisar de consistência de leitura sem bloqueio.

---

## 5) Application (MediatR)
### 5.1 Reservations
- **CreateReservationCommand** → Handler
  - Validações: `StartTime < EndTime`, `PriceCents >= 0`, `Currency == "BRL"`.
  - Verifica **Resource** ativo (via `IResourceRepository`).
  - `CreateIfNoOverlapAsync` no repositório.
  - **Cria PaymentIntent** pendente associado (via `IPaymentIntentRepository.CreateForReservationAsync`).
  - (Em versão anterior também **logava auditoria** via `IAuditRepository` e publicava evento `reservations.created` via `IEventPublisher`).
- **CancelReservationCommand** → Handler
  - Atualiza `Status` para `CanceledByUser`.
  - Cancela PaymentIntents `Pending/Authorized` da reserva.
  - (Registra auditoria quando disponível).
- **Queries**
  - `GetReservationByIdQuery`
  - `ListReservationsByUserQuery`
  - `ListReservationsByResourceQuery`
  - Mapeiam `ReservationStatus` ⇄ `byte` apenas na borda (repositório).

### 5.2 Payments
- **CreatePaymentIntentCommand** → Handler (`Pending`).
- **CapturePaymentIntentCommand** → Handler (`Captured`) + auditoria.
- **GetPaymentIntentByIdQuery**.

### 5.3 KYC
- Endpoints para criar/ver/atualizar verificação e obter a última por usuário
  (handlers/entidades não colados aqui, mas presentes no projeto).

---

## 6) API (Controllers e rotas)
> Prefixo de rota entre colchetes, método e resumo.

- **[GET] /ping** — sanidade geral.
- **[GET] /health/db** — conexão SQL simples.

- **UsersController**
  - **[POST] /users** — cria usuário.
  - **[GET] /users/{id}** — obtém usuário.

- **ResourcesController** ([Authorize] no controller)
  - **[POST] /resources** — cria recurso.
  - **[GET] /resources/{id}** — obtém por id.
  - **[GET] /resources?active={bool}** — lista.
  - **[PATCH] /resources/{id}** — atualiza (nome, localização, preço, ativo).

- **ReservationsController** ([Authorize])
  - **[POST] /reservations** — cria reserva (retorna `201` com id). Em conflito retorna `409`.
  - **[GET] /reservations/{id}**
  - **[GET] /reservations/users/{userId}?from=&to=&status=&page=&pageSize=**
  - **[GET] /reservations/resources/{resourceId}?from=&to=&status=&page=&pageSize=**
  - **[POST] /reservations/{id}/cancel** — cancela reserva + PI vinculado.

- **PaymentsController**
  - **[POST] /payments/intents** — cria PI.
  - **[GET] /payments/intents/{id}** — consulta PI.
  - **[POST] /payments/intents/{id}/capture** — captura PI.

- **KycController**
  - **[POST] /kyc/verifications**
  - **[GET] /kyc/verifications/{id}**
  - **[GET] /kyc/users/{userId}/latest**
  - **[PATCH] /kyc/verifications/{id}** — atualiza status/score/notes.

### 6.1 Autorização/Autenticação
- `[Authorize]` aplicado em **Reservations** e **Resources** (e possivelmente outros).
- Comentários indicam uso de **JWT** (ex.: `UserId` poderia vir do token). Garanta:
  - Middleware de autenticação JWT configurado.
  - Políticas/roles onde necessário.

---

## 7) Fluxos principais
### 7.1 Criar Reserva (e PaymentIntent vinculado)
1. **API** recebe `CreateReservationCommand`.
2. **Application** valida, checa `Resource` ativo, chama `CreateIfNoOverlapAsync` (transação serializable + locks).
3. Em caso de sucesso, cria **PaymentIntent(Pending)** para a reserva.
4. (Opcional) Publica evento `reservations.created` e registra auditoria.

### 7.2 Cancelar Reserva (e PaymentIntent vinculado)
1. **API** chama `CancelReservationCommand`.
2. **Application** atualiza status da reserva para `CanceledByUser`.
3. Cancela **PaymentIntents** pendentes/autorizados dessa reserva.
4. Registra auditoria.

### 7.3 Listagens paginadas
- `ListByUserAsync` / `ListByResourceAsync` com filtros `from`, `to`, `status` e paginação `page/pageSize`.
- Ordenação por `StartTime DESC`.

### 7.4 Capturar PaymentIntent
- **API** chama `CapturePaymentIntentCommand` → `Status=Captured` + auditoria.

---

## 8) Observabilidade e BI
- **MongoDB** armazena logs/auditoria de API (ex.: Api Audit Logs).
- **Metabase** já configurado para leitura (não focaremos mais nele).
- Endpoint `/health/db` para checar conectividade SQL.
- Sugestão: adicionar structured logging (Serilog), correlação (TraceId), e dashboard de métricas (Prometheus/Grafana) futuramente.

---

## 9) Configuração / Infra
### 9.1 Variáveis de ambiente (exemplo)
```
ConnectionStrings__SqlServer="Server=localhost,11433;Database=MatchPoint;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True"
ASPNETCORE_ENVIRONMENT=Development
JWT__Authority=... (se houver)
JWT__Audience=...   (se houver)
```

### 9.2 Docker (exemplo rápido)
- Containers em uso (observados): **sqlserver**, **mongo**, **mongo-express**, **redis**, **rabbitmq**; **metabase** era opcional.
- A API pode ser executada localmente ou conteinerizada conforme `Dockerfile`/`docker-compose` do projeto (não colado aqui).

---

## 10) Pontos de atenção / melhorias
- **Duplicidade de classes/records** coladas: há dois `CapturePaymentIntentCommand` e duas versões de `CreateReservationHandler`/`CancelReservationCommandHandler` nos trechos enviados. Certifique-se de manter **apenas uma definição final** por tipo.
- Evitar `WITH (NOLOCK)` se consistência de leitura for crítica; considerar **READ COMMITTED SNAPSHOT**.
- Centralizar **auditoria** (`IAuditRepository`): hoje aparece em alguns handlers, padronizar em pipeline (MediatR behaviors).
- Validar **timezone** na borda do sistema (entradas sempre em UTC ou conversões claras).
- Padronizar **DTOs**: hoje alguns handlers fazem cast enum ↔ byte; considere usar enums nativos nos DTOs públicos.
- Testes: adicionar **unitários** e **integração** para regras de sobreposição e transações.

---

## 11) Exemplos de chamadas HTTP
```http
POST /reservations
Content-Type: application/json
Authorization: Bearer <token>

{
  "userId": 10,
  "resourceId": 5,
  "startTime": "2025-08-31T15:00:00Z",
  "endTime":   "2025-08-31T16:00:00Z",
  "priceCents": 12000,
  "currency": "BRL",
  "notes": "Quadra 2"
}
```

```http
POST /payments/intents/{id}/capture
Authorization: Bearer <token>
```

```http
GET /reservations/users/10?from=2025-08-01T00:00:00Z&to=2025-09-01T00:00:00Z&status=Scheduled&page=1&pageSize=20
Authorization: Bearer <token>
```

---

## 12) Roadmap técnico curto
- **Autenticação JWT**: revisar configuração/claims e integrar `UserId` nos comandos via contexto.
- **Webhooks de pagamentos** (quando trocar o `Provider` “Simulado” por gateway real).
- **Métricas** (Prometheus) e logs estruturados (Serilog).
- **Migrations** (EF Core ou DbUp) para versionar DDL.
- **CI/CD** com validações (build, testes, migrações) e deploy.

---

## 13) Anexos
- Este documento foi gerado automaticamente a partir do código compartilhado e pode ser mantido no repositório em `docs/Documentação geral.md`.

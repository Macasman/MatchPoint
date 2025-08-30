# MatchPoint - Documentação Técnica (Audit Log)

## Visão Geral do Fluxo
Fluxo responsável por gravar **eventos de auditoria** em `audit.AuditEvents` sempre que ações importantes ocorrerem (criação, atualização de status, captura de pagamento, etc.).  
Objetivo: garantir **rastreabilidade, compliance e auditoria**.

---

## Domain

### `AuditEvent.cs`
Modelo de domínio que representa um evento de auditoria.

**Propriedades**
- `AuditId` → identificador único (identity).
- `Aggregate` → nome da entidade (ex.: Reservation, PaymentIntent, KycVerification).
- `AggregateId` → identificador da entidade.
- `Action` → ação realizada (Created, Updated, Captured, etc).
- `Data` → JSON ou texto com detalhes adicionais.
- `UserId` → usuário que realizou a ação (quando aplicável).
- `CreationDate` → data/hora da auditoria (UTC).

---

## Application

### `IAuditRepository.cs`
Interface que define operação de gravação de eventos de auditoria:
- `LogAsync(AuditEvent e, CancellationToken ct)` → insere um evento no banco.

---

## Infrastructure

### `AuditRepository.cs`
Implementação ADO.NET do `IAuditRepository`.

**Métodos**
- `LogAsync`: executa `INSERT INTO audit.AuditEvents ...`.

**Observações**
- Usa `SqlConnection` + `SqlCommand` diretamente.
- Apenas gravação (não há leitura/exposição via API).

---

## API (uso indireto)
Não há controller dedicada para `AuditEvents`.  
Os registros são gravados automaticamente nos Handlers de **Reservations**, **Payments** e **KYC** sempre que uma ação ocorre.

Exemplos:
- No `CreateReservationHandler`: grava evento `"Reservation Created"`.
- No `CapturePaymentIntentHandler`: grava evento `"PaymentIntent Captured"`.
- No `UpdateKycStatusHandler`: grava evento `"KycVerification Updated"`.

---

## SQL (estrutura da tabela)
```sql
CREATE SCHEMA audit;
GO

CREATE TABLE audit.AuditEvents
(
    AuditId       BIGINT IDENTITY(1,1) PRIMARY KEY,
    Aggregate     NVARCHAR(100) NOT NULL,
    AggregateId   BIGINT NOT NULL,
    Action        NVARCHAR(50) NOT NULL,
    Data          NVARCHAR(MAX) NULL,
    UserId        BIGINT NULL,
    CreationDate  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

---

## DI / Program.cs
```csharp
builder.Services.AddSingleton<IAuditRepository, AuditRepository>();
```

---

## Fluxo Fim-a-fim
1. Um Handler executa (ex.: `CreateReservationHandler`).
2. Após salvar a entidade principal, chama `_audit.LogAsync(...)`.
3. O evento é gravado em `audit.AuditEvents` com Aggregate, Id, Action e dados extras.

---

## Exemplos de Eventos Gravados
- **Reservation Created**
```json
{
  "Aggregate": "Reservation",
  "AggregateId": 101,
  "Action": "Created",
  "Data": "{ \"UserId\": 1, \"ResourceId\": 5 }",
  "UserId": 1
}
```
- **PaymentIntent Captured**
```json
{
  "Aggregate": "PaymentIntent",
  "AggregateId": 42,
  "Action": "Captured",
  "Data": "{ \"Captured\": true }",
  "UserId": null
}
```
- **KYC Updated**
```json
{
  "Aggregate": "KycVerification",
  "AggregateId": 7,
  "Action": "Updated",
  "Data": "{ \"Status\": 1, \"Score\": 90 }",
  "UserId": 3
}
```

---

## Roadmap Próximos Passos
- Criar endpoint de consulta de eventos (somente admin).
- Publicar eventos em RabbitMQ além de salvar no SQL.
- Criar política de retenção (ex.: particionar por data).

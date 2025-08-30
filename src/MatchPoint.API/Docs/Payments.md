# MatchPoint - Documentação Técnica (Pagamentos)

## Visão Geral do Fluxo
Fluxo responsável por criar **intenção de pagamento** (PaymentIntent), consultar status e capturar (simulação).
Arquitetura: **Clean Architecture + CQRS/MediatR + ADO.NET**.

---

## Domain

### `PaymentIntent.cs`
Modelo de domínio que representa a intenção de pagamento.

**Propriedades**
- `PaymentIntentId`, `ReservationId`, `AmountCents`, `Currency`, `Status`, `Provider`, `ProviderRef`, `CreationDate`, `UpdateDate`.

**Regra**
- Representa estado, validações básicas nos handlers.

---

## Application

### `IPaymentIntentRepository.cs`
Interface que define operações sobre PaymentIntent:
- `CreateAsync` → insere e retorna ID.
- `GetByIdAsync` → busca por ID.
- `CaptureAsync` → marca como `Captured` se pendente/autorizada.

### CQRS Handlers
- **CreatePaymentIntentCommand / Handler**
  - Entrada: ReservationId, AmountCents, Currency, Provider.
  - Valida amount > 0, cria entidade, chama repositório.
- **GetPaymentIntentByIdQuery / Handler**
  - Entrada: ID, retorna entidade ou null.
- **CapturePaymentIntentCommand / Handler**
  - Entrada: ID, atualiza status p/ Captured se permitido.

---

## Infrastructure

### `PaymentIntentRepository.cs`
Implementação ADO.NET do IPaymentIntentRepository.

**Métodos**
- `CreateAsync`: INSERT + OUTPUT INSERTED.ID.
- `GetByIdAsync`: SELECT com NOLOCK.
- `CaptureAsync`: UPDATE status.

**Observações**
- Usa `SqlConnection` + `SqlCommand`.
- NOLOCK para leitura não bloqueante.

---

## API

### `PaymentsController.cs`
Exposição REST do fluxo de pagamentos.

**Endpoints**
- `POST /payments/intents`
  - Cria intenção → retorna `201 { id }`.
- `GET /payments/intents/{id}`
  - Consulta intenção → retorna `200` ou `404`.
- `POST /payments/intents/{id}/capture`
  - Captura intenção → `200 { captured: true }` ou `400 { captured: false }`.

---

## DI / Program.cs
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<IPaymentIntentRepository, PaymentIntentRepository>();
```

---

## Fluxo Fim-a-fim
1. POST /reservations → cria reserva.
2. POST /payments/intents → cria PaymentIntent (status=Pending).
3. GET /payments/intents/{id} → consulta status.
4. POST /payments/intents/{id}/capture → atualiza p/ Captured.

---

## Exemplos de Uso (Swagger)
- Criar intenção:
```json
POST /payments/intents
{ "reservationId": 1, "amountCents": 5000, "currency": "BRL" }
```
- Consultar:
```http
GET /payments/intents/42
```
- Capturar:
```http
POST /payments/intents/42/capture
```

---

## Roadmap Próximos Passos
- KYC endpoints (`kyc.KycVerifications`).
- Audit trail (`audit.AuditEvents`).
- RabbitMQ eventos.
- Redis cache de consultas.

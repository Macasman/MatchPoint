# MatchPoint - Documentação Técnica (Reservations)

## Visão Geral do Fluxo
Fluxo responsável por criar e consultar **reservas**.  
Arquitetura: **Clean Architecture + CQRS/MediatR + ADO.NET**.

---

## Domain

### `Reservation.cs`
Modelo de domínio que representa uma reserva.

**Propriedades**
- `ReservationId`, `UserId`, `ResourceId`, `StartTime`, `EndTime`, `Status`, `PriceCents`, `Currency`, `Notes`, `CreationDate`, `UpdateDate`.

**Regras**
- `StartTime < EndTime` (checado em constraint e handler).
- `Status`: 1=Agendada, 2=Concluída, 3=Cancelada.

---

## Application

### `IReservationRepository.cs`
Interface que define operações sobre Reservation:
- `CreateAsync` → insere e retorna ID.
- `GetByIdAsync` → busca por ID.

### CQRS Handlers
- **CreateReservationCommand / Handler**
  - Entrada: UserId, ResourceId, StartTime, EndTime, PriceCents, Currency, Notes.
  - Valida StartTime < EndTime.
  - Chama repositório e retorna ReservationId.
- **GetReservationByIdQuery / Handler**
  - Entrada: ReservationId.
  - Retorna a entidade ou null.

---

## Infrastructure

### `ReservationRepository.cs`
Implementação ADO.NET do IReservationRepository.

**Métodos**
- `CreateAsync`: `INSERT INTO booking.Reservations ... OUTPUT INSERTED.ReservationId`.
- `GetByIdAsync`: SELECT com NOLOCK.

**Observações**
- Usa `SqlConnection` + `SqlCommand` diretamente.
- Mapeamento manual do `SqlDataReader`.

---

## API

### `ReservationsController.cs`
Exposição REST do fluxo de reservas.

**Endpoints**
- `POST /reservations`
  - Cria reserva → retorna `201 { id }`.
- `GET /reservations/{id}`
  - Consulta reserva → retorna `200` ou `404`.

---

## DI / Program.cs
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
```

---

## Fluxo Fim-a-fim
1. POST /reservations → cria reserva (agendada, status=1).
2. GET /reservations/{id} → consulta reserva.

---

## Exemplos de Uso (Swagger)
- Criar reserva:
```json
POST /reservations
{
  "userId": 1,
  "resourceId": 1,
  "startTime": "2025-09-01T18:00:00",
  "endTime": "2025-09-01T19:00:00",
  "priceCents": 5000,
  "currency": "BRL",
  "notes": "teste"
}
```
- Consultar:
```http
GET /reservations/42
```

---

## Roadmap Próximos Passos
- Endpoint de listagem (reservas por usuário / recurso).
- Cancelamento de reserva (status=3).
- Integração com pagamentos (linkar ao PaymentIntent).
- Auditoria (gravar evento em audit.AuditEvents).

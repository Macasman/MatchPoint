# MatchPoint - Documentação Técnica (KYC)

## Visão Geral do Fluxo
Fluxo responsável por registrar, consultar e atualizar **verificações de KYC (Know Your Customer)**.  
Arquitetura: **Clean Architecture + CQRS/MediatR + ADO.NET**.

---

## Domain

### `KycVerification.cs`
Modelo de domínio que representa uma verificação de KYC.

**Propriedades**
- `KycId`, `UserId`, `Status`, `Provider`, `Score`, `Notes`, `CreationDate`, `UpdateDate`.

**Status possíveis**
- 0 = Pendente  
- 1 = Aprovado  
- 2 = Reprovado  
- 3 = Análise

**Regras**
- `Score` entre 0 e 100 (ou nulo).

---

## Application

### `IKycVerificationRepository.cs`
Interface que define operações sobre KycVerification:
- `CreateAsync` → insere e retorna ID.
- `GetByIdAsync` → busca por ID.
- `GetLatestByUserAsync` → retorna a última verificação de um usuário.
- `UpdateStatusAsync` → atualiza status, score e notas.

### CQRS Handlers
- **CreateKycVerificationCommand / Handler**
  - Cria nova verificação para um usuário (status inicial = 0).
- **GetKycByIdQuery / Handler**
  - Busca verificação pelo `KycId`.
- **GetLatestKycByUserQuery / Handler**
  - Retorna a verificação mais recente de um usuário.
- **UpdateKycStatusCommand / Handler**
  - Atualiza status, score e notas de uma verificação existente.

---

## Infrastructure

### `KycVerificationRepository.cs`
Implementação ADO.NET do IKycVerificationRepository.

**Métodos**
- `CreateAsync`: INSERT com OUTPUT INSERTED.KycId.
- `GetByIdAsync`: SELECT por KycId.
- `GetLatestByUserAsync`: SELECT TOP(1) ordenado por CreationDate DESC.
- `UpdateStatusAsync`: UPDATE de status, score e notas.

**Observações**
- Usa `SqlConnection` + `SqlCommand` diretamente.
- Leitura com `WITH (NOLOCK)`.

---

## API

### `KycController.cs`
Exposição REST do fluxo de verificações KYC.

**Endpoints**
- `POST /kyc/verifications`
  - Cria verificação → retorna `201 { id }`.
- `GET /kyc/verifications/{id}`
  - Consulta por ID → retorna `200` ou `404`.
- `GET /kyc/users/{userId}/latest`
  - Consulta a última verificação de um usuário.
- `PATCH /kyc/verifications/{id}`
  - Atualiza status/score/notas de uma verificação.

---

## DI / Program.cs
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.Load("MatchPoint.Application")));
builder.Services.AddSingleton<IKycVerificationRepository, KycVerificationRepository>();
```

---

## Fluxo Fim-a-fim
1. POST /kyc/verifications → cria verificação (status = Pendente).
2. GET /kyc/verifications/{id} → consulta por ID.
3. GET /kyc/users/{userId}/latest → consulta a mais recente de um usuário.
4. PATCH /kyc/verifications/{id} → atualiza status, score e notas.

---

## Exemplos de Uso (Swagger)
- Criar verificação:
```json
POST /kyc/verifications
{
  "userId": 1,
  "provider": "Simulado",
  "score": 75.5,
  "notes": "Documento legível"
}
```
- Consultar por ID:
```http
GET /kyc/verifications/1
```
- Última por usuário:
```http
GET /kyc/users/1/latest
```
- Atualizar status:
```json
PATCH /kyc/verifications/1
{
  "status": 1,
  "score": 88.4,
  "notes": "Aprovado após revisão"
}
```

---

## Roadmap Próximos Passos
- Integrar provedores externos de OCR/KYC.
- Gravar eventos em `audit.AuditEvents`.
- Publicar eventos no RabbitMQ após atualizações.

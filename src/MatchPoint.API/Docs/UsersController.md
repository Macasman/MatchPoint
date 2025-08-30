# 👤 UsersController

## Base Route
```
/users
```

## Endpoints

### `POST /users`
Cria um novo usuário.

#### Request Body
```json
{
  "name": "Usuário de Teste",
  "email": "user@teste.com",
  "phone": "11988880000",
  "documentId": "12345678900",
  "birthDate": "1990-01-01",
  "password": "123456"
}
```

#### Responses
- **201 Created**
  ```json
  {
    "id": 1
  }
  ```
- **400 Bad Request**
  - Se e-mail já existir ou campos obrigatórios forem inválidos.

---

### `GET /users/{id}`
Consulta um usuário pelo ID.

#### Response
```json
{
  "userId": 1,
  "name": "Usuário de Teste",
  "email": "user@teste.com",
  "phone": "11988880000",
  "documentId": "12345678900",
  "birthDate": "1990-01-01",
  "isActive": true,
  "creationDate": "2025-08-30T03:30:00Z"
}
```

#### Responses
- **200 OK** → Usuário encontrado
- **404 Not Found** → Se usuário não existir

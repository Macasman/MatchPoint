# 🔑 AuthController

## Base Route
```
/auth
```

## Endpoints

### `POST /auth/login`
Autentica um usuário e retorna um JWT.

#### Request Body
```json
{
  "email": "user@teste.com",
  "password": "123456"
}
```

#### Responses
- **200 OK**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
  ```
- **401 Unauthorized**
  ```json
  {
    "error": "Invalid credentials"
  }
  ```

#### Observações
- O token JWT expira em 2 horas.
- Claims incluídos:
  - `sub`: UserId
  - `email`: Email
  - `uid`: UserId

# API Contract: Autenticação

Base path: `/api/v1/auth`

All responses use the project error format when failing:

```json
{
  "message": "Erro de validacao",
  "errors": ["Campo obrigatorio"]
}
```

## POST /register

Creates a public account with role `Jogador`.

**Request**:

```json
{
  "nome": "Igor Kaique",
  "email": "igor@example.com",
  "senha": "Senha1234",
  "confirmacaoSenha": "Senha1234"
}
```

**201 Response**:

```json
{
  "id": "uuid",
  "nome": "Igor Kaique",
  "email": "igor@example.com",
  "roles": ["Jogador"],
  "ativo": true,
  "jogadorId": "uuid-or-null"
}
```

**Errors**: `400`, `409`.

## POST /login

Authenticates an active user and starts a session.

**Request**:

```json
{
  "email": "igor@example.com",
  "senha": "Senha1234"
}
```

**200 Response**:

```json
{
  "accessToken": "jwt",
  "expiresIn": 900,
  "usuario": {
    "id": "uuid",
    "nome": "Igor Kaique",
    "email": "igor@example.com",
    "roles": ["Jogador"],
    "ativo": true,
    "jogadorId": "uuid-or-null"
  }
}
```

Refresh token is returned as secure HttpOnly cookie.

**Errors**: `400`, `401`, `403`, `429`.

## POST /logout

Revokes the current refresh session.

**Auth**: authenticated user or valid refresh cookie.

**204 Response**: empty.

## POST /refresh

Rotates refresh token and issues a new access token.

**Auth**: valid refresh cookie.

**200 Response**:

```json
{
  "accessToken": "jwt",
  "expiresIn": 900,
  "usuario": {
    "id": "uuid",
    "nome": "Igor Kaique",
    "email": "igor@example.com",
    "roles": ["Jogador"],
    "ativo": true,
    "jogadorId": "uuid-or-null"
  }
}
```

**Errors**: `401`, `403`, `429`.

## POST /forgot-password

Starts password recovery without exposing whether the e-mail exists.

**Request**:

```json
{
  "email": "igor@example.com"
}
```

**200 Response**:

```json
{
  "message": "Se o e-mail estiver cadastrado, enviaremos instrucoes de recuperacao."
}
```

## POST /reset-password

Resets password with a valid token.

**Request**:

```json
{
  "email": "igor@example.com",
  "token": "reset-token",
  "novaSenha": "NovaSenha123",
  "confirmacaoSenha": "NovaSenha123"
}
```

**204 Response**: empty.

**Errors**: `400`.

## POST /change-password

Changes password for authenticated user.

**Auth**: authenticated.

**Request**:

```json
{
  "senhaAtual": "Senha1234",
  "novaSenha": "NovaSenha123",
  "confirmacaoSenha": "NovaSenha123"
}
```

**204 Response**: empty.

**Errors**: `400`, `401`.

## GET /me

Returns current authenticated user.

**Auth**: authenticated.

**200 Response**:

```json
{
  "id": "uuid",
  "nome": "Igor Kaique",
  "email": "igor@example.com",
  "roles": ["Jogador"],
  "ativo": true,
  "jogadorId": "uuid-or-null",
  "discord": {
    "vinculado": false,
    "username": null
  }
}
```

## PUT /me/profile

Updates allowed own profile fields.

**Auth**: authenticated.

**Request**:

```json
{
  "nome": "Igor"
}
```

**200 Response**: same shape as `/me`.

## GET /me/permissions

Returns current roles and permission flags for frontend visibility.

**Auth**: authenticated.

**200 Response**:

```json
{
  "roles": ["Jogador"],
  "permissions": ["CanEditOwnProfile", "CanConfirmPresence"],
  "effectiveRole": "Jogador"
}
```

## Future Discord Endpoints

- `GET /discord/login`
- `GET /discord/callback`
- `POST /me/discord/link`
- `GET /me/discord/callback`
- `GET /me/discord`
- `DELETE /me/discord`

These are contract placeholders for future OAuth implementation and are not part of the MVP implementation unless explicitly approved later.

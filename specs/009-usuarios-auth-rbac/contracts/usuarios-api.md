# API Contract: Usuários

Base path: `/api/v1/usuarios`

All endpoints require authentication. Authorization depends on policies described in the plan.

## GET /

Lists users available to the authenticated actor.

**Query parameters**:

- `search`: optional text search by name or e-mail.
- `nome`: optional name filter.
- `email`: optional e-mail filter.
- `role`: optional role filter.
- `status`: optional `Ativo` or `Desativado`.
- `page`: default 1.
- `pageSize`: default 20, max 100.

**200 Response**:

```json
{
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "uuid",
      "nome": "Jogador Um",
      "email": "jogador@example.com",
      "roles": ["Jogador"],
      "ativo": true,
      "jogadorId": "uuid-or-null",
      "discordVinculado": false,
      "dataCadastro": "2026-06-20T00:00:00Z",
      "ultimoLoginEm": null,
      "acoesPermitidas": ["Editar", "AlterarRoles", "Desativar"]
    }
  ],
  "totalItems": 1,
  "totalPages": 1
}
```

## GET /{id}

Gets user details when actor can view the target user.

**200 Response**:

```json
{
  "id": "uuid",
  "nome": "Jogador Um",
  "email": "jogador@example.com",
  "roles": ["Jogador"],
  "ativo": true,
  "jogadorId": "uuid-or-null",
  "discord": {
    "vinculado": false,
    "username": null,
    "vinculadoEm": null
  },
  "dataCadastro": "2026-06-20T00:00:00Z",
  "dataAtualizacao": "2026-06-20T00:00:00Z",
  "ultimoLoginEm": null,
  "acoesPermitidas": ["Editar", "AlterarRoles", "Desativar", "ResetarSenha"]
}
```

**Errors**: `403`, `404`.

## PUT /{id}

Updates basic user data according to hierarchy.

**Request**:

```json
{
  "nome": "Jogador Atualizado"
}
```

**200 Response**: same shape as user details.

**Errors**: `400`, `403`, `404`.

## PUT /{id}/roles

Replaces roles of target user according to hierarchy.

**Request**:

```json
{
  "roles": ["Jogador", "Capitão"]
}
```

**200 Response**:

```json
{
  "id": "uuid",
  "roles": ["Jogador", "Capitão"],
  "dataAtualizacao": "2026-06-20T00:00:00Z"
}
```

**Errors**: `400`, `403`, `404`, `409`.

## PATCH /{id}/ativar

Activates target user.

**200 Response**: user details.

**Errors**: `403`, `404`.

## PATCH /{id}/desativar

Deactivates target user and revokes sessions.

**200 Response**: user details.

**Errors**: `403`, `404`, `409`.

## POST /{id}/reset-password

Administrative password reset.

**Request**:

```json
{
  "novaSenha": "SenhaTemporaria123",
  "confirmacaoSenha": "SenhaTemporaria123"
}
```

**204 Response**: empty.

**Errors**: `400`, `403`, `404`.

## GET /roles

Lists roles the current actor can assign/manage.

**200 Response**:

```json
{
  "items": [
    { "nome": "Moderador", "nivel": 300 },
    { "nome": "Capitão", "nivel": 200 },
    { "nome": "Jogador", "nivel": 100 }
  ]
}
```

## GET /{id}/auditoria

Lists audit events for a user. SuperAdmin only.

**Query parameters**: `page`, `pageSize`.

**200 Response**:

```json
{
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "uuid",
      "acao": "RolesAlteradas",
      "usuarioAlvoId": "uuid",
      "usuarioExecutorId": "uuid",
      "dataCadastro": "2026-06-20T00:00:00Z",
      "detalhes": "Roles: Jogador -> Jogador, Capitão"
    }
  ],
  "totalItems": 1,
  "totalPages": 1
}
```

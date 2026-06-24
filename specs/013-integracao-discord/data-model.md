# Data Model: Integração Discord

## ExternalAccount

Representa uma conta externa vinculada a um usuário interno.

Campos:

- `id`: UUID
- `usuario_id`: UUID, FK para `usuarios.id`
- `provider`: string, exemplo `Discord`
- `provider_user_id`: string, Discord ID oficial
- `username`: string opcional
- `display_name`: string opcional
- `email`: string opcional
- `avatar_url`: string opcional
- `linked_at`: timestamp
- `last_sync_at`: timestamp opcional
- `unlinked_at`: timestamp opcional

Índices:

- Único ativo por `(provider, provider_user_id)` com filtro `unlinked_at IS NULL`.
- Único ativo por `(usuario_id, provider)` com filtro `unlinked_at IS NULL`.
- Índice por `usuario_id`.

## ExternalAuthState

Representa state OAuth temporário anti-CSRF.

Campos:

- `id`: UUID
- `state_hash`: string único
- `flow`: string (`Login` ou `Link`)
- `usuario_id`: UUID opcional
- `return_url`: string opcional
- `created_at`: timestamp
- `expires_at`: timestamp
- `consumed_at`: timestamp opcional

Regras:

- State deve expirar.
- State deve ser invalidado após uso.
- State não deve ser salvo em texto puro.

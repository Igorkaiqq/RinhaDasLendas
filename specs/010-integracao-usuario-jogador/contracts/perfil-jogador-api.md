# API Contract: Perfil de Jogador do Usuário Autenticado

Base path: `/api/v1/auth/me/jogador`

All endpoints require an authenticated user.

## GET /

Returns the player profile linked to the current authenticated user.

### Responses

`200 OK`

```json
{
  "id": "4c42d5ff-f5b2-4f32-94cc-c7be79d7a30e",
  "usuarioId": "79751bd6-a0c6-47d0-b35d-466c767c9113",
  "nomeExibicao": "Igor",
  "discord": "igor#0001",
  "riotId": "Igor#BR1",
  "opGgUrl": "https://www.op.gg/summoners/br/Igor-BR1",
  "deepLolUrl": "https://www.deeplol.gg/summoner/br/Igor-BR1",
  "elo": "Ouro",
  "divisao": "IV",
  "status": "Ativo",
  "dataCadastro": "2026-06-21T10:00:00Z",
  "dataAtualizacao": "2026-06-21T10:00:00Z",
  "preferencias": [
    { "rota": "Top", "prioridade": 1, "naoJogoNemLascando": false },
    { "rota": "Jungle", "prioridade": 2, "naoJogoNemLascando": false },
    { "rota": "Mid", "prioridade": 3, "naoJogoNemLascando": false },
    { "rota": "Adc", "prioridade": 4, "naoJogoNemLascando": false },
    { "rota": "Support", "prioridade": 5, "naoJogoNemLascando": true }
  ]
}
```

`404 Not Found`

```json
{
  "message": "Perfil de jogador nao encontrado",
  "errors": []
}
```

`401 Unauthorized` when the user is not authenticated.

## POST /

Completes the current authenticated user's player profile by creating a linked active player.

### Request

```json
{
  "nomeExibicao": "Igor",
  "discord": "igor#0001",
  "riotId": "Igor#BR1",
  "opGgUrl": "https://www.op.gg/summoners/br/Igor-BR1",
  "deepLolUrl": "https://www.deeplol.gg/summoner/br/Igor-BR1",
  "elo": "Ouro",
  "divisao": "IV",
  "preferencias": [
    { "rota": "Top", "prioridade": 1, "naoJogoNemLascando": false },
    { "rota": "Jungle", "prioridade": 2, "naoJogoNemLascando": false },
    { "rota": "Mid", "prioridade": 3, "naoJogoNemLascando": false },
    { "rota": "Adc", "prioridade": 4, "naoJogoNemLascando": false },
    { "rota": "Support", "prioridade": 5, "naoJogoNemLascando": true }
  ]
}
```

### Responses

`201 Created` with the created player profile response.

`400 Bad Request` for invalid required fields or invalid route preferences.

```json
{
  "message": "Erro de validacao",
  "errors": [
    "Informe exatamente cinco preferencias de rota."
  ]
}
```

`409 Conflict` when the authenticated user already has a linked player profile.

```json
{
  "message": "Usuario ja possui perfil de jogador vinculado",
  "errors": []
}
```

`401 Unauthorized` when the user is not authenticated.

## PUT /

Updates the current authenticated user's linked player profile.

### Request

Same shape as `POST /`.

### Responses

`200 OK` with updated player profile response.

`400 Bad Request` for invalid required fields or invalid route preferences.

`404 Not Found` when the authenticated user does not have a linked player profile.

`401 Unauthorized` when the user is not authenticated.

## Authenticated Profile Impact

After successful completion, `GET /api/v1/auth/me` and auth responses should expose the new `jogadorId` for the authenticated user.

## Error Semantics

- `401`: missing, expired or invalid authentication.
- `403`: authenticated user lacks permission for administrative endpoints.
- `404`: no linked player exists for the self-service read/update flow.
- `409`: attempting to create a second linked player profile for the same user.

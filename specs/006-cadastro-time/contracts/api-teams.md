# API Contract: Times

Base route: `/api/v1/times`

All responses use DTOs. Endpoints must not expose domain entities directly.

## DTOs

### TimeResponseDto

```json
{
  "id": "uuid",
  "nome": "Os Sem Baron",
  "tag": "OSB",
  "observacoes": "Time principal de sabado",
  "status": "Ativo",
  "capitao": {
    "id": "uuid",
    "nomeExibicao": "Kaique"
  },
  "quantidadeJogadores": 5,
  "membros": [
    {
      "jogadorId": "uuid",
      "nomeExibicao": "Kaique",
      "principal": true,
      "capitao": true
    }
  ],
  "dataCadastro": "2026-06-19T00:00:00Z",
  "dataAtualizacao": "2026-06-19T00:00:00Z"
}
```

### CreateTimeRequestDto

```json
{
  "nome": "Os Sem Baron",
  "tag": "OSB",
  "observacoes": "Time principal de sabado",
  "capitaoId": "uuid",
  "jogadoresIds": ["uuid"]
}
```

### UpdateTimeRequestDto

```json
{
  "nome": "Os Sem Baron",
  "tag": "OSB",
  "observacoes": "Time principal atualizado",
  "capitaoId": "uuid",
  "jogadoresIds": ["uuid"]
}
```

## Endpoints

### GET `/api/v1/times`

Lists teams with pagination and filters.

Query parameters:

| Name | Type | Required | Notes |
|------|------|----------|-------|
| page | integer | No | Default 1 |
| pageSize | integer | No | Default 20 |
| search | string | No | Searches by team name, tag or member display name |
| status | string | No | `Ativo` or `Inativo` |

Success: `200 OK`

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

### GET `/api/v1/times/{id}`

Returns one team by id.

Success: `200 OK` with `TimeResponseDto`

Errors:
- `404 Not Found` when team does not exist.

### POST `/api/v1/times`

Creates an active team.

Success: `201 Created` with `TimeResponseDto`

Errors:
- `400 Bad Request` for missing name/tag, invalid members, inactive players, more than five players or captain outside composition.
- `409 Conflict` for duplicate active name or tag.

### PUT `/api/v1/times/{id}`

Updates team name, tag, notes, captain and composition.

Success: `200 OK` with `TimeResponseDto`

Errors:
- `400 Bad Request` for invalid payload or invalid composition.
- `404 Not Found` when team does not exist.
- `409 Conflict` for duplicate active name or tag.

### PATCH `/api/v1/times/{id}/inativar`

Marks a team as inactive without deleting it.

Success: `200 OK` with `TimeResponseDto`

Errors:
- `404 Not Found` when team does not exist.

### PATCH `/api/v1/times/{id}/reativar`

Reactivates an inactive team when required data remains valid.

Success: `200 OK` with `TimeResponseDto`

Errors:
- `400 Bad Request` when current composition or required data is invalid.
- `404 Not Found` when team does not exist.
- `409 Conflict` for duplicate active name or tag.

## Error Shape

Use existing API error/message conventions. Validation errors should be clear for final users and include message codes when the message infrastructure is adopted by the endpoint.

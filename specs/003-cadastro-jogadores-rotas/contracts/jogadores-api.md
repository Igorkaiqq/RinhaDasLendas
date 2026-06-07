# API Contract: Jogadores

Base path: `/api/v1/jogadores`

## POST /api/jogadores
Create a new player with route preferences.

Request (JSON):

```json
{
  "nomeExibicao": "string",
  "nomeReal": "string?",
  "discord": "string?",
  "riotId": "string?",
  "opGgUrl": "string?",
  "deepLolUrl": "string?",
  "elo": "string?",
  "preferencias": [
    { "rota": "Mid", "prioridade": 1, "naoJogoNemLascando": false },
    { "rota": "Adc", "prioridade": 2, "naoJogoNemLascando": false },
    { "rota": "Jungle", "prioridade": 3, "naoJogoNemLascando": false },
    { "rota": "Top", "prioridade": 4, "naoJogoNemLascando": false },
    { "rota": "Support", "prioridade": 5, "naoJogoNemLascando": false }
  ]
}
```

Responses:
- `201 Created` with created resource (including `id`)
- `400 Bad Request` for validation errors
- Error response format MUST follow project standards:

```json
{
  "message": "Erro de validação",
  "errors": ["Nome é obrigatório"]
}
```

## GET /api/v1/jogadores
List players with preferences. Support pagination query params `page` and
`pageSize` per API standards.

Response: `200 OK` with paginated result and array of players including
preferences. Use DTOs (JogadorResponseDto) and avoid returning entities.

## GET /api/v1/jogadores/{id}
Get a single player by id. Return `404` if not found and use `JogadorResponseDto`.

## PUT /api/v1/jogadores/{id}
Update player fields and preferences (expects full/complete preferences payload).
Return `400` for validation errors and `404` if player not found.

## PATCH /api/v1/jogadores/{id}/inativar
Mark player as inactive. Return `204 No Content` on success or `404` if not found.

Notes:
- Validation rules MUST enforce exactly 5 preferences, unique priorities and at
  most one `naoJogoNemLascando` = true.
- API MUST return clear validation messages for frontend display and use the
  standard error format. All endpoints MUST accept and return DTOs (Request/Response).



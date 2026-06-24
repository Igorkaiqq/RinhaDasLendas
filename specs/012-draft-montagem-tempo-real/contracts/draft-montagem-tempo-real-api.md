# Contract: Draft Montagem Tempo Real

## HTTP Endpoints

### Get realtime state

```http
GET /api/v1/draft-montagens/{id}/realtime-state
Authorization: Bearer <token>
```

**200 Response**

```json
{
  "montagem": {},
  "serverNow": "2026-06-24T20:00:00Z",
  "modo": "TempoReal",
  "turnoSequencia": 3,
  "turnoAtualTimeId": "00000000-0000-0000-0000-000000000000",
  "turnoAtualCapitaoId": "00000000-0000-0000-0000-000000000000",
  "turnoIniciadoEm": "2026-06-24T19:59:30Z",
  "turnoExpiraEm": "2026-06-24T20:00:00Z",
  "duracaoTurnoSegundos": 30,
  "canCurrentUserPick": true,
  "escolhas": [],
  "substituicoes": []
}
```

**404 Response**: standard API error with localized message.

### Start realtime draft

```http
POST /api/v1/draft-montagens/{id}/iniciar-tempo-real
Authorization: Bearer <token>
```

**200 Response**: `DraftMontagemRealtimeStateDto`.

**400/403/404 Response**: standard API error with localized messages.

### Register pick

```http
POST /api/v1/draft-montagens/{id}/picks
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "jogadorId": "00000000-0000-0000-0000-000000000000"
}
```

**200 Response**: `DraftMontagemRealtimeStateDto`.

**Validation cases**:

- User has no linked player profile.
- User is not an active captain in this draft.
- User is not the captain of the current turn.
- Turn is expired.
- Player is not free.
- Player is reserve.
- Team is full.
- Draft is not open or not in realtime mode.

### Emergency substitution

```http
POST /api/v1/draft-montagens/{id}/reservas/substituir
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "timeId": "00000000-0000-0000-0000-000000000000",
  "jogadorSaiuId": "00000000-0000-0000-0000-000000000000",
  "reservaEntrouId": "00000000-0000-0000-0000-000000000000",
  "motivo": "Substituição por ausência"
}
```

**200 Response**: `DraftMontagemRealtimeStateDto`.

**Validation cases**:

- Current user lacks draft management permission.
- Exiting player is not in the target team.
- Entering player is not reserve.
- Draft state does not allow substitution.
- Reason exceeds maximum length.

## Realtime Hub

### Hub path

```text
/hubs/draft-montagens
```

### Client-to-server methods

- `JoinDraftMontagem(draftMontagemId)`
- `LeaveDraftMontagem(draftMontagemId)`

### Server-to-client events

- `DraftMontagemStateUpdated`: full realtime state after any persisted change.
- `DraftMontagemTurnStarted`: emitted when a new turn starts.
- `DraftMontagemPickRegistered`: emitted after a valid pick.
- `DraftMontagemTurnTimedOut`: emitted after automatic timeout.
- `DraftMontagemCompleted`: emitted when the draft can no longer continue.
- `DraftMontagemCanceled`: emitted when the draft is canceled.
- `DraftMontagemSubstitutionRegistered`: emitted after emergency substitution.
- `DraftMontagemError`: localized message payload for non-command hub errors.

## DTO Notes

- `montagem` reuses the current draft montagem response shape and adds realtime fields through the wrapper state.
- User-facing messages must use backend resources.
- Frontend must map all visible labels through locale keys.

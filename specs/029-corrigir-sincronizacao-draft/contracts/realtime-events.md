# Realtime Events Contract

## Scope

SignalR usa `/hubs/draft-montagens`, exige autenticação e opera por grupo `draft-montagem:{draftMontagemId}`. Este documento não define endpoints REST.

## Client-to-Hub Methods

### `JoinDraftMontagem`

Signature: `Task JoinDraftMontagem(Guid draftMontagemId)`.

Before `Groups.AddToGroupAsync`, the Hub sends `CanViewDraftMontagemQuery(draftMontagemId)`. It allows only an authenticated human (`UserId != null`, `IsBot == false`) and a draft found by the non-archived repository query. Missing, archived and non-human requests all throw the same localized `DraftRealtimeUnavailable` Hub error and do not reveal which condition failed. The personalized GET uses the same query/rule.

The client calls Join after `HubConnection.start()` and after every `onreconnected`, before its canonical GET. Initial open and recovery remain degraded until that GET also succeeds.

### `LeaveDraftMontagem`

Signature: `Task LeaveDraftMontagem(Guid draftMontagemId)`.

Best-effort cleanup. Generation invalidation remains authoritative if Leave cannot reach the server.

## Server-to-Client Events

### `DraftMontagemStateUpdated`

Payload: `DraftMontagemRealtimeSnapshotDto`.

```json
{
  "montagem": {
    "id": "11111111-1111-1111-1111-111111111111",
    "versaoEstado": 18,
    "dataAtualizacao": "2026-07-30T12:00:00Z"
  },
  "serverNow": "2026-07-30T12:00:00Z"
}
```

`montagem` is the complete existing public `DraftMontagemResponseDto`; the abbreviated example highlights ordering fields. The payload MUST NOT contain `canCurrentUserPick`, roles, claims, authorization flags, tokens or administrative identity-dependent data.

### `DraftMontagemArchived`

Payload: draft UUID string. It remains a list-level invalidation signal. If it targets the active dirty draft, the UI invokes the single unsaved-layout decision flow instead of immediately removing the board.

### `DraftMontagemRestored`

Payload: draft UUID string. It is the availability counterpart of `DraftMontagemArchived`; list clients reconcile the restored draft. Archive publication reloads the snapshot with `ReloadByIdIncludingArchivedAsync`; restore uses the normal non-archived reload.

## Publication Coverage

After a successful commit that advances visible state, one best-effort attempt is made for: presence confirmation/cancellation/manual add/manual removal/close/reopen; mode; captain definition/draw; pick order; realtime start; pick; timeout; reserve substitution; layout; finalization; cancellation; archive/restore; Discord claim, success, failure, expiration, republication and expiry reconciliation. SQL/aggregate transitions increment parent version/data atomically, return exact version stamps and publish each returned stamp after commit.

Rejected, conflicted and no-op commands do not publish a success event. A transport failure is logged/metered and does not change the committed command result.

## Ordering and Version Rules

1. The identity key is `(montagem.id, active generation, montagem.versaoEstado)`.
2. A different draft or stale generation is discarded.
3. A lower or equal version never reapplies shared state.
4. A higher version replaces shared state only when layout is clean; otherwise it becomes the highest pending canonical snapshot.
5. The same version may refresh `serverNow` and personalized metadata only from flat HTTP `{ montagem, serverNow, canCurrentUserPick }`, accepted by a newer global personalized sequence; it cannot overwrite shared fields.
6. SignalR event, GET, fallback, administrative detail carrying the canonical draft and mutation response pass through the same shared version gate. Administrative detail uses passive; passive/mutation lanes have independent request IDs; personalized metadata has one sequence across both. Only eligible-player/presence/captain enrichment uses the separate auxiliary request ID/abort lifecycle.
7. `dataAtualizacao` is display/audit metadata, not an ordering key.
8. Every accepted `serverNow` recalculates `serverClockOffsetMs = Date.parse(serverNow) - Date.now()`.

## Connection Status Contract

| Status | Entry | Exit |
|--------|-------|------|
| `connected` | start, Join and the following canonical GET succeed for the active generation | transport/group membership degrades, Join/rejoin/GET fails, or generation closes |
| `reconnecting` | `onreconnecting` or controlled restart starts | connection succeeds, fallback becomes active, or generation closes |
| `fallback` | degraded and periodic personalized GET is active | healthy rejoin/reconciliation or generation closes |
| `disconnected` | initial state, retries exhausted, or closed generation | controlled retry while the view remains active |

Retry delays are explicit: `0`, `2000`, `5000`, `10000`, `15000` ms. Fallback starts on a fixed 3000 ms cadence, each fallback GET has a 2000 ms timeout, overlapping requests are forbidden, and fallback never runs while healthy. This 3000 + 2000 ms bound proves worst-case convergence within 5000 ms after backend availability. `onclose` schedules a single controlled restart loop while its generation remains active; duplicate timers and callbacks are forbidden.

Join/rejoin failure sets membership false, stops the failed connection best-effort, activates fallback and schedules exactly one controlled restart. Canonical GET failure after a successful Join keeps fallback/degraded state. The client never emits `connected` before start + Join + GET canonical success.

Only degraded statuses are announced in the localized accessible UI. `connected` is silent.

## Observability

Structured dimensions: draft ID, `versaoEstado`, operation/event name, elapsed publication milliseconds, connection transition, conflict code and outcome. Never log access tokens, claims, payload bodies, player-sensitive fields or Hub URLs containing credentials.

The Application publisher receives no request cancellation token. Each notifier call receives only its own 5-second timeout token; timeout cancellation and every other notifier failure are absorbed and observed after commit.

# Unit 13 Fix Report

## Scope

- Prevent SignalR browser logs from exposing `access_token` query values at any emitted log level.
- Subscribe the frontend to global `DraftMontagemRestored` availability and preserve delivery after archive removes or disconnects the active draft.

## Root Causes

1. `HubConnectionBuilder` had no explicit safe logger. Suppressing informational logs would not protect warning, error or critical messages that include a credential-bearing WebSocket URL.
2. The backend correctly broadcasts archive and restore through `Clients.All`, but the view stopped its only Hub transport after the final visible draft was archived and while archived detail was selected. A disconnected client could not receive the global restoration event.

## TDD Evidence

RED failures were observed before implementation for:

- warning/error URL token redaction;
- restored callback registration and delivery;
- availability-only transport without an archived group Join;
- filtered observer convergence after archive teardown;
- dirty/include-archived restoration without clone loss or duplicate connection.

GREEN focused gate:

```text
npm test -- src/services/draftMontagemRealtime.spec.ts src/views/DraftsView.spec.ts
2 files passed, 224 tests passed

npm run lint:check
exit 0

npm run build
exit 0
```

Fresh full gate before commit:

```text
npm test
41 files passed, 619 tests passed

npm run lint:check
exit 0

npm run build
exit 0

git diff --check
exit 0
```

## Design

- The custom SignalR `ILogger` emits only Warning, Error and Critical diagnostics. Before console output it redacts case-insensitive `access_token` query values; tests invoke the real logger contract with realistic WebSocket URLs and prove the credential is absent.
- `DraftMontagemRealtimeConnection` accepts a global restored handler with the same lifecycle guard and teardown semantics as existing callbacks.
- A connection without a draft ID starts authenticated SignalR but does not invoke `JoinDraftMontagem`; it receives only global availability events.
- `DraftsView` owns exactly one transport. Normal detail uses a group connection. Empty and archived states use an availability-only connection. Each transition disconnects the previous transport before starting the replacement.
- Every restored event reloads the active server-side list filters. Clean selected archived detail opens a new canonical generation; dirty state keeps its clone and does not reopen or apply stale shared state.

## Compatibility

- No backend code, endpoint, payload, localization key or user-visible text changed.
- Existing flat HTTP, shared snapshot, monotonic version and dirty-layout contracts remain unchanged.

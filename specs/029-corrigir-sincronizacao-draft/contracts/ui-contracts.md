# UI Contracts: Draft Synchronization

## Ownership

| Concern | Owner |
|---------|-------|
| Active draft generation, canonical GET/fallback, final status, version gate, request lanes, 409 reconciliation, pending navigation intent | `DraftsView.vue` |
| SignalR start/Join/retry/callback/stop and readiness/degradation handoff | `draftMontagemRealtime.ts` |
| Editable clone, dirty/base version, local moves and save payload | `DraftVisualBoard.vue` |
| One accessible discard/continue decision | `DraftUnsavedLayoutDialog.vue` |

No new Pinia store or transport store is introduced.

## Open Sequence

1. `DraftsView` increments generation, cancels prior requests/timers and creates a realtime connection.
2. The realtime service registers callbacks, starts SignalR and invokes authorized `JoinDraftMontagem`.
3. Only after Join succeeds, `DraftsView` executes personalized `GET /realtime-state`; initial open and recovery remain degraded until this GET succeeds.
4. Event and GET use the same version gate. An event between Join and GET wins if its version is higher, but does not bypass the canonical GET health gate.
5. Optional eligible-player enrichment starts independently and cannot fail the main opening.

## Snapshot Lanes

`passive`: initial GET, reconnect GET, fallback GET, SignalR-triggered personalized refresh and administrative detail whenever that response carries the canonical draft.

`mutation`: direct success responses for mode, captain, order, start, pick, substitution, layout and other user mutations.

Each lane increments its own request ID for shared-state ordering. Acceptance requires matching draft and generation; stale responses are ignored within that lane. Shared state applies only a strictly greater `versaoEstado`. Every personalized HTTP request also receives a global `personalizedSequence`; capability/clock metadata applies only when this sequence is newer than `lastPersonalizedSequence` and its draft version is not below the current shared version.

On HTTP 409, the matching mutation remains locked, a passive personalized GET runs immediately, and retry is enabled only after reconciliation. There is no automatic mutation retry.

## Optional Enrichment

Only eligible-player/presence/captain searches use this lifecycle. They carry draft ID, generation, independent `auxiliaryRequestId` and `AbortSignal`; they never increment/invalidate passive or mutation request IDs and never consume `personalizedSequence`. Administrative detail that carries the canonical draft is not auxiliary and uses the passive lane. A stale auxiliary response is ignored. Failure clears/disables only the dependent selector, retains detail/realtime state and exposes localized retry guidance only near that control.

## Connection Status

Type: `DraftConnectionStatus = 'connected' | 'reconnecting' | 'fallback' | 'disconnected'`.

Only degraded values render. The region uses `role="status"`, `aria-live="polite"`, `aria-atomic="true"`, visible text plus semantic icon, and no color-only meaning. Required translation namespaces in both locale files:

- `drafts.sync.status.reconnecting`
- `drafts.sync.status.fallback`
- `drafts.sync.status.disconnected`
- `drafts.sync.actions.retry`

Connected state removes the region to avoid repeated announcements.

`connected` requires successful Hub start, successful Join and successful canonical GET for the active generation, both initially and after recovery. Join/rejoin failure emits degraded status, activates fallback, stops/restarts transport in one controlled loop and never briefly reports healthy state. Canonical GET failure after Join also remains degraded. Fallback runs every 3000 ms with a 2000 ms request timeout; the SignalR retry delays remain `[0, 2000, 5000, 10000, 15000]`.

## Dirty Board Contract

`DraftVisualBoard` emits:

- `dirty-change(dirty: boolean, baseVersion: number)` after local edit or accepted save application;
- `save(payload: DraftMontagemLayoutPayload)` where payload includes `versaoEstado = baseVersion`;

`DraftVisualBoard` receives:

- `canonicalResetToken: number`: monotonic command changed only after explicit discard; a change forces clone from current canonical props, sets `baseVersion`, clears dirty and closes stale local dialogs;
- `acceptedSaveVersion: number | null`: version from the currently accepted layout mutation response; it clears dirty only when it is greater than the board base, matches `props.montagem.versaoEstado`, and belongs to the outstanding save initiated from that base.

Incoming props behavior:

- clean board: clone accepted higher canonical state;
- dirty board and same/lower version: preserve clone;
- dirty board and higher version: preserve clone/base, emit dirty state, and let `DraftsView` retain the pending canonical snapshot;
- successful save: view applies returned canonical props and then sets `acceptedSaveVersion`; board clears only when both match the outstanding save;
- unrelated event or stale save response with the same/older version: never clears dirty;
- explicit discard: view selects the greatest pending canonical snapshot and increments `canonicalResetToken`; board force-resets even if ordinary dirty prop guards would preserve local state;
- 409: preserve clone/base and set reconciliation required.

## Single Unsaved Layout Dialog

One dialog instance receives `intent: 'remote-update' | 'switch-draft' | 'route-leave' | 'draft-removed' | 'archive'` and the target metadata needed to resume.

Actions:

- `keep-editing`: close dialog, preserve clone/base, cancel navigation/destructive intent, and retain `requiresReconciliation` for remote conflict;
- `discard`: apply the highest pending canonical snapshot, clear dirty, then execute exactly the queued intent;
- no third save-and-continue action in this feature, avoiding hidden async branching.

The dialog traps focus, has title/description, returns focus when intent is cancelled, supports Escape as `keep-editing`, and uses existing dialog tokens/components. Required keys in PT/EN cover title, remote/navigation descriptions, continue-editing and discard labels.

## Guards

- Draft selection, route leave, active draft removal by filter/archive and archive action call the same `requestIntent` coordinator.
- `onBeforeRouteLeave` returns false until the dialog resolves.
- `beforeunload` sets `event.preventDefault()` and `event.returnValue` only while dirty; browsers provide native localized copy.
- Component unmount removes `beforeunload`, stops realtime, aborts enrichment/fallback and increments generation.
- A second intent while the dialog is open replaces neither the first intent nor the pending canonical snapshot; it is ignored until resolution.

## Administrative Audit Actor

`DraftMontagemAcaoAdministrativa` exposes `responsavelTipo: 'User' | 'System'` and `responsavelUsuarioId?: string | null`. For `User`, render the existing localized responsible-user value. For `System`, never render an empty ID; render `drafts.audit.actor.system` as `Sistema` in Portuguese and `System` in English. Tests cover DTO mapping, TypeScript fixture, archive/admin audit rendering and synchronized locale keys.

## Responsive and Accessibility Acceptance

The status and dialog remain usable at 1440, 1280, 1024, 768 and 480 px. Buttons have visible focus, minimum existing control heights, correct destructive styling for discard, and localized labels with correct Portuguese accents. Keyboard and screen-reader checks cover status announcement, dialog focus, Escape, continue and discard.

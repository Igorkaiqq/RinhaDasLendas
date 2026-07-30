# Quickstart: Validate Draft Synchronization

## Prerequisites

- Worktree: `.worktrees/feature-024`
- Active branch: `feature/029-corrigir-sincronizacao-draft`
- Existing devcontainer containers reused when available
- Node dependencies installed under `FrontEnd/`
- Two authenticated browser sessions with access to the same draft; one Admin+ session for layout scenarios

## 1. Static Contract Checks

```bash
git diff --check
git grep -n -E 'TO[D]O|TB[D]|NEEDS[[:space:]]+CLARIFICATION|\[(FEATURE|DATE)\]' -- specs/029-corrigir-sincronizacao-draft/plan.md specs/029-corrigir-sincronizacao-draft/research.md specs/029-corrigir-sincronizacao-draft/data-model.md specs/029-corrigir-sincronizacao-draft/quickstart.md specs/029-corrigir-sincronizacao-draft/contracts docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md
```

Expected: `git diff --check` exits 0; the unfinished-marker scan returns no matches.

## 2. Backend Tests and Build

Reuse/start the existing devcontainer and run .NET only inside it:

```bash
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

If Linux Docker integration is unavailable, use the stable Docker Desktop container:

```bash
docker.exe start rinhadaslendas_devcontainer-app-1
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

Expected: all tests pass and build has zero errors. Focused evidence must include shared payload without user capability, authorized Join, publisher failure after commit, complete mutation matrix, 409/no event, minimal projections, per-draft worker isolation and system actor audit.

## 3. Migration Validation

Generate/apply only the additive system-authorship migration planned in `data-model.md`; no timer index migration is expected because `(status, modo, turno_expira_em)` already exists.

```bash
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet ef database update --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~Migration
```

Expected: existing audit rows are `User`, system rows allow null user FK, consistency constraint rejects invalid combinations, and rollback script is reviewed before deployment.

## 4. Frontend Verification

```bash
npm --prefix FrontEnd test
npm --prefix FrontEnd run lint:check
npm --prefix FrontEnd run build
```

Expected: Vitest passes, ESLint reports no errors, and Vue/TypeScript production build succeeds. Focused tests must prove Join before GET, failed Join/rejoin degradation, explicit retry/onclose/fallback, stale generation disposal, strict shared versioning, global personalized sequence, isolated auxiliary requests, 409 reconciliation, dirty reset/save signals and synchronized PT/EN keys.

## 5. Multiclient Journey

1. Start API and frontend through the existing devcontainer/Vite workflow.
2. Open one normal participant session and one Admin+ session on the same draft.
3. Confirm/cancel/add/remove presence, close/reopen presence, select mode, define/draw captains, define order and start realtime.
4. Pick, let one turn timeout, substitute a reserve, save layout, finalize/cancel, archive and restore where state allows.
5. For every committed transition, record both clients' draft ID and highest `versaoEstado`.

Expected: for every commit, capture `commitObservedAt` and `clientAppliedAt`; assert `clientAppliedAt - commitObservedAt <= 2000 ms` in both sessions. Both clients end on the greatest persisted version; personalized pick capability differs correctly; rejected/no-op mutations emit no success event.

## 6. Connection Loss and Ordering

1. With both clients connected, block the Hub transport for one session while leaving HTTP available.
2. Produce state changes in the other session.
3. Observe `reconnecting`, then `fallback`; verify GET occurs at most every 5 seconds.
4. Restore the Hub, record `backendAvailableAt`, and verify re-Join followed by GET, fallback cancellation and canonical application at `convergedAt`.
5. Exhaust automatic retries and leave the view open; verify one controlled retry loop continues.
6. Reject Join and rejoin while transport start succeeds; verify status never becomes `connected`, fallback starts and one restart is scheduled.
7. Delay an old GET so a newer SignalR event/mutation resolves first; release the GET.
8. Switch drafts while requests are pending.

Expected: assert `convergedAt - backendAvailableAt <= 5000 ms`; no duplicate timer/callback, no polling while connected, equal/lower shared version never reapplied, old generation ignored and server-clock offset recalculated only by accepted personalized sequence.

## 7. Conflict and Unsaved Layout

1. In Admin+, move a player without saving and note the base version.
2. Trigger a higher remote version from the second session.
3. Choose continue editing; verify clone/base and `canonicalResetToken` remain unchanged.
4. Repeat and choose discard; verify the greatest pending canonical snapshot is applied, token increments once and board resets.
5. Save successfully; verify only the matching `acceptedSaveVersion` plus canonical prop clears dirty.
6. Deliver an unrelated equal-version event and a stale save response; verify neither clears dirty.
7. While dirty, test draft switch, route change, filter/removal, archive and browser close.
8. Produce a layout 409 by saving from the second session first.

Expected: one dialog protects every internal intent; native browser confirmation protects close; 409 performs GET before retry and never erases the clone; only explicit `canonicalResetToken` or matching `acceptedSaveVersion` clears dirty.

## 8. Worker and Observability Validation

1. Seed multiple expired realtime/presence drafts, including one that conflicts or fails domain validation.
2. Capture SQL for candidate scans and verify they select only `id`.
3. Exercise Discord claim/success/failure/expiration/republication and verify each visible transition atomically increments parent version/data and returns the stamp; no-op does not increment.
4. Force request cancellation immediately after commit and verify publisher still attempts with its own token.
5. Force the 5-second publisher timeout and verify success remains functional and timeout is observed.
6. Make one worker item throw a non-host `OperationCanceledException` and another generic exception; verify later IDs continue. Then cancel host and verify propagation.

Expected: candidate SQL selects only ID; every item has independent scope/command; only host cancellation stops the loop; system audit renders `Sistema`/`System`; archive/restored events arrive best-effort; committed mutation remains successful; logs/metrics contain draft/version/outcome without token/payload.

## 9. Accessibility and i18n

Validate keyboard and screen reader at 1440, 1024, 768 and 480 px. Switch locale between Portuguese and English.

Expected: only degraded connection states are announced through polite live region; dialog focus/Escape/buttons work; statuses, conflict guidance and unsaved-layout copy are localized; PT/EN key sets match; Portuguese accents are correct; no visible hardcoded text exists in new frontend or backend paths.

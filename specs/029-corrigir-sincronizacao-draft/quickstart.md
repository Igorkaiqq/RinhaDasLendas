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
git grep -n -E 'TO[D]O|TB[D]|NEEDS[[:space:]]+CLARIFICATION|\[(FEATURE|DATE)\]' -- specs/029-corrigir-sincronizacao-draft/spec.md specs/029-corrigir-sincronizacao-draft/plan.md specs/029-corrigir-sincronizacao-draft/research.md specs/029-corrigir-sincronizacao-draft/data-model.md specs/029-corrigir-sincronizacao-draft/quickstart.md specs/029-corrigir-sincronizacao-draft/tasks.md specs/029-corrigir-sincronizacao-draft/contracts docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md
```

Expected: `git diff --check` exits 0; the unfinished-marker scan returns no matches.

## 2. Backend Tests and Build

Select exactly one executable path in this order.

### 2.1 Direct SDK

First check whether the shell is already inside the devcontainer:

```bash
dotnet --version
```

If it succeeds, run directly:

```bash
dotnet test BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build BackEnd/RinhaDasLendas.sln --configuration Release
```

### 2.2 Linux Docker Compose

If `dotnet --version` fails, inspect and reuse the named Compose project before creating or starting anything:

```bash
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml ps -a
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml up -d postgres app
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

### 2.3 Docker Desktop Windows CLI

Only if both direct `dotnet` and Linux `docker` are unavailable, inspect the existing devcontainer containers, start them and execute through the stable app container:

```bash
docker.exe ps -a --filter "label=com.docker.compose.project=rinhadaslendas_devcontainer"
docker.exe start rinhadaslendas_devcontainer-postgres-1 rinhadaslendas_devcontainer-app-1
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

Expected: all tests pass and build has zero errors. Focused evidence must include shared payload without user capability, authorized Join, publisher failure after commit, complete mutation matrix, 409/no event, minimal projections, per-draft worker isolation and system actor audit.

## 3. Migration Validation

The exact production baseline is `20260726104907_AddDraftMontagemArchiving`. Feature 028 and Feature 029 must then be applied in order:

1. `20260729124041_CorrigirNucleoCicloDraft`
2. `20260731012844_AddDraftMontagemSystemActor`

No timer index migration is expected because `(status, modo, turno_expira_em)` already exists. The commands below use only the stable devcontainer names and `/tmp/feature029-migration`, never a repository path for backup artifacts.

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 mkdir -p /tmp/feature029-migration
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet ef migrations list --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api --configuration Release
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet ef migrations script 20260726104907_AddDraftMontagemArchiving 20260731012844_AddDraftMontagemSystemActor --idempotent --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api --configuration Release --output /tmp/feature029-migration/028-029-idempotent.sql
docker.exe exec rinhadaslendas_devcontainer-app-1 test -s /tmp/feature029-migration/028-029-idempotent.sql
```

Expected before deploy: both Feature 028 and Feature 029 migrations are pending when the database is at the stated baseline. Stop if an unexpected migration appears between the from/to IDs.

Prepare the standard rollback backup at exactly Feature 028. If the environment is still at the Archiving baseline, first apply only Feature 028 with the reviewed compatible migration artifact; the explicit target prevents applying Feature 029. Stop on every other source migration, stop writers before the dump, and recheck the exact maximum migration before creating the backup:

```powershell
$feature027 = "20260726104907_AddDraftMontagemArchiving"
$feature028 = "20260729124041_CorrigirNucleoCicloDraft"
$currentMigration = (docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 -Atc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory";').Trim()
if ($LASTEXITCODE -ne 0) { throw "Could not read the source migration." }
if ($currentMigration -eq $feature027) {
  docker.exe exec -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=rinha_das_lendas;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update $feature028 --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api --configuration Release --no-build
  if ($LASTEXITCODE -ne 0) { throw "Feature 028 migration failed." }
  $currentMigration = (docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 -Atc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory";').Trim()
  if ($LASTEXITCODE -ne 0) { throw "Could not validate Feature 028 after migration." }
}
if ($currentMigration -ne $feature028) { throw "Backup refused: expected exact maximum migration $feature028, found $currentMigration." }
docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml stop app
$currentMigration = (docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 -Atc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory";').Trim()
if ($LASTEXITCODE -ne 0 -or $currentMigration -ne $feature028) { throw "Backup refused after stopping writers: expected exact maximum migration $feature028, found $currentMigration." }
docker.exe exec rinhadaslendas_devcontainer-postgres-1 pg_dump -U postgres -d rinha_das_lendas --format=custom --file=/tmp/rinha-das-lendas-feature028-before-feature029.dump
if ($LASTEXITCODE -ne 0) { throw "Feature 028 backup failed." }
docker.exe exec rinhadaslendas_devcontainer-postgres-1 test -s /tmp/rinha-das-lendas-feature028-before-feature029.dump
if ($LASTEXITCODE -ne 0) { throw "Feature 028 backup is empty." }
docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml start app
```

The devcontainer command above is the compatible reviewed migration artifact for this repository. In another environment, run the equivalent Feature 028 migration bundle or pre-Feature029 deployment artifact with the same explicit target and exact postcondition before the dump. A retained backup at `20260726104907_AddDraftMontagemArchiving` is compatible only with a pre-Feature028 binary; it is not the standard Feature 029 rollback backup and must not be used for the procedure below.

Restore the Feature 028 backup into a fresh database and fail unless its exact maximum migration is Feature 028:

```powershell
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres --if-exists rinha_feature029_restore_check
docker.exe exec rinhadaslendas_devcontainer-postgres-1 createdb -U postgres rinha_feature029_restore_check
docker.exe exec rinhadaslendas_devcontainer-postgres-1 pg_restore -U postgres --exit-on-error --dbname=rinha_feature029_restore_check /tmp/rinha-das-lendas-feature028-before-feature029.dump
if ($LASTEXITCODE -ne 0) { throw "Feature 028 test restore failed." }
$restoredMigration = (docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore_check -v ON_ERROR_STOP=1 -Atc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory";').Trim()
if ($LASTEXITCODE -ne 0 -or $restoredMigration -ne $feature028) { throw "Restore refused: expected exact maximum migration $feature028, found $restoredMigration." }
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore_check -v ON_ERROR_STOP=1 -c "SELECT count(*) AS system_rows FROM draft_montagem_acoes_administrativas a WHERE to_jsonb(a)->>'responsavel_tipo' = 'System';"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore_check -v ON_ERROR_STOP=1 -c "SELECT count(*) AS invalid_actor_rows FROM draft_montagem_acoes_administrativas WHERE responsavel_usuario_id IS NULL;"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres rinha_feature029_restore_check
```

Expected: restore exits zero, `restoredMigration` equals Feature 028 exactly, `system_rows = 0`, and `invalid_actor_rows = 0`.

Exercise the exact downgrade in a disposable database. This first restores the Feature 028 backup, migrates through Feature 029, confirms no `System` rows, downgrades only Feature 029 to `20260729124041_CorrigirNucleoCicloDraft`, and verifies the resulting schema/history:

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres --if-exists rinha_feature029_downgrade_check
docker.exe exec rinhadaslendas_devcontainer-postgres-1 createdb -U postgres rinha_feature029_downgrade_check
docker.exe exec rinhadaslendas_devcontainer-postgres-1 pg_restore -U postgres --exit-on-error --dbname=rinha_feature029_downgrade_check /tmp/rinha-das-lendas-feature028-before-feature029.dump
docker.exe exec -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=rinha_feature029_downgrade_check;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update 20260731012844_AddDraftMontagemSystemActor --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api --configuration Release --no-build
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_downgrade_check -v ON_ERROR_STOP=1 -c "SELECT count(*) AS system_rows FROM draft_montagem_acoes_administrativas WHERE responsavel_tipo = 'System';"
docker.exe exec -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=rinha_feature029_downgrade_check;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update 20260729124041_CorrigirNucleoCicloDraft --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api --configuration Release --no-build
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_downgrade_check -v ON_ERROR_STOP=1 -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" >= '20260729124041' ORDER BY \"MigrationId\";"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_downgrade_check -v ON_ERROR_STOP=1 -c "SELECT count(*) AS actor_column_count FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'draft_montagem_acoes_administrativas' AND column_name = 'responsavel_tipo';"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres rinha_feature029_downgrade_check
```

Expected: pre-downgrade `system_rows = 0`; post-downgrade history contains Feature 028 but not Feature 029; `actor_column_count = 0`; the disposable database is removed.

Apply the reviewed exact script and validate postconditions:

```bash
docker.exe exec -e PGPASSWORD=postgres rinhadaslendas_devcontainer-app-1 psql -h postgres -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 --file=/tmp/feature029-migration/028-029-idempotent.sql
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" IN ('20260729124041_CorrigirNucleoCicloDraft','20260731012844_AddDraftMontagemSystemActor') ORDER BY \"MigrationId\";"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -v ON_ERROR_STOP=1 -c "SELECT count(*) AS invalid_actor_rows FROM draft_montagem_acoes_administrativas WHERE NOT ((responsavel_tipo = 'User' AND responsavel_usuario_id IS NOT NULL) OR (responsavel_tipo = 'System' AND responsavel_usuario_id IS NULL));"
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~Migration
```

Expected after deploy: both migration IDs are returned, `invalid_actor_rows = 0`, existing audit rows are `User`, and the migration suite passes. Deploy backend before frontend and retain one replica.

For a pre-`System` restore cutover, restore the Feature 028 backup into a fresh database and deploy a pre-Feature029 backend binary that is compatible with Feature 028. The exact migration check is a fail-closed gate immediately before recreating `app`; a mismatch throws and leaves the current service untouched:

```powershell
$feature028 = "20260729124041_CorrigirNucleoCicloDraft"
$preFeature029Compose = $env:RINHA_PRE_FEATURE029_COMPOSE_FILE
$preFeature029Image = $env:RINHA_PRE_FEATURE029_IMAGE
if ([string]::IsNullOrWhiteSpace($preFeature029Compose) -or -not (Test-Path $preFeature029Compose)) { throw "Cutover refused: RINHA_PRE_FEATURE029_COMPOSE_FILE must identify the approved pre-Feature029 deployment artifact." }
if ([string]::IsNullOrWhiteSpace($preFeature029Image)) { throw "Cutover refused: RINHA_PRE_FEATURE029_IMAGE must identify the approved pre-Feature029 image." }
$override = Join-Path $env:TEMP "rinhadaslendas-feature029-restore.yml"
@"
services:
  app:
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=rinha_feature029_restore;Username=postgres;Password=postgres"
"@ | Set-Content -Encoding utf8 $override
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres --if-exists rinha_feature029_restore
docker.exe exec rinhadaslendas_devcontainer-postgres-1 createdb -U postgres rinha_feature029_restore
docker.exe exec rinhadaslendas_devcontainer-postgres-1 pg_restore -U postgres --exit-on-error --dbname=rinha_feature029_restore /tmp/rinha-das-lendas-feature028-before-feature029.dump
if ($LASTEXITCODE -ne 0) { throw "Feature 028 cutover restore failed." }
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore -v ON_ERROR_STOP=1 -c "SELECT count(*) AS system_rows FROM draft_montagem_acoes_administrativas a WHERE to_jsonb(a)->>'responsavel_tipo' = 'System';"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore -v ON_ERROR_STOP=1 -c "SELECT count(*) AS invalid_actor_rows FROM draft_montagem_acoes_administrativas WHERE responsavel_usuario_id IS NULL;"
if (-not (Test-Path .devcontainer/.env)) { Copy-Item .devcontainer/.env.example .devcontainer/.env }
$resolvedImages = @(docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml -f $preFeature029Compose -f $override config --images)
if ($LASTEXITCODE -ne 0 -or $resolvedImages -notcontains $preFeature029Image) { throw "Cutover refused: Compose does not resolve the approved pre-Feature029 image $preFeature029Image." }
$cutoverMigration = (docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_feature029_restore -v ON_ERROR_STOP=1 -Atc 'SELECT max("MigrationId") FROM "__EFMigrationsHistory";').Trim()
if ($LASTEXITCODE -ne 0 -or $cutoverMigration -ne $feature028) { throw "Cutover refused: expected exact maximum migration $feature028, found $cutoverMigration." }
docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml -f $preFeature029Compose -f $override up -d --no-deps --force-recreate app
docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml -f $preFeature029Compose -f $override exec -T app printenv ConnectionStrings__DefaultConnection
```

Expected cutover: Compose resolves exactly the approved pre-Feature029 image; the printed connection targets `Database=rinha_feature029_restore`; `cutoverMigration` equals Feature 028 exactly; `system_rows = 0`; `invalid_actor_rows = 0`. Keep the previous database intact until application health checks and a read-only draft query succeed. To return to the normal Compose database, remove the rollback artifact and connectivity overrides, then recreate `app`:

```powershell
docker.exe compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml up -d --no-deps --force-recreate app
Remove-Item $override
```

Rollback policy:

- Before any `System` row exists, use the verified Feature 028 backup, execute the disposable downgrade and fresh-database restore/cutover procedures above with a pre-Feature029 binary; never restore over the existing database.
- After the first `System` row, never downgrade `20260731012844_AddDraftMontagemSystemActor`: the tested `P0001` guard prevents data loss.
- After the first `System` row, rollback is binary-only: deploy the previous application artifact while keeping `ConnectionStrings__DefaultConnection` pointed at the existing database with the additive Feature 029 schema. Do not run `database update` to an older migration and do not restore the pre-Feature-029 backup.
- Correct any post-`System` incompatibility with a new additive roll-forward migration, apply it to the existing database, and then redeploy the corrected binary.
- A database restore is an incident operation: stop writers, preserve the failed database, restore the verified custom-format backup to a new database first, validate migration history and actor invariants, then switch connectivity.

## 4. Frontend Verification

```bash
npm --prefix FrontEnd test
npm --prefix FrontEnd run lint:check
npm --prefix FrontEnd run build
```

Expected: Vitest passes, ESLint reports no errors, and Vue/TypeScript production build succeeds. Focused tests must prove exact start -> Join -> canonical GET ordering initially and after recovery, no `connected` before GET success, failed Join/rejoin/GET degradation, 3000 ms fallback interval with 2000 ms request timeout, explicit retry/onclose, stale generation disposal, strict shared versioning, global personalized sequence, passive administrative detail, isolated eligible-player/captain auxiliary requests, 409 reconciliation, dirty reset/save signals and synchronized PT/EN keys.

## 5. Multiclient Journey

Para uma execução local reproduzível, crie um banco descartável com prefixo `rinha_feature029_browser_qa_`, aplique todas as migrations e execute explicitamente o projeto descrito em `BackEnd/scripts/qa/README.md`. A fixture recebe conexão, UUIDs e credenciais efêmeras somente pelo ambiente, recusa bancos não descartáveis ou já populados e não adiciona endpoint nem bypass à aplicação.

1. Start API and frontend through the existing devcontainer/Vite workflow.
2. Open one normal participant session and one Admin+ session on the same draft.
3. Confirm/cancel/add/remove presence, close/reopen presence, select mode, define/draw captains, define order and start realtime.
4. Pick, let one turn timeout, substitute a reserve, save layout, finalize/cancel, archive and restore where state allows.
5. For every committed transition, record both clients' draft ID and highest `versaoEstado`.

Expected: for every commit, capture `commitObservedAt` and `clientAppliedAt`; assert `clientAppliedAt - commitObservedAt <= 2000 ms` in both sessions. Both clients end on the greatest persisted version; personalized pick capability differs correctly; rejected/no-op mutations emit no success event.

## 6. Connection Loss and Ordering

1. With both clients connected, block the Hub transport for one session while leaving HTTP available.
2. Produce state changes in the other session.
3. Observe `reconnecting`, then `fallback`; verify fallback starts every 3000 ms, each GET is canceled after 2000 ms and no overlapping request is created.
4. Restore the Hub, record `backendAvailableAt`, and verify start/reconnection followed by Join and canonical GET; only after GET succeeds may status become `connected`, fallback cancel and canonical state apply at `convergedAt`.
5. Exhaust automatic retries and leave the view open; verify one controlled retry loop continues.
6. Reject Join/rejoin while transport start succeeds, then separately fail the canonical GET after Join; verify neither path becomes `connected`, fallback starts and only the Join failure schedules the controlled transport restart.
7. Delay an old GET so a newer SignalR event/mutation resolves first; release the GET.
8. Switch drafts while requests are pending.

Expected: assert `convergedAt - backendAvailableAt <= 5000 ms` from the explicit 3000 ms cadence plus 2000 ms request deadline; no duplicate timer/callback/request, no polling while connected, equal/lower shared version never reapplied, old generation ignored and server-clock offset recalculated only by accepted personalized sequence.

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
3. Exercise Discord claim/success/failure/expiration/republication and expiry reconciliation; verify each visible transition atomically increments parent version/data, returns and publishes the exact stamp, while no-op does not increment or publish.
4. Force request cancellation immediately after commit and verify publisher still attempts with its own token.
5. Force the 5-second publisher timeout and verify success remains functional and timeout is observed.
6. Make one worker item throw a non-host `OperationCanceledException` and another generic exception; verify later IDs continue. Then cancel host and verify propagation.

Expected: candidate SQL selects only ID; every item has independent scope/command; only host cancellation stops the loop; system audit renders `Sistema`/`System`; archive/restored events arrive best-effort; committed mutation remains successful; expiry reconciliation publishes returned stamps; old helper/notifier method/adapter/doubles have zero references; logs/metrics contain draft/version/outcome without token/payload.

### Unit 12 automated operational evidence (2026-08-02)

- Actual isolated PostgreSQL plus TestServer SignalR: two authenticated clients traverse presence, publication, mode, captains, order, start, timeout, picks, substitution, finalization, archive and restore. Per-transition baselines require exactly one publisher observation and exactly one snapshot per client with the same version/ordinal; client timestamps must follow that version's post-commit timestamp under the `<= 2000 ms` bound; no-op/rejection deltas and all final queues must be zero.
- Frontend fallback: three deterministic fake-clock runs start the deferred HTTP request at exactly `3000 ms`, accept success before `5000 ms`, and separately prove abort at the `2000 ms` request deadline.
- Unit 12 backend integration matrix: `11/11` tests passed for GET/Join authorization distinctions, real publisher success/failure/timeout/request-cancellation independence, executed PostgreSQL candidate projections, publication/republication/reconciliation and multiclient delivery. Existing focused actor/worker suites remain part of the full backend gate.
- Unit 12 frontend matrix: `298/298` tests passed for transport/reconnect, fallback, stale races, dirty clone, auxiliary isolation, accessible dialog and i18n.
- Full gates: backend `838/838`; frontend `614/614`; backend Release build with `0` warnings and `0` errors; ESLint clean; frontend production build successful.
- Metrics expose only bounded `event` and `outcome` labels. Draft ID, state version, operation and failure type remain structured log fields and are absent from metric tags.
- Migration verification: `11/11` migration tests passed. EF listed exactly `20260729124041_CorrigirNucleoCicloDraft` and `20260731012844_AddDraftMontagemSystemActor` after the stated baseline, generated the non-empty exact from/to script, and a disposable Feature 028 backup/restore returned Feature 028 as the exact maximum migration in source and restored databases.

### Unit 13 browser and final gate evidence (2026-08-03)

- Disposable database `rinha_feature029_browser_qa_unit13_e8de489` was migrated through `20260731012844_AddDraftMontagemSystemActor` and populated only by `BackEnd/scripts/qa/RinhaDasLendas.BrowserQaFixture`. Two isolated Chromium sessions authenticated as distinct SuperAdmin users linked to active players.
- The real UI traversed presence cancellation/reconfirmation, manual close with two starters per team and one reserve, realtime mode, captain selection, randomized order, start, worker timeouts, authenticated picks, captain substitution by the reserve and automatic finalization. Persisted evidence contained `7` timeout choices, `2` authenticated picks and `1` substitution; `POST .../ordem-escolha` returned `200` at HEAD `e8de489`.
- The actual one-second hosted worker processed expired turns. A safe disposable-only SQL expiry adjustment is documented in `BackEnd/scripts/qa/README.md`; no runtime endpoint or application bypass was added.
- Finalized archive response ended at epoch `1785731754446`; the second session removed it at `1785731754441` (`-5 ms`, event observed before response completion). Finalized restore response ended at `1785731891716`; the second session restored it to the default list at `1785731891703` (`-13 ms`). Both satisfy the `<= 2000 ms` event bound.
- API and Hub were stopped together. The replacement API logged `Now listening` at epoch `1785732321795`; both sessions completed the first recovered canonical GET at `1785732323292`, yielding `1497 ms` from backend availability to convergence, within `<= 5000 ms`.
- A real authenticated stale layout PUT used base version `1` against persisted version `2` and returned `409` in `18 ms`. Dirty layout protected draft switch, route change and archive, and remote update exposed explicit continue/discard reconciliation.
- Feature 022 semantics were preserved: archiving the active manual draft changed it to `Cancelada`; it appeared while archived with `includeArchived`; restore removed the archive condition while preserving `Cancelada`, so it legitimately left that result and remained hidden from the default list. Default-list `Restored` convergence used the `Finalizada` draft.
- Chromium consoles contained outage diagnostics only; no access token, claims, payload body or credential-bearing Hub URL appeared. Screenshots: `/tmp/opencode/feature029-unit13-final-desktop-e8de489.png`, `/tmp/opencode/feature029-unit13-final-tablet-e8de489.png` and `/tmp/opencode/feature029-unit13-final-mobile-e8de489.png`.
- Fresh final gates: backend `854/854`; frontend `628/628` in `41/41` files; migration `11/11`; backend Release build `0` warnings and `0` errors; ESLint clean; frontend production build successful with `2801` transformed modules and only the known chunk-size warning.

### Deployment and rollback policy

1. Follow section 3: verify the exact baseline/from-to IDs, preflight `System` rows, test the backup restore, apply the reviewed idempotent script and verify postconditions before deploying the backend.
2. Deploy the backend before the frontend and retain the one-replica constraint; this feature does not add Redis, backplane, outbox or another store.
3. If rollback is needed before any `System` audit row exists, the migration guard permits the tested schema downgrade.
4. After the first `System` audit row, do not downgrade `20260731012844_AddDraftMontagemSystemActor`: its `P0001` guard intentionally blocks data-loss rollback. Roll back application binaries only while retaining the additive compatible schema, then roll forward with a corrective migration if required.
5. After deployment, verify bounded realtime `event`/`outcome` metrics and structured draft/version logs without tokens, claims, payloads or sensitive Hub URLs.

## 9. Accessibility and i18n

Validate keyboard and screen reader at 1440, 1280, 1024, 768 and 480 px. Switch locale between Portuguese and English.

Expected: only degraded connection states are announced through polite live region; dialog focus/Escape/buttons work; statuses, conflict guidance and unsaved-layout copy are localized; PT/EN key sets match; Portuguese accents are correct; no visible hardcoded text exists in new frontend or backend paths.

Automated i18n evidence on 2026-08-02: `pt.json` and `en.json` each contain `1063` leaf keys with `0` key drift; `Messages.resx`, `Messages.pt-BR.resx` and `Messages.en-US.resx` each contain `232` resources with `0` drift. The i18n suite passed `36/36` tests. No user-visible text or validation message was introduced by Unit 12.

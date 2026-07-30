---

description: "Task list for correcting draft synchronization and operation"
---

# Tasks: Corrigir Sincronização e Operação do Draft

**Input**: Design documents from `/specs/029-corrigir-sincronizacao-draft/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md` and approved executable plan `docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md`

**Tests**: TDD is mandatory. In every implementation unit, write the listed tests first, run the focused command and observe the expected failure, implement the minimum change, then run focused tests plus the relevant build before proceeding.

**Organization**: Tasks are grouped by the five user stories. The two shared backend units are foundational; the thirteen numbered implementation units from the approved plan remain identifiable and end with a compiling, tested checkpoint.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on an incomplete task
- **[Story]**: Present only in user story phases (`US1` through `US5`)
- Every task includes exact repository-relative file paths

## Path Conventions

- Backend source: `BackEnd/src/`
- Backend tests: `BackEnd/tests/RinhaDasLendas.Tests/`
- Frontend source and tests: `FrontEnd/src/`
- Feature evidence and contracts: `specs/029-corrigir-sincronizacao-draft/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a clean, reproducible baseline and freeze the approved contracts before TDD begins.

- [ ] T001 [P] Record passing backend test/build and frontend test/lint/build baselines using `BackEnd/RinhaDasLendas.sln` and `FrontEnd/package.json`
- [ ] T002 [P] Inventory existing notifier, publisher, mutation, worker and frontend synchronization references against `docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md` and `specs/029-corrigir-sincronizacao-draft/contracts/`

**Checkpoint**: Baseline evidence exists and no implementation starts from an unexplained failure.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Deliver approved implementation units 1 and 2: the identity-safe shared contract with authorized Join/GET parity, followed by the resilient Application publisher and additive notifier seam required by every mutation.

**CRITICAL**: No user story implementation begins until both units pass their focused tests and the backend builds.

### Implementation Unit 1: Shared Contract and Authorized Hub

- [ ] T003 [P] Write failing GET/Join parity, non-human/missing/archived rejection and shared-payload shape tests in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeAccessTests.cs`
- [ ] T004 [P] Write failing authenticated Hub authorization and identical localized rejection tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagensHubAuthorizationIntegrationTests.cs`
- [ ] T005 Run the Unit 1 RED filter and confirm failures originate from missing shared access behavior in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeAccessTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagensHubAuthorizationIntegrationTests.cs`
- [ ] T006 Implement `DraftMontagemRealtimeSnapshotDto`, preserve the flat personalized DTO, and centralize GET/Join access parity in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemRealtimeSnapshotDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemRealtimeStateDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Queries/DraftMontagens/CanViewDraftMontagemQuery.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CanViewDraftMontagemQueryHandler.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/GetDraftMontagemRealtimeStateQueryHandler.cs`
- [ ] T007 Implement authorized `JoinDraftMontagem` and the common localized rejection in `BackEnd/src/RinhaDasLendas.Api/Hubs/DraftMontagensHub.cs`, `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx` and `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- [ ] T008 Run the Unit 1 focused tests and backend build to prove shared DTO/Hub/GET parity in `BackEnd/RinhaDasLendas.sln`

### Implementation Unit 2: Application Publisher

- [ ] T009 [P] Add additive shared-notifier and telemetry doubles without removing the personalized notifier seam in `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- [ ] T010 Write failing normal, archived reload, request-cancellation independence, exact five-second timeout and failure-absorption tests in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimePublisherTests.cs`
- [ ] T011 Run the Unit 2 RED filter and confirm the publisher/ports are absent in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimePublisherTests.cs`
- [ ] T012 Implement the publisher, availability enum and additive notifier/telemetry ports in `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimePublisher.cs`, `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`, `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeTelemetry.cs`, `BackEnd/src/RinhaDasLendas.Application/Services/DraftMontagemRealtimePublisher.cs`, `BackEnd/src/RinhaDasLendas.Application/Enums/DraftMontagemAvailabilityChange.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeStateFactory.cs`
- [ ] T013 Implement SignalR/telemetry adapters and dependency injection while retaining the old notifier method for caller migration in `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`, `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs` and `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T014 Run the Unit 2 focused tests and backend build to prove best-effort post-commit publication in `BackEnd/RinhaDasLendas.sln`

**Checkpoint**: Shared broadcasts cannot contain user capability, GET and Join reject identically, and the publisher absorbs observable transport failures without request-token coupling.

---

## Phase 3: User Story 2 - Recover Synchronization After Connection Loss (Priority: P1)

**Goal**: Start listening before canonical GET, recover with one controlled retry/fallback lifecycle, and dispose every callback, request and timer from stale draft generations.

**Independent Test**: Interrupt Hub transport during a turn, mutate from another session, exhaust automatic reconnect, restore transport and verify exact start/reconnect -> Join -> canonical GET ordering converges within five seconds with no `connected` before GET success and no duplicate polling, callback or timer.

### Implementation Unit 8: Frontend Transport Lifecycle

- [ ] T015 [US2] Write failing transport tests for callbacks-before-start, exact start -> Join readiness handoff initially and after recovery, no direct `connected` emission, initial Join failure, rejoin failure, `onclose`, explicit SignalR retry delays, single restart timer and idempotent teardown in `FrontEnd/src/services/draftMontagemRealtime.spec.ts`
- [ ] T016 [US2] Run the Unit 8 RED suite and confirm failures expose default reconnect and missing Join-health semantics in `FrontEnd/src/services/draftMontagemRealtime.spec.ts`
- [ ] T017 [US2] Define the shared event and `connected | reconnecting | fallback | disconnected` transport contracts without changing flat HTTP state in `FrontEnd/src/types/draftMontagem.ts`
- [ ] T018 [US2] Implement callback registration before start, authorized Join/rejoin, transport-ready/degraded handoff for view-owned canonical GET/fallback, SignalR delays `[0, 2000, 5000, 10000, 15000]`, one controlled restart and generation-safe teardown in `FrontEnd/src/services/draftMontagemRealtime.ts`
- [ ] T019 [US2] Run the Unit 8 tests and frontend production build to prove the transport service never declares final `connected` before the view-owned canonical GET in `FrontEnd/src/services/draftMontagemRealtime.spec.ts` and `FrontEnd/package.json`

**Checkpoint**: User Story 2 transport lifecycle is independently testable; unhealthy membership degrades visibly through callbacks and active generations continue controlled recovery.

---

## Phase 4: User Story 1 - Keep Every Client on the Same State (Priority: P1) MVP

**Goal**: Publish every committed visible transition once, keep shared state monotonic and identity-neutral, preserve personalized capabilities per session, and reconcile all conflicts before another mutation.

**Independent Test**: Open two sessions on one draft, execute the full lifecycle, delay old HTTP/SignalR responses and force mutation conflicts; both sessions must converge to the greatest persisted version without sharing capability or applying stale state.

### Implementation Unit 3: Draft Handler Lifecycle

- [ ] T020 [US1] Write the failing parameterized save-before-publish/no-publish matrix for presence, mode, captains, order, start, pick, timeout, substitution, layout, finalization, cancellation, archive and restore in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs`
- [ ] T021 [US1] Add failing layout base-version unit coverage plus a `WebApplicationFactory` HTTP test proving stale layout returns 409/`MV103` with unchanged database and zero publication in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- [ ] T022 [US1] Run the Unit 3 RED filters and confirm uncovered handlers, direct notifier callers and stale-layout HTTP behavior fail in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- [ ] T023 [US1] Add layout base version to request validation in `BackEnd/src/RinhaDasLendas.Application/Dtos/SalvarLayoutDraftMontagemRequestDto.cs` and `BackEnd/src/RinhaDasLendas.Application/Validators/SalvarLayoutDraftMontagemValidator.cs`
- [ ] T024 [US1] Migrate presence/mode/captain/order/start handlers to post-commit publisher calls in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ConfirmarPresencaDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarPresencaDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdicionarPresencaManualDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RemoverPresencaManualDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/EncerrarPresencaDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ReabrirPresencaDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SelecionarModoDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DefinirCapitaesDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SortearCapitaesDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DefinirOrdemEscolhaDraftMontagemCommandHandler.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/IniciarDraftMontagemTempoRealCommandHandler.cs`
- [ ] T025 [US1] Migrate pick/timeout/substitution/layout/finalization/cancellation/archive/restore handlers and enforce layout 409 before mutation in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPickDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AvancarTurnoDraftMontagemTimeoutCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SubstituirReservaDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SalvarLayoutDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/FinalizarDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ArquivarDraftMontagemCommandHandler.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RestaurarDraftMontagemCommandHandler.cs`
- [ ] T026 [US1] Prove all non-publication handlers use the Application publisher while deliberately retaining the additive old notifier method and adapter for Unit 4 publication callers in `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`, `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs`
- [ ] T027 [US1] Run the Unit 3 mutation matrix and backend build to prove one post-commit attempt and zero rejected/no-op events in `BackEnd/RinhaDasLendas.sln`

### Implementation Unit 4: Publication SQL, Version and Availability

- [ ] T028 [P] [US1] Write failing atomic version/date/stamp/no-op tests for claim, success, failure, expiration, republication and expiry reconciliation in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemPublicationVersionIntegrationTests.cs`
- [ ] T029 [P] [US1] Update failing publication/reconciliation tests for version stamps, archive/restore and publisher migration in `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemPublicationRealtimeTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeNotificationPublisherTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPublicationReconciliationServiceTests.cs`
- [ ] T030 [US1] Run the Unit 4 RED filters and confirm raw SQL still returns bool/ID without an atomic parent version stamp in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemPublicationVersionIntegrationTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPublicationReconciliationServiceTests.cs`
- [ ] T031 [US1] Add version stamp and claim result models plus repository contracts in `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemVersionStamp.cs`, `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPublicacaoClaimResult.cs` and `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- [ ] T032 [US1] Make visible Discord claim/success/failure/expiration/expiry-reconciliation SQL and republication aggregate methods atomically update parent version/date and return nullable stamps in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs` and `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T033 [US1] Publish each returned stamp after commit for Discord transitions, republication and expiry reconciliation in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarPublicacaoDiscordDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarCancelamentoDraftArquivadoCommandHandler.cs` and `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemPublicationReconciliationService.cs`
- [ ] T034 [US1] Migrate publication tests/reconciliation callers and all old notifier mocks/doubles, delete the old helper, then remove the old notifier method and adapter member after zero references in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeNotificationPublisher.cs`, `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`, `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeNotificationPublisherTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCommandHandlerTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCancellationMetricsTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemArchivingHandlerTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- [ ] T035 [US1] Run the Unit 4 focused suites, zero-reference scans for the old helper/notifier API/adapter/doubles and backend build to prove monotonic SQL publication, expiry reconciliation and exact availability events in `BackEnd/RinhaDasLendas.sln`

### Implementation Unit 9: Version Lanes, Personalized Sequence and Conflict

- [ ] T036 [P] [US1] Write failing flat-response and mutation API conflict tests in `FrontEnd/src/services/draftMontagens.spec.ts`
- [ ] T037 [P] [US1] Write failing tests for strict greater shared versions, draft/generation rejection, passive/mutation request lanes, passive administrative detail, global personalized sequence, official-clock offset, exact initial/recovery start -> Join -> canonical GET health gate, failed GET degradation, fixed 3000 ms fallback cadence, 2000 ms request timeout/no overlap and GET-before-unlock on 409 in `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T038 [US1] Run the Unit 9 RED suites and confirm equal-version shared merge and one global request gate fail in `FrontEnd/src/services/draftMontagens.spec.ts` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T039 [US1] Preserve flat HTTP parsing and expose structured conflict handling in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T040 [US1] Implement draft/generation/version acceptance, passive/mutation lanes including canonical administrative detail, global personalized sequencing, start + Join + canonical GET health gating, failed-GET degradation, fixed 3000 ms fallback with 2000 ms timeout/no overlap, accepted `serverNow` offset and mutation locking through immediate canonical GET in `FrontEnd/src/views/DraftsView.vue`
- [ ] T041 [US1] Run the Unit 9 focused frontend suites and production build to prove lower/equal shared snapshots never regress canonical state in `FrontEnd/src/services/draftMontagens.spec.ts`, `FrontEnd/src/views/DraftsView.spec.ts` and `FrontEnd/package.json`

**Checkpoint**: User Story 1 is independently functional across backend and frontend; every visible commit attempts identity-safe publication, versions are monotonic, and conflicts reconcile before retry.

---

## Phase 5: User Story 3 - Preserve Success and Automatic Processing Continuity (Priority: P1)

**Goal**: Keep committed operations successful after notification failure, isolate every automatic item in its own scope/command, and record explicit system authorship without duplicates.

**Independent Test**: Force publisher timeout after commit and make the first eligible worker item conflict or throw; the mutation remains successful, later IDs complete, and automatic administrative actions show a valid `System` actor.

### Implementation Unit 5: Actor Migration and Administrative Audit

- [ ] T042 [P] [US3] Write failing actor factory/entity/DTO tests for `User` and `System` nullability in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemSystemActorMigrationTests.cs`
- [ ] T043 [P] [US3] Write failing localized system-actor rendering and synchronized locale-key tests in `FrontEnd/src/views/DraftsView.spec.ts` and `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T044 [US3] Run the Unit 5 RED backend/frontend suites and confirm current contracts require a human user ID in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemSystemActorMigrationTests.cs` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T045 [US3] Implement actor enum/value object, factories and administrative entity invariants in `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemActorType.cs`, `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemActor.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemAcaoAdministrativa.cs` and `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T046 [US3] Update nullable actor DTO mapping and EF configuration in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs` and `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T047 [US3] Add the additive backfill/nullability/check-constraint migration and snapshot in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260730180000_AddDraftMontagemSystemActor.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260730180000_AddDraftMontagemSystemActor.Designer.cs` and `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs`
- [ ] T048 [US3] Expose and render `responsavelTipo` with localized `Sistema`/`System` instead of a fake/blank user ID in `FrontEnd/src/types/draftMontagem.ts`, `FrontEnd/src/views/DraftsView.vue`, `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json`
- [ ] T049 [US3] Run the Unit 5 migration/backend build and frontend tests/build to prove valid system audit rendering in `BackEnd/RinhaDasLendas.sln` and `FrontEnd/package.json`

### Implementation Unit 6: Turn Candidates and Timer

- [ ] T050 [P] [US3] Write failing ID-only candidate projection tests in `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs`
- [ ] T051 [P] [US3] Write failing timer tests for per-ID scopes, reload/revalidation, timeout history, system cancel, generic/non-host cancellation continuation and host cancellation propagation in `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemTurnTimerServiceTests.cs`
- [ ] T052 [US3] Run the Unit 6 RED filters and confirm the current scan loads aggregates and shares a processing scope in `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs` and `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemTurnTimerServiceTests.cs`
- [ ] T053 [US3] Add the ID-only realtime candidate and repository projection in `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemRealtimeCandidate.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs` and `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T054 [US3] Implement the per-draft expiration command that reloads and chooses timeout versus maximum-duration system cancellation in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/ProcessarTurnoDraftMontagemExpiradoCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ProcessarTurnoDraftMontagemExpiradoCommandHandler.cs`
- [ ] T055 [US3] Refactor the timer to one scan scope and one observed command scope per candidate while propagating only host cancellation in `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemTurnTimerService.cs`
- [ ] T056 [US3] Run the Unit 6 focused tests and backend build to prove candidate minimality, isolated continuation and exactly one system cancellation audit in `BackEnd/RinhaDasLendas.sln`

### Implementation Unit 7: Presence Worker and Logging

- [ ] T057 [US3] Write failing presence-worker tests for ID-only scan, per-ID scopes, generic failure, non-host cancellation, host cancellation and subsequent-item continuation in `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPresenceClosureServiceTests.cs`
- [ ] T058 [US3] Run the Unit 7 RED filter and confirm the current worker mutates tracked aggregates in one shared scope in `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPresenceClosureServiceTests.cs`
- [ ] T059 [US3] Add the ID-only presence candidate and repository projection in `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPresenceClosureCandidate.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs` and `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T060 [US3] Implement the per-draft automatic presence command with reload/revalidation, system actor and post-commit publisher in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/EncerrarPresencaDraftMontagemAutomaticamenteCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler.cs`
- [ ] T061 [US3] Refactor presence closure to isolated command scopes with bounded per-ID failure logs and reduce routine EF command logging to Warning in `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemPresenceClosureService.cs` and `BackEnd/src/RinhaDasLendas.Api/appsettings.json`
- [ ] T062 [US3] Run the Unit 7 focused tests and backend build to prove one failed draft never stops later candidates in `BackEnd/RinhaDasLendas.sln`

**Checkpoint**: User Story 3 is independently functional; committed success survives notification failure, every automatic item is isolated, and system actions are auditable without impersonation.

---

## Phase 6: User Story 4 - Protect Unsaved Manual Layout (Priority: P1)

**Goal**: Preserve the board clone and base version until explicit discard or matching persisted save, using one accessible decision flow for remote updates and every navigation/destructive intent.

**Independent Test**: Edit layout locally, deliver a higher remote version, force layout 409, then attempt draft switch, route leave, filtering/removal, archive and browser close; no path discards the clone without an explicit decision.

### Implementation Unit 11: Dirty Signals, Dialog and Guards

- [ ] T063 [P] [US4] Write failing clone/base, remote-higher preservation, explicit reset token, matching accepted save and stale/equal no-clean tests in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- [ ] T064 [P] [US4] Write failing focus trap, Escape, continue-editing and discard tests for the single dialog in `FrontEnd/src/components/drafts/DraftUnsavedLayoutDialog.spec.ts`
- [ ] T065 [US4] Write failing pending-intent, greatest canonical snapshot, 409 preservation, switch/route/removal/archive and `beforeunload` guard tests in `FrontEnd/src/views/DraftsView.spec.ts` and synchronized-copy tests in `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T066 [US4] Run the Unit 11 RED suites and confirm the current prop watcher overwrites dirty local state in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`, `FrontEnd/src/components/drafts/DraftUnsavedLayoutDialog.spec.ts` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T067 [US4] Implement editable clone, `dirty-change`, base-version save payload, `canonicalResetToken` and `acceptedSaveVersion` matching in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T068 [US4] Implement the accessible single-intent discard/continue dialog with existing design tokens in `FrontEnd/src/components/drafts/DraftUnsavedLayoutDialog.vue`
- [ ] T069 [US4] Coordinate pending canonical state, reconciliation-required state and every internal/native guard in `FrontEnd/src/views/DraftsView.vue`
- [ ] T070 [US4] Add synchronized localized status, conflict, dialog and guard guidance with reviewed Portuguese accents in `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json`
- [ ] T071 [US4] Run the full frontend test/lint/build gate to prove dirty clears only on explicit reset or matching save in `FrontEnd/package.json`

**Checkpoint**: User Story 4 is independently functional; remote updates, conflicts and all exit intents preserve unsaved work until the Admin+ explicitly chooses.

---

## Phase 7: User Story 5 - Operate Timers and Enrichment Without Degradation (Priority: P2)

**Goal**: Keep frequent worker scans ID-only, isolate optional presence/captain enrichment from canonical synchronization, and retain useful failure signals without routine query noise.

**Independent Test**: Run both workers against many ineligible drafts and fail an auxiliary request while opening another draft; scans select only IDs, only candidates load aggregates, and main detail/realtime/actions remain available.

### Implementation Unit 10: Optional Auxiliary Enrichment

- [ ] T072 [P] [US5] Extend projection assertions to reject participant, presence, pick, substitution, publication and audit collection loading in `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs`
- [ ] T073 [P] [US5] Write failing auxiliary request-ID, draft/generation, abort, stale-response and localized dependent-control retry tests in `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T074 [US5] Run the Unit 10 RED suites and confirm optional enrichment still shares canonical request lifecycle in `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T075 [US5] Implement independent `auxiliaryRequestId`/`AbortController` handling that never advances canonical lanes or personalized sequence in `FrontEnd/src/views/DraftsView.vue`
- [ ] T076 [US5] Add localized auxiliary failure/retry copy without blocking detail or unrelated actions in `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json`
- [ ] T077 [US5] Run the Unit 10 focused backend projection and frontend view/i18n suites plus both builds to prove optional enrichment and routine scans cannot degrade the cycle in `BackEnd/RinhaDasLendas.sln` and `FrontEnd/package.json`

**Checkpoint**: User Story 5 is independently functional; candidate scans stay minimal, auxiliary failure is contained, and operational failures remain visible without routine EF query noise.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Execute approved units 12 and 13, prove temporal multiclient outcomes, reconcile contracts/evidence, audit internationalization and prepare the additive deployment sequence.

### Implementation Unit 12: Multiclient Integration and Observability

- [ ] T078 [P] Write failing two-client full-cycle tests including SQL publication/republication/expiry reconciliation, archive/restore, canceled-request publisher, worker isolation and explicit `<= 2 s` plus worst-case 3000 ms fallback + 2000 ms request `<= 5 s` assertions in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemRealtimeMultiClientIntegrationTests.cs`
- [ ] T079 [P] Extend cycle and frontend timing/ordering seams for personalized capabilities, no-op silence, exact recovery start -> Join -> canonical GET -> `connected` and failed-GET degradation in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T080 Run the Unit 12 RED backend/frontend filters and confirm temporal and full-journey evidence is missing in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemRealtimeMultiClientIntegrationTests.cs` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T081 Add bounded event/outcome metrics and safe structured publication logs without tokens, claims, payloads or sensitive Hub URLs in `BackEnd/src/RinhaDasLendas.Api/Observability/ApiMetrics.cs` and `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs`
- [ ] T082 Complete only the instrumentation/test seams needed for unequivocal multiclient timing evidence in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemRealtimeMultiClientIntegrationTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs` and `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T083 Run the full backend test/build and frontend test/build gate with exact `<= 2 s` event and `<= 5 s` recovery assertions in `BackEnd/RinhaDasLendas.sln` and `FrontEnd/package.json`

### Implementation Unit 13: Verification, Browser, Review and Deploy Evidence

- [ ] T084 Execute the Unit 13 direct-dotnet/Linux-Compose/Windows-Docker decision tree plus complete backend test/build, migration test/update review and frontend test/lint/build gates from `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- [ ] T085 Execute the two-session browser journey for presence through finalization, timeout, substitution, archive/restore, connection loss and dirty layout, recording measured `<= 2 s` and 3000 ms + 2000 ms worst-case `<= 5 s` evidence in `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- [ ] T086 [P] Reconcile flat HTTP, shared SignalR, actor, conflict and UI contracts against implementation in `specs/029-corrigir-sincronizacao-draft/contracts/realtime-sync.openapi.yaml`, `specs/029-corrigir-sincronizacao-draft/contracts/realtime-events.md` and `specs/029-corrigir-sincronizacao-draft/contracts/ui-contracts.md`
- [ ] T087 Audit frontend hardcoded text, synchronized PT/EN keys, Portuguese accents, placeholders, buttons, titles, badges, toasts, empty states and validation messages in `FrontEnd/src/i18n/locales/pt.json`, `FrontEnd/src/i18n/locales/en.json` and `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T088 Audit backend hardcoded user messages, localized validators/errors and synchronized resources in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx` and `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- [ ] T089 Run keyboard/screen-reader and responsive review at 1440, 1280, 1024, 768 and 480 px for degraded status and unsaved-layout decisions, recording evidence in `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- [ ] T090 Run architecture/security/code review for CQRS boundaries, authorized Hub access, safe logs, no duplicate publication and absence of Redis/backplane/outbox/store additions, recording findings in `docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md`
- [ ] T091 Validate additive migration backup/rollback order, backend-before-frontend deployment, single-replica constraint and post-deploy observability evidence in `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- [ ] T092 Run zero-old-helper, unfinished-marker, locale-drift and whitespace checks and record the final pass evidence in `specs/029-corrigir-sincronizacao-draft/quickstart.md`

**Checkpoint**: All thirteen units pass; browser, accessibility, i18n, review and deployment evidence demonstrate FR-001 through FR-032 and SC-001 through SC-011.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1; Unit 1 precedes Unit 2 and blocks every story.
- **Phase 3 (US2)**: Depends on Phase 2; transport lifecycle is required before view-level synchronization work.
- **Phase 4 (US1)**: Depends on Phases 2 and 3; Unit 3 precedes Unit 4, and both precede Unit 9 integration.
- **Phase 5 (US3)**: Depends on Phase 2 and the publisher/mutation contracts from Phase 4; Unit 5 precedes system-worker branches in Units 6 and 7.
- **Phase 6 (US4)**: Depends on Unit 9 conflict/version gates from Phase 4 and executes Unit 11.
- **Phase 7 (US5)**: Depends on candidate infrastructure from Units 6/7 and canonical lanes from Unit 9; auxiliary work is formalized as Unit 10.
- **Phase 8 (Polish)**: Depends on all five stories; Unit 12 precedes final Unit 13 evidence.

### User Story Dependencies

- **US2 (P1)**: Starts after Foundational and establishes the transport lifecycle consumed by US1.
- **US1 (P1)**: Starts after US2 transport; backend publication is independently testable before frontend convergence is integrated.
- **US3 (P1)**: Starts after US1 publisher/mutation migration so automatic commands use the single post-commit path.
- **US4 (P1)**: Starts after US1 version/conflict gates and remains independently testable through board/view tests.
- **US5 (P2)**: Starts after US1 canonical lanes and US3 candidate projections; it independently proves containment and query minimality.

### Approved Unit Order

Units 10 e 11 são independentes depois da Unit 9. A ordem física prioriza a US4/P1 (Unit 11) antes da US5/P2 (Unit 10), sem alterar seus contratos ou checkpoints.

1. Unit 1 shared DTO and authorized Hub/GET parity.
2. Unit 2 additive notifier and Application publisher.
3. Unit 8 frontend transport lifecycle, required before view coordination.
4. Unit 3 complete handler matrix and layout-base conflict.
5. Unit 4 versioned publication SQL and old helper removal.
6. Unit 9 version lanes, personalized sequence and conflict reconciliation.
7. Unit 5 actor migration and administrative UI/i18n.
8. Unit 6 timer candidates and system cancellation.
9. Unit 7 presence worker and logging.
10. Unit 10 optional eligible-player/captain auxiliary lifecycle.
11. Unit 11 dirty signals, dialog and guards.
12. Unit 12 multiclient integration and observability.
13. Unit 13 full verification and deployment evidence.

### Within Each Implementation Unit

- RED tests are written before production changes and must fail for the expected missing behavior.
- The focused RED command runs before GREEN implementation.
- Production changes are minimal and retain temporary additive seams only where the next migration unit requires them.
- Focused tests and the relevant build must pass before the unit checkpoint; do not carry a broken compile into the next unit.
- Unit 3 retains the additive old notifier method/adapter/doubles after migrating non-publication handlers; Unit 4 migrates publication/reconciliation callers, deletes the old helper, then removes that old notifier surface only after zero-reference scans and a green build.

---

## Parallel Opportunities

- T001 and T002 can run in parallel.
- T003 and T004 can run in parallel before T005; T009 can prepare doubles independently before T010-T011.
- T028 and T029 can run in parallel before Unit 4 implementation.
- T036 and T037 can run in parallel before T038.
- T042 and T043 can run in parallel before T044.
- T050 and T051 can run in parallel before T052.
- T063 and T064 can run in parallel before T065-T066.
- T072 and T073 can run in parallel before T074.
- T078 and T079 can run in parallel before T080.
- T086 can run in parallel with T087-T088 after runtime verification; T089-T092 require the completed implementation and recorded results.

## Parallel Example: User Story 1

```text
Task T028: Write SQL publication version integration tests.
Task T029: Update publisher/reconciliation unit and service tests.

Task T036: Write flat-response and API conflict tests.
Task T037: Write view version-lane and personalized-sequence tests.
```

## Parallel Example: User Story 3

```text
Task T042: Write backend actor migration/DTO tests.
Task T043: Write frontend actor rendering/i18n tests.

Task T050: Write candidate projection tests.
Task T051: Write timer isolation and cancellation tests.
```

## Parallel Example: User Story 4

```text
Task T063: Write board clone/reset/save tests.
Task T064: Write dialog accessibility tests.
```

---

## Implementation Strategy

### MVP First

1. Complete Setup and both Foundational units.
2. Complete US2 transport lifecycle because US1 view convergence consumes it.
3. Complete US1 Units 3, 4 and 9.
4. Stop and validate the two-session US1 independent test before adding worker/layout scope.

### Incremental Delivery

1. Deliver shared authorization and resilient publication without changing feature 028 rules.
2. Add connection recovery and monotonic multiclient convergence.
3. Add system actor plus isolated timer/presence processing.
4. Add deterministic unsaved-layout protection.
5. Prove candidate/enrichment containment, then execute multiclient and deployment gates.

### TDD and Compile-Safe Strategy

1. Treat each approved unit as RED, focused RED run, GREEN, focused verification and build.
2. Keep notifier changes additive in Unit 2 so existing callers compile.
3. Migrate non-publication handlers in Unit 3 while retaining the additive old notifier surface so the repository compiles.
4. Migrate publication reconciliation, handlers and doubles in Unit 4; delete the old helper before removing the old notifier method/adapter/doubles, then prove zero references and build.
5. Add actor schema before workers emit `System`; add transport lifecycle before view lanes; add version/conflict lanes before Unit 10 auxiliary lifecycle and Unit 11 dirty guards.
6. Never relax temporal assertions, retry mutations automatically or add distributed infrastructure to make tests pass.

---

## Requirement Coverage

| Requirement | Task coverage |
|-------------|---------------|
| FR-001 | T003-T009, T012-T014, T036-T041 |
| FR-002 | T003-T008, T036-T041 |
| FR-003 | T020-T035 |
| FR-004 | T009-T014, T078-T083 |
| FR-005 | T009-T014, T081-T083 |
| FR-006 | T020-T035 |
| FR-007 | T015-T019, T036-T041 |
| FR-008 | T003-T008, T015-T019 |
| FR-009 | T015-T019 |
| FR-010 | T015-T019, T036-T041 |
| FR-011 | T015-T019, T036-T041, T073-T077 |
| FR-012 | T036-T041 |
| FR-013 | T036-T041 |
| FR-014 | T036-T041 |
| FR-015 | T036-T041 |
| FR-016 | T021-T027, T036-T041, T065-T071 |
| FR-017 | T037-T041 |
| FR-018 | T015-T019, T036-T041, T065-T071, T087-T089 |
| FR-019 | T036-T041, T073-T077 |
| FR-020 | T073-T077 |
| FR-021 | T063-T071 |
| FR-022 | T063-T071 |
| FR-023 | T063-T071 |
| FR-024 | T063-T071 |
| FR-025 | T063-T071 |
| FR-026 | T050-T062 |
| FR-027 | T042-T062 |
| FR-028 | T050-T062, T072-T077 |
| FR-029 | T020-T035, T050-T062, T078-T083 |
| FR-030 | T057-T062, T072-T077, T081-T083 |
| FR-031 | T003-T008, T042-T049, T063-T077, T086-T089 |
| FR-032 | T010-T019, T086-T092 |
| SC-001 | T078-T085 |
| SC-002 | T015-T019, T036-T041, T078-T085 |
| SC-003 | T036-T041 |
| SC-004 | T020-T035 |
| SC-005 | T010-T014, T078-T083 |
| SC-006 | T050-T062, T078-T083 |
| SC-007 | T063-T071 |
| SC-008 | T050-T062, T072-T077 |
| SC-009 | T073-T077 |
| SC-010 | T042-T049, T063-T071, T087-T089 |
| SC-011 | T078-T085 |

## Notes

- `[P]` means the task is safe to execute concurrently only at the point shown by its unit dependencies.
- Story labels appear only in user story phases; Setup, Foundational and Polish tasks intentionally have no story label.
- T001 and T002 are procedural setup tasks and intentionally do not map to FR/SC coverage.
- HTTP remains flat and personalized; SignalR remains shared and identity-neutral.
- One replica is the delivery target; no Redis, backplane, outbox, event sourcing or frontend store is introduced.
- Commit only after each complete, passing implementation unit or approved logical group, using a Brazilian Portuguese commit message.

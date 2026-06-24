# Tasks: Draft em Tempo Real na Montagem de Times

**Input**: Design documents from `/specs/012-draft-montagem-tempo-real/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/draft-montagem-tempo-real-api.md`, `quickstart.md`

## Phase 1: Setup

- [ ] T001 Add SignalR frontend dependency in `FrontEnd/package.json`
- [ ] T002 Add realtime draft montagem message codes in `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [ ] T003 Add localized backend resource entries in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx`, and `Messages.en-US.resx`
- [ ] T004 Add frontend realtime draft montagem translation keys in `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json`
- [ ] T005 Add frontend realtime constants in `FrontEnd/src/constants/messageCode.ts` and `FrontEnd/src/types/draftMontagem.ts`

## Phase 2: Foundational Blocking Tasks

- [ ] T006 [P] Add domain enums for realtime mode, choice type, and substitution status in `BackEnd/src/RinhaDasLendas.Domain/Enums/`
- [ ] T007 [P] Add realtime DTO contracts in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemDtos.cs`
- [ ] T008 Add `DraftMontagemEscolha` domain entity in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemEscolha.cs`
- [ ] T009 Add `DraftMontagemSubstituicao` domain entity in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemSubstituicao.cs`
- [ ] T010 Extend `DraftMontagem` realtime state fields and collections in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T011 Extend repository contract for realtime queries and active captain lookup in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- [ ] T012 Configure realtime entities and new columns in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T013 Add EF migration for realtime draft montagem tables and columns in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`
- [ ] T014 Implement repository realtime methods in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T015 Register SignalR and realtime services in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T016 Add draft montagem hub shell in `BackEnd/src/RinhaDasLendas.Api/Hubs/DraftMontagensHub.cs`
- [ ] T017 Add frontend realtime service shell in `FrontEnd/src/services/draftMontagemRealtime.ts`
- [ ] T018 Add domain documentation for realtime draft montagem in `docs/domain/draft-montagem-tempo-real.md`

## Phase 3: User Story 1 - Capitão escolhe jogadores no seu turno (P1)

**Goal**: Capitão ativo da vez escolhe jogador livre, backend valida regra crítica e avança o turno.

**Independent Test**: Criar montagem em tempo real com capitães e livres, autenticar como capitão da vez, escolher jogador livre e verificar jogador no time, histórico atualizado e próximo turno.

### Tests for User Story 1

- [ ] T019 [P] [US1] Add domain tests for starting realtime draft and valid pick in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemRealtimeTests.cs`
- [ ] T020 [P] [US1] Add domain tests rejecting non-current captain, non-captain user, full team, picked player and reserve pick in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemRealtimeTests.cs`
- [ ] T021 [P] [US1] Add pick validator tests in `BackEnd/tests/RinhaDasLendas.Tests/Validators/DraftMontagemRealtimeValidatorTests.cs`
- [ ] T022 [P] [US1] Add pick handler tests with current user/player mapping in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/RegistrarPickDraftMontagemCommandHandlerTests.cs`

### Implementation for User Story 1

- [ ] T023 [US1] Add `IniciarDraftMontagemTempoRealCommand` and `RegistrarPickDraftMontagemCommand` in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/`
- [ ] T024 [US1] Add validators for start realtime and pick requests in `BackEnd/src/RinhaDasLendas.Application/Validators/DraftMontagemValidator.cs`
- [ ] T025 [US1] Implement domain methods for starting realtime mode, validating captain turn and registering pick in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T026 [US1] Implement start realtime handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/IniciarDraftMontagemTempoRealCommandHandler.cs`
- [ ] T027 [US1] Implement pick handler using `ICurrentUser` and repository player lookup in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPickDraftMontagemCommandHandler.cs`
- [ ] T028 [US1] Add start realtime and pick endpoints in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T029 [US1] Extend frontend draft montagem types for realtime state and choice history in `FrontEnd/src/types/draftMontagem.ts`
- [ ] T030 [US1] Add start realtime and pick API calls in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T031 [US1] Update `DraftVisualBoard.vue` to disable drag-and-drop in realtime mode and show pick actions only for current captain in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T032 [US1] Update `DraftsView.vue` to pass authenticated player context and handle realtime pick actions in `FrontEnd/src/views/DraftsView.vue`

## Phase 4: User Story 2 - Participantes acompanham o draft sincronizado (P1)

**Goal**: Todos os usuários conectados à mesma montagem recebem estado oficial atualizado sem recarregar a página.

**Independent Test**: Abrir a mesma montagem em duas sessões, realizar escolha em uma e verificar atualização automática na outra.

### Tests for User Story 2

- [ ] T033 [P] [US2] Add hub authorization/group tests in `BackEnd/tests/RinhaDasLendas.Tests/Realtime/DraftMontagensHubTests.cs`
- [ ] T034 [P] [US2] Add frontend realtime service tests for connect, join, state update and disconnect in `FrontEnd/src/services/draftMontagemRealtime.spec.ts`
- [ ] T035 [P] [US2] Add component test for receiving updated montagem state in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`

### Implementation for User Story 2

- [ ] T036 [US2] Implement join/leave group methods in `BackEnd/src/RinhaDasLendas.Api/Hubs/DraftMontagensHub.cs`
- [ ] T037 [US2] Add `IDraftMontagemRealtimeNotifier` application interface in `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- [ ] T038 [US2] Implement SignalR notifier in `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- [ ] T039 [US2] Publish state updates after start and pick handlers in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/`
- [ ] T040 [US2] Add realtime state query and handler in `BackEnd/src/RinhaDasLendas.Application/Queries/DraftMontagens/` and `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/`
- [ ] T041 [US2] Add realtime state endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T042 [US2] Implement authenticated SignalR connection and group subscription in `FrontEnd/src/services/draftMontagemRealtime.ts`
- [ ] T043 [US2] Connect `DraftsView.vue` to realtime service and replace selected montagem on state events in `FrontEnd/src/views/DraftsView.vue`
- [ ] T044 [US2] Add disconnected/reconnecting localized UI state in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`

## Phase 5: User Story 3 - Turno expira automaticamente (P1)

**Goal**: O backend avança a vez automaticamente após 30 segundos sem escolha.

**Independent Test**: Iniciar turno, não escolher, aguardar expiração e confirmar timeout registrado, próximo capitão e atualização em tempo real.

### Tests for User Story 3

- [ ] T045 [P] [US3] Add domain tests for timeout, skipped full teams and completion when no eligible slots remain in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemRealtimeTests.cs`
- [ ] T046 [P] [US3] Add timeout handler tests for expired and non-expired turns in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/AvancarTurnoDraftMontagemTimeoutCommandHandlerTests.cs`
- [ ] T047 [P] [US3] Add frontend timer display tests in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`

### Implementation for User Story 3

- [ ] T048 [US3] Add `AvancarTurnoDraftMontagemTimeoutCommand` in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/AvancarTurnoDraftMontagemTimeoutCommand.cs`
- [ ] T049 [US3] Implement domain timeout and next-turn methods in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T050 [US3] Add repository method to list expired realtime drafts in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T051 [US3] Implement timeout command handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AvancarTurnoDraftMontagemTimeoutCommandHandler.cs`
- [ ] T052 [US3] Add backend hosted service for expired turn processing in `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemTurnTimerService.cs`
- [ ] T053 [US3] Register hosted timer service in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T054 [US3] Render backend-based countdown and timeout states in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`

## Phase 6: User Story 4 - Reserva atua como complemento emergencial (P2)

**Goal**: Reservas ficam fora do pool de escolha e entram somente por substituição emergencial administrativa.

**Independent Test**: Confirmar que reserva não pode ser escolhido; executar substituição administrativa válida e ver reserva entrar no time com histórico.

### Tests for User Story 4

- [ ] T055 [P] [US4] Add domain tests for emergency substitution validation in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemSubstituicaoTests.cs`
- [ ] T056 [P] [US4] Add handler tests for reserve substitution authorization and validation in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/SubstituirReservaDraftMontagemCommandHandlerTests.cs`
- [ ] T057 [P] [US4] Add frontend component tests for reserve section and disabled pick affordance in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`

### Implementation for User Story 4

- [ ] T058 [US4] Add `SubstituirReservaDraftMontagemCommand` in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/SubstituirReservaDraftMontagemCommand.cs`
- [ ] T059 [US4] Add substitution request validator in `BackEnd/src/RinhaDasLendas.Application/Validators/DraftMontagemValidator.cs`
- [ ] T060 [US4] Implement domain substitution method in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T061 [US4] Implement substitution handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SubstituirReservaDraftMontagemCommandHandler.cs`
- [ ] T062 [US4] Add substitution endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T063 [US4] Add substitution API call in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T064 [US4] Add reserve emergency UI controls for organizers in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T065 [US4] Publish substitution realtime event in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SubstituirReservaDraftMontagemCommandHandler.cs`

## Phase 7: User Story 5 - Organizadores gerenciam ciclo do draft (P2)

**Goal**: Organizadores iniciam, cancelam e finalizam o ciclo de tempo real com feedback claro.

**Independent Test**: Organizador inicia draft, acompanha mudanças, cancela/finaliza e novas escolhas ficam bloqueadas.

### Tests for User Story 5

- [ ] T066 [P] [US5] Add handler tests for finalize/cancel realtime notification behavior in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/DraftMontagemStateCommandHandlersTests.cs`
- [ ] T067 [P] [US5] Add frontend tests for start/cancel/finalize button states in `FrontEnd/src/views/DraftsView.spec.ts`

### Implementation for User Story 5

- [ ] T068 [US5] Ensure cancel/finalize handlers clear active realtime turn in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemStateCommandHandlers.cs`
- [ ] T069 [US5] Publish realtime updates after cancel/finalize in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemStateCommandHandlers.cs`
- [ ] T070 [US5] Add start realtime CTA and lifecycle controls in `FrontEnd/src/views/DraftsView.vue`
- [ ] T071 [US5] Add lifecycle status badges and history summary in `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`

## Final Phase: Polish & Cross-Cutting Concerns

- [ ] T072 [P] Audit frontend hardcoded user-visible text in `FrontEnd/src/views/DraftsView.vue` and `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T073 [P] Audit backend hardcoded user-facing messages in realtime draft handlers, validators, hub and timer service under `BackEnd/src/`
- [ ] T074 [P] Add/update docs for realtime draft operations in `docs/domain/draft-montagem-tempo-real.md`
- [ ] T075 [P] Run and fix backend tests with `dotnet test` from `BackEnd/`
- [ ] T076 [P] Run and fix frontend checks with `npm run build` and `npm run test` from `FrontEnd/`
- [ ] T077 Verify `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json` contain synchronized keys
- [ ] T078 Verify backend resources exist in default, pt-BR and en-US resource files
- [ ] T079 Perform final i18n audit and document results in implementation summary

## Dependencies

- Phase 1 must complete before Phase 2.
- Phase 2 blocks all user stories.
- US1, US2 and US3 together form the MVP for realtime draft picks.
- US2 depends on the state DTOs and notifier interface from Phase 2 and pick events from US1.
- US3 depends on realtime state fields and next-turn rules from US1.
- US4 depends on reserve state rules from Phase 2 and state broadcast from US2.
- US5 depends on lifecycle fields from Phase 2 and notifier from US2.

## Parallel Execution Examples

### Phase 2

- T006, T007, T008 and T009 can run in parallel after setup.
- T011 and T017 can run in parallel once DTO direction is clear.

### US1

- T019, T020, T021 and T022 can run in parallel.
- T029 and T030 can run while backend handlers T026 and T027 are being implemented.

### US2

- T033, T034 and T035 can run in parallel.
- T036, T037 and T042 can run in parallel after foundational SignalR setup.

### US3

- T045, T046 and T047 can run in parallel.
- T052 and T054 can run in parallel after T051 defines timeout behavior.

### US4

- T055, T056 and T057 can run in parallel.
- T063 and T064 can run after the request/response contract is stable.

## Implementation Strategy

### MVP First

Complete Phase 1, Phase 2, US1, US2 and US3 first. This delivers realtime captain picks, synchronization and backend timer without emergency substitution UI.

### Incremental Delivery

1. Deliver domain persistence and setup.
2. Deliver valid/invalid captain picks.
3. Deliver realtime synchronization.
4. Deliver backend timeout.
5. Deliver reserve substitution and lifecycle polish.

### Format Validation

All tasks follow the required checklist format with checkbox, task ID, optional parallel marker, story label for user story phases, and explicit file paths.

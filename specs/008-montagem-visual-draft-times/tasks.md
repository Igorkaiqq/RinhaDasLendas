# Tasks: Montagem Visual de Draft e Times

**Input**: Design documents from `/specs/008-montagem-visual-draft-times/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by project standards for domain rules, validators, application handlers, endpoint coverage and frontend critical flows.

## Phase 1: Setup

- [ ] T001 Add draft montagem message codes in `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [ ] T002 Add localized backend messages for draft montagem in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx`, and `Messages.en-US.resx`
- [ ] T003 Add frontend route/type/status constants in `FrontEnd/src/types/draftMontagem.ts` and `FrontEnd/src/constants/draftMontagemStatus.ts`
- [ ] T004 Add frontend service shell in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T005 Add visual draft entry point/tab/CTA in `FrontEnd/src/views/DraftsView.vue`

## Phase 2: Foundational

- [ ] T006 [P] Create domain enums in `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemStatus.cs`, `DraftMontagemParticipanteEstado.cs`, and `DraftMontagemCriterioCapitaes.cs`
- [ ] T007 [P] Create request/response DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemDtos.cs`
- [ ] T008 Create domain entities in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`, `DraftMontagemTime.cs`, and `DraftMontagemParticipante.cs`
- [ ] T009 [P] Add repository contract in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- [ ] T010 Add DbSets and mappings in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T011 Add EF migration for draft montagem tables in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`
- [ ] T012 Implement repository in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T013 Register repository in `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- [ ] T014 [P] Add domain documentation in `docs/domain/draft-montagens.md`

## Phase 3: User Story 1 - Selecionar Jogadores Cadastrados (P1)

**Goal**: Organizer creates a visual montage using active registered players, without manual textarea input.

**Independent Test**: Select active players, define team size, see calculated setup and create montage.

- [ ] T015 [P] [US1] Add domain creation/calculation tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T016 [P] [US1] Add create validator tests in `BackEnd/tests/RinhaDasLendas.Tests/Validators/CreateDraftMontagemValidatorTests.cs`
- [ ] T017 [P] [US1] Add create handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/CreateDraftMontagemCommandHandlerTests.cs`
- [ ] T018 [US1] Create command in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/CreateDraftMontagemCommand.cs`
- [ ] T019 [US1] Create validator in `BackEnd/src/RinhaDasLendas.Application/Validators/DraftMontagemValidator.cs`
- [ ] T020 [US1] Implement create handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CreateDraftMontagemCommandHandler.cs`
- [ ] T021 [US1] Add create endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T022 [US1] Implement create API call in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T023 [US1] Create setup component `FrontEnd/src/components/drafts/visual/DraftVisualSetup.vue`
- [ ] T024 [US1] Add player search/filter/selection to setup using registered players
- [ ] T025 [US1] Show calculated teams/captains/reserves summary before creation

## Phase 4: User Story 2 - Montar Times por Drag and Drop (P1)

**Goal**: Organizer moves players between free area, teams and reserves without duplicates.

**Independent Test**: Move players across areas, validate no duplication and blocked full teams.

- [ ] T026 [P] [US2] Add domain movement/layout tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T027 [P] [US2] Add save layout handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/SalvarLayoutDraftMontagemCommandHandlerTests.cs`
- [ ] T028 [US2] Create save layout command in `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/SalvarLayoutDraftMontagemCommand.cs`
- [ ] T029 [US2] Implement backend layout validation for duplicates, capacity and participant states
- [ ] T030 [US2] Add save layout endpoint and movement endpoint contract coverage in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T031 [US2] Implement get/list queries and handlers for reopening board
- [ ] T032 [US2] Add get/list endpoints in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T033 [US2] Implement frontend get/list/save layout calls in `FrontEnd/src/services/draftMontagens.ts`
- [ ] T034 [US2] Create board component `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T035 [US2] Create team drop zone component `FrontEnd/src/components/drafts/visual/DraftVisualTeam.vue`
- [ ] T036 [US2] Create free players and reserves panels in `FrontEnd/src/components/drafts/visual/`
- [ ] T037 [US2] Implement drag/drop state updates with duplicate prevention in frontend
- [ ] T038 [US2] Add save layout action, pending-changes state and error recovery
- [ ] T039 [US2] Persist per-player contextual route in layout save request/response

## Phase 5: User Story 3 - Calcular Times, Capitaes e Reservas (P1)

**Goal**: System calculates dynamic teams/captains/reserves for selected player count and team size.

**Independent Test**: Validate 15/18/20 players with team size 5 produce expected results.

- [ ] T040 [P] [US3] Add calculation tests for 15/18/20 players in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T041 [US3] Implement domain calculation for `QuantidadeTimes`, `QuantidadeReservas`, and required captains
- [ ] T042 [US3] Ensure setup summary mirrors backend calculation rules
- [ ] T043 [US3] Add UI validation for insufficient players and invalid team size
- [ ] T044 [US3] Add responsive layout behavior for 1, 2, 3, 4, 6 and 8 teams

## Phase 6: User Story 4 - Consultar Detalhes do Jogador (P2)

**Goal**: Click opens player details; drag does not open details.

**Independent Test**: Click and drag same card, validate different behavior.

- [ ] T045 [P] [US4] Add frontend component tests for click vs drag in `FrontEnd/src/components/drafts/visual/DraftPlayerCard.spec.ts`
- [ ] T046 [US4] Create player card component `FrontEnd/src/components/drafts/visual/DraftPlayerCard.vue`
- [ ] T047 [US4] Create details drawer `FrontEnd/src/components/drafts/visual/PlayerDetailsDrawer.vue`
- [ ] T048 [US4] Show full player data, route preferences and timestamps in drawer
- [ ] T049 [US4] Add drag threshold logic to avoid accidental drawer opening
- [ ] T050 [US4] Add contextual route selector to player card without mutating player profile preferences

## Phase 7: User Story 5 - Definir e Substituir Capitaes (P2)

**Goal**: Organizer selects, draws and replaces one captain per generated team.

**Independent Test**: Select/draw captains for N teams and replace one captain.

- [ ] T051 [P] [US5] Add captain validation tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T052 [P] [US5] Add draw captains handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/SortearCapitaesDraftMontagemCommandHandlerTests.cs`
- [ ] T053 [US5] Create draw captains command and handler
- [ ] T054 [US5] Add draw captains endpoint
- [ ] T055 [US5] Implement captain selector component `FrontEnd/src/components/drafts/visual/DraftCaptainSelector.vue`
- [ ] T056 [US5] Add captain replacement UI and validation feedback
- [ ] T057 [US5] Ensure captain badge follows player when moved between areas

## Phase 8: User Story 6 - Gerenciar Reservas (P2)

**Goal**: Reserves are visible and can be promoted when there is a vacancy.

**Independent Test**: Create 18-player montage, promote reserve after opening vacancy.

- [ ] T058 [P] [US6] Add reserve state transition tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T059 [US6] Add reserve panel states and empty state
- [ ] T060 [US6] Add promote reserve behavior through drag/drop and action menu
- [ ] T061 [US6] Add blocked promotion feedback when all teams are full
- [ ] T062 [US6] Include reserves in save layout and get response mapping

## Phase 9: User Story 7 - Exportar Resultado Visual (P3)

**Goal**: Organizer exports readable image of final montage.

**Independent Test**: Export montage with teams and reserves, verify image content.

- [ ] T063 [P] [US7] Add frontend test or manual validation checklist for export controls
- [ ] T064 [US7] Create export controls component `FrontEnd/src/components/drafts/visual/DraftExportControls.vue`
- [ ] T065 [US7] Add capture-only result area that excludes editing controls
- [ ] T066 [US7] Include montage name, teams, captains, players, routes, reserves and date in export area
- [ ] T067 [US7] Add export error handling and user feedback

## Phase 10: Finalize, Cancel and Read States

- [ ] T068 [P] Add finalize/cancel handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/DraftMontagens/`
- [ ] T069 Create finalize command, validator and endpoint
- [ ] T070 Create cancel command, validator and endpoint
- [ ] T071 Add read-only finalized board behavior in frontend
- [ ] T072 Add cancel confirmation dialog or reuse existing draft cancel dialog pattern

## Phase 11: Polish & Cross-Cutting

- [ ] T073 [P] Add service tests in `FrontEnd/src/services/draftMontagens.spec.ts`
- [ ] T074 [P] Add board/setup component tests in `FrontEnd/src/components/drafts/visual/`
- [ ] T075 Add endpoint coverage to `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- [ ] T076 Update message catalog in `docs/messages/message-catalog.md`
- [ ] T077 Update quickstart with any final validation notes discovered during implementation
- [ ] T078 Run backend tests from `BackEnd/` and fix failures
- [ ] T079 Run frontend tests/build from `FrontEnd/` and fix failures

## Dependencies

- Phase 1 before all other phases.
- Phase 2 blocks every user story.
- US1, US2 and US3 form the MVP and must be completed before finalization/export are considered complete.
- US4 depends on player card component from US2.
- US5 depends on generated teams from US3.
- US6 depends on participant states from US2 and reserve calculation from US3.
- US7 depends on board rendering from US2.

## Parallel Examples

- T006, T007, T009 and T014 can run in parallel.
- T015, T016 and T017 can run in parallel before US1 implementation.
- T026 and T027 can run in parallel before US2 implementation.
- T045 and T051 can run in parallel after player/team components exist.
- T073 and T074 can run in parallel during polish.

## Implementation Strategy

1. Build backend aggregate, persistence and create/read endpoints first.
2. Build setup flow and board MVP with save layout.
3. Add captain/reserve polish and player details.
4. Add export and finalization.
5. Run full backend/frontend verification.

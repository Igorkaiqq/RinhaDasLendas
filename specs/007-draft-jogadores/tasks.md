# Tasks: Draft de Jogadores

**Input**: Design documents from `/specs/007-draft-jogadores/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by project standards for domain rules, validators, application use cases, integration endpoints and frontend critical flows.

## Phase 1: Setup

- [ ] T001 Add draft message codes in `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [ ] T002 Add draft localized backend messages in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx`, and `Messages.en-US.resx`
- [ ] T003 Add draft frontend i18n keys in `FrontEnd/src/i18n/locales/pt-BR.json` and `FrontEnd/src/i18n/locales/en-US.json`
- [ ] T004 Add draft route constants/navigation availability in `FrontEnd/src/constants/appRoutes.ts` and `FrontEnd/src/components/layout/AppShell.vue`

## Phase 2: Foundational

- [ ] T005 [P] Create draft enums in `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftStatus.cs`, `DraftTime.cs`, and `DraftCriterioSelecao.cs`
- [ ] T006 [P] Create draft request/response DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftDtos.cs`
- [ ] T007 Create draft domain entities in `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftSessao.cs`, `DraftParticipante.cs`, and `DraftEscolha.cs`
- [ ] T008 [P] Add draft repository contract in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftRepository.cs`
- [ ] T009 Add draft DbSets and mappings in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T010 Add EF migration for draft tables in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260619000200_CreateDrafts.cs` and update `RinhaDasLendasDbContextModelSnapshot.cs`
- [ ] T011 Implement draft repository in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftRepository.cs`
- [ ] T012 Register draft repository in `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- [ ] T013 [P] Add draft frontend types/constants in `FrontEnd/src/types/draft.ts` and `FrontEnd/src/constants/draftStatus.ts`
- [ ] T014 [P] Add draft domain documentation in `docs/domain/drafts.md`

## Phase 3: User Story 1 - Criar Sessao de Draft (P1)

**Goal**: Organizer creates a valid draft session with manual captains or draw-ready inputs.

**Independent Test**: Create session with active unique players and verify status, captains, available players and next pick.

- [ ] T015 [P] [US1] Add draft domain creation tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftSessaoTests.cs`
- [ ] T016 [P] [US1] Add create draft validator tests in `BackEnd/tests/RinhaDasLendas.Tests/Validators/CreateDraftValidatorTests.cs`
- [ ] T017 [P] [US1] Add create draft handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/Drafts/CreateDraftCommandHandlerTests.cs`
- [ ] T018 [US1] Create draft commands in `BackEnd/src/RinhaDasLendas.Application/Commands/Drafts/CreateDraftCommand.cs`
- [ ] T019 [US1] Create draft validators in `BackEnd/src/RinhaDasLendas.Application/Validators/DraftValidator.cs`
- [ ] T020 [US1] Implement create draft handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/CreateDraftCommandHandler.cs`
- [ ] T021 [US1] Implement draft response mapper/helpers in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/DraftHandlerHelpers.cs`
- [ ] T022 [US1] Add create endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftsController.cs`
- [ ] T023 [US1] Add draft service create/list/get methods in `FrontEnd/src/services/drafts.ts`
- [ ] T024 [US1] Add draft create modal in `FrontEnd/src/components/drafts/DraftCreateModal.vue`
- [ ] T025 [US1] Add draft view shell and create flow in `FrontEnd/src/views/DraftsView.vue`

## Phase 4: User Story 2 - Registrar Escolhas Alternadas (P1)

**Goal**: Organizer registers picks in order, no duplicate players, automatic completion.

**Independent Test**: Pick available players until both teams reach the configured limit and verify final status.

- [ ] T026 [P] [US2] Add draft pick domain tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftSessaoTests.cs`
- [ ] T027 [P] [US2] Add pick handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/Drafts/RegistrarPickDraftCommandHandlerTests.cs`
- [ ] T028 [US2] Create pick command in `BackEnd/src/RinhaDasLendas.Application/Commands/Drafts/RegistrarPickDraftCommand.cs`
- [ ] T029 [US2] Implement pick handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/RegistrarPickDraftCommandHandler.cs`
- [ ] T030 [US2] Add pick endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftsController.cs`
- [ ] T031 [US2] Add frontend pick API call in `FrontEnd/src/services/drafts.ts`
- [ ] T032 [US2] Implement draft board in `FrontEnd/src/components/drafts/DraftBoard.vue`
- [ ] T033 [US2] Implement pick history in `FrontEnd/src/components/drafts/DraftPickHistory.vue`

## Phase 5: User Story 3 - Sortear Capitaes e Ordem (P2)

**Goal**: Organizer can create a draft using drawn captains and/or drawn first pick order.

**Independent Test**: Create session with draw options and verify two distinct captains, criteria labels and next pick.

- [ ] T034 [P] [US3] Add draw criteria domain tests in `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftSessaoTests.cs`
- [ ] T035 [P] [US3] Add create handler draw tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/Drafts/CreateDraftCommandHandlerTests.cs`
- [ ] T036 [US3] Add captain and first pick draw logic in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/CreateDraftCommandHandler.cs`
- [ ] T037 [US3] Expose draw criteria fields in `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftDtos.cs`
- [ ] T038 [US3] Add draw controls to `FrontEnd/src/components/drafts/DraftCreateModal.vue`

## Phase 6: User Story 4 - Consultar Resultado do Draft (P3)

**Goal**: Users can list and inspect drafts with teams, available players, pick history and status.

**Independent Test**: Consult a draft after picks and verify teams, timeline, available players and closed-session behavior.

- [ ] T039 [P] [US4] Add get/list query tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/Drafts/GetDraftsQueryHandlerTests.cs`
- [ ] T040 [P] [US4] Add cancel handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Handlers/Drafts/CancelarDraftCommandHandlerTests.cs`
- [ ] T041 [US4] Create draft queries in `BackEnd/src/RinhaDasLendas.Application/Queries/Drafts/GetDraftsQuery.cs`
- [ ] T042 [US4] Implement draft query handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/GetDraftsQueryHandler.cs`
- [ ] T043 [US4] Create cancel command in `BackEnd/src/RinhaDasLendas.Application/Commands/Drafts/CancelarDraftCommand.cs`
- [ ] T044 [US4] Implement cancel handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Drafts/CancelarDraftCommandHandler.cs`
- [ ] T045 [US4] Add list/get/cancel endpoints in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftsController.cs`
- [ ] T046 [US4] Add draft status badge in `FrontEnd/src/components/drafts/DraftStatusBadge.vue`
- [ ] T047 [US4] Add cancel dialog in `FrontEnd/src/components/drafts/DraftCancelDialog.vue`
- [ ] T048 [US4] Complete list/filter/detail/cancel interactions in `FrontEnd/src/views/DraftsView.vue`

## Phase 7: Polish & Cross-Cutting

- [ ] T049 [P] Add draft service tests in `FrontEnd/src/services/drafts.spec.ts`
- [ ] T050 [P] Add draft component tests in `FrontEnd/src/components/drafts/DraftBoard.spec.ts` and `DraftCreateModal.spec.ts`
- [ ] T051 Add draft endpoint coverage to `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- [ ] T052 Update message catalog in `docs/messages/message-catalog.md`
- [ ] T053 Run backend build/tests from `BackEnd/` and fix failures
- [ ] T054 Run frontend lint/tests/build from `FrontEnd/` and fix failures
- [ ] T055 Update `specs/007-draft-jogadores/checklists/requirements.md` with implementation validation notes

## Dependencies

- Phase 1 before all other phases.
- Phase 2 blocks every user story.
- US1 and US2 form MVP and must be implemented before US3 and US4 are considered complete.
- US3 depends on US1 creation flow.
- US4 depends on foundational queries and benefits from US2 pick history.

## Parallel Examples

- T005, T006, T008, T013 and T014 can run in parallel after setup.
- T015, T016 and T017 can run in parallel before US1 implementation.
- T026 and T027 can run in parallel before US2 implementation.
- T049 and T050 can run in parallel during polish.

## Implementation Strategy

1. Build MVP first: domain, persistence, create session and pick flow.
2. Add draw criteria after manual draft is stable.
3. Finish read/cancel UI and endpoint coverage.
4. Run backend and frontend verification, then update checklist notes.

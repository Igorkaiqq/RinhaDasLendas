# Tasks: Cadastro de Jogadores e Preferências de Rotas

**Input**: Design documents from `specs/003-cadastro-jogadores-rotas/`

**Prerequisites**: `plan.md`, `spec.md`, `data-model.md`, `contracts/jogadores-api.md`, `quickstart.md`

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 [P] Create backend domain feature folder structure for jogadores in `BackEnd/src/RinhaDasLendas.Domain/Entities/` and `BackEnd/src/RinhaDasLendas.Domain/Repositories/`
- [X] T002 [P] Create frontend feature route and page stub in `FrontEnd/src/router/index.ts` and `FrontEnd/src/views/PlayersView.vue`
- [X] T003 [P] Create frontend player service skeleton in `FrontEnd/src/services/players.ts`
- [X] T004 [P] Register backend feature dependencies in `BackEnd/src/RinhaDasLendas.Application/DependencyInjection.cs` and `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T005 Create domain entities `Jogador` and `PreferenciaRota` in `BackEnd/src/RinhaDasLendas.Domain/Entities/Jogador.cs` and `BackEnd/src/RinhaDasLendas.Domain/Entities/PreferenciaRota.cs`
- [X] T006 Create `IJogadorRepository` in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IJogadorRepository.cs`
- [X] T007 Implement EF Core `RinhaDasLendasDbContext` and mapping for `jogadores` and `preferencias_rotas` in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [X] T008 Create `JogadorRepository` implementation in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/JogadorRepository.cs`
- [X] T009 Add application DTOs and validators for jogador create/update requests in `BackEnd/src/RinhaDasLendas.Application/Dtos/JogadorCreateRequestDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/JogadorUpdateRequestDto.cs`, and `BackEnd/src/RinhaDasLendas.Application/Validators/JogadorValidator.cs`
- [X] T010 Implement standard API error response handling in `BackEnd/src/RinhaDasLendas.Api/Program.cs` or `BackEnd/src/RinhaDasLendas.Api/Filters/ValidationExceptionFilter.cs`
- [X] T011 Add frontend shared components for player status and route preferences in `FrontEnd/src/components/PlayerStatusBadge.vue` and `FrontEnd/src/components/RoutePreferencesPanel.vue`
- [X] T012 Create initial EF Core migration for `jogadores` and `preferencias_rotas` in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`

---

## Phase 3: User Story 1 - Cadastrar jogador ativo (Priority: P1) 🎯 MVP

**Goal**: Allow an organizer to create a new active player with required name and optional account, elo, and profile links.

**Independent Test**: Create a player via the frontend form and verify the new player appears in the list with name, elo, status, and saved optional fields.

### Tests

- [X] T013 [P] [US1] Add unit tests for create jogador validation and command handling in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/CreateJogadorTests.cs`
- [X] T014 [P] [US1] Add integration tests for POST `/api/v1/jogadores` in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/JogadoresApiTests.cs`

### Implementation

- [X] T015 [US1] Add `CreateJogadorCommand` and handler in `BackEnd/src/RinhaDasLendas.Application/Commands/Jogadores/CreateJogadorCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/CreateJogadorCommandHandler.cs`
- [X] T016 [US1] Add `JogadorResponseDto` and mapping support in `BackEnd/src/RinhaDasLendas.Application/Dtos/JogadorResponseDto.cs`
- [X] T017 [US1] Implement POST `/api/v1/jogadores` in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
- [X] T018 [P] [US1] Implement frontend create jogador form and submission flow in `FrontEnd/src/views/PlayersView.vue` and `FrontEnd/src/services/players.ts`
- [X] T019 [P] [US1] Add frontend create jogador validations and error display in `FrontEnd/src/views/PlayersView.vue`

---

## Phase 4: User Story 2 - Atualizar preferências de rotas (Priority: P1)

**Goal**: Allow a player to update route preference order and mark one blocked route, enforcing unique priorities and at most one blocked route.

**Independent Test**: Update preferences for a registered player and verify the system persists exactly five unique priorities and accepts at most one blocked route.

### Tests

- [X] T020 [P] [US2] Add unit tests for route preference validation and update command in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/UpdatePreferenciasTests.cs`
- [X] T021 [P] [US2] Add integration tests for PUT `/api/v1/jogadores/{id}/preferencias-rotas` in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/JogadoresApiTests.cs`

### Implementation

- [X] T022 [US2] Add `UpdatePreferenciasCommand` and handler in `BackEnd/src/RinhaDasLendas.Application/Commands/Jogadores/UpdatePreferenciasCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/UpdatePreferenciasCommandHandler.cs`
- [X] T023 [P] [US2] Implement validation for exactly five unique priorities and at most one blocked route in `BackEnd/src/RinhaDasLendas.Application/Validators/JogadorValidator.cs`
- [X] T024 [US2] Implement PUT `/api/v1/jogadores/{id}/preferencias-rotas` in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
- [X] T025 [P] [US2] Implement frontend route preference editor in `FrontEnd/src/components/RoutePreferenceEditor.vue` and integrate it into `FrontEnd/src/views/PlayersView.vue`
- [X] T026 [P] [US2] Add frontend validation messaging for preference uniqueness and blocked route selection in `FrontEnd/src/components/RoutePreferenceEditor.vue`

---

## Phase 5: User Story 3 - Inativar e consultar jogadores (Priority: P2)

**Goal**: Allow organizers to mark players inactive while preserving their records, and query players with status and preferences.

**Independent Test**: Mark a player inactive and verify the player remains stored but is excluded from active-only listings while still queryable by ID.

### Tests

- [X] T027 [P] [US3] Add unit tests for inactivate command behavior and active/inactive filtering in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/InativarJogadorTests.cs`
- [X] T028 [P] [US3] Add integration tests for PATCH `/api/v1/jogadores/{id}/inativar` and GET `/api/v1/jogadores` in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/JogadoresApiTests.cs`

### Implementation

- [X] T029 [US3] Add `InativarJogadorCommand` and handler in `BackEnd/src/RinhaDasLendas.Application/Commands/Jogadores/InativarJogadorCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/InativarJogadorCommandHandler.cs`
- [X] T030 [P] [US3] Add `GetJogadoresQuery` and query handler for active/inactive filtering in `BackEnd/src/RinhaDasLendas.Application/Queries/Jogadores/GetJogadoresQuery.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/GetJogadoresQueryHandler.cs`
- [X] T031 [P] [US3] Implement GET `/api/v1/jogadores` and GET `/api/v1/jogadores/{id}` in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
- [X] T032 [P] [US3] Implement frontend player list view with active/inactive status display in `FrontEnd/src/views/PlayersView.vue`
- [X] T033 [P] [US3] Implement frontend inactive toggle action and status badge in `FrontEnd/src/components/PlayerStatusBadge.vue`

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T034 [P] Update `specs/003-cadastro-jogadores-rotas/quickstart.md` with final validation and manual test instructions
- [X] T035 [P] Add Swagger/OpenAPI documentation for jogadores endpoints in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [X] T036 [P] Add frontend error handling and user-facing validation messages in `FrontEnd/src/services/players.ts`
- [X] T037 [P] Add feature documentation or reference comments for jogador rules in `specs/003-cadastro-jogadores-rotas/` and `BackEnd/src/RinhaDasLendas.Application/`
- [X] T038 [P] Run the end-to-end validation flow and capture results in `specs/003-cadastro-jogadores-rotas/quickstart.md`
- [X] T039 [US1] Add `UpdateJogadorCommand` and handler for basic player data in `BackEnd/src/RinhaDasLendas.Application/Commands/Jogadores/UpdateJogadorCommand.cs` and `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/UpdateJogadorCommandHandler.cs`
- [X] T040 [US1] Implement PUT `/api/v1/jogadores/{id}/dados-basicos` in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
---

## Dependencies & Execution Order

- Phase 1: Setup can begin immediately and is parallelizable where noted.
- Phase 2: Foundational depends on Phase 1 completion and blocks all user story work.
- Phase 3+: User Story phases depend on Foundational completion and can proceed in parallel after that.
- Final Phase: Polish depends on all user stories being implemented.

### User Story Dependencies

- US1: Independent after foundational infrastructure is ready.
- US2: Independent after foundational infrastructure is ready; integrates with US1 data model but can be tested separately.
- US3: Independent after foundational infrastructure is ready; can be tested separately.

## Parallel Execution Examples

- Setup tasks `T001` through `T004` can be worked in parallel.
- Foundational domain, repository, validator and migration tasks `T005` through `T012` can be parallelized across backend and frontend team members.
- US1 tasks `T013` through `T019` can be implemented concurrently by separating backend tests, command handler, API route, and frontend form work.
- US2 tasks `T020` through `T026` can be executed in parallel across validation, backend update endpoint, and frontend preference editor.
- US3 tasks `T027` through `T033` can be executed in parallel across inactivation command, query handler, API endpoints, and frontend status UI.

## Implementation Strategy

- MVP first: complete Phase 1, Phase 2, then Phase 3 (US1) and validate independently.
- Incremental delivery: add US2 after US1 is validated; add US3 after US2 and validate each story independently.
- Final polish: complete API documentation, front-end error handling, and quickstart validation once all stories are implemented.

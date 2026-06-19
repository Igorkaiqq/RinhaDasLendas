# Tasks: Cadastro de Time

**Input**: Design documents from `/specs/006-cadastro-time/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included because project standards require tests for domain rules, validators and application use cases, plus frontend behavior where applicable.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when files do not conflict and dependencies are complete
- **[Story]**: User story label, used only for story phases
- Every task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare shared folders, navigation placeholders and message/i18n surfaces used by all team stories.

- [X] T001 [P] Create backend team commands folder in BackEnd/src/RinhaDasLendas.Application/Commands/Times/.gitkeep
- [X] T002 [P] Create backend team queries folder in BackEnd/src/RinhaDasLendas.Application/Queries/Times/.gitkeep
- [X] T003 [P] Create backend team handlers folder in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/.gitkeep
- [X] T004 [P] Create frontend teams component folder in FrontEnd/src/components/teams/.gitkeep
- [X] T005 [P] Create team type placeholder in FrontEnd/src/types/team.ts
- [X] T006 [P] Create team status constant placeholder in FrontEnd/src/constants/teamStatus.ts
- [X] T007 [Docs] Add feature notes for teams to docs/domain/times.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared domain, persistence, DTO, repository and API foundations that all stories depend on.

**CRITICAL**: No user story implementation should start until this phase is complete.

### Tests for Foundational Rules

- [X] T008 [P] Add Time creation invariant tests in BackEnd/tests/RinhaDasLendas.Tests/Domain/TimeTests.cs
- [X] T009 [P] Add Time member duplication invariant tests in BackEnd/tests/RinhaDasLendas.Tests/Domain/TimeTests.cs
- [X] T010 [P] Add Time captain invariant tests in BackEnd/tests/RinhaDasLendas.Tests/Domain/TimeTests.cs
- [X] T011 [P] Add Time maximum member invariant tests in BackEnd/tests/RinhaDasLendas.Tests/Domain/TimeTests.cs

### Implementation for Foundations

- [X] T012 [P] Create TimeStatus enum in BackEnd/src/RinhaDasLendas.Domain/Enums/TimeStatus.cs
- [X] T013 [P] Create TimeMembro entity in BackEnd/src/RinhaDasLendas.Domain/Entities/TimeMembro.cs
- [X] T014 Create Time entity with composition and status behavior in BackEnd/src/RinhaDasLendas.Domain/Entities/Time.cs
- [X] T015 [P] Create ITimeRepository interface in BackEnd/src/RinhaDasLendas.Domain/Repositories/ITimeRepository.cs
- [X] T016 [P] Create TimeResponseDto and member DTOs in BackEnd/src/RinhaDasLendas.Application/Dtos/TimeResponseDto.cs
- [X] T017 [P] Create CreateTimeRequestDto in BackEnd/src/RinhaDasLendas.Application/Dtos/CreateTimeRequestDto.cs
- [X] T018 [P] Create UpdateTimeRequestDto in BackEnd/src/RinhaDasLendas.Application/Dtos/UpdateTimeRequestDto.cs
- [X] T019 Add Times DbSet and Fluent API mappings in BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs
- [X] T020 Create EF migration for times and team members in BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/
- [X] T021 Implement TimeRepository in BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/TimeRepository.cs
- [X] T022 Register ITimeRepository dependency injection in BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs
- [X] T023 Add team message codes to BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs
- [X] T024 Add team messages to BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx
- [X] T025 Add team Portuguese messages to BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx
- [X] T026 Add team English messages to BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx
- [X] T027 Add team catalog entries to docs/messages/message-catalog.md

**Checkpoint**: Domain, persistence, DTOs, repository registration and team messages are ready.

---

## Phase 3: User Story 1 - Cadastrar um time (Priority: P1) MVP

**Goal**: Allow an organizer to create an active team with name, tag, optional notes, valid active players and optional captain.

**Independent Test**: Create a team with valid active players and verify it is persisted as active and returned with correct composition.

### Tests for User Story 1

- [X] T028 [P] [US1] Add CreateTimeValidator tests in BackEnd/tests/RinhaDasLendas.Tests/Validators/CreateTimeValidatorTests.cs
- [X] T029 [P] [US1] Add CreateTimeCommandHandler success test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/CreateTimeCommandHandlerTests.cs
- [X] T030 [P] [US1] Add CreateTimeCommandHandler duplicate active name/tag tests in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/CreateTimeCommandHandlerTests.cs
- [X] T031 [P] [US1] Add TimesController create endpoint test in BackEnd/tests/RinhaDasLendas.Tests/Controllers/TimesControllerTests.cs
- [X] T032 [P] [US1] Add frontend team service create test in FrontEnd/src/services/teams.spec.ts
- [X] T033 [P] [US1] Add TeamFormModal validation test in FrontEnd/src/components/teams/TeamFormModal.spec.ts

### Implementation for User Story 1

- [X] T034 [P] [US1] Create CreateTimeCommand in BackEnd/src/RinhaDasLendas.Application/Commands/Times/CreateTimeCommand.cs
- [X] T035 [P] [US1] Create CreateTimeValidator in BackEnd/src/RinhaDasLendas.Application/Validators/CreateTimeValidator.cs
- [X] T036 [US1] Implement CreateTimeCommandHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/CreateTimeCommandHandler.cs
- [X] T037 [US1] Add POST /api/v1/times endpoint in BackEnd/src/RinhaDasLendas.Api/Controllers/TimesController.cs
- [X] T038 [P] [US1] Implement team TypeScript types in FrontEnd/src/types/team.ts
- [X] T039 [P] [US1] Implement team status constants in FrontEnd/src/constants/teamStatus.ts
- [X] T040 [US1] Implement create team API service in FrontEnd/src/services/teams.ts
- [X] T041 [US1] Add team translations to FrontEnd/src/i18n/locales/pt-BR.json
- [X] T042 [US1] Add team translations to FrontEnd/src/i18n/locales/en-US.json
- [X] T043 [US1] Create TeamFormModal component in FrontEnd/src/components/teams/TeamFormModal.vue
- [X] T044 [US1] Create TeamsView create flow in FrontEnd/src/views/TeamsView.vue
- [X] T045 [US1] Register Teams route in FrontEnd/src/router/index.ts
- [X] T046 [US1] Add Teams navigation item in FrontEnd/src/components/layout/SidebarNav.vue

**Checkpoint**: User Story 1 is complete and independently testable as MVP.

---

## Phase 4: User Story 2 - Editar composicao e dados do time (Priority: P1)

**Goal**: Allow organizers to update team name, tag, notes, captain and member composition without duplicates.

**Independent Test**: Edit an existing team, change members and captain, and verify list/detail responses show updated data.

### Tests for User Story 2

- [X] T047 [P] [US2] Add UpdateTimeValidator tests in BackEnd/tests/RinhaDasLendas.Tests/Validators/UpdateTimeValidatorTests.cs
- [X] T048 [P] [US2] Add UpdateTimeCommandHandler success test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/UpdateTimeCommandHandlerTests.cs
- [X] T049 [P] [US2] Add UpdateTimeCommandHandler captain removal tests in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/UpdateTimeCommandHandlerTests.cs
- [X] T050 [P] [US2] Add TimesController update endpoint test in BackEnd/tests/RinhaDasLendas.Tests/Controllers/TimesControllerTests.cs
- [X] T051 [P] [US2] Add TeamFormModal edit mode test in FrontEnd/src/components/teams/TeamFormModal.spec.ts

### Implementation for User Story 2

- [X] T052 [P] [US2] Create UpdateTimeCommand in BackEnd/src/RinhaDasLendas.Application/Commands/Times/UpdateTimeCommand.cs
- [X] T053 [P] [US2] Create UpdateTimeValidator in BackEnd/src/RinhaDasLendas.Application/Validators/UpdateTimeValidator.cs
- [X] T054 [US2] Implement UpdateTimeCommandHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/UpdateTimeCommandHandler.cs
- [X] T055 [US2] Add PUT /api/v1/times/{id} endpoint in BackEnd/src/RinhaDasLendas.Api/Controllers/TimesController.cs
- [X] T056 [US2] Add update team API service in FrontEnd/src/services/teams.ts
- [X] T057 [US2] Extend TeamFormModal edit and captain behavior in FrontEnd/src/components/teams/TeamFormModal.vue
- [X] T058 [US2] Wire edit action in TeamsView in FrontEnd/src/views/TeamsView.vue

**Checkpoint**: User Story 2 is complete and editable teams remain valid.

---

## Phase 5: User Story 3 - Consultar e filtrar times (Priority: P2)

**Goal**: Allow users to list, inspect, search and filter teams by name, tag, status or player.

**Independent Test**: Create teams with different names, tags, members and statuses, then confirm filters return only matching teams.

### Tests for User Story 3

- [X] T059 [P] [US3] Add GetTimesQueryHandler pagination and filter tests in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/GetTimesQueryHandlerTests.cs
- [X] T060 [P] [US3] Add GetTimeByIdQueryHandler test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/GetTimeByIdQueryHandlerTests.cs
- [X] T061 [P] [US3] Add TimesController list/detail endpoint tests in BackEnd/tests/RinhaDasLendas.Tests/Controllers/TimesControllerTests.cs
- [X] T062 [P] [US3] Add frontend team list service test in FrontEnd/src/services/teams.spec.ts
- [X] T063 [P] [US3] Add TeamList state tests in FrontEnd/src/components/teams/TeamList.spec.ts

### Implementation for User Story 3

- [X] T064 [P] [US3] Create GetTimesQuery in BackEnd/src/RinhaDasLendas.Application/Queries/Times/GetTimesQuery.cs
- [X] T065 [P] [US3] Create GetTimeByIdQuery in BackEnd/src/RinhaDasLendas.Application/Queries/Times/GetTimeByIdQuery.cs
- [X] T066 [US3] Implement GetTimesQueryHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/GetTimesQueryHandler.cs
- [X] T067 [US3] Implement GetTimeByIdQueryHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/GetTimeByIdQueryHandler.cs
- [X] T068 [US3] Add GET /api/v1/times and GET /api/v1/times/{id} endpoints in BackEnd/src/RinhaDasLendas.Api/Controllers/TimesController.cs
- [X] T069 [US3] Add list/detail team API service in FrontEnd/src/services/teams.ts
- [X] T070 [P] [US3] Create TeamStatusBadge component in FrontEnd/src/components/teams/TeamStatusBadge.vue
- [X] T071 [P] [US3] Create TeamCard component in FrontEnd/src/components/teams/TeamCard.vue
- [X] T072 [P] [US3] Create TeamList component in FrontEnd/src/components/teams/TeamList.vue
- [X] T073 [US3] Implement search, filters and empty state in FrontEnd/src/views/TeamsView.vue

**Checkpoint**: User Story 3 is complete and teams are discoverable.

---

## Phase 6: User Story 4 - Inativar ou excluir time (Priority: P3)

**Goal**: Allow organizers to inactivate and reactivate teams while preserving records for history.

**Independent Test**: Inactivate a team, verify it remains consultable as inactive, then reactivate it when valid.

### Tests for User Story 4

- [X] T074 [P] [US4] Add InativarTimeCommandHandler test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/InativarTimeCommandHandlerTests.cs
- [X] T075 [P] [US4] Add ReativarTimeCommandHandler test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/ReativarTimeCommandHandlerTests.cs
- [X] T076 [P] [US4] Add TimesController inactivate/reactivate endpoint tests in BackEnd/tests/RinhaDasLendas.Tests/Controllers/TimesControllerTests.cs
- [X] T077 [P] [US4] Add TeamDeleteDialog confirmation test in FrontEnd/src/components/teams/TeamDeleteDialog.spec.ts

### Implementation for User Story 4

- [X] T078 [P] [US4] Create InativarTimeCommand in BackEnd/src/RinhaDasLendas.Application/Commands/Times/InativarTimeCommand.cs
- [X] T079 [P] [US4] Create ReativarTimeCommand in BackEnd/src/RinhaDasLendas.Application/Commands/Times/ReativarTimeCommand.cs
- [X] T080 [US4] Implement InativarTimeCommandHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/InativarTimeCommandHandler.cs
- [X] T081 [US4] Implement ReativarTimeCommandHandler in BackEnd/src/RinhaDasLendas.Application/Handlers/Times/ReativarTimeCommandHandler.cs
- [X] T082 [US4] Add PATCH inativar/reativar endpoints in BackEnd/src/RinhaDasLendas.Api/Controllers/TimesController.cs
- [X] T083 [US4] Add inactivate/reactivate team API service in FrontEnd/src/services/teams.ts
- [X] T084 [US4] Create TeamDeleteDialog component in FrontEnd/src/components/teams/TeamDeleteDialog.vue
- [X] T085 [US4] Wire inactivate and reactivate actions in FrontEnd/src/views/TeamsView.vue

**Checkpoint**: User Story 4 is complete and inactive teams are preserved but not treated as active.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature and update documentation after all selected stories are complete.

- [X] T086 [P] Update feature checklist results in specs/006-cadastro-time/checklists/requirements.md
- [X] T087 [P] Update quickstart validation notes in specs/006-cadastro-time/quickstart.md
- [X] T088 [P] Run backend build validation for BackEnd/RinhaDasLendas.sln
- [X] T089 [P] Run backend test suite for BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
- [X] T090 [P] Run frontend lint validation in FrontEnd/package.json
- [X] T091 [P] Run frontend build validation in FrontEnd/package.json
- [X] T092 Verify team message codes used in BackEnd/src and FrontEnd/src exist in docs/messages/message-catalog.md
- [X] T093 Verify Teams UI follows docs/design/DESIGN_SYSTEM.md, docs/design/DESIGN_TOKENS.md and docs/design/UI_GUIDELINES.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 (Phase 4)**: Depends on US1 domain/API foundations and can reuse create form components.
- **US3 (Phase 5)**: Depends on Foundational and benefits from US1/US2 persisted data.
- **US4 (Phase 6)**: Depends on US3 list/status visibility and shared status behavior.
- **Polish**: Depends on selected stories being complete.

### User Story Dependencies

- **US1 (P1)**: Required MVP and should be implemented first.
- **US2 (P1)**: Requires the same domain and persistence model as US1; implement after US1.
- **US3 (P2)**: Can start after Foundational, but full UI value appears after US1.
- **US4 (P3)**: Requires status and list/detail behavior to validate inactive teams.

### Within Each User Story

- Tests listed for a story should be written before implementation tasks.
- Domain and validator changes before handlers.
- Handlers before controller endpoints.
- Services/types before view integration on frontend.

---

## Parallel Opportunities

- **Setup**: T001 through T006 can run in parallel.
- **Foundational tests**: T008 through T011 can run in parallel.
- **Foundational implementation**: T012, T013, T015, T016, T017, T018 can run in parallel before T014 and T019.
- **US1 tests**: T028 through T033 can run in parallel.
- **US1 frontend/backend**: T034, T035, T038, T039 can run in parallel before handlers and components.
- **US2 tests**: T047 through T051 can run in parallel.
- **US3 components**: T070 through T072 can run in parallel after service/types exist.
- **US4 commands**: T078 and T079 can run in parallel before handlers.
- **Polish validations**: T088 through T091 can run in parallel.

---

## Parallel Example: User Story 1

```bash
Task: "T028 [US1] Add CreateTimeValidator tests in BackEnd/tests/RinhaDasLendas.Tests/Validators/CreateTimeValidatorTests.cs"
Task: "T029 [US1] Add CreateTimeCommandHandler success test in BackEnd/tests/RinhaDasLendas.Tests/Handlers/Times/CreateTimeCommandHandlerTests.cs"
Task: "T032 [US1] Add frontend team service create test in FrontEnd/src/services/teams.spec.ts"
Task: "T033 [US1] Add TeamFormModal validation test in FrontEnd/src/components/teams/TeamFormModal.spec.ts"
```

## Parallel Example: User Story 3

```bash
Task: "T070 [US3] Create TeamStatusBadge component in FrontEnd/src/components/teams/TeamStatusBadge.vue"
Task: "T071 [US3] Create TeamCard component in FrontEnd/src/components/teams/TeamCard.vue"
Task: "T072 [US3] Create TeamList component in FrontEnd/src/components/teams/TeamList.vue"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: US1 Cadastrar um time.
4. Stop and validate team creation independently through backend tests and `/times` UI.

### Incremental Delivery

1. Deliver US1 to allow creating reusable active teams.
2. Deliver US2 to keep team data and composition updated.
3. Deliver US3 to make teams searchable and operationally useful.
4. Deliver US4 to preserve history through inactivation/reactivation.
5. Run final validation commands from quickstart.md.

### Validation Commands

```bash
dotnet build BackEnd/RinhaDasLendas.sln
dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
npm run lint --prefix FrontEnd
npm run build --prefix FrontEnd
```

---

## Notes

- Tasks are not yet approved for implementation.
- Respect DDD: domain rules stay in Domain, not in controllers or Vue components.
- Respect API standards: controllers return DTOs and proper HTTP status codes.
- Respect frontend design docs before implementing Teams UI.
- Do not physically delete teams in this feature.

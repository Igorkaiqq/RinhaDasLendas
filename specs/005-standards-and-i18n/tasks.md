# Tasks: Standardization and Internationalization Framework

**Input**: Design documents from `/specs/005-standards-and-i18n/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included where the feature explicitly requires build, lint, backend tests, frontend tests, and independent validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. Each task states whether it changes Docs, Backend, or Frontend.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (US1, US2, US3...)
- Include exact file paths in every task description

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare folders and confirm baseline project context before story work starts.

- [X] T001 [Docs] Create standards documentation folder with placeholder index in docs/standards/README.md
- [X] T002 [Docs] Create messages documentation folder with placeholder index in docs/messages/README.md
- [X] T003 [P] [Backend] Create backend message folder placeholder in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/.gitkeep
- [X] T004 [P] [Frontend] Create frontend constants folder placeholder in FrontEnd/src/constants/.gitkeep
- [X] T005 [P] [Frontend] Create frontend i18n locale folder placeholder in FrontEnd/src/i18n/locales/.gitkeep

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared naming, catalog, and compatibility rules that all user stories must follow.

**CRITICAL**: No user story implementation should start until this phase is complete.

- [X] T006 [Docs] Record canonical feature scope and phase order in specs/005-standards-and-i18n/REVISION_SUMMARY.md
- [X] T007 [Docs] Align message code ranges with contracts in specs/005-standards-and-i18n/contracts/message-code-structure.md
- [X] T008 [Docs] Align branch pattern examples with current feature branch in specs/005-standards-and-i18n/contracts/branch-naming-contract.md
- [X] T009 [P] [Docs] Add architecture compliance notes for standards and i18n in docs/standards/README.md
- [X] T010 [P] [Docs] Add message catalog maintenance rules in docs/messages/README.md

**Checkpoint**: Standards and message rules are defined enough for story work to begin.

---

## Phase 3: User Story 1 - Establish Documentation Standards (Priority: P1) MVP

**Goal**: Provide clear, centralized documentation for branch naming, commits, Spec Kit workflow, PRs, constants, and i18n expectations.

**Independent Test**: A new developer can read docs/standards/ and understand branch naming, commit format, Spec Kit phases, and PR expectations without reading implementation code.

### Implementation for User Story 1

- [X] T011 [P] [US1] [Docs] Write standards overview and navigation in docs/standards/README.md
- [X] T012 [P] [US1] [Docs] Document feature branch pattern feature/NNN-slug with examples in docs/standards/BRANCH_NAMING.md
- [X] T013 [P] [US1] [Docs] Document Portuguese semantic commit message rules in docs/standards/COMMIT_MESSAGES.md
- [X] T014 [P] [US1] [Docs] Document Constitution to Specify to Plan to Tasks to Implement workflow in docs/standards/SPECS_AND_PLANNING.md
- [X] T015 [P] [US1] [Docs] Document pull request title, description, and review expectations in docs/standards/PR_STANDARDS.md
- [X] T016 [P] [US1] [Docs] Document constants, enums, and type usage rules in docs/standards/CONSTANTS_AND_ENUMS.md
- [X] T017 [P] [US1] [Docs] Document i18n key, locale file, and fallback rules in docs/standards/I18N_GUIDELINES.md
- [X] T018 [US1] [Docs] Cross-link AGENTS.md and architecture docs from docs/standards/README.md
- [X] T019 [US1] [Docs] Validate US1 onboarding scenario against specs/005-standards-and-i18n/quickstart.md

**Checkpoint**: User Story 1 is complete and independently reviewable through docs/standards/.

---

## Phase 4: User Story 2 - Implement Message Catalog System (Priority: P1)

**Goal**: Provide a centralized message catalog with stable codes, Portuguese text, English text, categories, context, and maintenance rules.

**Independent Test**: A developer can find MSIS001 and MV001 in docs/messages/, verify category and localized text, and understand how to register a new code.

### Implementation for User Story 2

- [X] T020 [P] [US2] [Docs] Write message catalog purpose and usage guide in docs/messages/README.md
- [X] T021 [P] [US2] [Docs] Create Info message entries MI001 through MI009 in docs/messages/message-catalog.md
- [X] T022 [P] [US2] [Docs] Create Success message entries MSIS001 through MSIS009 in docs/messages/message-catalog.md
- [X] T023 [P] [US2] [Docs] Create Error message entries ME001 through ME020 in docs/messages/message-catalog.md
- [X] T024 [P] [US2] [Docs] Create Validation message entries MV001 through MV019 in docs/messages/message-catalog.md
- [X] T025 [P] [US2] [Docs] Create Confirmation message entries MC001 through MC009 in docs/messages/message-catalog.md
- [X] T026 [P] [US2] [Docs] Create Alert message entries MA001 through MA009 in docs/messages/message-catalog.md
- [X] T027 [US2] [Docs] Create quick reference grouped by category in docs/messages/message-codes.md
- [X] T028 [US2] [Docs] Document code immutability and deprecation policy in docs/messages/message-codes.md
- [X] T029 [US2] [Docs] Validate catalog has at least 50 unique codes in docs/messages/message-catalog.md

**Checkpoint**: User Story 2 is complete and message codes are discoverable before backend/frontend usage.

---

## Phase 5: User Story 3 - Create Backend Message Response Infrastructure (Priority: P1)

**Goal**: Add reusable backend message code, category, provider, resource, and response wrapper infrastructure without breaking existing player endpoints.

**Independent Test**: Backend tests verify localized lookup for pt-BR and en-US and message response DTO shape supports messageCode, message, messageCategory, data, and details.

### Tests for User Story 3

- [X] T030 [P] [US3] [Backend] Add ResourceMessageProvider pt-BR lookup test in BackEnd/tests/RinhaDasLendas.Tests/Messages/ResourceMessageProviderTests.cs
- [X] T031 [P] [US3] [Backend] Add ResourceMessageProvider en-US lookup test in BackEnd/tests/RinhaDasLendas.Tests/Messages/ResourceMessageProviderTests.cs
- [X] T032 [P] [US3] [Backend] Add fallback lookup test for unknown message code in BackEnd/tests/RinhaDasLendas.Tests/Messages/ResourceMessageProviderTests.cs
- [X] T033 [P] [US3] [Backend] Add MessageResponseDto shape test in BackEnd/tests/RinhaDasLendas.Tests/Messages/MessageResponseDtoTests.cs

### Implementation for User Story 3

- [X] T034 [P] [US3] [Backend] Create message code constants in BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs
- [X] T035 [P] [US3] [Backend] Create message category enum in BackEnd/src/RinhaDasLendas.Domain/Enums/MessageCategory.cs
- [X] T036 [P] [US3] [Backend] Create message response DTO in BackEnd/src/RinhaDasLendas.Application/Dtos/MessageResponseDto.cs
- [X] T037 [P] [US3] [Backend] Create message provider interface in BackEnd/src/RinhaDasLendas.Application/Interfaces/IMessageProvider.cs
- [X] T038 [P] [US3] [Backend] Add default Portuguese resources in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx
- [X] T039 [P] [US3] [Backend] Add explicit Portuguese resources in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx
- [X] T040 [P] [US3] [Backend] Add English resources in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx
- [X] T041 [US3] [Backend] Implement resource-backed provider in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/ResourceMessageProvider.cs
- [X] T042 [US3] [Backend] Register IMessageProvider dependency injection in BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs
- [X] T043 [US3] [Backend] Build backend to validate infrastructure in BackEnd/RinhaDasLendas.sln

**Checkpoint**: User Story 3 is complete and backend message infrastructure can be used by future endpoint changes.

---

## Phase 6: User Story 4 - Implement Frontend Message Service Layer (Priority: P2)

**Goal**: Provide a frontend message service and initial message code typing so components can retrieve localized messages by code.

**Independent Test**: Vitest verifies getMessage('MI001', 'pt-BR') returns Portuguese text, getMessage('MI001', 'en-US') returns English text, and unknown codes return a fallback.

### Tests for User Story 4

- [X] T044 [P] [US4] [Frontend] Add pt-BR message lookup test in FrontEnd/src/services/messageService.spec.ts
- [X] T045 [P] [US4] [Frontend] Add en-US message lookup test in FrontEnd/src/services/messageService.spec.ts
- [X] T046 [P] [US4] [Frontend] Add unknown code fallback test in FrontEnd/src/services/messageService.spec.ts

### Implementation for User Story 4

- [X] T047 [P] [US4] [Frontend] Create locale code type and defaults in FrontEnd/src/types/i18n.ts
- [X] T048 [P] [US4] [Frontend] Create message code enum in FrontEnd/src/constants/messageCode.ts
- [X] T049 [P] [US4] [Frontend] Create message category enum in FrontEnd/src/constants/messageCategory.ts
- [X] T050 [US4] [Frontend] Implement getMessage service in FrontEnd/src/services/messageService.ts
- [X] T051 [US4] [Frontend] Replace hardcoded player service feedback messages with message service calls in FrontEnd/src/services/players.ts
- [X] T052 [US4] [Frontend] Run frontend unit tests for message service in FrontEnd/src/services/messageService.spec.ts

**Checkpoint**: User Story 4 is complete and frontend code can retrieve messages through one service.

---

## Phase 7: User Story 5 - Implement Internationalization Base Structure (Priority: P2)

**Goal**: Add pt-BR and en-US frontend i18n infrastructure and move sidebar, topbar, and player page user-facing text into locale files.

**Independent Test**: Sidebar, topbar, and players page render from translation keys for pt-BR and can switch to en-US without page reload.

### Tests for User Story 5

- [X] T053 [P] [US5] [Frontend] Add i18n locale loading test in FrontEnd/src/i18n/i18n.spec.ts
- [X] T054 [P] [US5] [Frontend] Add navigation translation key test in FrontEnd/src/components/layout/SidebarNav.spec.ts

### Implementation for User Story 5

- [X] T055 [P] [US5] [Frontend] Add Vue I18n setup in FrontEnd/src/i18n/index.ts
- [X] T056 [P] [US5] [Frontend] Add Portuguese translations in FrontEnd/src/i18n/locales/pt-BR.json
- [X] T057 [P] [US5] [Frontend] Add English translations in FrontEnd/src/i18n/locales/en-US.json
- [X] T058 [US5] [Frontend] Register i18n plugin in FrontEnd/src/main.ts
- [X] T059 [US5] [Frontend] Replace sidebar labels with translation keys in FrontEnd/src/components/layout/SidebarNav.vue
- [X] T060 [US5] [Frontend] Replace topbar labels with translation keys in FrontEnd/src/components/layout/Topbar.vue
- [X] T061 [US5] [Frontend] Replace player page labels with translation keys in FrontEnd/src/views/PlayersView.vue
- [X] T062 [US5] [Frontend] Replace player modal labels with translation keys in FrontEnd/src/components/players/PlayerFormModal.vue
- [X] T063 [US5] [Frontend] Add locale switching control in FrontEnd/src/components/layout/ProfileMenu.vue
- [X] T064 [US5] [Frontend] Run frontend build validation in FrontEnd/package.json

**Checkpoint**: User Story 5 is complete and frontend text is ready for pt-BR/en-US usage.

---

## Phase 8: User Story 6 - Standardize Constants, Enums, and Types (Priority: P2)

**Goal**: Centralize closed-set values in backend and frontend and replace magic strings in navigation, player status, League roles, ranks, and message code handling.

**Independent Test**: Sidebar uses AppRoutes constants, player status uses centralized enums/types, and frontend/backend builds pass without magic-string regressions in touched files.

### Tests for User Story 6

- [X] T065 [P] [US6] [Backend] Add project structure test for backend constants folder in BackEnd/tests/RinhaDasLendas.Tests/ProjectStructureTests.cs
- [X] T066 [P] [US6] [Frontend] Add route constants test in FrontEnd/src/constants/appRoutes.spec.ts

### Implementation for User Story 6

- [X] T067 [P] [US6] [Frontend] Create app route constants in FrontEnd/src/constants/appRoutes.ts
- [X] T068 [P] [US6] [Frontend] Create player status constants in FrontEnd/src/constants/playerStatus.ts
- [X] T069 [P] [US6] [Frontend] Create League role constants in FrontEnd/src/constants/leagueRoles.ts
- [X] T070 [P] [US6] [Frontend] Create League rank type in FrontEnd/src/types/leagueRank.ts
- [X] T071 [US6] [Frontend] Use AppRoutes constants in FrontEnd/src/router/index.ts
- [X] T072 [US6] [Frontend] Use AppRoutes constants in FrontEnd/src/components/layout/SidebarNav.vue
- [X] T073 [US6] [Frontend] Use PlayerStatus constants in FrontEnd/src/components/PlayerStatusBadge.vue
- [X] T074 [US6] [Frontend] Use LeagueRole constants in FrontEnd/src/components/RoutePreferenceEditor.vue
- [X] T075 [P] [US6] [Backend] Add message validation constants in BackEnd/src/RinhaDasLendas.Domain/Constants/ValidationConstants.cs
- [X] T076 [US6] [Backend] Run backend build validation in BackEnd/RinhaDasLendas.sln

**Checkpoint**: User Story 6 is complete and common fixed values are centralized.

---

## Phase 9: User Story 7 - Document Feature Development Process (Priority: P3)

**Goal**: Document how future features must update messages, translations, constants, standards docs, and validation runs.

**Independent Test**: A feature lead can open docs/standards/FEATURE_CHECKLIST.md and know exactly what to update before implementation and before review.

### Implementation for User Story 7

- [X] T077 [P] [US7] [Docs] Create feature development checklist in docs/standards/FEATURE_CHECKLIST.md
- [X] T078 [P] [US7] [Docs] Add message catalog update steps to docs/standards/FEATURE_CHECKLIST.md
- [X] T079 [P] [US7] [Docs] Add translation file update steps to docs/standards/FEATURE_CHECKLIST.md
- [X] T080 [P] [US7] [Docs] Add constants and enums review steps to docs/standards/FEATURE_CHECKLIST.md
- [X] T081 [P] [US7] [Docs] Add validation command checklist to docs/standards/FEATURE_CHECKLIST.md
- [X] T082 [US7] [Docs] Link feature checklist from docs/standards/README.md
- [X] T083 [US7] [Docs] Validate US7 governance scenario against specs/005-standards-and-i18n/quickstart.md

**Checkpoint**: User Story 7 is complete and future feature governance is documented.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature and update generated artifacts after all selected stories are complete.

- [X] T084 [P] [Docs] Update quickstart validation results in specs/005-standards-and-i18n/quickstart.md
- [X] T085 [P] [Docs] Update revision notes with implementation decisions in specs/005-standards-and-i18n/REVISION_NOTES.md
- [X] T086 [P] [Backend] Run complete backend test suite in BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
- [X] T087 [P] [Frontend] Run frontend lint validation in FrontEnd/package.json
- [X] T088 [P] [Frontend] Run frontend build validation in FrontEnd/package.json
- [X] T089 [Docs] Verify every message code used in BackEnd/src and FrontEnd/src exists in docs/messages/message-catalog.md
- [X] T090 [Docs] Confirm no unchecked standards links remain in docs/standards/README.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories
- **US1 (Phase 3)**: Depends on Foundational
- **US2 (Phase 4)**: Depends on Foundational and can run after or beside US1, but final catalog should align with US1 docs
- **US3 (Phase 5)**: Depends on US2 message catalog
- **US4 (Phase 6)**: Depends on US2 message catalog
- **US5 (Phase 7)**: Depends on US4 frontend message service
- **US6 (Phase 8)**: Depends on Foundational and can run in parallel with US4 after shared constants decisions are documented
- **US7 (Phase 9)**: Depends on US1 through US6 patterns being defined
- **Polish**: Depends on all selected user stories

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; no dependency on code stories
- **US2 (P1)**: Can start after Foundational; provides catalog for backend/frontend stories
- **US3 (P1)**: Requires US2
- **US4 (P2)**: Requires US2
- **US5 (P2)**: Requires US4
- **US6 (P2)**: Can start after Foundational; coordinate with US4/US5 for message and route constants
- **US7 (P3)**: Requires patterns from US1-US6

### Within Each User Story

- Tests listed for a story should be written before implementation tasks when applicable.
- Documentation tasks can run in parallel when they modify different files.
- Backend resource files can run in parallel, then provider implementation follows.
- Frontend constants can run in parallel, then component adoption follows.

---

## Parallel Opportunities

- **Setup**: T003, T004, and T005 can run in parallel.
- **Foundational**: T009 and T010 can run in parallel after T006-T008.
- **US1**: T011 through T017 can run in parallel because each changes a separate standards document.
- **US2**: T021 through T026 can run in parallel if each contributor owns one message category section.
- **US3**: T030 through T040 can run in parallel before T041 and T042.
- **US4**: T044 through T049 can run in parallel before T050.
- **US5**: T053 through T057 can run in parallel before plugin registration and component updates.
- **US6**: T067 through T070 and T075 can run in parallel before usage refactors.
- **US7**: T078 through T081 can run in parallel after T077 creates the checklist file.

---

## Parallel Example: User Story 1

```bash
Task: "T012 [US1] [Docs] Document feature branch pattern feature/NNN-slug with examples in docs/standards/BRANCH_NAMING.md"
Task: "T013 [US1] [Docs] Document Portuguese semantic commit message rules in docs/standards/COMMIT_MESSAGES.md"
Task: "T014 [US1] [Docs] Document Constitution to Specify to Plan to Tasks to Implement workflow in docs/standards/SPECS_AND_PLANNING.md"
Task: "T015 [US1] [Docs] Document pull request title, description, and review expectations in docs/standards/PR_STANDARDS.md"
```

## Parallel Example: User Story 3

```bash
Task: "T034 [US3] [Backend] Create message code constants in BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs"
Task: "T035 [US3] [Backend] Create message category enum in BackEnd/src/RinhaDasLendas.Domain/Enums/MessageCategory.cs"
Task: "T038 [US3] [Backend] Add default Portuguese resources in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx"
Task: "T040 [US3] [Backend] Add English resources in BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx"
```

## Parallel Example: User Story 5

```bash
Task: "T055 [US5] [Frontend] Add Vue I18n setup in FrontEnd/src/i18n/index.ts"
Task: "T056 [US5] [Frontend] Add Portuguese translations in FrontEnd/src/i18n/locales/pt-BR.json"
Task: "T057 [US5] [Frontend] Add English translations in FrontEnd/src/i18n/locales/en-US.json"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: US1 documentation standards.
4. Complete Phase 4: US2 message catalog.
5. Complete Phase 5: US3 backend message infrastructure.
6. Stop and validate P1 scope before starting frontend i18n work.

### Incremental Delivery

1. Deliver US1 so standards are visible to the team.
2. Deliver US2 so message codes are reviewable before code adoption.
3. Deliver US3 so backend has a reusable infrastructure.
4. Deliver US4 and US6 to centralize frontend messaging and constants.
5. Deliver US5 to activate full i18n in visible UI areas.
6. Deliver US7 to lock the process for future features.

### Validation Commands

```bash
dotnet build BackEnd/RinhaDasLendas.sln
dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
npm run lint --prefix FrontEnd
npm run build --prefix FrontEnd
```

---

## Notes

- [P] tasks modify separate files or independent sections and can be assigned in parallel.
- [Docs], [Backend], and [Frontend] labels clarify the affected area.
- Keep each task small; do not combine documentation, backend, and frontend changes in one implementation task.
- Existing API contracts must remain backward compatible throughout US3 and later adoption work.

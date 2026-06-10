# Tasks: Layout Base e Gestao de Jogadores

**Input**: Design documents from `/specs/004-layout-jogadores/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No automated test suite was requested for this frontend feature. Validation tasks use `npm run build`, `npm run lint`, quickstart scenarios, and backend regression tests only if API consumption changes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare design inputs, inspect current frontend structure, and make the implementation safe to start.

- [X] T001 Obtain the node-specific Figma URL for the main layout and Jogadores screen and record fileKey/nodeId plus screenshot notes in specs/004-layout-jogadores/research.md
- [X] T002 Map Figma MCP colors, typography, spacing, component hierarchy, and screenshots against docs/design/DESIGN_TOKENS.md in specs/004-layout-jogadores/research.md
- [X] T003 [P] Review current route and shell behavior in FrontEnd/src/App.vue and FrontEnd/src/router/index.ts against specs/004-layout-jogadores/contracts/layout-ui.md
- [X] T004 [P] Review current Jogadores service and screen behavior in FrontEnd/src/services/players.ts and FrontEnd/src/views/PlayersView.vue against specs/004-layout-jogadores/contracts/jogadores-ui-service.md
- [X] T005 [P] Create component target directories FrontEnd/src/components/layout/ and FrontEnd/src/components/players/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared types, navigation metadata, fake-service contract, and design tokens before story work begins.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T006 Define layout navigation and user summary types in FrontEnd/src/types/layout.ts
- [X] T007 Define shared player UI form and feedback types in FrontEnd/src/types/players.ts
- [X] T008 [P] Add deterministic fake player data and fake service operations in FrontEnd/src/services/fakePlayers.ts
- [X] T009 Update FrontEnd/src/services/players.ts to expose API-first operations with fake fallback through the same service contract
- [X] T010 Normalize global CSS token usage and remove duplicate toast/sidebar fragments in FrontEnd/src/styles/main.css
- [X] T011 Update FrontEnd/src/router/index.ts with stable routes for dashboard, jogadores, times, draft, partidas, estatisticas, and configuracoes

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Navegar pelo layout principal (Priority: P1) MVP

**Goal**: Users can access protected pages through a consistent sidebar, topbar, and central content area without breaking existing routes.

**Independent Test**: Open Dashboard/Home and Jogadores, verify sidebar/topbar/content region appear on both pages, active navigation is visible, and placeholder sections do not break navigation.

### Implementation for User Story 1

- [X] T012 [P] [US1] Implement reusable shell wrapper in FrontEnd/src/components/layout/AppShell.vue
- [X] T013 [P] [US1] Implement sidebar navigation component with active-route state in FrontEnd/src/components/layout/SidebarNav.vue
- [X] T014 [P] [US1] Implement topbar component with app name and user summary in FrontEnd/src/components/layout/Topbar.vue
- [X] T015 [P] [US1] Implement profile menu placeholder actions in FrontEnd/src/components/layout/ProfileMenu.vue
- [X] T016 [US1] Replace inline shell markup in FrontEnd/src/App.vue with FrontEnd/src/components/layout/AppShell.vue
- [X] T017 [US1] Add placeholder protected page view for future sections in FrontEnd/src/views/PlaceholderView.vue
- [X] T018 [US1] Wire placeholder routes and route metadata in FrontEnd/src/router/index.ts
- [X] T019 [US1] Add desktop and tablet shell styles for sidebar, topbar, active states, and content region in FrontEnd/src/styles/main.css

**Checkpoint**: User Story 1 is independently functional and can be demoed as the MVP shell.

---

## Phase 4: User Story 2 - Consultar jogadores no novo padrao visual (Priority: P1)

**Goal**: Users can view players in the redesigned Jogadores screen with name, Discord, elo, preferred routes, OP.GG link, and status.

**Independent Test**: Open Jogadores with API or fake data and verify each player shows required fields, loading skeleton, empty state, OP.GG link behavior, and readable desktop/tablet layout.

### Implementation for User Story 2

- [X] T020 [P] [US2] Extract player list rendering into FrontEnd/src/components/players/PlayerList.vue
- [X] T021 [P] [US2] Extract player card display into FrontEnd/src/components/players/PlayerCard.vue
- [X] T022 [P] [US2] Move player list loading, empty, and error presentation into FrontEnd/src/components/players/PlayerListStates.vue
- [X] T023 [US2] Refactor FrontEnd/src/views/PlayersView.vue to consume PlayerList.vue and the shared player service contract
- [X] T024 [US2] Add OP.GG external link rendering and missing-link fallback in FrontEnd/src/components/players/PlayerCard.vue
- [X] T025 [US2] Ensure elo, Discord, status, and ordered route preferences render consistently using FrontEnd/src/components/PlayerStatusBadge.vue and FrontEnd/src/components/RoutePreferencesPanel.vue
- [X] T026 [US2] Add desktop and tablet list/card styles matching Figma-derived tokens in FrontEnd/src/styles/main.css
- [X] T027 [US2] Validate Jogadores list scenarios from specs/004-layout-jogadores/quickstart.md and record any design gaps in specs/004-layout-jogadores/research.md

**Checkpoint**: User Story 2 is independently functional and validates the redesigned player list.

---

## Phase 5: User Story 3 - Gerenciar jogadores temporariamente (Priority: P2)

**Goal**: Users can create, edit, and delete/inactivate players during the session through the redesigned Jogadores experience.

**Independent Test**: Create a player, edit visible fields and route preferences, confirm deletion/inactivation, and verify validation messages appear for invalid input.

### Implementation for User Story 3

- [X] T028 [P] [US3] Implement create/edit form drawer component in FrontEnd/src/components/players/PlayerFormDrawer.vue
- [X] T029 [P] [US3] Implement destructive action confirmation dialog in FrontEnd/src/components/players/PlayerDeleteDialog.vue
- [X] T030 [US3] Refactor create-player form state from FrontEnd/src/views/PlayersView.vue into FrontEnd/src/components/players/PlayerFormDrawer.vue
- [X] T031 [US3] Add edit-player flow using updatePlayerBasics and updateRoutePreferences in FrontEnd/src/views/PlayersView.vue
- [X] T032 [US3] Add delete/inactivate flow with confirmation using the service contract in FrontEnd/src/views/PlayersView.vue
- [X] T033 [US3] Add required-field, URL, elo/division, and route-preference validation messaging in FrontEnd/src/components/players/PlayerFormDrawer.vue
- [X] T034 [US3] Ensure fake service create, update, and delete/inactivate behavior is deterministic in FrontEnd/src/services/fakePlayers.ts
- [X] T035 [US3] Add drawer, dialog, form, and feedback styles for desktop/tablet in FrontEnd/src/styles/main.css

**Checkpoint**: User Story 3 is independently functional for temporary player management.

---

## Phase 6: User Story 4 - Reutilizar a estrutura em futuras telas (Priority: P3)

**Goal**: Future protected pages can reuse the same shell, navigation, topbar, and content structure without copying layout code.

**Independent Test**: Connect a protected placeholder page to the shared shell and confirm it receives the same sidebar, topbar, active-state behavior, and content spacing as existing pages.

### Implementation for User Story 4

- [X] T036 [P] [US4] Document AppShell usage expectations in specs/004-layout-jogadores/contracts/layout-ui.md
- [X] T037 [US4] Ensure FrontEnd/src/views/HomeView.vue renders as shell content without page-level layout duplication
- [X] T038 [US4] Ensure FrontEnd/src/views/PlaceholderView.vue accepts route metadata for Times, Draft, Partidas, Estatisticas, and Configuracoes
- [X] T039 [US4] Move reusable page heading and action-area styles into shared selectors in FrontEnd/src/styles/main.css
- [X] T040 [US4] Validate placeholder protected pages against specs/004-layout-jogadores/quickstart.md

**Checkpoint**: User Story 4 is independently functional for future screen reuse.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate build quality, design fidelity, responsiveness, and documentation.

- [X] T041 [P] Run frontend lint with npm run lint in FrontEnd/
- [X] T042 [P] Run frontend build with npm run build in FrontEnd/
- [X] T043 Run backend regression tests with dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj if FrontEnd/src/services/players.ts API calls changed
- [X] T044 Validate desktop and tablet quickstart scenarios in specs/004-layout-jogadores/quickstart.md
- [X] T045 Compare implementation against Figma screenshot and update specs/004-layout-jogadores/research.md with any accepted visual deviations
- [X] T046 Update specs/004-layout-jogadores/quickstart.md if final validation commands or routes differ from the plan
- [X] T047 Review FrontEnd/src/styles/main.css for token-only colors, no overlapping text, and no one-off visual values outside documented design tokens

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies, can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Stories (Phase 3+)**: Depend on Foundational completion.
- **Polish (Phase 7)**: Depends on all implemented user stories for the chosen scope.

### User Story Dependencies

- **US1 Navegar pelo layout principal (P1)**: Starts after Foundation; no dependency on other stories; recommended MVP.
- **US2 Consultar jogadores no novo padrao visual (P1)**: Starts after Foundation; can run in parallel with US1 after shared service/types exist, but final visual integration benefits from US1 shell.
- **US3 Gerenciar jogadores temporariamente (P2)**: Starts after Foundation; depends on player service contract and benefits from US2 list components.
- **US4 Reutilizar a estrutura em futuras telas (P3)**: Starts after US1 shell is available; can be validated independently with placeholder pages.

### Blocking Design Dependency

- T001 and T002 are blocking because the feature requires Figma fidelity. If the Figma node-specific URL is still unavailable, stop before visual implementation and obtain explicit approval to proceed using only docs/design/.

### Within Each User Story

- Shared types/services before view integration.
- Components before page refactors that consume them.
- CSS token/style work after component structure exists.
- Quickstart validation after the story's visible behavior is complete.

---

## Parallel Opportunities

- T003, T004, and T005 can run in parallel after T001/T002 are understood.
- T008 can run in parallel with T006 and T007.
- T012, T013, T014, and T015 can run in parallel because they create separate layout component files.
- T020, T021, and T022 can run in parallel because they create separate player display component files.
- T028 and T029 can run in parallel because they create separate player management component files.
- T041 and T042 can run in parallel in separate terminal sessions after implementation is complete.

---

## Parallel Example: User Story 1

```text
Task: "T012 Implement reusable shell wrapper in FrontEnd/src/components/layout/AppShell.vue"
Task: "T013 Implement sidebar navigation component with active-route state in FrontEnd/src/components/layout/SidebarNav.vue"
Task: "T014 Implement topbar component with app name and user summary in FrontEnd/src/components/layout/Topbar.vue"
Task: "T015 Implement profile menu placeholder actions in FrontEnd/src/components/layout/ProfileMenu.vue"
```

## Parallel Example: User Story 2

```text
Task: "T020 Extract player list rendering into FrontEnd/src/components/players/PlayerList.vue"
Task: "T021 Extract player card display into FrontEnd/src/components/players/PlayerCard.vue"
Task: "T022 Move player list loading, empty, and error presentation into FrontEnd/src/components/players/PlayerListStates.vue"
```

## Parallel Example: User Story 3

```text
Task: "T028 Implement create/edit form drawer component in FrontEnd/src/components/players/PlayerFormDrawer.vue"
Task: "T029 Implement destructive action confirmation dialog in FrontEnd/src/components/players/PlayerDeleteDialog.vue"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2, including Figma extraction or approved fallback.
2. Complete Phase 3 (US1).
3. Stop and validate shared shell navigation independently.
4. Demo Dashboard/Home and Jogadores inside the same shell.

### Incremental Delivery

1. Add US1 shared shell and placeholder navigation.
2. Add US2 redesigned player list.
3. Add US3 create/edit/delete temporary management.
4. Add US4 reuse polish for future screens.
5. Run Phase 7 validation.

### Parallel Team Strategy

After Phase 2:

- Developer A: US1 layout shell components.
- Developer B: US2 player display components.
- Developer C: US3 player form and confirmation components.
- Developer D: US4 placeholder/reuse validation after US1 shell is available.

## Notes

- Every task uses a concrete repository path.
- Tasks marked [P] touch different files or can be validated independently.
- Do not implement visual values that conflict with docs/design/ or Figma MCP output.
- Commit after each phase or coherent task group using Brazilian Portuguese commit messages.

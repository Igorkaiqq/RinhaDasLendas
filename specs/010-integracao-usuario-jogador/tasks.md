# Tasks: Integração Usuário-Jogador

**Input**: Design documents from `/specs/010-integracao-usuario-jogador/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/perfil-jogador-api.md, quickstart.md

**Tests**: Required by project standards for domain rules, validators, application use cases and frontend behavior touched by this feature.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks
- **[Story]**: Maps task to a user story from spec.md
- Every task includes exact file paths

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm current code shape and prepare shared contracts/types before story work.

- [ ] T001 Review current auth/player DTOs and handlers in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/JogadorCreateRequestDto.cs`, and `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/AuthService.cs`
- [ ] T002 Review current player creation/update handlers in `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/` and repository methods in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/JogadorRepository.cs`
- [ ] T003 Review current registration/profile frontend flow in `FrontEnd/src/views/RegisterView.vue`, `FrontEnd/src/views/ProfileView.vue`, `FrontEnd/src/services/auth.ts`, and `FrontEnd/src/services/authState.ts`
- [ ] T004 [P] Review reusable player form components in `FrontEnd/src/components/players/PlayerFormModal.vue` and `FrontEnd/src/components/RoutePreferenceEditor.vue`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared backend/frontend types and reusable validation required by all user stories.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 Create self-service player profile DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/MeuJogadorDtos.cs`
- [ ] T006 Create self-service player profile commands and queries in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/MeuJogadorCommands.cs` and `BackEnd/src/RinhaDasLendas.Application/Queries/Auth/MeuJogadorQueries.cs`
- [ ] T007 Refactor player validation reuse so self-service and admin player creation share required route/profile rules in `BackEnd/src/RinhaDasLendas.Application/Validators/JogadorValidator.cs`
- [ ] T008 Add repository contract methods for lookup by user and user-linked creation support in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IJogadorRepository.cs`
- [ ] T009 Implement repository lookup/update helpers for user-linked player profiles in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/JogadorRepository.cs`
- [ ] T010 [P] Add frontend player profile types in `FrontEnd/src/types/meuJogador.ts`
- [ ] T011 [P] Add frontend self-service player profile service skeleton in `FrontEnd/src/services/meuJogador.ts`
- [ ] T012 [P] Add message mappings for pending/completed player profile states in `FrontEnd/src/services/messageService.ts`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Completar Perfil de Jogador Após Cadastro (Priority: P1) MVP

**Goal**: Usuário cadastrado acessa a plataforma, vê pendência de perfil e completa jogador vinculado ativo.

**Independent Test**: Cadastrar usuário, logar, completar perfil obrigatório e confirmar que o usuário passa a ter `jogadorId` e aparece como jogador ativo.

### Tests for User Story 1

- [ ] T013 [P] [US1] Add validator tests for valid and invalid self-service player profile completion in `BackEnd/tests/RinhaDasLendas.Tests/Validators/MeuJogadorValidatorTests.cs`
- [ ] T014 [P] [US1] Add application tests for completing own player profile in `BackEnd/tests/RinhaDasLendas.Tests/Auth/CompleteMeuJogadorProfileTests.cs`
- [ ] T015 [P] [US1] Add integration tests for `POST /api/v1/auth/me/jogador` in `BackEnd/tests/RinhaDasLendas.Tests/Integration/MeuJogadorIntegrationTests.cs`
- [ ] T016 [P] [US1] Add frontend service tests for completing own player profile in `FrontEnd/src/services/meuJogador.spec.ts`
- [ ] T017 [P] [US1] Add component tests for pending profile CTA/form behavior in `FrontEnd/src/components/users/CompletePlayerProfileCard.spec.ts`

### Implementation for User Story 1

- [ ] T018 [US1] Implement complete own player profile handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/CompleteMeuJogadorProfileCommandHandler.cs`
- [ ] T019 [US1] Ensure complete handler rejects duplicate linked player profiles in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/CompleteMeuJogadorProfileCommandHandler.cs`
- [ ] T020 [US1] Add self-service player profile endpoint for completion in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T021 [US1] Update authenticated user build logic to return new linked `jogadorId` after completion in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/AuthService.cs`
- [ ] T022 [US1] Implement frontend `completeMeuJogadorProfile` service call in `FrontEnd/src/services/meuJogador.ts`
- [ ] T023 [US1] Create reusable complete player profile card/form in `FrontEnd/src/components/users/CompletePlayerProfileCard.vue`
- [ ] T024 [US1] Reuse route preference editor in the complete profile form in `FrontEnd/src/components/users/CompletePlayerProfileCard.vue`
- [ ] T025 [US1] Show pending profile CTA on profile page when `jogadorId` is null in `FrontEnd/src/views/ProfileView.vue`
- [ ] T026 [US1] Refresh auth state after successful completion in `FrontEnd/src/views/ProfileView.vue` and `FrontEnd/src/services/authState.ts`
- [ ] T027 [US1] Add route/navigation guard allowance for authenticated users with pending player profile in `FrontEnd/src/router/guards.ts`

**Checkpoint**: US1 is complete when a newly registered user can complete profile and become an active linked player.

---

## Phase 4: User Story 2 - Impedir Perfil de Jogador Incompleto (Priority: P2)

**Goal**: Usuários sem perfil de jogador completo não aparecem em listagem de jogadores nem em seleção de draft.

**Independent Test**: Criar usuário sem jogador, abrir listagem e draft, e confirmar ausência até completar perfil.

### Tests for User Story 2

- [ ] T028 [P] [US2] Add backend integration test proving user without linked player is absent from player list in `BackEnd/tests/RinhaDasLendas.Tests/Integration/JogadoresUsuarioIntegrationTests.cs`
- [ ] T029 [P] [US2] Add backend integration test proving completed linked player appears in draft setup data in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemUsuarioJogadorIntegrationTests.cs`
- [ ] T030 [P] [US2] Add frontend test for draft/player pending profile messaging in `FrontEnd/src/views/DraftsView.spec.ts`

### Implementation for User Story 2

- [ ] T031 [US2] Verify player list queries include only actual `Jogador` records and do not synthesize users in `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/GetJogadoresQueryHandler.cs`
- [ ] T032 [US2] Ensure draft montagem player source uses only active `Jogador` records in `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/`
- [ ] T033 [US2] Add pending profile notice when authenticated user without `jogadorId` enters player-facing areas in `FrontEnd/src/components/users/PendingPlayerProfileNotice.vue`
- [ ] T034 [US2] Render pending profile notice in draft page for users without linked player in `FrontEnd/src/views/DraftsView.vue`
- [ ] T035 [US2] Render pending profile notice in players page for users without linked player in `FrontEnd/src/views/PlayersView.vue`
- [ ] T036 [US2] Keep create/edit player admin actions hidden for non-admin pending users in `FrontEnd/src/views/PlayersView.vue`

**Checkpoint**: US2 is complete when incomplete users can browse but never appear as selectable players.

---

## Phase 5: User Story 3 - Atualizar Próprio Perfil de Jogador (Priority: P3)

**Goal**: Usuário com jogador vinculado edita seus próprios dados completos e preferências de rota.

**Independent Test**: Usuário com jogador vinculado altera elo, links e rotas; listagem/draft passam a mostrar dados atualizados.

### Tests for User Story 3

- [ ] T037 [P] [US3] Add application tests for updating own linked player profile in `BackEnd/tests/RinhaDasLendas.Tests/Auth/UpdateMeuJogadorProfileTests.cs`
- [ ] T038 [P] [US3] Add integration tests for `GET` and `PUT /api/v1/auth/me/jogador` in `BackEnd/tests/RinhaDasLendas.Tests/Integration/MeuJogadorIntegrationTests.cs`
- [ ] T039 [P] [US3] Add frontend service tests for fetching and updating own player profile in `FrontEnd/src/services/meuJogador.spec.ts`
- [ ] T040 [P] [US3] Add profile view tests for editing linked player profile in `FrontEnd/src/views/ProfileView.spec.ts`

### Implementation for User Story 3

- [ ] T041 [US3] Implement get own player profile query handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/GetMeuJogadorProfileQueryHandler.cs`
- [ ] T042 [US3] Implement update own player profile command handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/UpdateMeuJogadorProfileCommandHandler.cs`
- [ ] T043 [US3] Add self-service get/update endpoints in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T044 [US3] Ensure update command cannot alter another user's player profile in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/UpdateMeuJogadorProfileCommandHandler.cs`
- [ ] T045 [US3] Implement `getMeuJogadorProfile` and `updateMeuJogadorProfile` calls in `FrontEnd/src/services/meuJogador.ts`
- [ ] T046 [US3] Add linked player profile editor state to `FrontEnd/src/views/ProfileView.vue`
- [ ] T047 [US3] Display current linked player data and save updates in `FrontEnd/src/components/users/CompletePlayerProfileCard.vue`
- [ ] T048 [US3] Update auth/user state only when linked player identity changes in `FrontEnd/src/views/ProfileView.vue`

**Checkpoint**: US3 is complete when users maintain their own player data without admin intervention.

---

## Phase 6: User Story 4 - Administração Visualiza Vínculo Usuário-Jogador (Priority: P4)

**Goal**: Administradores identificam usuários com perfil completo ou pendente na administração de usuários.

**Independent Test**: SuperAdmin abre lista/detalhes de usuários e vê estado distinto para usuários com e sem jogador vinculado.

### Tests for User Story 4

- [ ] T049 [P] [US4] Add backend tests for user summary/details player link status in `BackEnd/tests/RinhaDasLendas.Tests/Usuarios/UsuarioJogadorStatusTests.cs`
- [ ] T050 [P] [US4] Add frontend tests for user admin link status rendering in `FrontEnd/src/components/users/UserList.spec.ts`
- [ ] T051 [P] [US4] Add frontend tests for user details modal player status rendering in `FrontEnd/src/components/users/UserDetailsModal.spec.ts`

### Implementation for User Story 4

- [ ] T052 [US4] Extend user summary/details DTOs with player link status if missing in `BackEnd/src/RinhaDasLendas.Application/Dtos/UsuarioDtos.cs`
- [ ] T053 [US4] Populate player link status in user service list/detail queries in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/UsuarioService.cs`
- [ ] T054 [US4] Update frontend user types for player link status in `FrontEnd/src/types/users.ts`
- [ ] T055 [US4] Render player profile status chip in user list in `FrontEnd/src/components/users/UserList.vue`
- [ ] T056 [US4] Render player profile summary/pending state in user details modal in `FrontEnd/src/components/users/UserDetailsModal.vue`

**Checkpoint**: US4 is complete when admins can identify pending player profiles without inspecting player screens.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation and cleanup across all stories.

- [ ] T057 [P] Update auth/player profile quick notes in `docs/domain/usuarios-jogadores.md`
- [ ] T058 [P] Update user-facing copy for player profile completion in `FrontEnd/src/i18n/` if i18n keys are used by touched components
- [ ] T059 Run backend build with `docker compose -f ".devcontainer/docker-compose.yml" run --rm --no-deps app dotnet build "BackEnd/RinhaDasLendas.sln"`
- [ ] T060 Run backend tests with `docker compose -f ".devcontainer/docker-compose.yml" run --rm --no-deps app dotnet test "BackEnd/RinhaDasLendas.sln"`
- [ ] T061 Run frontend lint with `docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run lint`
- [ ] T062 Run frontend build with `docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run build`
- [ ] T063 Run frontend tests with `docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run test`
- [ ] T064 Execute manual quickstart scenarios from `specs/010-integracao-usuario-jogador/quickstart.md`
- [ ] T065 Ensure no temporary containers remain running after manual validation using `docker compose -f ".devcontainer/docker-compose.yml" down`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 (Phase 3)**: Depends on Foundational and is MVP.
- **US2 (Phase 4)**: Depends on Foundational; benefits from US1 for positive completion scenario.
- **US3 (Phase 5)**: Depends on US1 because a linked player must exist before editing.
- **US4 (Phase 6)**: Depends on Foundational; can be implemented after or parallel to US1 if DTO shape is stable.
- **Polish (Phase 7)**: Depends on all selected story phases.

### User Story Dependencies

- **US1**: No dependency on other user stories after foundation.
- **US2**: Can validate negative case independently; positive case depends on US1 completion.
- **US3**: Depends on US1.
- **US4**: Can proceed after foundation but should align with US1 data shape.

### Within Each User Story

- Tests before implementation where possible.
- DTOs/validators before handlers.
- Handlers before API endpoints.
- Services before frontend views.
- Component/service tests before final UI wiring where practical.

---

## Parallel Opportunities

- T004 can run parallel with T001-T003.
- T010-T012 can run parallel after T005-T009 are understood.
- T013-T017 can run parallel because they touch different test files.
- T028-T030 can run parallel.
- T037-T040 can run parallel.
- T049-T051 can run parallel.
- T057-T058 can run parallel with final validation preparation.

## Parallel Example: User Story 1

```text
Task: "T013 Add validator tests in BackEnd/tests/RinhaDasLendas.Tests/Validators/MeuJogadorValidatorTests.cs"
Task: "T014 Add application tests in BackEnd/tests/RinhaDasLendas.Tests/Auth/CompleteMeuJogadorProfileTests.cs"
Task: "T015 Add integration tests in BackEnd/tests/RinhaDasLendas.Tests/Integration/MeuJogadorIntegrationTests.cs"
Task: "T016 Add frontend service tests in FrontEnd/src/services/meuJogador.spec.ts"
Task: "T017 Add component tests in FrontEnd/src/components/users/CompletePlayerProfileCard.spec.ts"
```

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Setup and Foundational phases.
2. Complete US1 backend tests, handler and endpoint.
3. Complete US1 frontend service, CTA and form.
4. Validate with newly registered user completing profile.
5. Stop and verify the user appears as active player.

### Incremental Delivery

1. US1 creates linked player profile.
2. US2 ensures incomplete users never enter draft pool.
3. US3 allows self-service maintenance of player data.
4. US4 improves admin visibility of pending/completed player profiles.
5. Polish runs full validation and documentation.

### Notes

- Do not implement Discord OAuth or Riot API lookup in this feature.
- Do not merge `Usuario` and `Jogador`; the concepts remain separate.
- Do not create alternative root folders outside `BackEnd/` and `FrontEnd/`.
- Preserve unrelated worktree changes in `.devcontainer/`, `exemplo/`, and `opencode.json` unless explicitly requested.

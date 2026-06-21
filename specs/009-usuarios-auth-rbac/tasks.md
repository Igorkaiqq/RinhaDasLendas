# Tasks: Usuários, Autenticação, Autorização e RBAC

**Input**: Design documents from `/specs/009-usuarios-auth-rbac/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are included because the project mandates xUnit/FluentAssertions/Moq for domain rules, validators and application use cases, and Vitest for frontend behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after foundational auth/RBAC infrastructure exists.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase when files do not conflict
- **[Story]**: User story mapping from `spec.md`
- Every task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare dependencies, constants, configuration placeholders and documentation before auth implementation.

- [ ] T001 [Backend] Add Identity/JWT package references in `BackEnd/Directory.Packages.props`
- [ ] T002 [Backend] Add project package references for Identity/JWT/EF stores in `BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj`
- [ ] T003 [Backend] Add project package references for Identity/JWT/EF stores in `BackEnd/src/RinhaDasLendas.Infrastructure/RinhaDasLendas.Infrastructure.csproj`
- [ ] T004 [Backend] Add authentication configuration placeholders in `BackEnd/src/RinhaDasLendas.Api/appsettings.json`
- [ ] T005 [Backend] Add testing package references if missing in `BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj`
- [ ] T006 [Frontend] Add auth-related route constants in `FrontEnd/src/constants/appRoutes.ts`
- [ ] T007 [Docs] Add auth/RBAC message codes to `docs/messages/message-catalog.md`
- [ ] T008 [Backend] Mirror auth/RBAC message constants in `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [ ] T009 [Frontend] Mirror auth/RBAC message constants in `FrontEnd/src/constants/messageCode.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core auth entities, persistence, policies and frontend session shell required by all user stories.

**CRITICAL**: No user story work should begin until this phase is complete.

### Backend Foundation Tests

- [ ] T010 [P] [Tests] Add role hierarchy service tests in `BackEnd/tests/RinhaDasLendas.Tests/Authorization/RoleHierarchyServiceTests.cs`
- [ ] T011 [P] [Tests] Add refresh token domain/service tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/RefreshTokenServiceTests.cs`
- [ ] T012 [P] [Tests] Add user deactivation/session revocation tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/UserSessionRevocationTests.cs`

### Backend Foundation Implementation

- [ ] T013 [Backend] Create role constants and hierarchy levels in `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthRoles.cs`
- [ ] T014 [Backend] Create permission constants in `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthPermissions.cs`
- [ ] T015 [Backend] Create role hierarchy service in `BackEnd/src/RinhaDasLendas.Domain/Services/RoleHierarchyService.cs`
- [ ] T016 [Backend] Create Identity user model in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/ApplicationUser.cs`
- [ ] T017 [Backend] Create Identity role model in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/ApplicationRole.cs`
- [ ] T018 [Backend] Create refresh token entity in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/RefreshToken.cs`
- [ ] T019 [Backend] Create Discord link entity in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/VinculoDiscord.cs`
- [ ] T020 [Backend] Create user audit entity in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/AuditoriaUsuario.cs`
- [ ] T021 [Backend] Update `RinhaDasLendasDbContext` to inherit Identity context and map auth tables in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T022 [Backend] Configure Identity, role seeds and auth services in `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- [ ] T023 [Backend] Configure JWT authentication, authorization policies and middleware order in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T024 [Backend] Create authorization requirement/handler for role hierarchy in `BackEnd/src/RinhaDasLendas.Api/Authorization/RoleHierarchyAuthorizationHandler.cs`
- [ ] T025 [Backend] Create current user abstraction in `BackEnd/src/RinhaDasLendas.Application/Interfaces/ICurrentUser.cs`
- [ ] T026 [Backend] Implement current user accessor in `BackEnd/src/RinhaDasLendas.Api/Services/CurrentUser.cs`
- [ ] T027 [Backend] Create token service abstraction in `BackEnd/src/RinhaDasLendas.Application/Interfaces/ITokenService.cs`
- [ ] T028 [Backend] Implement JWT/refresh token service in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/TokenService.cs`
- [ ] T029 [Backend] Create audit service abstraction in `BackEnd/src/RinhaDasLendas.Application/Interfaces/IUsuarioAuditoriaService.cs`
- [ ] T030 [Backend] Implement user audit service in `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/UsuarioAuditoriaService.cs`
- [ ] T031 [Backend] Add EF migration for Identity/auth tables in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`
- [ ] T032 [Backend] Update Swagger security definition for bearer auth in `BackEnd/src/RinhaDasLendas.Api/Program.cs`

### Frontend Foundation Tests

- [ ] T033 [P] [Frontend] Add auth role/permission constant tests in `FrontEnd/src/constants/authRoles.spec.ts`
- [ ] T034 [P] [Frontend] Add route guard tests in `FrontEnd/src/router/guards.spec.ts`
- [ ] T035 [P] [Frontend] Add auth service tests for token refresh/logout behavior in `FrontEnd/src/services/auth.spec.ts`

### Frontend Foundation Implementation

- [ ] T036 [Frontend] Create auth role constants in `FrontEnd/src/constants/authRoles.ts`
- [ ] T037 [Frontend] Create permission constants in `FrontEnd/src/constants/permissions.ts`
- [ ] T038 [Frontend] Create auth/user types in `FrontEnd/src/types/auth.ts`
- [ ] T039 [Frontend] Create users admin types in `FrontEnd/src/types/users.ts`
- [ ] T040 [Frontend] Implement auth service shell in `FrontEnd/src/services/auth.ts`
- [ ] T041 [Frontend] Update Axios interceptors for bearer token and refresh retry in `FrontEnd/src/services/api.ts`
- [ ] T042 [Frontend] Implement auth state composable in `FrontEnd/src/services/authState.ts`
- [ ] T043 [Frontend] Implement route guards in `FrontEnd/src/router/guards.ts`
- [ ] T044 [Frontend] Register auth route metadata and guards in `FrontEnd/src/router/index.ts`

**Checkpoint**: Auth/RBAC foundation exists; user stories can begin.

---

## Phase 3: User Story 1 - Cadastro Público de Conta (Priority: P1) MVP

**Goal**: Visitors can create an account with role `Jogador`.

**Independent Test**: Register a valid user and verify active account with role `Jogador`; duplicate e-mail and invalid passwords fail.

### Tests for User Story 1

- [ ] T045 [P] [US1] Add register validator tests in `BackEnd/tests/RinhaDasLendas.Tests/Validators/RegisterValidatorTests.cs`
- [ ] T046 [P] [US1] Add register handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/RegisterUserCommandHandlerTests.cs`
- [ ] T047 [P] [US1] Add register endpoint integration tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/AuthRegisterIntegrationTests.cs`
- [ ] T048 [P] [US1] Add register view tests in `FrontEnd/src/views/RegisterView.spec.ts`

### Implementation for User Story 1

- [ ] T049 [US1] Create register DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`
- [ ] T050 [US1] Create register command in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/RegisterUserCommand.cs`
- [ ] T051 [US1] Create register validator in `BackEnd/src/RinhaDasLendas.Application/Validators/AuthValidator.cs`
- [ ] T052 [US1] Implement register handler assigning `Jogador` role in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/RegisterUserCommandHandler.cs`
- [ ] T053 [US1] Add `/api/v1/auth/register` action in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T054 [US1] Implement register API function in `FrontEnd/src/services/auth.ts`
- [ ] T055 [US1] Create register page in `FrontEnd/src/views/RegisterView.vue`
- [ ] T056 [US1] Add public register route in `FrontEnd/src/router/index.ts`

**Checkpoint**: Public account registration works independently.

---

## Phase 4: User Story 2 - Login, Logout e Persistência de Sessão (Priority: P1)

**Goal**: Active users can login, refresh session, stay authenticated after reload and logout.

**Independent Test**: Login as active user, access protected route, refresh session, reload page and logout.

### Tests for User Story 2

- [ ] T057 [P] [US2] Add login/refresh/logout handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/LoginRefreshLogoutHandlerTests.cs`
- [ ] T058 [P] [US2] Add session integration tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/AuthSessionIntegrationTests.cs`
- [ ] T059 [P] [US2] Add login view tests in `FrontEnd/src/views/LoginView.spec.ts`
- [ ] T060 [P] [US2] Add profile menu logout tests in `FrontEnd/src/components/layout/ProfileMenu.spec.ts`

### Implementation for User Story 2

- [ ] T061 [US2] Add login/refresh/logout DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`
- [ ] T062 [US2] Create login command in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/LoginCommand.cs`
- [ ] T063 [US2] Create refresh command in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/RefreshSessionCommand.cs`
- [ ] T064 [US2] Create logout command in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/LogoutCommand.cs`
- [ ] T065 [US2] Implement login handler with audit and last-login update in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/LoginCommandHandler.cs`
- [ ] T066 [US2] Implement refresh handler with rotation/reuse detection in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/RefreshSessionCommandHandler.cs`
- [ ] T067 [US2] Implement logout handler revoking current session in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/LogoutCommandHandler.cs`
- [ ] T068 [US2] Add `/login`, `/refresh` and `/logout` actions in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T069 [US2] Create login page in `FrontEnd/src/views/LoginView.vue`
- [ ] T070 [US2] Wire login/logout/refresh methods in `FrontEnd/src/services/auth.ts`
- [ ] T071 [US2] Update profile menu logout action in `FrontEnd/src/components/layout/ProfileMenu.vue`
- [ ] T072 [US2] Add session expired UI in `FrontEnd/src/components/layout/SessionExpiredDialog.vue`

**Checkpoint**: Session lifecycle works independently.

---

## Phase 5: User Story 3 - Recuperação, Redefinição e Alteração de Senha (Priority: P1)

**Goal**: Users can recover, reset and change password securely.

**Independent Test**: Forgot-password returns generic response; valid reset token changes password; authenticated change requires current password.

### Tests for User Story 3

- [ ] T073 [P] [US3] Add password validator tests in `BackEnd/tests/RinhaDasLendas.Tests/Validators/PasswordValidatorTests.cs`
- [ ] T074 [P] [US3] Add password recovery handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/PasswordRecoveryHandlerTests.cs`
- [ ] T075 [P] [US3] Add forgot/reset/change password integration tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/AuthPasswordIntegrationTests.cs`
- [ ] T076 [P] [US3] Add forgot/reset password view tests in `FrontEnd/src/views/ForgotPasswordView.spec.ts`

### Implementation for User Story 3

- [ ] T077 [US3] Add password recovery DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`
- [ ] T078 [US3] Create forgot/reset/change password commands in `BackEnd/src/RinhaDasLendas.Application/Commands/Auth/PasswordCommands.cs`
- [ ] T079 [US3] Add password validators in `BackEnd/src/RinhaDasLendas.Application/Validators/AuthValidator.cs`
- [ ] T080 [US3] Implement forgot password handler with generic response in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/ForgotPasswordCommandHandler.cs`
- [ ] T081 [US3] Implement reset password handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/ResetPasswordCommandHandler.cs`
- [ ] T082 [US3] Implement change password handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/ChangePasswordCommandHandler.cs`
- [ ] T083 [US3] Add password actions in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T084 [US3] Add forgot/reset/change password API functions in `FrontEnd/src/services/auth.ts`
- [ ] T085 [US3] Create forgot password page in `FrontEnd/src/views/ForgotPasswordView.vue`
- [ ] T086 [US3] Create reset password page in `FrontEnd/src/views/ResetPasswordView.vue`
- [ ] T087 [US3] Create change password form in `FrontEnd/src/components/users/ChangePasswordForm.vue`

**Checkpoint**: Password recovery and password change work independently.

---

## Phase 6: User Story 4 - Perfil do Usuário e Perfil de Jogador (Priority: P1)

**Goal**: Authenticated users can view/update own profile and see linked player/Discord status.

**Independent Test**: Authenticated user opens profile, updates own name and sees player/Discord status.

### Tests for User Story 4

- [ ] T088 [P] [US4] Add current user/profile handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/CurrentUserProfileHandlerTests.cs`
- [ ] T089 [P] [US4] Add player-user link tests in `BackEnd/tests/RinhaDasLendas.Tests/Jogadores/JogadorUsuarioVinculoTests.cs`
- [ ] T090 [P] [US4] Add profile view tests in `FrontEnd/src/views/ProfileView.spec.ts`

### Implementation for User Story 4

- [ ] T091 [US4] Add `UsuarioId` support to jogador entity in `BackEnd/src/RinhaDasLendas.Domain/Entities/Jogador.cs`
- [ ] T092 [US4] Update jogador mapping for `usuario_id` and optional Discord in `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T093 [US4] Add EF migration for jogador-user link in `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`
- [ ] T094 [US4] Add current user/profile DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`
- [ ] T095 [US4] Create current user query and profile update command in `BackEnd/src/RinhaDasLendas.Application/Queries/Auth/GetCurrentUserQuery.cs`
- [ ] T096 [US4] Implement current user/profile handlers in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/CurrentUserHandlers.cs`
- [ ] T097 [US4] Add `/me`, `/me/profile` and `/me/permissions` actions in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T098 [US4] Implement profile API methods in `FrontEnd/src/services/auth.ts`
- [ ] T099 [US4] Create profile page in `FrontEnd/src/views/ProfileView.vue`
- [ ] T100 [US4] Create Discord link status section placeholder in `FrontEnd/src/components/users/DiscordLinkSection.vue`

**Checkpoint**: Own profile flow works independently.

---

## Phase 7: User Story 5 - Controle de Acesso por Roles (Priority: P1)

**Goal**: Backend and frontend enforce roles/policies for protected actions.

**Independent Test**: Users with each role attempt protected routes/endpoints and receive allow/deny decisions matching spec.

### Tests for User Story 5

- [ ] T101 [P] [US5] Add authorization policy integration tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/AuthorizationPoliciesIntegrationTests.cs`
- [ ] T102 [P] [US5] Add route guard role tests in `FrontEnd/src/router/guards.spec.ts`
- [ ] T103 [P] [US5] Add sidebar authorization tests in `FrontEnd/src/components/layout/SidebarNav.spec.ts`

### Implementation for User Story 5

- [ ] T104 [US5] Configure named authorization policies in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T105 [US5] Apply authorization attributes to existing protected controllers in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
- [ ] T106 [US5] Apply authorization attributes to existing protected controllers in `BackEnd/src/RinhaDasLendas.Api/Controllers/TimesController.cs`
- [ ] T107 [US5] Apply authorization attributes to existing protected controllers in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftsController.cs`
- [ ] T108 [US5] Apply authorization attributes to existing protected controllers in `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T109 [US5] Extend frontend route metadata with permissions in `FrontEnd/src/router/index.ts`
- [ ] T110 [US5] Filter sidebar items by permission in `FrontEnd/src/components/layout/AppShell.vue`
- [ ] T111 [US5] Add forbidden page in `FrontEnd/src/views/ForbiddenView.vue`

**Checkpoint**: Access control works before advanced user management.

---

## Phase 8: User Story 6 - Gerenciamento Administrativo de Usuários (Priority: P1)

**Goal**: SuperAdmin/Admin can list, filter and manage users according to hierarchy.

**Independent Test**: SuperAdmin manages all; Admin manages only lower roles; prohibited actions are hidden and backend returns 403.

### Tests for User Story 6

- [ ] T112 [P] [US6] Add user listing/query handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Usuarios/GetUsuariosQueryHandlerTests.cs`
- [ ] T113 [P] [US6] Add user role update handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Usuarios/UpdateUsuarioRolesCommandHandlerTests.cs`
- [ ] T114 [P] [US6] Add user activation/deactivation handler tests in `BackEnd/tests/RinhaDasLendas.Tests/Usuarios/UsuarioStatusCommandHandlerTests.cs`
- [ ] T115 [P] [US6] Add admin user API integration tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/UsuariosAdminIntegrationTests.cs`
- [ ] T116 [P] [US6] Add users admin view tests in `FrontEnd/src/views/UsersAdminView.spec.ts`
- [ ] T117 [P] [US6] Add user roles editor tests in `FrontEnd/src/components/users/UserRolesEditor.spec.ts`

### Implementation for User Story 6

- [ ] T118 [US6] Add user management DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/UsuarioDtos.cs`
- [ ] T119 [US6] Create user queries in `BackEnd/src/RinhaDasLendas.Application/Queries/Usuarios/GetUsuariosQuery.cs`
- [ ] T120 [US6] Create user management commands in `BackEnd/src/RinhaDasLendas.Application/Commands/Usuarios/UsuarioCommands.cs`
- [ ] T121 [US6] Add user management validators in `BackEnd/src/RinhaDasLendas.Application/Validators/UsuarioValidator.cs`
- [ ] T122 [US6] Implement user listing/details handlers in `BackEnd/src/RinhaDasLendas.Application/Handlers/Usuarios/GetUsuariosQueryHandler.cs`
- [ ] T123 [US6] Implement user update/status/roles handlers in `BackEnd/src/RinhaDasLendas.Application/Handlers/Usuarios/UsuarioCommandHandlers.cs`
- [ ] T124 [US6] Implement administrative password reset handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Usuarios/ResetUsuarioPasswordCommandHandler.cs`
- [ ] T125 [US6] Create users controller in `BackEnd/src/RinhaDasLendas.Api/Controllers/UsuariosController.cs`
- [ ] T126 [US6] Implement users service in `FrontEnd/src/services/users.ts`
- [ ] T127 [US6] Create users admin page in `FrontEnd/src/views/UsersAdminView.vue`
- [ ] T128 [US6] Create users list component in `FrontEnd/src/components/users/UserList.vue`
- [ ] T129 [US6] Create user filters component in `FrontEnd/src/components/users/UserFilters.vue`
- [ ] T130 [US6] Create user details drawer in `FrontEnd/src/components/users/UserDetailsDrawer.vue`
- [ ] T131 [US6] Create user roles editor in `FrontEnd/src/components/users/UserRolesEditor.vue`
- [ ] T132 [US6] Create user status confirmation dialog in `FrontEnd/src/components/users/UserStatusConfirmDialog.vue`
- [ ] T133 [US6] Create admin reset password dialog in `FrontEnd/src/components/users/ResetUserPasswordDialog.vue`

**Checkpoint**: Administrative user management works independently.

---

## Phase 9: User Story 7 - Elegibilidade de Capitães no Draft (Priority: P2)

**Goal**: Draft captain selection only lists active players linked to users with explicit `Capitão` role.

**Independent Test**: Admin/SuperAdmin without `Capitão` are excluded; users with explicit `Capitão` and active player are included.

### Tests for User Story 7

- [ ] T134 [P] [US7] Add captain eligibility query tests in `BackEnd/tests/RinhaDasLendas.Tests/Drafts/CapitaoElegibilidadeTests.cs`
- [ ] T135 [P] [US7] Add draft captain UI tests in `FrontEnd/src/components/drafts/DraftCreateModal.spec.ts`

### Implementation for User Story 7

- [ ] T136 [US7] Add captain eligibility repository query in `BackEnd/src/RinhaDasLendas.Domain/Repositories/IJogadorRepository.cs`
- [ ] T137 [US7] Implement captain eligibility query in `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/JogadorRepository.cs`
- [ ] T138 [US7] Add captain eligibility query/handler in `BackEnd/src/RinhaDasLendas.Application/Queries/Jogadores/GetCapitaesElegiveisQuery.cs`
- [ ] T139 [US7] Add eligible captains endpoint in `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`
- [ ] T140 [US7] Update frontend player service with eligible captains call in `FrontEnd/src/services/players.ts`
- [ ] T141 [US7] Update draft captain selection to use eligible captains in `FrontEnd/src/components/drafts/DraftCreateModal.vue`

**Checkpoint**: Draft captain role rule works independently.

---

## Phase 10: User Story 8 - Preparação para Discord Futuro (Priority: P2)

**Goal**: Model, contracts and UI can represent Discord link status without enabling OAuth implementation.

**Independent Test**: Profile can display Discord not-linked/linked status and backend stores unique link data when seeded/tested.

### Tests for User Story 8

- [ ] T142 [P] [US8] Add Discord link model tests in `BackEnd/tests/RinhaDasLendas.Tests/Auth/VinculoDiscordTests.cs`
- [ ] T143 [P] [US8] Add Discord status endpoint tests in `BackEnd/tests/RinhaDasLendas.Tests/Integration/DiscordLinkStatusIntegrationTests.cs`
- [ ] T144 [P] [US8] Add Discord link section tests in `FrontEnd/src/components/users/DiscordLinkSection.spec.ts`

### Implementation for User Story 8

- [ ] T145 [US8] Add Discord status DTOs in `BackEnd/src/RinhaDasLendas.Application/Dtos/AuthDtos.cs`
- [ ] T146 [US8] Add Discord status query in `BackEnd/src/RinhaDasLendas.Application/Queries/Auth/GetDiscordLinkStatusQuery.cs`
- [ ] T147 [US8] Implement Discord status handler in `BackEnd/src/RinhaDasLendas.Application/Handlers/Auth/GetDiscordLinkStatusQueryHandler.cs`
- [ ] T148 [US8] Add `/api/v1/auth/me/discord` status action in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T149 [US8] Add placeholder responses or feature-disabled handling for future Discord OAuth actions in `BackEnd/src/RinhaDasLendas.Api/Controllers/AuthController.cs`
- [ ] T150 [US8] Add Discord status API method in `FrontEnd/src/services/auth.ts`
- [ ] T151 [US8] Wire Discord status display in `FrontEnd/src/components/users/DiscordLinkSection.vue`

**Checkpoint**: Discord future support is represented without implementing OAuth.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, localization and full regression checks.

- [ ] T152 [P] [Docs] Update API test report scope in `docs/api/RELATORIO_TESTES_HTTP.md`
- [ ] T153 [P] [Docs] Add auth/RBAC domain notes in `docs/domain/usuarios-auth-rbac.md`
- [ ] T154 [Backend] Ensure error middleware handles authentication/authorization errors consistently in `BackEnd/src/RinhaDasLendas.Api/Filters/ApiExceptionMiddleware.cs`
- [ ] T155 [Backend] Review CORS/cookie settings for frontend origin in `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T156 [Frontend] Add i18n entries for auth/RBAC screens in `FrontEnd/src/i18n/index.ts`
- [ ] T157 [Backend] Run `dotnet build` from `BackEnd/`
- [ ] T158 [Backend] Run `dotnet test` from `BackEnd/`
- [ ] T159 [Frontend] Run `npm run lint` from `FrontEnd/`
- [ ] T160 [Frontend] Run `npm run build` from `FrontEnd/`
- [ ] T161 [Frontend] Run `npm run test:unit` from `FrontEnd/`
- [ ] T162 [Docs] Validate quickstart scenarios in `specs/009-usuarios-auth-rbac/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **US1 Cadastro**: Depends on Foundation.
- **US2 Login/Sessão**: Depends on Foundation and benefits from US1 for public account creation, but can be tested with seeded users.
- **US3 Senhas**: Depends on Foundation and authenticated/session components from US2 for change-password.
- **US4 Perfil**: Depends on Foundation and US2 authentication.
- **US5 RBAC**: Depends on Foundation and should complete before exposing US6 admin screens.
- **US6 Gerenciamento**: Depends on Foundation and US5 policies.
- **US7 Capitães**: Depends on Foundation, user-player link from US4 and roles from US5/US6.
- **US8 Discord Futuro**: Depends on Foundation and US4 profile surface.
- **Polish**: Depends on selected stories being complete.

### MVP Scope

The minimum useful/authenticated MVP is:

- Phase 1 Setup.
- Phase 2 Foundational.
- Phase 3 US1 Cadastro.
- Phase 4 US2 Login/Sessão.
- Phase 7 US5 RBAC baseline.

### Parallel Opportunities

- T010-T012 can run in parallel.
- T013-T020 can run in parallel after constants are agreed.
- T033-T035 can run in parallel with backend foundation tests.
- Tests within each user story can run in parallel.
- Frontend view/component tasks can run in parallel with backend handlers after contracts are stable.
- US8 can be implemented later without blocking the local auth MVP.

## Parallel Example: User Story 6

```text
Task: "Add user listing/query handler tests in BackEnd/tests/RinhaDasLendas.Tests/Usuarios/GetUsuariosQueryHandlerTests.cs"
Task: "Add user role update handler tests in BackEnd/tests/RinhaDasLendas.Tests/Usuarios/UpdateUsuarioRolesCommandHandlerTests.cs"
Task: "Add users admin view tests in FrontEnd/src/views/UsersAdminView.spec.ts"
Task: "Add user roles editor tests in FrontEnd/src/components/users/UserRolesEditor.spec.ts"
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundation.
2. Implement US1 registration.
3. Implement US2 login/session/logout.
4. Implement US5 basic protected routes/endpoints.
5. Validate with quickstart registration/login/RBAC scenarios.

### Incremental Delivery

1. Add US3 password recovery/change.
2. Add US4 profile and user-player link.
3. Add US6 administrative user management.
4. Add US7 captain eligibility.
5. Add US8 Discord future support.
6. Complete polish and full regression.

## Notes

- Stop before implementation until these tasks are approved.
- Keep controllers thin and place business rules in handlers/domain services.
- Never store raw refresh tokens.
- Never expose entities directly through API responses.
- Preserve unrelated worktree changes when implementing later.

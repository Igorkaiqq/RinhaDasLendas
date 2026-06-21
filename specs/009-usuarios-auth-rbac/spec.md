# Feature Specification: Usuários, Autenticação, Autorização e RBAC

**Feature Branch**: `feature/009-usuarios-auth-rbac`

**Created**: 2026-06-20

**Status**: Draft

**Input**: User description: "Implementar módulo completo de Usuários, Autenticação, Autorização e Controle de Acesso (RBAC), com cadastro, login, logout, sessão, refresh token, recuperação de senha, gerenciamento administrativo de usuários, hierarquia de roles e preparação futura para Discord OAuth2."

## Objetivo

Implementar controle de acesso no RinhaDasLendas para que usuários possam criar conta, entrar no sistema, manter sessão, recuperar acesso, editar o próprio perfil e executar ações somente quando possuírem permissão.

O módulo deve introduzir gerenciamento administrativo de usuários com roles hierárquicas, proteger telas e ações sensíveis, registrar auditoria de mudanças administrativas e preparar a evolução futura para login/vínculo com Discord sem tornar Discord obrigatório nesta entrega.

## Contexto

O sistema já possui jogadores, times, drafts e montagem visual, mas ainda assume uso interno confiável. A Constituição do projeto exige autenticação antes de alterações importantes, proteção de dados de usuários e que integrações externas não travem o MVP.

O cadastro atual de jogador representa perfil de jogo. Esta feature diferencia conta de acesso do perfil de jogador: usuário autentica e possui permissões; jogador contém dados de jogo, Riot ID, elo, divisão, preferências de rota e participação em drafts/partidas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastro Público de Conta (Priority: P1)

Como visitante, quero criar uma conta com nome, e-mail e senha para acessar o sistema como Jogador.

**Why this priority**: Sem cadastro de conta, não existe base autenticada para sessão, permissões e perfil próprio.

**Independent Test**: Cadastrar usuário com dados válidos e verificar que a conta fica ativa com role `Jogador`, sem acesso administrativo.

**Acceptance Scenarios**:

1. **Given** um visitante sem conta, **When** informa nome, e-mail, senha e confirmação válidos, **Then** uma conta ativa é criada com role `Jogador`.
2. **Given** e-mail já cadastrado, **When** outro cadastro usa o mesmo e-mail, **Then** o sistema rejeita a criação com erro claro.
3. **Given** senha fraca ou confirmação divergente, **When** o visitante envia o formulário, **Then** o sistema apresenta validações apropriadas.

---

### User Story 2 - Login, Logout e Persistência de Sessão (Priority: P1)

Como usuário cadastrado, quero entrar e sair do sistema com sessão persistente para usar funcionalidades protegidas com segurança.

**Why this priority**: Autenticação é o requisito mínimo para proteger alterações importantes.

**Independent Test**: Fazer login com usuário ativo, recarregar a aplicação mantendo sessão, renovar sessão expirada e fazer logout invalidando a sessão atual.

**Acceptance Scenarios**:

1. **Given** usuário ativo com credenciais válidas, **When** faz login, **Then** recebe sessão válida e acessa telas protegidas.
2. **Given** usuário desativado, **When** tenta login, **Then** o acesso é bloqueado.
3. **Given** senha incorreta, **When** tenta login, **Then** recebe erro genérico sem confirmar se o e-mail existe.
4. **Given** sessão válida, **When** a página é recarregada, **Then** o usuário permanece autenticado se a sessão ainda puder ser renovada.
5. **Given** usuário autenticado, **When** faz logout, **Then** a sessão atual é revogada e telas protegidas deixam de ser acessíveis.

---

### User Story 3 - Recuperação, Redefinição e Alteração de Senha (Priority: P1)

Como usuário, quero recuperar ou alterar minha senha para manter acesso à conta sem intervenção administrativa desnecessária.

**Why this priority**: Contas locais precisam de fluxo seguro de recuperação e manutenção de senha.

**Independent Test**: Solicitar recuperação, redefinir senha com token válido, bloquear token inválido/expirado e alterar senha autenticado informando senha atual.

**Acceptance Scenarios**:

1. **Given** e-mail cadastrado, **When** solicita recuperação de senha, **Then** o sistema inicia o fluxo e responde com mensagem genérica.
2. **Given** e-mail inexistente, **When** solicita recuperação de senha, **Then** o sistema responde com a mesma mensagem genérica.
3. **Given** token de redefinição válido, **When** informa nova senha válida, **Then** a senha anterior deixa de funcionar.
4. **Given** usuário autenticado, **When** informa senha atual correta e nova senha válida, **Then** a senha é alterada.
5. **Given** senha atual incorreta, **When** tenta alterar senha, **Then** a alteração é rejeitada.

---

### User Story 4 - Perfil do Usuário e Perfil de Jogador (Priority: P1)

Como usuário autenticado, quero visualizar e atualizar meu perfil para manter meus dados básicos e meu perfil de jogador corretos.

**Why this priority**: Todo usuário comum deve conseguir manter seus próprios dados sem acesso administrativo.

**Independent Test**: Usuário consulta o próprio perfil, altera dados permitidos e visualiza vínculo com jogador e Discord futuro quando existir.

**Acceptance Scenarios**:

1. **Given** usuário autenticado, **When** acessa o perfil, **Then** vê nome, e-mail, roles, status, jogador vinculado e status do vínculo Discord quando existir.
2. **Given** usuário autenticado, **When** atualiza dados básicos permitidos, **Then** as alterações são salvas.
3. **Given** usuário com perfil de jogador incompleto, **When** acessa áreas que exigem jogador ativo, **Then** o sistema orienta completar o perfil.

---

### User Story 5 - Controle de Acesso por Roles (Priority: P1)

Como sistema, quero aplicar permissões por roles para bloquear ações indevidas mesmo quando alguém tente chamar endpoints diretamente.

**Why this priority**: O controle de acesso deve proteger backend e frontend, não apenas ocultar botões.

**Independent Test**: Validar acessos e bloqueios usando usuários com roles `Jogador`, `Capitão`, `Moderador`, `Admin` e `SuperAdmin`.

**Acceptance Scenarios**:

1. **Given** Jogador autenticado, **When** tenta acessar gerenciamento de usuários, **Then** é bloqueado.
2. **Given** Moderador autenticado, **When** tenta alterar roles, **Then** é bloqueado.
3. **Given** Admin autenticado, **When** tenta alterar usuário Admin ou SuperAdmin, **Then** é bloqueado.
4. **Given** SuperAdmin autenticado, **When** altera roles de qualquer usuário permitido pelas regras de segurança, **Then** a ação é executada e auditada.

---

### User Story 6 - Gerenciamento Administrativo de Usuários (Priority: P1)

Como SuperAdmin ou Admin, quero listar, pesquisar, filtrar e gerenciar usuários conforme minhas permissões.

**Why this priority**: RBAC exige operação administrativa clara para suporte, promoção, rebaixamento e desativação de contas.

**Independent Test**: SuperAdmin visualiza todos; Admin visualiza e atua apenas sobre usuários abaixo dele; ações proibidas não aparecem no frontend e são bloqueadas no backend.

**Acceptance Scenarios**:

1. **Given** SuperAdmin, **When** acessa gerenciamento de usuários, **Then** visualiza e gerencia todos os usuários.
2. **Given** Admin, **When** acessa gerenciamento de usuários, **Then** visualiza usuários administráveis e não vê ações proibidas sobre Admin/SuperAdmin.
3. **Given** Moderador, **When** acessa usuários, **Then** pode visualizar usuários conforme permissão de consulta, mas não altera roles nem senhas de terceiros.
4. **Given** alteração de role, ativação, desativação ou reset administrativo de senha, **When** a operação conclui, **Then** a ação é registrada em auditoria.

---

### User Story 7 - Elegibilidade de Capitães no Draft (Priority: P2)

Como organizador, quero que somente usuários com role explícita `Capitão` apareçam como opção de capitão em drafts.

**Why this priority**: A role Capitão precisa influenciar diretamente o fluxo de draft sem confundir hierarquia administrativa com elegibilidade de jogo.

**Independent Test**: Criar usuários com diferentes roles e verificar que somente jogadores ativos vinculados a usuários com role `Capitão` aparecem na seleção.

**Acceptance Scenarios**:

1. **Given** usuários com roles diferentes, **When** a tela de draft lista capitães, **Then** apenas usuários com role explícita `Capitão` aparecem.
2. **Given** Admin ou SuperAdmin sem role `Capitão`, **When** a tela lista capitães, **Then** eles não aparecem por hierarquia administrativa.
3. **Given** Admin também possui role `Capitão`, **When** a tela lista capitães, **Then** ele aparece como capitão elegível se possuir jogador ativo.

---

### User Story 8 - Preparação para Discord Futuro (Priority: P2)

Como usuário, quero que minha conta possa futuramente ser vinculada ao Discord para login, identificação pelo bot e confirmações de presença.

**Why this priority**: Discord é parte da visão do produto, mas não deve bloquear o MVP sem integração externa.

**Independent Test**: Validar que o modelo e os fluxos previstos suportam vínculo único com Discord sem exigir OAuth nesta entrega.

**Acceptance Scenarios**:

1. **Given** usuário sem conta no futuro, **When** entrar com Discord, **Then** o sistema poderá criar conta com role `Jogador` e solicitar complemento do perfil.
2. **Given** usuário com conta local no futuro, **When** vincular Discord, **Then** o sistema poderá salvar vínculo único com a conta.
3. **Given** usuário vinculado no futuro, **When** bot identificar Discord ID, **Then** o sistema poderá localizar usuário e jogador vinculados.

## Edge Cases

- Token de sessão expirado durante navegação.
- Sessão revogada em outro dispositivo.
- Usuário desativado com sessão ainda aberta.
- Tentativa de reutilização de refresh token já rotacionado.
- Login com senha incorreta repetidas vezes.
- Tentativa de descobrir e-mails cadastrados por mensagens diferentes.
- Admin tentando promover usuário para Admin ou SuperAdmin.
- Admin tentando alterar outro Admin ou SuperAdmin.
- Usuário tentando alterar as próprias roles.
- Último SuperAdmin ativo tentando se desativar ou remover sua própria role.
- Jogador legado sem usuário vinculado.
- Usuário administrativo sem perfil de jogador.
- Usuário com múltiplas roles, incluindo role administrativa e `Capitão`.
- Discord ID já vinculado a outro usuário em fluxo futuro.
- Falha no provedor Discord em fluxo futuro.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow public account registration with name, e-mail, password and password confirmation.
- **FR-002**: System MUST assign role `Jogador` to every public account created successfully.
- **FR-003**: System MUST reject duplicate e-mails.
- **FR-004**: System MUST validate required fields, e-mail format, password confirmation and password strength.
- **FR-005**: System MUST allow active users to login with e-mail and password.
- **FR-006**: System MUST block login for deactivated users.
- **FR-007**: System MUST provide generic login failure messages that do not reveal whether the e-mail exists.
- **FR-008**: System MUST allow users to logout and revoke the current session.
- **FR-009**: System MUST support session renewal without requiring the user to login again while the refresh session is valid.
- **FR-010**: System MUST revoke sessions when a user is deactivated.
- **FR-011**: System MUST support forgot password and reset password flows with generic request responses.
- **FR-012**: System MUST allow authenticated users to change their own password by informing the current password.
- **FR-013**: System MUST allow authenticated users to view the current authenticated profile.
- **FR-014**: System MUST allow authenticated users to update allowed own profile fields.
- **FR-015**: System MUST protect backend endpoints according to authentication and authorization rules.
- **FR-016**: System MUST protect frontend routes according to authentication and authorization rules.
- **FR-017**: System MUST hide menus, buttons and actions the current user cannot perform.
- **FR-018**: System MUST keep backend authorization as source of truth even when frontend hides actions.
- **FR-019**: System MUST support roles `SuperAdmin`, `Admin`, `Moderador`, `Capitão` and `Jogador`.
- **FR-020**: System MUST apply a role hierarchy where users can administer only users below their effective administrative level.
- **FR-021**: System MUST allow SuperAdmin to manage all users and roles, subject to the rule that the system must retain at least one active SuperAdmin.
- **FR-022**: System MUST allow Admin to manage only Moderador, Capitão and Jogador users.
- **FR-023**: System MUST prevent Admin from creating, promoting, demoting, disabling or editing Admin and SuperAdmin users.
- **FR-024**: System MUST prevent Moderador, Capitão and Jogador from changing roles.
- **FR-025**: System MUST provide an administrative user management screen for authorized users.
- **FR-026**: User management MUST support listing, pagination, search and filters by name, e-mail, role and status.
- **FR-027**: User management MUST support viewing user details, creation date, last login when available, status, roles and Discord link status when available.
- **FR-028**: Authorized administrators MUST be able to update basic user data, activate, deactivate, change allowed roles and reset user password.
- **FR-029**: System MUST audit login, logout, password reset, role changes, activation, deactivation and administrative password reset events.
- **FR-030**: System MUST model user account and player profile as separate concepts.
- **FR-031**: System MUST support linking a user account to a player profile.
- **FR-032**: System MUST allow existing players to remain without user account until linked.
- **FR-033**: System MUST allow administrative users to exist without player profile.
- **FR-034**: New public accounts SHOULD start or create a linked player profile flow so the user can become an active player after completing game-specific data.
- **FR-035**: Draft captain selection MUST include only active players linked to users with explicit role `Capitão`.
- **FR-036**: Admin and SuperAdmin MUST NOT be eligible as draft captains by hierarchy alone; they need explicit role `Capitão`.
- **FR-037**: System MUST reserve model support for future Discord login, Discord-only registration, local account linking, unlinking and Discord-to-player synchronization.
- **FR-038**: Future Discord link MUST be unique per Discord account and per local user account.
- **FR-039**: System MUST provide clear user-facing messages for validation, expired session, insufficient permission and recoverable errors.

### Business Rules

- **BR-001**: Every public registration receives role `Jogador` by default.
- **BR-002**: A user may have multiple roles.
- **BR-003**: Administrative power is determined by the highest hierarchical role the user has.
- **BR-004**: Draft captain eligibility requires explicit role `Capitão`, not only a higher administrative role.
- **BR-005**: SuperAdmin can administer Admin, Moderador, Capitão and Jogador.
- **BR-006**: Admin can administer Moderador, Capitão and Jogador.
- **BR-007**: Moderador, Capitão and Jogador cannot administer roles.
- **BR-008**: Admin cannot promote anyone to Admin or SuperAdmin.
- **BR-009**: Admin cannot alter Admin or SuperAdmin users.
- **BR-010**: The system must always retain at least one active SuperAdmin.
- **BR-011**: Deactivated users cannot login.
- **BR-012**: Users without permission cannot access protected screens or execute protected actions.
- **BR-013**: User account data and player game data must remain separate.
- **BR-014**: Discord integration must remain optional until implemented and cannot block local login.

### Key Entities

- **Usuário**: account used for authentication, session, authorization, status, profile data and administrative control.
- **Role**: named access role assigned to users, with hierarchical level for administrative decisions.
- **Sessão/Refresh Token**: persistent session record that allows renewal and revocation.
- **Vínculo Discord**: optional future link between a user account and a Discord identity.
- **Jogador**: game profile used by drafts, matches, routes, Riot ID, elo and player status.
- **Auditoria de Usuário**: record of sensitive user/account events and administrative changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid public registrations create an active account with role `Jogador`.
- **SC-002**: 100% of duplicate e-mail registration attempts are rejected.
- **SC-003**: Active users with valid credentials can login and access protected areas in normal usage.
- **SC-004**: Deactivated users are blocked from login in 100% of tested cases.
- **SC-005**: Users without required permission are blocked from protected user management actions in 100% of tested cases.
- **SC-006**: SuperAdmin can perform all allowed user management actions while preserving at least one active SuperAdmin.
- **SC-007**: Admin can manage Moderador, Capitão and Jogador users, and cannot manage Admin or SuperAdmin users.
- **SC-008**: Role changes, user activation/deactivation and administrative password reset are audit-visible after completion.
- **SC-009**: The user management screen allows authorized users to find a user by name or e-mail in under 30 seconds with up to 100 registered users.
- **SC-010**: Draft captain selection excludes every user without explicit role `Capitão`.
- **SC-011**: A user can recover or redefine password without exposing whether an e-mail exists.
- **SC-012**: The primary authentication flow works without Discord integration.

## UI / UX Requirements

- Login, cadastro, recuperação de senha e redefinição de senha MUST follow the dark-first design system.
- Forms MUST use vertical layout and may use at most two columns on desktop.
- Primary actions MUST use the documented primary color; destructive confirmations MUST use the danger style.
- Loading states SHOULD use skeletons where the content area is known.
- Empty states in user management MUST show friendly message and a clear action when applicable.
- User details and quick edits SHOULD use drawer components.
- Confirmation for activation, deactivation and administrative reset MUST use modal dialogs.
- The sidebar MUST hide administrative entries when the user lacks permission.
- Forbidden access MUST show a clear access-denied state and a safe navigation option.
- Expired session MUST show a clear message and redirect to login after failed renewal.
- Discord actions SHOULD appear as a future-ready section in profile only when enabled by configuration or clearly marked as unavailable.

## Security Requirements

- Passwords, tokens and secrets MUST never be logged or exposed in responses.
- Passwords MUST be stored only as secure hashes.
- Refresh sessions MUST be revocable.
- Session renewal MUST support token rotation and reuse detection.
- Login and password recovery MUST avoid e-mail enumeration.
- Login, registration, password recovery and session renewal SHOULD be rate-limited.
- User deactivation MUST revoke existing sessions.
- Administrative operations MUST be audited.
- CORS MUST remain restricted to approved frontend origins.
- Cookies or token storage choices MUST prioritize protection against token theft.

## Assumptions

- Esta feature será executada antes de qualquer integração real com Discord.
- O sistema continua sendo de uso interno, mas autenticação e autorização devem seguir boas práticas modernas.
- Usuários legados/jogadores existentes poderão ser vinculados gradualmente.
- Um SuperAdmin inicial será criado por processo seguro durante a implementação.
- E-mail transacional real pode ser configurado em etapa posterior, mas o fluxo de recuperação deve estar previsto.
- Backend é a fonte de verdade para autorização; frontend apenas melhora a experiência ocultando ações indisponíveis.
- A role `Capitão` representa elegibilidade de jogo e não é herdada automaticamente de roles administrativas.

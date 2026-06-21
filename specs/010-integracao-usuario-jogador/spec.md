# Feature Specification: Integração Usuário-Jogador

**Feature Branch**: `feature/010-integracao-usuario-jogador`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "Cadastra primeiro, ele acessa a plataforma e completa o perfil com todos os parametros obrigatorios"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Completar Perfil de Jogador Após Cadastro (Priority: P1)

Como usuário recém-cadastrado com role Jogador, quero acessar a plataforma e completar meu perfil de jogador com todas as informações obrigatórias para que eu possa aparecer na lista de jogadores e participar dos drafts.

**Why this priority**: Sem este fluxo, a comunidade consegue criar conta, mas não entra no conjunto de jogadores usado pelas funcionalidades centrais do produto.

**Independent Test**: Pode ser testado cadastrando uma nova conta, fazendo login, completando o perfil obrigatório e verificando que o usuário passa a ter jogador vinculado e aparece como jogador ativo.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado sem jogador vinculado, **When** ele acessa a plataforma, **Then** o sistema mostra uma chamada clara para completar o perfil de jogador.
2. **Given** um usuário autenticado sem jogador vinculado, **When** ele envia todos os dados obrigatórios válidos do perfil de jogador, **Then** o sistema cria um jogador ativo vinculado à conta do usuário.
3. **Given** um usuário com perfil de jogador completo, **When** ele acessa seu perfil ou a listagem de jogadores, **Then** o sistema exibe o vínculo e os dados de jogador preenchidos.
4. **Given** um usuário com perfil de jogador completo, **When** um organizador cria um draft, **Then** esse jogador está disponível para seleção conforme as regras de status e elegibilidade existentes.

---

### User Story 2 - Impedir Perfil de Jogador Incompleto (Priority: P2)

Como organizador, quero que apenas usuários com perfil de jogador completo apareçam no pool de jogadores para evitar draft com dados insuficientes de rota, elo ou conta de jogo.

**Why this priority**: Drafts dependem de dados completos para montar times de forma confiável e transparente.

**Independent Test**: Pode ser testado com uma conta recém-criada sem perfil completo e confirmando que ela não aparece na lista de jogadores nem no draft até concluir o preenchimento obrigatório.

**Acceptance Scenarios**:

1. **Given** um usuário cadastrado sem perfil de jogador, **When** a listagem de jogadores é aberta, **Then** esse usuário não aparece como jogador.
2. **Given** um usuário cadastrado sem perfil de jogador, **When** a criação de draft lista jogadores disponíveis, **Then** esse usuário não é incluído.
3. **Given** um usuário tenta completar perfil sem uma informação obrigatória, **When** ele envia o formulário, **Then** o sistema rejeita o envio e mostra mensagens claras sobre o que falta corrigir.

---

### User Story 3 - Atualizar Próprio Perfil de Jogador (Priority: P3)

Como jogador com perfil já completo, quero atualizar meus dados de jogo quando meu Riot ID, elo, links ou preferências de rota mudarem para manter meus dados corretos para futuros drafts.

**Why this priority**: Dados de jogador mudam com frequência; permitir atualização reduz trabalho administrativo e mantém o draft útil.

**Independent Test**: Pode ser testado com um usuário que já possui jogador vinculado, editando seus dados e verificando que a listagem e drafts futuros usam as informações atualizadas.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado com jogador vinculado, **When** ele edita seus dados obrigatórios com valores válidos, **Then** o sistema atualiza o perfil de jogador vinculado.
2. **Given** um usuário autenticado com jogador vinculado, **When** ele altera preferências de rota mantendo as regras de cinco rotas e prioridades únicas, **Then** o sistema salva as novas preferências.
3. **Given** um usuário autenticado, **When** ele tenta editar jogador de outra conta, **Then** o sistema impede a alteração.

---

### User Story 4 - Administração Visualiza Vínculo Usuário-Jogador (Priority: P4)

Como administrador, quero identificar rapidamente quais usuários já completaram perfil de jogador para orientar membros pendentes e entender quem pode participar de drafts.

**Why this priority**: A comunidade pode ter usuários cadastrados que ainda não viraram jogadores; administradores precisam enxergar esse estado sem consultar dados técnicos.

**Independent Test**: Pode ser testado abrindo a administração de usuários e confirmando que usuários com e sem jogador vinculado têm estados visuais distintos.

**Acceptance Scenarios**:

1. **Given** a lista administrativa de usuários, **When** usuários possuem ou não jogador vinculado, **Then** o sistema mostra claramente o status de perfil de jogador.
2. **Given** um administrador abre detalhes de um usuário, **When** esse usuário possui jogador vinculado, **Then** o sistema mostra o identificador ou resumo do jogador associado.
3. **Given** um administrador abre detalhes de um usuário sem jogador vinculado, **When** visualiza o status, **Then** o sistema mostra que o perfil de jogador está pendente.

### Edge Cases

- Usuário recém-cadastrado fecha o onboarding sem completar o perfil de jogador.
- Usuário tenta completar perfil de jogador mais de uma vez.
- Usuário informa Riot ID, Discord, OP.GG ou DeepLOL já usado por outro jogador, quando houver restrição de unicidade aplicável.
- Usuário marca mais de uma rota como “não jogo nem lascando”.
- Usuário envia prioridades de rota repetidas, fora do intervalo ou com rota faltando.
- Usuário informa URLs inválidas ou que não usam formato seguro aceito.
- Usuário desativado possui jogador vinculado; o jogador não deve ser tratado como participante válido de novos fluxos que dependam de usuário ativo.
- Jogadores legados sem usuário vinculado continuam existindo e podem ser administrados conforme regras já existentes.
- Usuário administrativo sem intenção de jogar pode permanecer sem jogador vinculado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a newly registered user to access the platform before completing a player profile.
- **FR-002**: System MUST clearly indicate when the current authenticated user does not have a linked player profile.
- **FR-003**: System MUST provide a completion flow for the authenticated user to create a linked player profile.
- **FR-004**: The player profile completion flow MUST require display name, Discord, Riot ID, OP.GG URL, DeepLOL URL, elo, division and route preferences.
- **FR-005**: The player profile completion flow MUST require preferences for exactly five League routes.
- **FR-006**: Route preferences MUST use unique priorities from 1 to 5.
- **FR-007**: Route preferences MUST allow at most one route marked as “não jogo nem lascando”.
- **FR-008**: System MUST reject incomplete or invalid player profile completion attempts with clear user-facing messages.
- **FR-009**: System MUST create a linked active player profile only for the authenticated user completing their own profile.
- **FR-010**: System MUST prevent a user from creating more than one linked player profile.
- **FR-011**: System MUST ensure that a linked player profile references exactly one user account.
- **FR-012**: System MUST keep existing legacy players without user account valid and manageable.
- **FR-013**: System MUST keep administrative users allowed to exist without player profile.
- **FR-014**: System MUST exclude users without completed player profile from the player list used by player-facing and draft flows.
- **FR-015**: System MUST include completed linked player profiles in player listing and draft selection according to existing active-player rules.
- **FR-016**: System MUST allow an authenticated user with a linked player profile to update their own player data.
- **FR-017**: System MUST prevent an authenticated user from completing, viewing privileged details of, or editing another user's player profile through the self-service flow.
- **FR-018**: System MUST update the authenticated profile state after player profile completion so the platform recognizes the new linked player.
- **FR-019**: System MUST show administrators whether each user has a completed linked player profile.
- **FR-020**: System MUST show user details with the linked player status and summary when available.
- **FR-021**: System MUST preserve existing role and captain eligibility rules; completing a player profile does not grant captain eligibility without explicit captain role.
- **FR-022**: System MUST provide clear empty, pending and success states for the player profile completion experience.
- **FR-023**: System MUST validate player profile data consistently whether created by self-service completion or administrative player creation.
- **FR-024**: System MUST keep important player mutations protected by authentication and server-side authorization.

### Key Entities *(include if feature involves data)*

- **Usuário**: authenticated account that may or may not have completed a linked player profile.
- **Jogador**: game profile containing display name, Discord, Riot ID, external profile links, elo, division, status and route preferences.
- **Perfil de Jogador Pendente**: user state where an authenticated account exists but no linked player profile has been completed.
- **Preferência de Rota**: ordered preference for each of the five League routes, including optional “não jogo nem lascando” marker.
- **Vínculo Usuário-Jogador**: one-to-one association that makes a completed player profile belong to exactly one user account.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of newly registered users without player profile see a clear path to complete their player profile after login.
- **SC-002**: 100% of valid player profile completion submissions create exactly one active player linked to the authenticated user.
- **SC-003**: 100% of users without completed player profile are absent from draft player selection until completion.
- **SC-004**: 100% of completed linked player profiles appear in the player list within one page refresh or navigation cycle.
- **SC-005**: 100% of invalid route preference submissions are rejected with messages that identify the invalid fields.
- **SC-006**: Users can complete the player profile flow in under 5 minutes when they already know their Riot ID and external profile links.
- **SC-007**: Administrators can identify whether a listed user has a linked player profile in under 10 seconds.
- **SC-008**: Existing legacy players without linked user remain visible and manageable after the feature is released.

## Assumptions

- Public registration remains lightweight and creates only the user account initially.
- Completing a player profile is required before a user becomes available as a player for drafts.
- All player profile fields listed in the requirements are mandatory for the self-service completion flow.
- Users may access basic platform areas while their player profile is pending, but they are not considered players for draft participation until completion.
- Existing users without player profile will use the same completion flow as newly registered users.
- Existing administrative player creation continues to support legacy/manual management flows.
- Discord integration remains manual text entry for this feature; no Discord OAuth is required.
- Riot integration remains manual text entry for this feature; no Riot API lookup is required.

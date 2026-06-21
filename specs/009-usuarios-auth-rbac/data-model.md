# Data Model: Usuários, Autenticação, Autorização e RBAC

## Usuario

Conta autenticável do sistema, baseada em Identity customizado.

**Fields**:

- `Id`: UUID.
- `Nome`: nome exibido do usuário, obrigatório, máximo 120.
- `Email`: e-mail do usuário, obrigatório, único.
- `NormalizedEmail`: e-mail normalizado para busca/unique.
- `UserName`: nome de usuário técnico, recomendado usar e-mail.
- `NormalizedUserName`: username normalizado.
- `PasswordHash`: hash de senha para conta local.
- `EmailConfirmed`: indica se e-mail foi confirmado.
- `Ativo`: indica se usuário pode autenticar.
- `UltimoLoginEm`: data/hora do último login bem-sucedido.
- `DataCadastro`: data/hora de criação.
- `DataAtualizacao`: data/hora de última alteração.
- `SecurityStamp`: versão de segurança para invalidação.
- `ConcurrencyStamp`: controle de concorrência Identity.

**Relationships**:

- Possui muitas roles via `UsuarioRole`.
- Pode possuir um `Jogador` vinculado.
- Pode possuir zero ou um `VinculoDiscord` ativo.
- Possui muitas `RefreshToken`.
- Pode aparecer como executor ou alvo em `AuditoriaUsuario`.

**Validation**:

- E-mail deve ser único.
- Usuário desativado não pode autenticar.
- Nome é obrigatório.
- Último SuperAdmin ativo não pode ser desativado ou rebaixado.

## Role

Papel de acesso atribuído aos usuários.

**Seed values**:

- `SuperAdmin`, nível 500.
- `Admin`, nível 400.
- `Moderador`, nível 300.
- `Capitão`, nível 200.
- `Jogador`, nível 100.

**Validation**:

- Nome normalizado único.
- Roles oficiais devem existir antes de cadastro público.

## UsuarioRole

Relacionamento many-to-many entre usuários e roles.

**Fields**:

- `UsuarioId`: UUID.
- `RoleId`: UUID.

**Validation**:

- Combinação usuário/role deve ser única.
- Alterações devem respeitar hierarquia do ator.

## RefreshToken

Sessão renovável e revogável do usuário.

**Fields**:

- `Id`: UUID.
- `UsuarioId`: UUID.
- `TokenHash`: hash do token, obrigatório.
- `FamiliaId`: UUID para agrupar rotações.
- `CriadoEm`: data/hora de criação.
- `ExpiraEm`: data/hora de expiração.
- `RevogadoEm`: data/hora de revogação opcional.
- `SubstituidoPorTokenId`: token posterior quando rotacionado.
- `IpCriacao`: IP de criação opcional.
- `UserAgentCriacao`: user agent opcional.
- `IpRevogacao`: IP de revogação opcional.
- `MotivoRevogacao`: logout, rotação, reutilização, desativação ou expiração.

**Validation**:

- Token puro nunca deve ser persistido.
- Token expirado ou revogado não pode renovar sessão.
- Reutilização de token rotacionado deve revogar a família.

## VinculoDiscord

Vínculo futuro entre conta local e identidade Discord.

**Fields**:

- `Id`: UUID.
- `UsuarioId`: UUID.
- `DiscordUserId`: ID estável do Discord, obrigatório quando vinculado.
- `DiscordUsername`: username atual opcional.
- `DiscordGlobalName`: nome global opcional.
- `DiscordAvatarHash`: avatar opcional.
- `VinculadoEm`: data/hora do vínculo.
- `DesvinculadoEm`: data/hora opcional de desvinculação.
- `Escopos`: escopos autorizados, opcional.

**Validation**:

- Um Discord ativo pertence a no máximo um usuário.
- Um usuário possui no máximo um vínculo Discord ativo.
- Desvinculação deve preservar histórico.

## Jogador

Perfil de jogo já existente, evoluído para vínculo opcional com usuário.

**New/changed fields**:

- `UsuarioId`: UUID opcional.
- `Status`: deve suportar diferenciação entre jogador ativo/inativo e perfil pendente, se aprovado na implementação.
- `Discord`: campo textual legado deve ser opcional para não conflitar com `VinculoDiscord`.

**Relationships**:

- Opcionalmente pertence a um `Usuario`.
- Continua sendo usado por times, drafts, montagens e preferências de rota.

**Validation**:

- `UsuarioId` deve ser único quando informado.
- Jogadores legados podem continuar sem usuário.
- Capitão elegível precisa ser jogador ativo vinculado a usuário com role explícita `Capitão`.

## AuditoriaUsuario

Registro de ações sensíveis de autenticação, sessão e administração.

**Fields**:

- `Id`: UUID.
- `UsuarioAlvoId`: usuário afetado, opcional.
- `UsuarioExecutorId`: usuário executor, opcional para ações públicas/login.
- `Acao`: tipo da ação, obrigatório.
- `Detalhes`: dados não sensíveis da mudança.
- `Ip`: IP opcional.
- `UserAgent`: user agent opcional.
- `DataCadastro`: data/hora do evento.

**Actions**:

- `UsuarioCriado`.
- `LoginSucesso`.
- `LoginFalha`.
- `Logout`.
- `RefreshTokenRotacionado`.
- `RefreshTokenReutilizado`.
- `SenhaAlterada`.
- `SenhaRedefinida`.
- `SenhaRedefinidaPorAdmin`.
- `UsuarioAtivado`.
- `UsuarioDesativado`.
- `RolesAlteradas`.
- `DiscordVinculado`.
- `DiscordDesvinculado`.

## Indexes and Constraints

- Unique `usuarios.normalized_email`.
- Unique `usuarios.normalized_user_name`.
- Index `usuarios.ativo`.
- Index `usuarios.ultimo_login_em`.
- Unique `roles.normalized_name`.
- PK composta em `usuario_roles(usuario_id, role_id)`.
- Unique `refresh_tokens.token_hash`.
- Index `refresh_tokens.usuario_id`.
- Index `refresh_tokens.familia_id`.
- Index `refresh_tokens.expira_em`.
- Unique filtrado `vinculos_discord.discord_user_id` para vínculo ativo.
- Unique filtrado `vinculos_discord.usuario_id` para vínculo ativo.
- Unique filtrado `jogadores.usuario_id` quando não nulo.
- Index `auditoria_usuarios.usuario_alvo_id`.
- Index `auditoria_usuarios.usuario_executor_id`.
- Index `auditoria_usuarios.data_cadastro`.

## State Transitions

### Usuario

- `Ativo` → `Desativado`: permitido por SuperAdmin ou Admin com hierarquia válida; revoga sessões.
- `Desativado` → `Ativo`: permitido por SuperAdmin ou Admin com hierarquia válida.
- Role set atual → novo role set: permitido somente quando ator pode administrar todas as roles afetadas.

### RefreshToken

- `Ativo` → `Rotacionado`: quando usado com sucesso em refresh.
- `Ativo` → `Revogado`: logout, desativação ou ação administrativa.
- `Rotacionado` reutilizado → revogar família completa.

### VinculoDiscord

- `NaoVinculado` → `Vinculado`: futuro OAuth válido e sem duplicidade.
- `Vinculado` → `Desvinculado`: ação do usuário autenticado ou administrativa futura.

# Research: Usuários, Autenticação, Autorização e RBAC

## Decision: Usar ASP.NET Core Identity com usuário customizado por UUID

**Rationale**: Identity entrega hashing seguro de senha, validação de senha, reset token, lockout, roles, claims e integração natural com ASP.NET Core. Um usuário customizado por UUID mantém o padrão do projeto de chaves UUID e permite adicionar `Nome`, `Ativo`, `UltimoLoginEm` e auditoria sem criar autenticação própria.

**Alternatives considered**:

- Autenticação manual com BCrypt: rejeitada por aumentar risco e recriar funcionalidades maduras de Identity.
- Identity sem customização: rejeitada porque o projeto exige UUID, campos próprios e snake_case.
- Provedor externo obrigatório: rejeitado porque Discord não deve travar o MVP.

## Decision: Access token JWT curto + refresh token rotacionável em cookie HttpOnly

**Rationale**: JWT curto simplifica proteção da API SPA. Refresh token HttpOnly reduz exposição a XSS quando comparado a armazenar refresh token em `localStorage`. Rotação e detecção de reutilização permitem revogar família de sessão em caso de token roubado.

**Alternatives considered**:

- Apenas cookie de autenticação: bom para apps server-rendered, mas menos alinhado com SPA/API e interceptors atuais.
- Guardar ambos os tokens no `localStorage`: rejeitado por risco maior de roubo via XSS.
- Refresh token sem persistência: rejeitado porque logout, revogação e reutilização não seriam controláveis.

## Decision: Roles múltiplas com role efetiva administrativa

**Rationale**: Usuário pode ser Admin e Capitão ao mesmo tempo. A hierarquia administrativa usa a maior role efetiva, mas elegibilidade de capitão exige role explícita `Capitão`, evitando confundir poder administrativo com papel no jogo.

**Alternatives considered**:

- Role única por usuário: rejeitada porque Admin/SuperAdmin também podem jogar.
- Hierarquia herdando Capitão automaticamente: rejeitada porque a spec exige somente role `Capitão` na seleção.

## Decision: Usuário e Jogador separados

**Rationale**: Usuário representa autenticação/autorização; Jogador representa dados de jogo. A separação preserva drafts, partidas e jogadores legados, e permite usuários administrativos sem perfil de jogador.

**Alternatives considered**:

- Mesclar usuário e jogador: rejeitada por acoplar autenticação ao domínio de jogo e dificultar administradores não jogadores.
- Exigir usuário para todo jogador imediatamente: rejeitada por risco de quebrar dados legados.

## Decision: Discord como vínculo futuro opcional

**Rationale**: A Constituição exige que integrações não travem o produto. Modelar `VinculoDiscord` agora reduz retrabalho e prepara login/vínculo/bot, mas OAuth real fica fora desta entrega.

**Alternatives considered**:

- Implementar Discord OAuth agora: rejeitada por ampliar escopo e depender de configuração externa.
- Ignorar Discord no modelo: rejeitada porque a feature explicitamente exige preparação futura.

## Decision: Auditoria simples e relacional

**Rationale**: Alterações sensíveis precisam rastreabilidade. Uma tabela de auditoria com ação, executor, alvo, data, IP e detalhes permite investigação sem criar sistema de logs complexo.

**Alternatives considered**:

- Somente logs técnicos: rejeitada porque logs podem rotacionar e não são adequados para tela administrativa.
- Event sourcing completo: rejeitado por sobreengenharia para uso interno.

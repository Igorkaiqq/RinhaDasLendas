# Research: Integração Usuário-Jogador

## Decision: Cadastro Público Continua Criando Apenas Usuário

**Rationale**: O usuário pediu explicitamente que a conta seja criada primeiro e que a pessoa complete o perfil depois de acessar a plataforma. Isso reduz atrito inicial, mantém o fluxo de autenticação simples e evita misturar cadastro de credenciais com dados detalhados de jogo.

**Alternatives considered**:
- Criar usuário e jogador no cadastro: rejeitado porque deixa o cadastro pesado e contraria a decisão do usuário.
- Bloquear login até completar perfil: rejeitado porque o usuário deve acessar a plataforma antes de completar dados.

## Decision: Perfil de Jogador Completo Como Pré-Requisito Para Draft

**Rationale**: Draft depende de rotas, elo e identificação de jogo. Usuários sem jogador vinculado não devem aparecer como jogadores, evitando participantes incompletos no pool.

**Alternatives considered**:
- Criar jogador pendente parcialmente preenchido: rejeitado porque polui listagem/draft e exige novo estado de domínio.
- Permitir draft com dados incompletos: rejeitado porque enfraquece a qualidade do draft.

## Decision: Reaproveitar Regras Existentes de Jogador

**Rationale**: Cadastro administrativo de jogador já possui validações de campos e preferências de rota. O self-service deve obedecer às mesmas invariantes para evitar divergência entre jogador criado por admin e jogador criado pelo próprio usuário.

**Alternatives considered**:
- Criar validações separadas para onboarding: rejeitado por duplicação e risco de comportamento inconsistente.
- Validar somente no frontend: rejeitado porque o backend deve ser fonte de verdade.

## Decision: Usar `jogadores.usuario_id` Como Associação Única

**Rationale**: A feature 09 já introduziu `UsuarioId` opcional em jogador e índice único filtrado. A feature 10 deve utilizar esse vínculo existente, sem criar tabelas novas desnecessárias.

**Alternatives considered**:
- Criar tabela intermediária usuário-jogador: rejeitado porque a relação é 1:1 e o campo existente já modela a regra.
- Fundir usuário e jogador: rejeitado porque a feature 09 definiu explicitamente conceitos separados.

## Decision: Incluir Edição Própria do Perfil de Jogador

**Rationale**: Riot ID, elo, links e preferências podem mudar. Sem edição própria, admins precisariam manter dados de todos os membros, aumentando atrito operacional.

**Alternatives considered**:
- Apenas completar uma vez: rejeitado porque dados de jogo mudam com frequência.
- Apenas admin edita: rejeitado por carga administrativa desnecessária.

## Decision: Administração Apenas Visualiza Estado do Vínculo Nesta Feature

**Rationale**: O problema principal é permitir que a comunidade se transforme em jogadores para draft. A tela administrativa deve indicar o estado, mas ações avançadas de vincular/desvincular manualmente podem ficar para evolução posterior.

**Alternatives considered**:
- Criar fluxo administrativo completo de vincular/desvincular: rejeitado por aumentar escopo sem ser necessário para o fluxo principal.
- Não alterar admin: rejeitado porque admins precisam identificar usuários pendentes.

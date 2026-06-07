# Feature Specification: Cadastro de Jogadores e Preferências de Rotas

**Feature Branch**: `003-cadastro-jogadores-rotas`

**Created**: 2026-06-06

**Status**: Draft

**Input**: User description: "Permitir o cadastro e gerenciamento dos jogadores participantes das partidas internas do grupo de League of Legends, incluindo preferências de rotas, rota bloqueada, status ativo/inativo e consulta de perfil."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar jogador ativo (Priority: P1)
Um organizador do grupo cadastra um jogador com nome obrigatório e informações opcionais de conta, elo e links de perfil.

**Why this priority**: Sem uma lista de jogadores confiável, o sistema não pode suportar filas, drafts ou registro de partidas.

**Independent Test**: Criar um jogador ativo via formulário e verificar que todos campos obrigatórios e opcionais são salvos corretamente.

**Acceptance Scenarios**:

1. **Given** que um usuário acessa a página de cadastro, **When** informa nome e demais dados, **Then** o jogador é criado com status ativo.
2. **Given** que o jogador é criado, **When** o usuário consulta a lista, **Then** o jogador aparece com nome, elo e status.

---

### User Story 2 - Atualizar preferências de rotas (Priority: P1)
Um jogador define a ordem de preferência de rotas e pode marcar uma rota como "não jogo nem lascando".

**Why this priority**: Preferências de rota são a base para filas, sorteios e formação de times futuras.

**Independent Test**: Atualizar as prioridades e rota bloqueada e verificar que as regras de unicidade e consistência são aplicadas.

**Acceptance Scenarios**:

1. **Given** um jogador cadastrado, **When** atualiza as prioridades para todos os cinco papéis, **Then** a nova ordem é salva.
2. **Given** um jogador configurado, **When** marca uma rota como bloqueada, **Then** o sistema aceita no máximo uma rota bloqueada.

---

### User Story 3 - Inativar e consultar jogadores (Priority: P2)
Um organizador marca jogadores como inativos e consulta a lista de jogadores com status e preferências.

**Why this priority**: Jogadores inativos devem ser mantidos no histórico, mas excluídos de novas filas e drafts.

**Independent Test**: Marcar um jogador como inativo e verificar que ele permanece nos registros, mas não é exibido como participante ativo.

**Acceptance Scenarios**:

1. **Given** um jogador ativo, **When** o usuário altera o status para inativo, **Then** o jogador permanece no sistema com status inativo.
2. **Given** a lista de jogadores, **When** o usuário visualiza a tabela, **Then** vê nome, elo, rotas preferidas, rota bloqueada e status.

---

### Edge Cases

- O que acontece quando um jogador tenta salvar prioridades de rota com valores duplicados?
- Como o sistema responde se um link de OP.GG ou Deeplol inválido for informado?
- Como tratar jogadores inativos que já existiam em partidas anteriores?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir criar um jogador com nome obrigatório.
- **FR-002**: O sistema MUST permitir editar qualquer informação do jogador após cadastro.
- **FR-003**: O sistema MUST validar que cada prioridade de rota é única e que existem exatamente cinco prioridades válidas.
- **FR-004**: O sistema MUST permitir marcar no máximo uma rota como "não jogo nem lascando" e validar essa regra.
- **FR-005**: O sistema MUST permitir consultar jogadores com nome, elo, rotas preferidas, rota bloqueada e status.
- **FR-006**: O sistema MUST permitir marcar jogadores como inativos sem excluí-los fisicamente.
- **FR-007**: Jogadores inativos MUST ser excluídos de filas e drafts futuros.
- **FR-008**: Links de OP.GG e Deeplol MUST ser validados quando fornecidos.
- **FR-009**: O sistema MUST salvar o status ativo/inativo e permitir a reversão de inativação.
- **FR-010**: O sistema MUST responder com mensagens claras para erros de validação.

### Key Entities *(include if feature involves data)*

- **Jogador**: representa um participante, com nome de exibição obrigatório, nome real opcional, conta Discord opcional, Riot ID opcional, links de perfil, elo informado e status ativo/inativo.
- **Preferência de Rota**: representa a ordem de prioridade das cinco rotas (Top, Jungle, Mid, ADC, Support) para cada jogador.
- **Rota Bloqueada**: representa a rota marcada como "não jogo nem lascando" por um jogador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Jogadores podem ser criados e visualizados corretamente em 100% dos casos de teste definidos.
- **SC-002**: Prioridades de rota podem ser atualizadas sem duplicações e com exatamente cinco valores válidos.
- **SC-003**: O sistema rejeita entradas inválidas de rota bloqueada em 100% dos casos de teste relacionados.
- **SC-004**: Jogadores inativos permanecem registrados e não aparecem em novas filas ou drafts em 100% dos casos aplicáveis.
- **SC-005**: A consulta de jogadores exibe nome, elo, rotas preferidas, rota bloqueada e status para todos os registros retornados.
- **SC-006**: Erros de validação são apresentados em linguagem clara e compreensível para o usuário.

## UI / UX Requirements

- The frontend MUST follow the design system's dark-first approach and use the
  established brand colors, surface palettes, semantic colors and typography.
- The player directory MUST prioritize cards over dense tables for the main list
  view, and use table layouts only for ranking/historical/statistics screens.
- The player card/list view MUST show avatar, name, elo, main route, blocked
  route badge and active/inactive status using the designated role and status
  colors.
- Editing player details SHOULD use a drawer interface for quick updates, and
  confirmation actions such as inactivation SHOULD use modal dialogs only.
- Forms MUST use a vertical layout and may use up to two columns on desktop;
  mobile MUST collapse to a single column.
- Primary actions MUST use `primary`; secondary actions should use `surface-2`;
  destructive actions should use `danger`.
- Loading states for player list and forms MUST favor skeletons over spinners.
- Empty states MUST provide a friendly message and a clear CTA to add a new
  player.
- The page MUST be responsive: desktop-first, tablet in two columns where useful,
  mobile in one column with a collapsible navigation layout.
- The interface MUST keep the information hierarchy: data first, actions second,
  navigation third, decoration last.

## Assumptions

- Este recurso será implementado antes de integrações com Discord ou Riot API.
- O cadastro de jogador serve como base para funcionalidades futuras de fila, draft e partidas.
- Links de OP.GG e Deeplol podem ser validados com regras gerais de formato de URL e domínio.
- Jogadores inativos permanecem no histórico do sistema, mas não participam de novos eventos.
- A interface de consulta é capaz de mostrar ao menos nome, elo, rotas, rota bloqueada e status em um único fluxo.

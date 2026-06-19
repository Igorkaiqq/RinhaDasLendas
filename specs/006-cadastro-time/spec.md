# Feature Specification: Cadastro de Time

**Feature Branch**: `feature/006-cadastro-time`

**Created**: 2026-06-10

**Status**: Draft

**Input**: User description: "Vamos implementar a etapa 6, que vai ser o cadastro de time."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar um time (Priority: P1)

Como organizador da RinhaDasLendas, quero cadastrar um time com identificacao clara e jogadores participantes, para preparar partidas, drafts e consultas futuras com times reutilizaveis.

**Why this priority**: Sem cadastro de times, a plataforma continua dependendo de combinacoes manuais em cada partida e nao consegue organizar equipes recorrentes.

**Independent Test**: Pode ser testado criando um time com nome, tag e jogadores validos e verificando se ele aparece na lista de times com status ativo e composicao correta.

**Acceptance Scenarios**:

1. **Given** que existem jogadores ativos cadastrados, **When** o organizador cria um time com nome, tag e pelo menos um jogador, **Then** o time e salvo como ativo e exibido na lista de times.
2. **Given** que o organizador esta cadastrando um time, **When** seleciona jogadores para a composicao, **Then** o sistema impede que o mesmo jogador seja adicionado mais de uma vez ao mesmo time.
3. **Given** que um time foi criado, **When** o organizador consulta sua ficha, **Then** ve nome, tag, status, capitao quando definido e jogadores vinculados.

---

### User Story 2 - Editar composicao e dados do time (Priority: P1)

Como organizador da rinha, quero alterar nome, tag, capitao e jogadores de um time existente, para manter a informacao atualizada conforme o grupo muda.

**Why this priority**: Times internos podem mudar de membros, nome ou lideranca; a gestao precisa acompanhar essas mudancas sem recriar registros.

**Independent Test**: Pode ser testado editando um time existente, trocando jogadores e capitao, e confirmando que a listagem e a ficha do time refletem os novos dados.

**Acceptance Scenarios**:

1. **Given** um time ativo existente, **When** o organizador altera nome ou tag para valores validos, **Then** o sistema atualiza os dados do time.
2. **Given** um time com jogadores vinculados, **When** o organizador define um capitao entre os membros do time, **Then** o capitao fica destacado na ficha e listagem quando aplicavel.
3. **Given** um time existente, **When** o organizador remove ou adiciona jogadores ativos respeitando os limites do time, **Then** a composicao e atualizada sem duplicidades.

---

### User Story 3 - Consultar e filtrar times (Priority: P2)

Como usuario autenticado, quero visualizar os times cadastrados e encontrar rapidamente um time por nome, tag, status ou jogador, para entender a organizacao atual da rinha.

**Why this priority**: A consulta torna o cadastro util para operacao diaria e prepara as proximas etapas de draft, partidas e estatisticas.

**Independent Test**: Pode ser testado cadastrando times com diferentes nomes, tags, jogadores e status, e confirmando que a lista apresenta e filtra os registros corretamente.

**Acceptance Scenarios**:

1. **Given** que existem times cadastrados, **When** o usuario acessa a tela de Times, **Then** ve uma lista com nome, tag, status, quantidade de jogadores e principais membros.
2. **Given** varios times cadastrados, **When** o usuario busca por nome, tag ou jogador, **Then** a lista mostra apenas os times correspondentes.
3. **Given** times ativos e inativos, **When** o usuario filtra por status, **Then** a lista apresenta somente os times do status selecionado.

---

### User Story 4 - Inativar ou excluir time (Priority: P3)

Como organizador da rinha, quero retirar um time de uso sem perder historico relevante, para impedir que times descontinuados aparecam em novos fluxos de organizacao.

**Why this priority**: Times antigos nao devem atrapalhar novas partidas ou drafts, mas podem precisar continuar visiveis em historicos futuros.

**Independent Test**: Pode ser testado inativando um time e verificando que ele permanece consultavel, mas deixa de aparecer como opcao ativa para novos usos.

**Acceptance Scenarios**:

1. **Given** um time ativo, **When** o organizador confirma a inativacao, **Then** o time passa para status inativo e permanece consultavel.
2. **Given** um time inativo, **When** o organizador reativa o time, **Then** ele volta a aparecer como opcao ativa.
3. **Given** uma solicitacao de exclusao de time, **When** o time possui historico associado, **Then** o sistema preserva o historico e orienta o uso de inativacao.

### Edge Cases

- O que acontece quando o nome ou tag informados ja pertencem a outro time ativo?
- Como o sistema responde quando o usuario tenta cadastrar um time sem nome ou sem tag?
- Como tratar tentativa de adicionar jogador inativo a um time ativo?
- Como tratar tentativa de selecionar mais jogadores do que o limite permitido para um time de partida?
- Como o sistema se comporta quando o capitao selecionado e removido da composicao?
- Como a tela comunica estado vazio quando ainda nao existe nenhum time cadastrado?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir cadastrar um time com nome obrigatorio e tag obrigatoria.
- **FR-002**: O sistema MUST permitir informar descricao ou observacoes opcionais para um time.
- **FR-003**: O sistema MUST permitir vincular jogadores ativos existentes a um time.
- **FR-004**: O sistema MUST impedir que o mesmo jogador seja vinculado mais de uma vez ao mesmo time.
- **FR-005**: O sistema MUST permitir definir um capitao do time somente entre jogadores vinculados ao proprio time.
- **FR-006**: O sistema MUST remover a marcacao de capitao ou solicitar nova escolha quando o capitao atual deixar a composicao do time.
- **FR-007**: O sistema MUST validar que nome e tag de time ativo nao dupliquem outro time ativo.
- **FR-008**: O sistema MUST limitar a composicao principal do time a no maximo cinco jogadores para uso em partidas padrao.
- **FR-009**: O sistema MUST permitir editar nome, tag, observacoes, capitao e composicao de um time existente.
- **FR-010**: O sistema MUST permitir consultar times com nome, tag, status, quantidade de jogadores, capitao e membros vinculados.
- **FR-011**: O sistema MUST permitir buscar times por nome, tag ou jogador vinculado.
- **FR-012**: O sistema MUST permitir filtrar times por status ativo ou inativo.
- **FR-013**: O sistema MUST permitir inativar um time sem apagar seu registro.
- **FR-014**: O sistema MUST permitir reativar um time inativo quando seus dados obrigatorios permanecerem validos.
- **FR-015**: O sistema MUST impedir que times inativos sejam oferecidos como opcao principal em novos fluxos de partida ou draft.
- **FR-016**: O sistema MUST apresentar confirmacao antes de inativar ou excluir um time.
- **FR-017**: O sistema MUST apresentar mensagens claras para erros de validacao e operacoes sem sucesso.
- **FR-018**: O sistema MUST apresentar estado vazio com acao clara para cadastrar o primeiro time.

### Key Entities *(include if feature involves data)*

- **Time**: representa uma equipe cadastrada na plataforma; possui nome, tag, status, observacoes opcionais, capitao opcional e jogadores vinculados.
- **Membro do Time**: representa o vinculo entre um jogador e um time, incluindo participacao na composicao principal e elegibilidade para ser capitao.
- **Jogador**: participante previamente cadastrado que pode ser vinculado a um time quando estiver ativo.
- **Capitao**: jogador vinculado ao time que atua como lideranca operacional para futuras etapas de draft e partidas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um organizador consegue cadastrar um time valido em ate 2 minutos durante uma sessao de uso.
- **SC-002**: 100% das tentativas de criar times com nome ou tag duplicados entre times ativos sao bloqueadas com mensagem compreensivel.
- **SC-003**: 100% das tentativas de adicionar jogador duplicado ao mesmo time sao bloqueadas antes da conclusao do cadastro ou edicao.
- **SC-004**: Um usuario consegue encontrar um time por nome, tag ou jogador em ate 10 segundos quando o registro existe.
- **SC-005**: Times inativos deixam de aparecer em fluxos principais de novos drafts ou partidas em 100% dos casos aplicaveis.
- **SC-006**: Pelo menos 90% dos usuarios internos conseguem identificar capitao, status e composicao de um time sem orientacao tecnica adicional.

## Assumptions

- O cadastro de jogadores ja existe e fornece a base de jogadores disponiveis para composicao dos times.
- No MVP, um time padrao de partida possui ate cinco jogadores na composicao principal.
- Jogadores inativos nao devem ser adicionados a times ativos, salvo decisao explicita futura.
- A exclusao fisica de time fica fora do fluxo principal quando houver historico associado; inativacao e o comportamento padrao seguro.
- Times inativos permanecem consultaveis para historico e auditoria operacional.
- Integrações externas com Discord ou Riot API nao sao necessarias para cadastrar ou gerenciar times nesta etapa.

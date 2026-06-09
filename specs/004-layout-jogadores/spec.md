# Feature Specification: Layout Base e Gestao de Jogadores

**Feature Branch**: `004-layout-jogadores`

**Created**: 2026-06-09

**Status**: Draft

**Input**: User description: "Etapa 4 - Layout Base e Gestao de Jogadores. Implementar o layout principal da aplicacao RinhaDasLendas utilizando o design definido no Figma, incluindo sidebar lateral, topbar/menu superior, tela de Jogadores, estrutura reutilizavel para futuras telas, dados temporarios enquanto nao existir backend, responsividade desktop/tablet e fidelidade ao Figma como fonte de verdade."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navegar pelo layout principal (Priority: P1)

Como usuario autenticado do RinhaDasLendas, quero acessar as paginas protegidas por um layout consistente com menu lateral, barra superior e area central, para encontrar rapidamente as secoes principais da plataforma.

**Why this priority**: O layout compartilhado e a base visual para as proximas telas. Sem ele, cada pagina tende a evoluir com navegacao e experiencia inconsistentes.

**Independent Test**: Pode ser testado acessando qualquer pagina protegida existente e verificando se o usuario visualiza a sidebar, a topbar e o conteudo principal sem perder acesso as rotas atuais.

**Acceptance Scenarios**:

1. **Given** um usuario em uma pagina protegida, **When** a pagina e carregada, **Then** a sidebar fixa exibe Dashboard, Jogadores, Times, Draft, Partidas, Estatisticas e Configuracoes.
2. **Given** um usuario em uma pagina protegida, **When** a pagina e carregada, **Then** a topbar exibe o nome da aplicacao, avatar, informacoes basicas do usuario e acesso ao menu de perfil.
3. **Given** um usuario navegando pelas secoes disponiveis, **When** seleciona um item da sidebar, **Then** a area central muda para a secao selecionada ou preserva o comportamento existente da rota correspondente.

---

### User Story 2 - Consultar jogadores no novo padrao visual (Priority: P1)

Como organizador da rinha, quero visualizar a lista de jogadores no padrao visual definido para a aplicacao, para conferir rapidamente quem esta cadastrado e suas informacoes principais.

**Why this priority**: A tela de Jogadores e o principal fluxo afetado pela etapa e valida se o novo layout funciona com dados reais do dominio.

**Independent Test**: Pode ser testado abrindo a tela de Jogadores e confirmando que cada jogador apresenta nome, Discord, elo, rotas preferidas, link OP.GG e status de forma escaneavel.

**Acceptance Scenarios**:

1. **Given** que existem jogadores cadastrados nos dados temporarios, **When** o usuario acessa Jogadores, **Then** a lista exibe nome, Discord, elo, rotas preferidas, link OP.GG e status para cada jogador.
2. **Given** que nenhum jogador esta disponivel nos dados temporarios, **When** o usuario acessa Jogadores, **Then** a tela exibe um estado vazio amigavel com acao principal para cadastrar jogador.
3. **Given** um jogador com link OP.GG informado, **When** o usuario aciona o link, **Then** o sistema oferece acesso ao perfil externo sem esconder as informacoes da listagem.

---

### User Story 3 - Gerenciar jogadores temporariamente (Priority: P2)

Como organizador da rinha, quero criar, editar e excluir jogadores mesmo antes da integracao final com backend, para validar o fluxo de gestao e preparar a experiencia de uso.

**Why this priority**: O gerenciamento completa a tela de Jogadores e permite validar interacoes de cadastro antes de conectar a persistencia definitiva.

**Independent Test**: Pode ser testado criando um jogador, alterando suas informacoes e removendo-o da lista durante a sessao de uso.

**Acceptance Scenarios**:

1. **Given** que o usuario esta na tela de Jogadores, **When** cadastra um jogador com dados validos, **Then** o novo jogador aparece na lista com as informacoes informadas.
2. **Given** que um jogador ja aparece na lista, **When** o usuario edita seus dados, **Then** a listagem reflete os novos valores.
3. **Given** que um jogador ja aparece na lista, **When** o usuario confirma a exclusao, **Then** o jogador deixa de aparecer na listagem temporaria.
4. **Given** que o usuario tenta salvar dados obrigatorios ausentes ou invalidos, **When** confirma o formulario, **Then** o sistema informa claramente quais campos precisam de ajuste.

---

### User Story 4 - Reutilizar a estrutura em futuras telas (Priority: P3)

Como equipe do produto, quero que o layout principal possa ser reaproveitado pelas proximas areas da plataforma, para reduzir retrabalho e manter a experiencia consistente.

**Why this priority**: A etapa deve preparar a base para Times, Draft, Partidas, Estatisticas e Configuracoes sem exigir redesenho da navegacao.

**Independent Test**: Pode ser testado conectando uma pagina protegida simples ao layout compartilhado e verificando se ela recebe a mesma estrutura de navegacao, topbar e area de conteudo.

**Acceptance Scenarios**:

1. **Given** uma nova pagina protegida, **When** ela utiliza o layout compartilhado, **Then** sidebar, topbar e area central seguem o mesmo padrao das demais paginas.
2. **Given** uma pagina sem conteudo definitivo, **When** ela e acessada pelo menu, **Then** o usuario visualiza uma area central coerente com a navegacao sem quebrar o layout principal.

### Edge Cases

- Quando a largura da tela for de tablet, a navegacao e a listagem de jogadores devem permanecer utilizaveis sem sobreposicao de textos, botoes ou conteudo.
- Quando um item da sidebar apontar para uma area ainda sem tela definitiva, o sistema deve preservar a navegacao existente e evitar erro visual ou rota quebrada.
- Quando dados temporarios forem reiniciados, a tela deve continuar compreensivel por meio de estado vazio.
- Quando um jogador tiver muitas rotas preferidas ou textos maiores, a informacao deve continuar legivel sem quebrar a listagem.
- Quando uma acao destrutiva for solicitada, o usuario deve confirmar antes da exclusao ser aplicada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST apresentar uma sidebar fixa nas paginas protegidas com os itens Dashboard, Jogadores, Times, Draft, Partidas, Estatisticas e Configuracoes.
- **FR-002**: O sistema MUST indicar visualmente a secao ativa na sidebar quando o usuario estiver em uma pagina correspondente.
- **FR-003**: O sistema MUST apresentar uma topbar nas paginas protegidas com o nome da aplicacao, avatar do usuario, informacoes basicas do usuario e acesso ao menu de perfil.
- **FR-004**: O sistema MUST estruturar paginas protegidas com sidebar, topbar e area central de conteudo compartilhadas.
- **FR-005**: O sistema MUST manter as rotas existentes acessiveis e sem regressao de navegacao durante a introducao do layout compartilhado.
- **FR-006**: A tela de Jogadores MUST exibir lista de jogadores com nome, Discord, elo, rotas preferidas, link OP.GG e status.
- **FR-007**: A tela de Jogadores MUST permitir criar jogador com os dados necessarios para a listagem e para o dominio atual de jogadores.
- **FR-008**: A tela de Jogadores MUST permitir editar os dados exibidos de um jogador existente.
- **FR-009**: A tela de Jogadores MUST permitir excluir jogador mediante confirmacao do usuario.
- **FR-010**: O sistema MUST validar campos obrigatorios e formatos relevantes antes de aceitar cadastro ou edicao de jogador.
- **FR-011**: O sistema MUST utilizar dados temporarios enquanto a integracao definitiva de backend nao estiver disponivel.
- **FR-012**: O sistema MUST comunicar estados de carregamento, lista vazia, erro de operacao e sucesso de acao de forma clara para o usuario.
- **FR-013**: O sistema MUST oferecer responsividade minima para desktop e tablet na navegacao principal e na tela de Jogadores.
- **FR-014**: O visual da sidebar, topbar e tela de Jogadores MUST seguir o design aprovado no Figma como fonte de verdade para cores, tipografia, espacamentos, componentes e layout.
- **FR-015**: O sistema MUST evitar criar variacoes visuais que contrariem os tokens e diretrizes de design existentes sem decisao explicita posterior.
- **FR-016**: A estrutura de layout MUST ser reutilizavel por futuras paginas protegidas da aplicacao.

### Key Entities *(include if feature involves data)*

- **Jogador**: Pessoa cadastrada para participar das rinhas; possui nome, identificador ou usuario do Discord, elo, rotas preferidas, link OP.GG e status.
- **Preferencia de Rota**: Conjunto ordenado de rotas associadas a um jogador para representar suas preferencias de jogo.
- **Usuario da Sessao**: Pessoa autenticada que acessa a aplicacao; possui informacoes basicas exibidas na topbar, como nome, avatar e identificacao resumida.
- **Item de Navegacao**: Entrada do menu principal que representa uma area protegida do sistema e pode possuir estado ativo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das paginas protegidas existentes exibem sidebar, topbar e area central no mesmo padrao visual.
- **SC-002**: Um usuario consegue acessar a tela de Jogadores e identificar as informacoes principais de um jogador em ate 10 segundos.
- **SC-003**: Um usuario consegue criar, editar e excluir um jogador temporario em ate 2 minutos durante uma sessao de uso.
- **SC-004**: A tela de Jogadores permanece utilizavel em larguras de desktop e tablet sem sobreposicao visual perceptivel nos fluxos principais.
- **SC-005**: A comparacao visual com o design aprovado no Figma nao apresenta divergencias relevantes de navegacao, hierarquia, componentes principais, cores, tipografia ou espacamentos.
- **SC-006**: Nenhuma rota existente deixa de ser acessivel apos a introducao do layout compartilhado.
- **SC-007**: Pelo menos 90% das acoes principais da tela de Jogadores sao compreendidas por usuarios internos sem orientacao tecnica adicional.

## Assumptions

- A feature se aplica a paginas protegidas da aplicacao; paginas publicas ou de autenticacao ficam fora deste escopo.
- A etapa usa dados temporarios para experiencia de interface ate que a integracao definitiva com backend esteja disponivel.
- O design aprovado no Figma esta disponivel durante a fase de planejamento e sera usado como fonte de verdade visual antes da implementacao.
- A exclusao nesta etapa remove o jogador da lista temporaria; regras definitivas de inativacao ou auditoria ficam para integracao persistente posterior.
- O escopo de responsividade obrigatorio desta etapa cobre desktop e tablet; mobile pode ser preparado, mas nao e criterio principal de aceite desta fase.
- As secoes do menu que ainda nao tiverem funcionalidade completa podem exibir conteudo basico ou preservar rotas existentes, desde que a navegacao nao quebre.

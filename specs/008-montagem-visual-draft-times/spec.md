# Feature Specification: Montagem Visual de Draft e Times

**Feature Branch**: `feature/008-montagem-visual-draft-times`

**Created**: 2026-06-20

**Status**: Draft

**Input**: User description: "Integrar o prototipo simples em `exemplo/index.html` com a aplicacao principal, substituindo textarea manual por jogadores cadastrados, drag and drop entre jogadores livres, times dinamicos e reservas, detalhes de jogador, capitães, rotas e exportacao de imagem."

## Objetivo

Criar uma experiencia real de montagem visual de drafts/times baseada nos jogadores cadastrados no RinhaDasLendas, preservando a simplicidade do prototipo atual usado pelo grupo: cards arrastaveis, area central de jogadores livres, boxes de times e exportacao visual do resultado.

A melhoria deve eliminar a digitacao manual de nomes, permitir organizacao livre por drag and drop, suportar quantidade dinamica de times e reservas, e persistir o resultado para consulta posterior.

## Contexto

O arquivo `exemplo/index.html` representa o fluxo usado hoje fora da aplicacao principal. Ele possui:

- Textarea para digitar nomes manualmente, um por linha.
- Botao para transformar nomes em cards arrastaveis.
- Area central de `Jogadores Livres`.
- Quatro boxes fixos de times: azul, vermelho, amarelo e verde.
- Cards com seletor visual de rotas `TOP`, `JG`, `MID`, `ADC`, `SUP`.
- Drop zones que permitem mover jogadores entre area central e times.
- Exportacao da area de times para imagem.

A aplicacao principal ja possui:

- Cadastro real de jogadores com nome, Discord, Riot ID, OP.GG, Deeplol, elo, divisao, status e preferencias de rota.
- Tela de jogadores com filtros por nome, elo, rota primaria e status.
- Backend com endpoints de jogadores em `/api/v1/jogadores`.
- Fluxo de draft atual em `/api/v1/drafts`, orientado a dois times, capitaes A/B, picks alternados e historico.
- Tela `/drafts` com listagem, criacao, tabuleiro de dois times, disponiveis e historico.

Esta feature evolui o conceito atual de draft para uma montagem visual multi-times. Ela nao deve depender de Discord ou Riot API.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Selecionar Jogadores Cadastrados para Montagem (Priority: P1)

Como organizador, quero criar uma montagem de draft usando jogadores ja cadastrados para nao precisar digitar nomes manualmente e evitar erros de duplicidade.

**Why this priority**: Sem jogadores reais cadastrados, a nova tela continuaria sendo apenas um prototipo manual e nao agregaria valor ao sistema principal.

**Independent Test**: Abrir a criacao de montagem, buscar jogadores ativos cadastrados, selecionar participantes e visualizar todos inicialmente em `Jogadores Livres`, sem textarea de nomes.

**Acceptance Scenarios**:

1. **Given** existem jogadores ativos cadastrados, **When** o organizador abre a criacao de montagem, **Then** o sistema lista jogadores disponiveis com busca e filtros.
2. **Given** o organizador selecionou participantes, **When** a montagem e iniciada, **Then** todos os participantes aparecem como cards na area `Jogadores Livres` ou `Reservas`, conforme o calculo da configuracao.
3. **Given** um jogador esta inativo, **When** o organizador filtra jogadores elegiveis, **Then** o jogador inativo nao deve ser selecionavel por padrao.

---

### User Story 2 - Montar Times por Drag and Drop (Priority: P1)

Como organizador, quero arrastar jogadores entre jogadores livres, reservas e times para montar os times visualmente durante a chamada.

**Why this priority**: Drag and drop e a interacao central do prototipo atual e o motivo da melhoria solicitada.

**Independent Test**: Criar uma montagem com ao menos 12 jogadores e tamanho de time 5, arrastar jogadores entre times e devolver um jogador ao pool, validando que cada jogador aparece em apenas uma area.

**Acceptance Scenarios**:

1. **Given** um jogador esta em `Jogadores Livres`, **When** o organizador arrasta o card para um time com vaga, **Then** o jogador passa a compor aquele time e sai da area anterior.
2. **Given** um jogador esta no Time 1, **When** o organizador arrasta o card para o Time 2 com vaga, **Then** o jogador muda de time e nao permanece duplicado no Time 1.
3. **Given** um jogador esta em qualquer time, **When** o organizador arrasta o card para `Jogadores Livres`, **Then** ele deixa o time e fica disponivel novamente.
4. **Given** um time ja atingiu o tamanho configurado, **When** o organizador tenta soltar outro jogador nele, **Then** o sistema bloqueia a acao e mostra mensagem clara.

---

### User Story 3 - Calcular Times, Capitaes e Reservas Automaticamente (Priority: P1)

Como organizador, quero informar o tamanho desejado dos times para que o sistema calcule automaticamente quantos times, capitaes e reservas devem existir.

**Why this priority**: O prototipo atual assume quatro times fixos, mas o uso real varia conforme quantidade de jogadores confirmados.

**Independent Test**: Criar montagens com 15, 18 e 20 jogadores, tamanho 5, e validar que o numero de times, capitaes e reservas corresponde aos exemplos definidos.

**Acceptance Scenarios**:

1. **Given** 15 jogadores e tamanho de equipe 5, **When** a montagem e iniciada, **Then** o sistema gera 3 times, 3 capitaes e nenhuma reserva.
2. **Given** 18 jogadores e tamanho de equipe 5, **When** a montagem e iniciada, **Then** o sistema gera 3 times, 3 capitaes e 3 reservas.
3. **Given** 20 jogadores e tamanho de equipe 5, **When** a montagem e iniciada, **Then** o sistema gera 4 times, 4 capitaes e nenhuma reserva.
4. **Given** o usuario altera o tamanho da equipe antes de iniciar, **When** a quantidade calculada muda, **Then** a tela atualiza o resumo de times, capitaes e reservas antes da confirmacao.

---

### User Story 4 - Consultar Detalhes do Jogador ao Clicar (Priority: P2)

Como organizador, quero clicar em um jogador para abrir seus detalhes completos sem atrapalhar o drag and drop.

**Why this priority**: O grupo precisa consultar elo, rota, Discord e Riot ID durante a montagem sem sair da tela.

**Independent Test**: Clicar em um card sem arrastar e validar abertura de drawer/modal; arrastar o mesmo card e validar que nao abre modal indevidamente.

**Acceptance Scenarios**:

1. **Given** um card de jogador esta visivel, **When** o usuario apenas clica nele, **Then** o sistema abre um modal/drawer com detalhes completos do jogador.
2. **Given** o usuario inicia drag no card, **When** solta o card em uma drop zone, **Then** o sistema move o jogador sem abrir o modal/drawer.
3. **Given** o modal/drawer esta aberto, **When** o usuario fecha, **Then** a montagem permanece no mesmo estado visual.

---

### User Story 5 - Definir e Substituir Capitaes (Priority: P2)

Como organizador, quero selecionar, sortear e substituir capitaes para cada time para deixar claro quem lidera cada equipe.

**Why this priority**: A constituicao exige capitaes manuais ou sorteados e transparencia dos criterios.

**Independent Test**: Criar uma montagem com tres times, selecionar tres capitaes, substituir um capitao e validar que apenas um capitao existe por time.

**Acceptance Scenarios**:

1. **Given** a montagem possui N times, **When** o sistema calcula os times, **Then** o numero de capitaes exigidos e N.
2. **Given** o organizador escolhe capitaes manualmente, **When** confirma a selecao, **Then** cada time recebe exatamente um capitao distinto.
3. **Given** o organizador solicita sorteio de capitaes, **When** o sorteio termina, **Then** o sistema escolhe capitaes distintos entre jogadores participantes e exibe que o criterio foi sorteio.
4. **Given** um time ja possui capitao, **When** o organizador promove outro jogador daquele time a capitao, **Then** o capitao anterior deixa de ser capitao automaticamente.

---

### User Story 6 - Gerenciar Reservas (Priority: P2)

Como organizador, quero visualizar e promover reservas para times quando houver vaga ou troca de jogadores.

**Why this priority**: Em rinhas reais, jogadores excedentes precisam ficar visiveis e prontos para substituir ausencias.

**Independent Test**: Criar montagem com 18 jogadores e times de 5, validar 3 reservas, remover um jogador de um time e promover uma reserva para a vaga.

**Acceptance Scenarios**:

1. **Given** ha jogadores excedentes, **When** a montagem e gerada, **Then** eles aparecem em uma area `Reservas` separada de `Jogadores Livres`.
2. **Given** um time possui vaga, **When** uma reserva e arrastada para esse time, **Then** a reserva passa a integrar o time.
3. **Given** todos os times estao completos, **When** o organizador tenta promover uma reserva sem remover outro jogador, **Then** o sistema bloqueia ou exige troca explicita.

---

### User Story 7 - Exportar Resultado Visual (Priority: P3)

Como organizador, quero exportar a montagem final em imagem para compartilhar no grupo.

**Why this priority**: A exportacao ja existe no prototipo e e util para comunicacao externa, mas nao bloqueia a montagem principal.

**Independent Test**: Montar times, acionar exportacao e validar que a imagem contem nome da montagem, times, capitaes, jogadores, reservas e data.

**Acceptance Scenarios**:

1. **Given** a montagem possui times definidos, **When** o organizador exporta a imagem, **Then** o arquivo gerado contem somente a area relevante do draft, sem controles de edicao.
2. **Given** ha reservas, **When** a imagem e exportada, **Then** os reservas aparecem em uma area identificada.

---

### Edge Cases

- Quantidade de jogadores menor que o tamanho minimo para formar ao menos um time completo.
- Quantidade de jogadores igual ao tamanho da equipe: gerar 1 time, 1 capitao e nenhuma reserva.
- Quantidade de jogadores que gera muitos times, exigindo layout responsivo e navegavel.
- Jogador removido/inativado depois de uma montagem ja criada.
- Usuario tenta soltar jogador em time completo.
- Usuario tenta criar montagem com capitao duplicado.
- Usuario tenta salvar montagem com jogador duplicado em duas areas.
- Usuario altera tamanho da equipe depois de ja ter arrastado jogadores.
- Falha de rede ao salvar movimentacao ou finalizar montagem.
- Exportacao de imagem em tela pequena ou com muitos times.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an organizer to create a visual draft/team-building session using registered players instead of manually typed names.
- **FR-002**: System MUST load active registered players for selection and MUST support searching by display name, Discord and Riot ID.
- **FR-003**: System MUST support filtering selectable players by elo, primary route, status and route preference.
- **FR-004**: System MUST remove the manual textarea-based player input from the real draft creation flow.
- **FR-005**: System MUST display selected participants initially in a `Jogadores Livres` area unless automatic allocation places them as captains or reserves.
- **FR-006**: System MUST allow players to be moved by drag and drop between free players, reserves and teams.
- **FR-007**: System MUST prevent the same player from appearing in more than one area or team at the same time.
- **FR-008**: System MUST block dropping a player into a full team unless the action is explicitly treated as a swap.
- **FR-009**: System MUST allow a player assigned to a team to be returned to `Jogadores Livres`.
- **FR-010**: System MUST calculate the number of teams as the maximum number of full teams possible from selected players and desired team size.
- **FR-011**: System MUST calculate the number of required captains as equal to the number of generated teams.
- **FR-012**: System MUST calculate reserves as selected players that do not fit into full teams after team calculation.
- **FR-013**: System MUST show a pre-creation summary with selected players, desired team size, generated teams, required captains and reserves.
- **FR-014**: System MUST not assume a fixed limit of four teams.
- **FR-015**: System MUST visually adapt the board to the number of generated teams, using compact cards, horizontal scrolling or grouped rows when many teams exist.
- **FR-016**: System MUST support manual captain selection for every generated team.
- **FR-017**: System MUST support drawing captains automatically from selected participants.
- **FR-018**: System MUST ensure every generated team has exactly one captain before the montage can be finalized.
- **FR-019**: System MUST ensure a captain belongs to the team where they are displayed.
- **FR-020**: System MUST allow replacing a captain without duplicating captain status.
- **FR-021**: System MUST open a player detail modal or drawer when the user clicks a player card without dragging.
- **FR-022**: System MUST not open the player detail modal or drawer when the interaction was a drag operation.
- **FR-023**: Player details MUST include display name, elo, division, Discord, Riot ID, OP.GG, Deeplol, route preferences, status, created date and updated date when available.
- **FR-024**: System MUST show route preferences on player cards in a compact form.
- **FR-025**: System MUST allow per-draft route intent selection on a player card without changing the player's registered route preferences.
- **FR-026**: System MUST preserve the registered route preferences as the default source of route information.
- **FR-027**: System MUST persist the current arrangement of teams, captains, reserves, free players and per-draft route intents.
- **FR-028**: System MUST allow reopening an existing montage with the same arrangement after page refresh.
- **FR-029**: System MUST allow canceling or discarding an unfinished montage with confirmation.
- **FR-030**: System MUST allow finalizing a montage when every required team is valid.
- **FR-031**: System MUST make finalized montages read-only by default while still allowing future explicit edit/reopen behavior if approved.
- **FR-032**: System MUST allow exporting the visual result as an image.
- **FR-033**: Exported image MUST include montage name, teams, captains, players, reserves, route intents and generation date.
- **FR-034**: System MUST provide clear user-facing validation messages for insufficient players, invalid captains, duplicated players, full teams and persistence failures.
- **FR-035**: System MUST keep critical validation rules in the backend/domain, not only in frontend drag and drop behavior.

### Business Rules

- **BR-001**: Desired team size is informed by the organizer and must be at least 1.
- **BR-002**: The default maximum team size remains 5 for League of Legends use, but the calculation must not assume exactly two teams.
- **BR-003**: Number of teams is `floor(total selected players / desired team size)`.
- **BR-004**: Number of reserves is `total selected players % desired team size`.
- **BR-005**: If the number of generated teams is 0, the montage cannot be created.
- **BR-006**: Number of captains must equal number of generated teams.
- **BR-007**: A player can be in exactly one of these states inside a montage: free, reserve, assigned to a team, or captain assigned to a team.
- **BR-008**: Captain is a player and counts toward the team size.
- **BR-009**: A reserve does not count toward any team until promoted.
- **BR-010**: Registered route preferences are the source of truth for player profile preferences.
- **BR-011**: Per-draft route intent is contextual to one montage and must not mutate the player's profile preferences.
- **BR-012**: Finalization is blocked while any team has more players than its configured size or lacks a captain.

### UX Proposal for Routes

Use both registered preferences and per-draft route intent:

- Player cards show registered route preferences as small ordered chips, highlighting priority 1 and warning when the player marked a route as `nao jogo nem lascando`.
- During the montage, the organizer may select one route intent for that player in this specific draft.
- The selected route intent is saved with the montage assignment and exported in the image.
- Changing route intent in the montage does not change the player's profile.
- If no per-draft route is selected, the UI displays the player's primary registered route as the suggested route.

This balances speed and accuracy: the group sees persistent preferences but can adapt when someone agrees to play a different route that day.

### Screen States

- **Loading**: Skeletons for player selection and draft board while players/montage data load.
- **Empty Players**: No active players available; show CTA to cadastrar jogadores.
- **Creation Setup**: Select participants, define team size, see calculated teams/captains/reserves.
- **Captain Selection**: Manual or drawn captain assignment before board starts.
- **Board Editing**: Drag and drop active, teams/reserves/free players visible.
- **Player Details Open**: Drawer/modal overlays details while preserving board state.
- **Validation Error**: Inline errors for insufficient players, duplicate player, full team or missing captain.
- **Saving**: Board interactions disabled or queued while current arrangement is saved.
- **Finalized**: Board read-only, export available, edit requires explicit action.
- **Exporting**: Controls hidden, capture area prepared, success/error feedback after export.

### Validation Rules

- Selected player IDs must exist and be active at creation time.
- Selected player IDs must be unique.
- Desired team size must be valid and compatible with at least one complete team.
- Generated team count must be at least 1.
- Captain IDs must be selected from participants and must be unique.
- Each generated team must have exactly one captain.
- A player cannot be assigned to multiple teams or both team and reserve/free states.
- A team cannot exceed configured team size.
- A finalized montage cannot be modified unless reopened by an explicit future flow.
- Route intent, when selected, must be one of Top, Jungle, Mid, Adc or Support.

### Error Cases

- **No players found**: Show empty state with action to go to player registration.
- **Insufficient players**: Explain how many players are needed for the chosen team size.
- **Inactive player selected**: Remove from selectable list and show message if stale data caused selection.
- **Duplicate assignment**: Reject change and keep the last valid state.
- **Full team drop**: Reject drop, highlight capacity limit and optionally suggest swap.
- **Missing captain**: Highlight teams without captain before finalization.
- **Persistence failure**: Keep local board state visible, show retry action, and avoid silently losing changes.
- **Export failure**: Show message and keep montage usable.

### Key Entities *(include if feature involves data)*

- **Montagem de Draft**: Represents a visual team-building session with name, notes, status, team size, generated team count, captain criteria, selected players, free players, reserves, teams, creation date and update date.
- **Time da Montagem**: Represents one generated team with display name, visual color/order, captain and assigned players.
- **Participante da Montagem**: Represents one registered player inside the montage, including current state, assigned team if any, whether they are captain, and per-draft route intent.
- **Reserva da Montagem**: A participant state for players exceeding full team capacity.
- **Historico de Movimento**: Optional audit trail of drag/drop assignments and captain changes, useful for transparency and debugging.
- **Jogador**: Existing registered player used as source for identity, elo, Discord, Riot ID, status and route preferences.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An organizer can create a 20-player, 4-team montage from registered players in under 3 minutes.
- **SC-002**: 100% of duplicate player assignment attempts are blocked or automatically resolved without duplicated cards.
- **SC-003**: 100% of tested scenarios for 15, 18 and 20 players with team size 5 produce the expected number of teams and reserves.
- **SC-004**: Users can move a player between free area, teams and reserves with drag and drop in one interaction.
- **SC-005**: Users can open player details by click without accidentally triggering it during drag in at least 95% of manual usability checks.
- **SC-006**: A finalized montage can be reloaded and still show the same teams, captains, reserves and route intents.
- **SC-007**: Exported images are readable and complete for montages with up to 6 teams on desktop.

## Impactos no Frontend

- Evoluir `/drafts` ou criar uma subvisao clara dentro de drafts para `Montagem Visual`.
- Substituir o fluxo de criacao atual por um setup em etapas: participantes, tamanho do time, calculo, capitaes e board.
- Criar board com area central de `Jogadores Livres`, area de `Reservas` e lista dinamica de times.
- Implementar interacao click vs drag em cards de jogadores.
- Reutilizar dados completos de jogadores ja carregados pela tela de jogadores.
- Criar drawer/modal de detalhes do jogador reutilizavel.
- Exibir chips de rota baseados nas preferencias cadastradas e seletor de rota contextual do draft.
- Adicionar responsividade para muitos times: grid fluido, scroll horizontal em desktop, agrupamento/accordion em telas pequenas.
- Manter visual dark-first, card-first e tokens existentes do design system.
- Adicionar feedback para drop invalido, time cheio e salvamento.
- Adicionar exportacao visual da area final sem capturar filtros, botoes e controles de edicao.

## Impactos no Backend/API

- O modelo atual de draft suporta dois times fixos (`TimeA`, `TimeB`) e picks alternados; ele nao representa montagem visual multi-times.
- A API atual de `/api/v1/drafts` precisa ser expandida ou uma nova familia de endpoints deve ser criada para montagens visuais.
- A validacao de jogadores ativos e unicos deve continuar no backend.
- A regra de quantidade dinamica de times, reservas, capitaes e capacidade por time deve existir no dominio/backend.
- Movimentos de jogadores devem ser persistidos e validados no backend para evitar duplicidade e time acima do limite.
- A resposta da montagem deve retornar times dinamicos em colecao, nao propriedades fixas `timeA` e `timeB`.
- O backend deve retornar dados suficientes para reconstruir o board apos reload.

## Impactos no Modelo de Dados

O modelo atual pode ser evoluido de duas formas:

1. Criar um novo agregado `MontagemDraft` separado do draft de picks alternados.
2. Generalizar `DraftSessao` para suportar multiplos times e participantes com estado.

Recomendacao: criar um novo agregado de montagem visual para evitar quebrar o fluxo atual de picks alternados. O draft atual continua atendendo a sessao de picks entre dois capitaes; a montagem visual atende a organizacao manual multi-times.

Dados necessarios:

- Identificador da montagem.
- Nome, observacoes, status, tamanho de equipe, quantidade calculada de times, criterio de capitaes.
- Participantes selecionados.
- Times dinamicos com nome, ordem, cor visual e capitao.
- Atribuicoes de jogadores a times, reservas ou livres.
- Rota contextual escolhida no draft.
- Datas de cadastro/atualizacao.
- Opcional: historico de movimentos.

## Possiveis Migrations Necessarias

- Criar tabela de montagens visuais.
- Criar tabela de times da montagem.
- Criar tabela de participantes/atribuicoes da montagem.
- Criar indices unicos para impedir jogador duplicado na mesma montagem.
- Criar indices por montagem, time e jogador.
- Adicionar constraints de status e estado do participante quando aplicavel.
- Opcional: criar tabela de historico de movimentos.

## Endpoints Novos ou Ajustes Necessarios

Preferencia recomendada: novos endpoints dedicados para nao misturar conceitos com o draft atual de picks alternados.

- `GET /api/v1/draft-montagens`: listar montagens com filtros por status e busca.
- `GET /api/v1/draft-montagens/{id}`: consultar board completo.
- `POST /api/v1/draft-montagens`: criar montagem com jogadores, tamanho de equipe e criterio de capitaes.
- `PUT /api/v1/draft-montagens/{id}/layout`: salvar estado atual dos times, reservas, livres e rotas contextuais.
- `POST /api/v1/draft-montagens/{id}/movimentos`: registrar um movimento individual de jogador.
- `POST /api/v1/draft-montagens/{id}/capitaes/sortear`: sortear ou recalcular capitaes.
- `PATCH /api/v1/draft-montagens/{id}/finalizar`: finalizar montagem.
- `PATCH /api/v1/draft-montagens/{id}/cancelar`: cancelar montagem.

Caso o produto prefira unificar com `/api/v1/drafts`, a resposta precisa deixar claro o tipo de draft para diferenciar `PicksAlternados` e `MontagemVisual`.

## Estrategia de Persistencia do Draft

- Criacao salva a configuracao inicial e todos os participantes.
- O board pode salvar automaticamente apos cada movimento ou salvar por lote com indicador de alteracoes pendentes.
- Para MVP, usar salvamento explicito `Salvar montagem` reduz risco de muitas requisicoes e simplifica tratamento de conflitos.
- Movimentos locais devem manter estado consistente mesmo antes do salvamento, mas finalizacao exige estado persistido com sucesso.
- A persistencia deve armazenar a posicao final dos jogadores e nao depender apenas do historico de movimentos.
- Historico de movimentos pode ser adicionado para auditoria, mas nao e obrigatorio para o MVP.

## Fluxo do Usuario

1. Organizador acessa `Drafts` e escolhe `Nova montagem visual`.
2. Sistema carrega jogadores ativos cadastrados.
3. Organizador pesquisa/filtra e seleciona participantes.
4. Organizador informa tamanho desejado de cada equipe.
5. Sistema mostra resumo: jogadores selecionados, times gerados, capitaes necessarios e reservas.
6. Organizador escolhe capitaes manualmente ou solicita sorteio.
7. Sistema abre board com `Jogadores Livres`, `Reservas` se houver e times dinamicos.
8. Organizador arrasta jogadores entre areas ate formar os times.
9. Organizador clica em cards quando precisa consultar detalhes.
10. Organizador seleciona ou ajusta rota contextual dos jogadores quando necessario.
11. Organizador salva ou finaliza a montagem.
12. Sistema permite exportar imagem do resultado.

## Componentes Novos Necessarios

- **DraftVisualSetupModal/View**: configuracao inicial de participantes, tamanho de equipe e resumo calculado.
- **DraftVisualBoard**: board principal com drop zones dinamicas.
- **DraftTeamColumn/Card**: box de time com nome editavel, cor, capitao, capacidade e drop zone.
- **DraftPlayerCard**: card arrastavel com nome, elo, rota primaria, chips de preferencias e indicador de capitao/reserva.
- **DraftFreePlayersPanel**: area de jogadores livres com busca/filtro local.
- **DraftReservesPanel**: area de reservas e promocao para vagas.
- **DraftCaptainSelector**: selecao, sorteio e substituicao de capitaes.
- **PlayerDetailsDrawer**: detalhes completos do jogador acionados por clique.
- **DraftExportControls**: exportacao da area visual.
- **DraftCapacitySummary**: resumo de times, vagas, reservas e validacoes.

## Tarefas Tecnicas Sugeridas

1. Documentar decisao de produto entre `MontagemVisual` separada vs generalizacao de `DraftSessao`.
2. Modelar agregado de montagem visual e regras de dominio.
3. Criar contratos de request/response para board dinamico.
4. Criar migrations para montagens, times e participantes.
5. Implementar validadores de criacao, layout e finalizacao.
6. Implementar handlers CQRS de criar, consultar, salvar layout, sortear capitaes, finalizar e cancelar.
7. Criar endpoints REST dedicados.
8. Criar servico frontend para montagens visuais.
9. Construir tela/setup de selecao de jogadores sem textarea.
10. Construir board drag and drop com areas dinamicas.
11. Criar drawer/modal de detalhes do jogador.
12. Adicionar rota contextual por participante.
13. Adicionar exportacao de imagem.
14. Criar testes de dominio para calculo de times/reservas/capitaes.
15. Criar testes de validadores e handlers.
16. Criar testes de componentes para click vs drag, duplicidade e time cheio.
17. Validar responsividade com 1, 2, 3, 4, 6 e 8 times.

## Riscos Tecnicos

- Drag and drop pode ser inconsistente em mobile se nao houver interacao alternativa por botoes.
- Muitas drop zones podem prejudicar usabilidade em telas pequenas.
- Persistir a cada movimento pode gerar conflitos ou latencia perceptivel.
- Misturar o fluxo de picks alternados com montagem visual pode tornar o dominio confuso.
- Exportacao de imagem pode falhar com scroll horizontal ou conteudo fora da area visivel.
- Click vs drag exige limiar claro para evitar abrir detalhes acidentalmente.
- Calculo automatico de reservas pode surpreender se o organizador espera criar time incompleto; a UI precisa explicar.

## Melhorias Futuras

- Balanceamento automatico por elo e rota.
- Sugestao de capitaes baseada em elo, historico ou preferencia do grupo.
- Sorteio completo de times com restricoes de rota.
- Modo snake draft multi-capitaes.
- Compartilhamento por link publico de leitura.
- Exportacao em formatos diferentes, como imagem quadrada para Discord e formato widescreen.
- Historico completo de alteracoes do board.
- Integracao com fila/check-in para preencher participantes automaticamente.
- Edicao colaborativa em tempo real.

## Assumptions

- A funcionalidade sera usada inicialmente por organizadores internos confiaveis.
- Jogadores cadastrados continuam sendo a fonte de verdade para identidade, elo, status e preferencias de rota.
- O fluxo atual de draft de dois times por picks alternados deve continuar existindo ate decisao explicita de substituicao.
- Montagens visuais podem formar mais de dois times e podem representar organizacao de partidas simultaneas ou times para uma noite de rinha.
- O MVP deve priorizar salvamento confiavel do layout final sobre colaboracao em tempo real.
- A quantidade maxima visualmente confortavel sera tratada por layout responsivo, nao por limite fixo de quatro times.

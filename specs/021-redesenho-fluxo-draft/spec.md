# Feature Specification: Redesenho do Fluxo de Draft

**Feature Branch**: `feature/021-redesenho-fluxo-draft`

**Created**: 2026-07-25

**Status**: Draft

**Input**: Redesenhar todo o fluxo visual de Draft, incluindo presença, capitães, ordem e escolhas, corrigir componentes e responsividade, e publicar a correção dos dias selecionados em Atualizações.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Operar o draft com hierarquia clara (Priority: P1)

Como organizador, quero identificar imediatamente o draft selecionado, seu estado, a próxima ação e o progresso geral, para conduzir todas as etapas sem interpretar controles dispersos ou componentes visualmente inconsistentes.

**Why this priority**: O draft é uma operação central e a interface atual comprime informações, mistura níveis de ação e dificulta reconhecer o próximo passo.

**Independent Test**: Um organizador percorre presença, capitães, ordem e escolhas em desktop e identifica em cada etapa o contexto, o progresso, a ação principal e as ações secundárias sem consultar instruções externas.

**Acceptance Scenarios**:

1. **Given** um draft em qualquer etapa ativa, **When** o organizador abre a tela, **Then** vê identificação, data, contadores, estado atual, progresso e próxima ação em uma hierarquia única e consistente.
2. **Given** uma etapa com ação principal disponível, **When** a interface é exibida, **Then** somente essa ação usa o tratamento visual primário da área e ações secundárias ou destrutivas usam tratamentos distintos.
3. **Given** um draft cancelado, **When** ele é selecionado, **Then** cancelamento é comunicado como estado terminal e nenhuma etapa ativa incorreta é apresentada.
4. **Given** a transição entre presença, capitães, ordem e escolhas, **When** o estado muda, **Then** a estrutura da tela permanece estável e apenas o conteúdo operacional da etapa é atualizado.

---

### User Story 2 - Gerenciar a lista de presença sem controles quebrados (Priority: P1)

Como organizador, quero consultar confirmados e reservas, buscar e adicionar jogadores e remover presenças autorizadas em uma área organizada, para manter a lista sem campos desalinhados, cards comprimidos ou rolagens concorrentes.

**Why this priority**: A presença concentra o maior número de jogadores e controles, e os problemas atuais prejudicam diretamente a preparação da partida.

**Independent Test**: Com ao menos 14 confirmados, o organizador localiza um jogador, adiciona outro, remove uma presença permitida e acompanha os estados do Discord sem perda de contexto ou deslocamento inesperado da interface.

**Acceptance Scenarios**:

1. **Given** uma lista com vários confirmados, **When** ela é exibida, **Then** cada jogador apresenta identidade, origem e ações permitidas de forma legível, sem truncamento que esconda a informação essencial.
2. **Given** permissão e presença aberta, **When** o organizador busca e seleciona um jogador elegível, **Then** busca, seleção e inclusão aparecem como um único fluxo coerente e operável por teclado.
3. **Given** uma ação de inclusão, remoção ou republicação em andamento, **When** o usuário interage novamente, **Then** duplicidade é bloqueada e feedback localizado preserva o contexto.
4. **Given** estados de publicação no Discord, **When** a presença é aberta, **Then** esses estados ficam agrupados como informação operacional e não competem com a ação principal do draft.
5. **Given** lista vazia ou nenhuma lista selecionada, **When** a área é aberta, **Then** uma mensagem localizada e a ação disponível para criar ou selecionar um draft orientam o usuário conforme sua permissão.

---

### User Story 3 - Conduzir capitães, ordem e escolhas (Priority: P1)

Como organizador, quero conduzir as etapas posteriores à presença em uma estrutura consistente, para definir capitães, estabelecer a ordem, acompanhar escolhas e finalizar o draft sem perder contexto.

**Why this priority**: O redesenho foi aprovado para todo o fluxo e só entrega valor completo se as etapas posteriores preservarem regras e ficarem tão claras quanto a presença.

**Independent Test**: Um draft com presença encerrada percorre definição de capitães, ordem, escolhas e finalização, mantendo permissões, resultados e próxima ação corretos em cada transição.

**Acceptance Scenarios**:

1. **Given** presença encerrada e capitães ainda não definidos, **When** a etapa é aberta, **Then** os candidatos elegíveis, a forma de definição e a ação principal ficam identificáveis sem competir com informações secundárias.
2. **Given** capitães definidos, **When** a ordem ainda não existe, **Then** capitães e ação de ordenação permanecem visíveis, e nenhuma escolha de jogador é apresentada como disponível prematuramente.
3. **Given** ordem definida e escolhas em andamento, **When** um jogador é escolhido, **Then** capitão da vez, jogadores disponíveis, times formados e progresso da rodada permanecem visíveis na mesma estrutura.
4. **Given** uma tentativa sem permissão, fora da vez ou com jogador indisponível, **When** a ação é solicitada, **Then** o estado não avança e uma mensagem localizada explica o impedimento sem apagar o contexto atual.
5. **Given** todas as escolhas concluídas, **When** o draft é finalizado, **Then** o resultado dos times substitui as ações de escolha e o progresso comunica finalização como estado terminal.

---

### User Story 4 - Usar todo o fluxo em qualquer viewport (Priority: P1)

Como usuário, quero operar o draft por mouse, teclado ou toque em desktop, tablet e mobile, para não perder ações, contexto ou legibilidade conforme a largura disponível.

**Why this priority**: O fluxo é usado durante a organização da partida e precisa continuar funcional fora de um desktop amplo.

**Independent Test**: O mesmo draft é operado em 1440px, 1024px, 768px e 320px sem overflow horizontal obrigatório, controles sobrepostos ou conteúdo inacessível.

**Acceptance Scenarios**:

1. **Given** desktop amplo, **When** a tela é aberta, **Then** o navegador de drafts e a área operacional coexistem sem comprimir indevidamente jogadores ou ações.
2. **Given** tablet, **When** a largura diminui, **Then** o navegador passa a uma disposição compacta e o conteúdo reduz colunas antes de ocorrer truncamento prejudicial.
3. **Given** mobile de 320px ou maior, **When** todas as etapas são percorridas, **Then** o fluxo usa uma coluna, mantém alvos de toque adequados e não exige rolagem horizontal da página.
4. **Given** navegação por teclado ou tecnologia assistiva, **When** o usuário percorre progresso, filtros, ações e jogadores, **Then** foco visível, nomes acessíveis, ordem lógica e indicação da etapa atual permanecem disponíveis.
5. **Given** preferência por movimento reduzido, **When** estados mudam, **Then** a interface comunica a mudança sem animações decorativas obrigatórias.

---

### User Story 5 - Navegar entre drafts com contexto suficiente (Priority: P2)

Como organizador, quero localizar e selecionar drafts por nome, data e status, para alternar entre listas sem percorrer cards altos ou interpretar badges genéricos.

**Why this priority**: A navegação sustenta o fluxo principal, mas pode ser validada separadamente da operação interna de cada etapa.

**Independent Test**: Com drafts em estados diferentes e datas variadas, o usuário filtra, reconhece o selecionado e alterna de item sem perder a posição ou o contexto.

**Acceptance Scenarios**:

1. **Given** múltiplos drafts, **When** a lista é apresentada, **Then** nome, data e status possuem prioridade visual consistente e o item selecionado é inequívoco.
2. **Given** status aberto, encerrado, em andamento, finalizado ou cancelado, **When** os itens são comparados, **Then** cada estado usa texto e tratamento semântico equivalente em português e inglês.
3. **Given** uma lista extensa, **When** o usuário navega pelos itens, **Then** consegue alcançar o item selecionado e a próxima ação usando no máximo uma região de rolagem vertical por vez.

---

### User Story 6 - Consultar a correção publicada nas Atualizações (Priority: P2)

Como usuário, quero encontrar no histórico a melhoria entregue na seleção de dias dos agendamentos, para entender que dias selecionados agora recebem confirmação visual clara.

**Why this priority**: A correção já está em produção e precisa ser comunicada no canal oficial do produto sem esperar o redesenho completo.

**Independent Test**: A página Atualizações apresenta `2026.07.3` no topo, em português e inglês, com categoria de correção, link para Configurações e somente uma versão em destaque.

**Acceptance Scenarios**:

1. **Given** a correção dos dias já publicada, **When** o usuário abre Atualizações, **Then** vê uma descrição orientada ao benefício, sem mensagem de commit ou detalhe técnico interno.
2. **Given** locale português ou inglês, **When** a versão `2026.07.3` é exibida, **Then** título, resumo e detalhe possuem estrutura e significado equivalentes.
3. **Given** a nova versão destacada, **When** o histórico é validado, **Then** a versão destacada anterior deixa de ser destaque e a ordem cronológica permanece correta.

### Edge Cases

- Um draft sem data informada mantém texto localizado e não quebra ordenação ou alinhamento.
- Nomes longos de draft e jogador preservam a informação essencial sem sobrepor status ou ações.
- Uma lista com zero, um, dez, quatorze ou mais jogadores mantém hierarquia e disposição previsíveis.
- Status desconhecido não é apresentado como presença aberta; usa fallback localizado e visual neutro.
- Cancelamento em qualquer etapa interrompe a progressão visual e não mantém ação primária incompatível.
- Falha ao buscar dados oferece ação localizada para tentar novamente; falha de atualização ao vivo ou Discord mantém os dados já conhecidos e preserva a alternativa manual autorizada.
- Permissões diferentes ocultam ou desabilitam ações sem deixar espaços vazios que deformem o layout.
- Textos maiores em inglês não provocam sobreposição, corte de botões ou overflow obrigatório.
- A versão `2026.07.3` não duplica ID, versão, data ou posição de destaque no histórico.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A tela MUST apresentar navegador de drafts e área operacional como regiões distintas, com o draft selecionado identificado de forma inequívoca.
- **FR-002**: A área operacional MUST apresentar nome, data, estado, contagem de confirmados, times e reservas antes dos controles específicos da etapa.
- **FR-003**: O progresso MUST representar presença, encerramento, capitães, ordem, escolhas e finalização em sequência compreensível, com integração Discord apresentada como indicador paralelo.
- **FR-004**: O progresso MUST distinguir etapas concluídas, atual e pendentes por mais de um sinal visual e MUST identificar semanticamente a etapa atual.
- **FR-005**: Draft cancelado MUST ser apresentado como terminal e MUST NOT ativar visualmente presença aberta ou outra etapa operacional.
- **FR-006**: Cada etapa MUST possuir no máximo uma ação com o tratamento visual primário; ações secundárias, destrutivas e de integração MUST usar tratamentos distintos.
- **FR-007**: Confirmar, encerrar, cancelar, definir capitães, estabelecer ordem, escolher jogadores e finalizar MUST preservar regras, permissões e resultados existentes em todos os cenários descritos nesta especificação.
- **FR-008**: A lista de presença MUST apresentar identidade, origem e ações permitidas de cada jogador sem reutilizar aparência que sugira que todo o item é um único botão.
- **FR-009**: Busca, seleção e inclusão manual MUST formar um fluxo agrupado, possuir rótulos localizados visíveis ou acessíveis e ser operável por teclado e toque.
- **FR-010**: A interface MUST impedir envio duplicado enquanto uma ação equivalente estiver em andamento e MUST apresentar feedback localizado de sucesso ou falha.
- **FR-011**: Estados e ações Discord MUST ficar agrupados e visualmente subordinados ao fluxo principal, preservando operação manual quando a integração estiver indisponível.
- **FR-012**: O navegador MUST exibir nome, data e status de cada draft, além de seleção atual clara e estados semânticos para todos os status suportados.
- **FR-013**: Estados desconhecidos MUST usar mensagem localizada neutra e MUST NOT ser convertidos visualmente para um estado incorreto.
- **FR-014**: A tela MUST permitir alcançar o item selecionado e a próxima ação usando no máximo uma região de rolagem vertical por vez.
- **FR-015**: Em desktop amplo, navegador e área operacional MUST coexistir sem ocultar nome, estado, contadores, ação principal ou identificação dos jogadores.
- **FR-016**: Em tablet, o navegador MUST assumir disposição compacta e grades MUST reduzir colunas antes que conteúdo essencial seja truncado.
- **FR-017**: Em viewports de 320px ou maiores, a tela MUST permanecer operável em uma coluna e sem overflow horizontal obrigatório da página.
- **FR-018**: Controles interativos MUST manter foco visível, nomes acessíveis, ordem de foco lógica e área acionável mínima de 44 por 44 pixels quando operados por toque.
- **FR-019**: Estados não podem depender exclusivamente de cor; texto, ícone, posição ou forma MUST complementar a codificação cromática.
- **FR-020**: A interface MUST respeitar preferência de movimento reduzido e usar movimento apenas para comunicar mudança de estado.
- **FR-021**: Durante carregamento, ações equivalentes MUST permanecer indisponíveis sem ocultar dados já conhecidos; em falha de busca MUST existir ação para tentar novamente; em estado vazio MUST existir ação para criar ou selecionar um draft conforme a permissão do usuário.
- **FR-022**: A alteração MUST preservar permissões, regras e resultados funcionais existentes; validade e progressão do draft MUST continuar obedecendo às regras atuais do produto.
- **FR-023**: Cores, espaçamentos, tipografia, formas, elevação e estados MUST respeitar integralmente os padrões visuais oficiais do produto, sem criar uma identidade paralela.
- **FR-024**: Todo texto visível novo ou alterado MUST apresentar conteúdo compreensível e equivalente em português e inglês, com acentuação portuguesa revisada.
- **FR-025**: O histórico MUST adicionar a versão `2026.07.3`, publicada em `2026-07-25`, descrevendo a confirmação visual dos dias selecionados em agendamentos.
- **FR-026**: A versão `2026.07.3` MUST ser a única destacada, usar categoria de correção, classificação editorial `drafts` por abranger listas de presença e link para Configurações onde o agendamento é operado.
- **FR-027**: O redesenho completo MUST ser publicado em versão posterior própria somente depois que todos os cenários de aceitação e critérios de sucesso desta especificação forem atendidos, sem ser anunciado antecipadamente em `2026.07.3`.

### Key Entities

- **Draft**: Fluxo operacional existente com identificação, data, status, participantes, times, capitães, ordem, escolhas e estados de publicação.
- **Participante do Draft**: Jogador confirmado ou reserva com identidade, origem da confirmação e ações permitidas conforme estado e autorização.
- **Etapa do Draft**: Estado sequencial concluído, atual, pendente ou terminal usado para comunicar progresso e próxima ação.
- **Atualização do Sistema**: Entrada editorial versionada, localizada, categorizada e opcionalmente vinculada a uma área acionável do produto.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos sete estados conhecidos, nome e estado atual aparecem em regiões identificadas; estados operacionais exibem no máximo uma ação primária antes das secundárias, e estados terminais não exibem ação de avanço.
- **SC-002**: Em viewports de 1440px, 1024px, 768px e 320px, 100% das etapas permanecem operáveis sem overflow horizontal obrigatório, controles sobrepostos ou ações inacessíveis.
- **SC-003**: Em listas com zero, um, dez, quatorze e trinta participantes, 100% dos jogadores mantêm nome, origem e ação permitida disponíveis, e nenhuma interação altera a largura dos itens ou provoca rolagem horizontal da página.
- **SC-004**: Todos os status conhecidos, incluindo cancelamento, apresentam texto localizado e estado visual correto; nenhum fallback ativa uma etapa incorreta.
- **SC-005**: Em 100% dos cenários críticos de confirmar, encerrar, cancelar, definir capitães, estabelecer ordem, escolher, remover, finalizar e republicar, permissões, bloqueio de duplicidade e resultado permanecem iguais aos anteriores ao redesenho.
- **SC-006**: O fluxo completo pode ser percorrido somente por teclado com foco sempre visível, ordem lógica e indicação programática da etapa atual.
- **SC-007**: Em português e inglês, 100% dos estados, ações, mensagens e conteúdos editoriais novos ou alterados exibem texto compreensível e equivalente, sem chave ou marcador técnico visível.
- **SC-008**: A versão `2026.07.3` aparece no topo do histórico em ambos os idiomas, com exatamente uma release destacada e link válido para Configurações.
- **SC-009**: Todos os cenários de aceitação das seis jornadas são concluídos sem perda de ação existente, informação essencial, permissão ou significado entre português e inglês.
- **SC-010**: Usuários conseguem concluir inclusão manual, remoção autorizada e avanço da etapa de presença sem alternar entre áreas de rolagem concorrentes.

## Assumptions

- O comportamento atual de draft, permissões, atualização ao vivo e integração Discord será preservado.
- O redesenho prioriza clareza operacional e consistência sem alterar regras de formação, presença, capitães, ordem ou escolhas.
- Desktop é a experiência principal, mas tablet e mobile fazem parte do critério de conclusão.
- A versão `2026.07.3` comunica somente a correção já entregue para seleção de dias; o redesenho receberá versão posterior.
- Dados reais existentes podem conter nomes longos, datas ausentes e status históricos e devem continuar apresentáveis.

## Out of Scope

- Alterar regras de negócio, permissões, comunicação de dados ou persistência do draft.
- Criar novas etapas, novos métodos de sorteio ou novas automações Discord.
- Redesenhar páginas fora do fluxo de Draft e do item obrigatório em Atualizações.
- Introduzir novos tokens, nova identidade visual ou modo claro.
- Publicar o redesenho no histórico antes de sua entrega efetiva.

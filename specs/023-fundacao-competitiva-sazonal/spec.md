# Feature Specification: Fundação Competitiva Sazonal

**Feature Branch**: `feature/023-fundacao-competitiva-sazonal`

**Created**: 2026-07-27

**Status**: Approved

**Input**: Estruturar Seasons, séries diárias e confrontos entre times como fundação para partidas, estatísticas, Fearless, elencos, notas, rating e rankings futuros.

## User Scenarios & Testing

### User Story 1 - Administrar a Season atual (Priority: P1)

Como Presidente, quero cadastrar e ativar as Seasons temáticas da Riot adotadas pela Rinha, suas competições e rodadas para que todo confronto oficial pertença ao recorte competitivo correto e o site use a Season atual por padrão.

**Why this priority**: Sem um recorte sazonal explícito, partidas, rankings e histórico futuro podem ser atribuídos ao período errado ou misturados de forma irreversível.

**Independent Test**: Pode ser testada criando Seasons consecutivas, ativando uma delas e verificando que consultas sem filtro usam a Season ativa, enquanto consultas históricas aceitam uma, várias ou todas as Seasons.

**Acceptance Scenarios**:

1. **Given** duas Seasons cadastradas e apenas uma ativa, **When** um membro abre uma listagem ou agregado competitivo sem informar Season, **Then** a Season ativa é aplicada e identificada na resposta.
2. **Given** Seasons encerradas e uma ativa, **When** um membro seleciona duas Seasons ou todas, **Then** o sistema retorna o recorte solicitado e identifica quais Seasons foram incluídas.
3. **Given** uma Season ativa, **When** o Presidente ativa a Season seguinte, **Then** a anterior é encerrada e permanece disponível no histórico.
4. **Given** uma partida ou série já confirmada, **When** a Season atual muda, **Then** o vínculo histórico original não é alterado automaticamente.
5. **Given** uma competição oficial com rodadas publicadas, **When** uma série oficial é criada, **Then** ela pertence à Season, competição e rodada informadas.

---

### User Story 2 - Operar uma série diária com Fearless (Priority: P1)

Como membro com capacidade explícita de operar séries, quero registrar uma série direta entre duas equipes temporárias formadas por capitães para acompanhar placar, partidas e campeões bloqueados pelo Fearless.

**Why this priority**: As séries diárias são o fluxo competitivo recorrente da Rinha e originam a elegibilidade futura de jogadores, estatísticas e rating.

**Independent Test**: Pode ser testada criando uma MD3 vinculada a um draft finalizado, registrando os campeões utilizados e verificando o placar e a lista Fearless antes de cada partida seguinte.

**Acceptance Scenarios**:

1. **Given** um draft diário finalizado com dois lados e capitães, **When** o ator com capacidade cria uma série MD3 Fearless, **Then** a série preserva data local, snapshots dos lados, dos capitães e da Season.
2. **Given** a primeira partida confirmada, **When** seus dez campeões utilizados são registrados, **Then** todos ficam indisponíveis para ambos os lados nas partidas seguintes da série.
3. **Given** um campeão bloqueado pelo Fearless, **When** um ator com capacidade tenta confirmá-lo em uma partida posterior, **Then** a confirmação é recusada com explicação clara.
4. **Given** partidas suficientes para definir o vencedor da MD3 ou MD5, **When** o último resultado é confirmado, **Then** a série é concluída e seu vencedor é calculado.
5. **Given** uma nova série com os mesmos participantes, **When** ela começa, **Then** nenhum bloqueio Fearless da série anterior é herdado.

---

### User Story 3 - Agendar confrontos entre times oficiais (Priority: P1)

Como membro com capacidade explícita de gerenciar agendamentos, quero agendar confrontos oficiais ou amistosos entre times cadastrados sem depender da lista recorrente de presença para separar corretamente competição e treinamento.

**Why this priority**: Times oficiais possuem confrontos próprios e não podem ser confundidos com as equipes temporárias formadas no draft diário.

**Independent Test**: Pode ser testada agendando um confronto oficial e um amistoso entre os mesmos times e verificando que apenas o oficial é elegível para projeções competitivas futuras.

**Acceptance Scenarios**:

1. **Given** dois times oficiais ativos, **When** um ator com capacidade agenda um confronto, **Then** informa natureza, Season, formato, horário e regras antes da disputa.
2. **Given** um confronto oficial confirmado, **When** ele é consultado, **Then** aparece como elegível para estatísticas, Score, Rating, votação e rankings futuros.
3. **Given** um amistoso confirmado, **When** ele é consultado, **Then** aparece em histórico e agregados amistosos separados, sem elegibilidade para Score, Rating, MVP/SVP, rankings ou recordes oficiais.
4. **Given** um confronto agendado entre times, **When** nenhuma lista de presença está associada, **Then** o agendamento continua válido.

---

### User Story 4 - Registrar evento com quatro times sem Fearless (Priority: P2)

Como membro com capacidade explícita de gerenciar competições, quero identificar eventos com quatro times e usar draft padrão para que o Fearless bilateral não seja aplicado incorretamente a todos os confrontos.

**Why this priority**: O formato multitimes possui regra diferente e precisa ser explícito para evitar bloqueios indevidos de campeões.

**Independent Test**: Pode ser testada criando um evento com quatro times e verificando que o modo Fearless não pode ser selecionado para o evento como um todo.

**Acceptance Scenarios**:

1. **Given** um evento com quatro times, **When** um ator com capacidade define suas regras gerais, **Then** o modo de draft é padrão.
2. **Given** um evento com quatro times, **When** alguém tenta ativar Fearless global, **Then** a operação é recusada antes da publicação das regras.
3. **Given** confrontos diretos dentro do evento, **When** suas séries são criadas, **Then** todas usam draft padrão e não compartilham bloqueios de campeões.

---

### User Story 5 - Auditar decisões competitivas (Priority: P2)

Como Presidente ou membro com capacidade explícita de auditoria, quero consultar quem alterou Seasons, séries, resultados e regras para resolver disputas sem apagar o histórico.

**Why this priority**: Alterações competitivas afetam participantes, elegibilidade e projeções futuras; decisões silenciosas diminuem a confiança no sistema.

**Independent Test**: Pode ser testada corrigindo um resultado com justificativa e verificando que o valor anterior, o novo valor, o ator e o motivo permanecem consultáveis.

**Acceptance Scenarios**:

1. **Given** uma série com resultado confirmado, **When** um ator autorizado corrige uma partida, **Then** informa motivo e o histórico anterior permanece auditável.
2. **Given** um usuário sem permissão esportiva, **When** tenta ativar Season, publicar regra ou corrigir série, **Then** a operação é negada sem alteração de estado.
3. **Given** uma regra já publicada, **When** uma nova versão é criada, **Then** séries anteriores continuam vinculadas à versão utilizada na época.

### Edge Cases

- Uma Season usa intervalo `[início, fim)` e não pode ter período inválido nem sobrepor qualquer outra Season do mesmo calendário competitivo.
- Season possui estados `Planejada`, `Ativa` e `Encerrada`, com transições `Planejada → Ativa → Encerrada`.
- A ativação concorrente usa a mesma versão do calendário como precondição; a primeira transição aceita invalida a versão e a segunda deve receber conflito.
- Antes da configuração inicial e entre ciclos ainda não ativados pode não existir Season ativa; Séries oficiais, tanto `DiariaTemporaria` quanto `ConfrontoOficial`, não podem ser confirmadas nesse estado.
- Uma série não pode misturar equipes temporárias e times oficiais sem natureza explicitamente permitida pelo regulamento.
- A mesma partida não pode pertencer a mais de uma série.
- Uma MD3 termina quando um lado alcança duas vitórias; uma MD5 termina ao alcançar três.
- Partidas excedentes após a definição do vencedor não podem ser confirmadas como parte competitiva da série.
- Série possui estados `Agendada`, `EmAndamento`, `Concluída`, `Cancelada` e `Anulada`; partida possui `Rascunho`, `Confirmada`, `Remake` e `Anulada`.
- Série pode ser cancelada enquanto `Agendada` ou `EmAndamento`; anulação de série concluída exige correção auditada. Estados finais não retornam ao fluxo normal.
- Somente partidas `Confirmadas` contam para placar; remake conta para bloqueio apenas quando seus picks são preservados; partida anulada não conta para placar nem bloqueio.
- Surrender é um motivo de término de partida confirmada e mantém vencedor; remake não possui vencedor competitivo.
- Em remake, um ator com capacidade de corrigir partidas deve escolher `Preservar picks` ou `Desconsiderar picks` antes da confirmação da partida seguinte, com justificativa auditada.
- Correção de picks anteriores deve reconstruir a lista Fearless e sinalizar partidas posteriores que se tornaram inconsistentes.
- Um draft arquivado pode continuar vinculado historicamente, sem ser restaurado ou reativado pela série.
- Alterar nome, membros ou capitão de um time não modifica snapshots de confrontos anteriores.
- Horários de operação e calendário da Rinha usam `America/Sao_Paulo` como referência de negócio.
- Uma Season encerrada não aceita novas Séries oficiais; correção retroativa autorizada e auditada limita-se a Séries e Partidas já existentes e não cria fatos históricos ausentes.
- O período de uma Season com confronto confirmado não pode ser alterado de modo a excluir esse confronto sem fluxo de correção explícito.

## Requirements

### Functional Requirements

- **FR-001**: O sistema MUST permitir cadastrar Season com nome, ano, ordem no ano, início e fim; toda Season nova começa como `Planejada`.
- **FR-002**: O sistema MUST manter no máximo uma Season ativa por vez; durante operação competitiva, a confirmação de qualquer Série oficial exige uma Season ativa e Seasons seguem `Planejada → Ativa → Encerrada`.
- **FR-003**: O sistema MUST aplicar a Season ativa como filtro padrão em listagens e agregados sazonais quando o usuário não informar recorte; se não houver Season ativa, MUST retornar coleção vazia com estado explícito de calendário não configurado, sem usar todas as Seasons como fallback; o catálogo administrativo de Seasons, detalhe por identificador e auditoria não recebem filtro implícito.
- **FR-004**: O sistema MUST permitir filtrar listagens e agregados sazonais por uma Season, várias Seasons ou todas, com opções mutuamente exclusivas.
- **FR-005**: O sistema MUST identificar as Seasons incluídas em qualquer resultado multisseason.
- **FR-006**: Encerrar uma Season MUST preservar séries, partidas, regras e histórico associados.
- **FR-007**: Alterar a Season ativa MUST NOT reatribuir automaticamente registros históricos.
- **FR-008**: Ativar uma Season MUST exigir a versão corrente do calendário, encerrar atomicamente a Season ativa anterior quando houver, incrementar a versão global e MUST NOT reabrir Season encerrada sem fluxo de correção autorizado.
- **FR-009**: Toda série MUST pertencer a uma Season.
- **FR-010**: Toda série MUST registrar natureza, formato, modo de draft, lados, capitães quando aplicável e versão das regras.
- **FR-011**: O sistema MUST distinguir equipes temporárias de draft e times oficiais.
- **FR-012**: Uma série diária de equipes temporárias MUST poder referenciar um DraftMontagem finalizado sem alterar seu ciclo de vida.
- **FR-013**: O vínculo com draft arquivado MUST permanecer histórico e MUST NOT restaurar ou reativar o draft.
- **FR-014**: O sistema MUST preservar snapshots dos lados e participantes usados na série.
- **FR-015**: O sistema MUST registrar cada partida em no máximo uma série.
- **FR-016**: Cada partida MUST registrar ordem na série, lados, resultado quando aplicável, picks confirmados e um dos estados `Rascunho`, `Confirmada`, `Remake` ou `Anulada`.
- **FR-017**: O vencedor da série MUST ser derivado apenas de partidas `Confirmadas`; remake e partida anulada não alteram placar.
- **FR-018**: O sistema MUST impedir partidas competitivas adicionais depois que um lado alcançar as vitórias necessárias.
- **FR-019**: O modo Fearless MUST ser permitido para séries diretas de dois lados, incluindo `DiariaTemporaria`, `ConfrontoOficial` e `Amistoso`, exceto quando a série pertencer a evento de quatro times; Fearless não torna o amistoso elegível para projeções oficiais.
- **FR-020**: No Fearless, campeão utilizado por qualquer lado MUST ficar bloqueado para ambos os lados nas partidas posteriores da mesma série.
- **FR-021**: Bloqueios Fearless MUST considerar picks confirmados, não hovers ou intenções.
- **FR-022**: Bloqueios Fearless MUST ser derivados de partidas `Confirmadas` e de remakes marcados como `Preservar picks`, reiniciando ao fim da série.
- **FR-023**: Correções de picks MUST reconstruir os bloqueios e sinalizar conflitos posteriores.
- **FR-024**: Um remake MUST exigir a decisão auditada `Preservar picks` ou `Desconsiderar picks` antes da partida seguinte; somente a primeira opção mantém seus campeões no bloqueio Fearless.
- **FR-025**: Eventos com quatro times MUST usar draft padrão no regulamento geral.
- **FR-026**: O sistema MUST recusar Fearless global em evento de quatro times.
- **FR-027**: Todo confronto pertencente a evento de quatro times MUST usar draft padrão e MUST NOT compartilhar bloqueios de campeões.
- **FR-028**: O sistema MUST permitir agendar confronto entre times sem lista de presença.
- **FR-029**: O agendamento MUST registrar horário, natureza, Season, lados esperados e regras publicadas.
- **FR-030**: O sistema MUST distinguir confronto oficial e amistoso; confronto oficial MUST pertencer a uma competição e rodada da mesma Season.
- **FR-031**: Todo amistoso MUST permanecer fora de Score, Rating, MVP/SVP, rankings e recordes oficiais.
- **FR-032**: Amistosos MUST permanecer consultáveis em histórico e agregados próprios claramente identificados.
- **FR-033**: Séries diárias oficiais em estado `Concluída` MUST ser identificáveis como fatos aptos a alimentar a contagem de elegibilidade da feature de elencos; a transição para `Concluída` representa a confirmação autorizada do resultado final.
- **FR-034**: Amistosos, séries canceladas e séries anuladas MUST ser identificados como inelegíveis para essa contagem futura.
- **FR-035**: O sistema MUST usar a identidade autenticada como origem do ator de toda alteração.
- **FR-036**: Criação e ativação de Season MUST exigir `CanManageSeasons`, concedida a SuperAdmin e Presidente; publicação de competição/regras, operação de série/agendamento e correções MUST exigir capacidades esportivas distintas e explícitas; os poderes finos do VicePresidente MUST ser aprovados antes do planejamento de implementação.
- **FR-037**: O sistema MUST preservar trilha de auditoria para mudanças de Season, regras, partidas, resultados e vínculos.
- **FR-038**: Correções MUST registrar ator, instante, justificativa, valor anterior e valor posterior.
- **FR-039**: Regras publicadas MUST ser versionadas e séries MUST manter a versão usada.
- **FR-040**: Repetir uma operação com a mesma chave e conteúdo MUST retornar o mesmo resultado sem duplicação; reutilizar a chave com conteúdo diferente MUST ser rejeitado como conflito.
- **FR-041**: Atualizações concorrentes MUST rejeitar versão obsoleta sem sobrescrita silenciosa.
- **FR-042**: Todo texto destinado ao usuário, incluindo labels, botões, títulos, placeholders, tooltips, nomes acessíveis, estados, badges, confirmações, vazios, toasts, erros, validações e mensagens de API ou domínio, MUST estar disponível em português e inglês pelos mecanismos de localização do produto.
- **FR-043**: A interface MUST diferenciar claramente Season, natureza do confronto, formato, modo de draft e estado.
- **FR-044**: A experiência MUST funcionar em desktop e dispositivos móveis sem esconder informação competitiva essencial.
- **FR-045**: O sistema MUST permitir criar competições dentro de uma Season e ordenar suas rodadas.
- **FR-046**: Toda série oficial MUST pertencer a exatamente uma competição e rodada; série diária usa a competição oficial configurada para o circuito diário.
- **FR-047**: O sistema MUST permitir criar séries MD3 e MD5.
- **FR-048**: A transição de série MUST seguir `Agendada → EmAndamento → Concluída`; cancelamento é permitido a partir de `Agendada` ou `EmAndamento`, e anulação é estado final acessível somente por correção auditada.
- **FR-049**: Série diária MUST registrar data local em `America/Sao_Paulo`, dois capitães, origem `DraftMontagem` finalizada e lados temporários antes de iniciar.

### Key Entities

- **Season**: Recorte competitivo temático com período, ordem e ciclo de vida; fornece o escopo padrão das consultas.
- **Competição**: Organização esportiva dentro de uma Season, com rodadas ordenadas e natureza oficial.
- **Rodada**: Recorte ordenado de uma competição ao qual séries oficiais são atribuídas.
- **Série**: Disputa MD3 ou MD5 entre dois lados, vinculada a uma Season, natureza e versão de regras.
- **Partida**: Unidade ordenada da série com resultado, picks confirmados e estado competitivo explícito.
- **Lado da Série**: Snapshot de uma equipe temporária ou time oficial no momento da disputa.
- **Conjunto de Regras**: Versão publicada que define formato, modo de draft e tratamento de situações excepcionais.
- **Bloqueio Fearless**: Projeção reconstruível dos campeões utilizados em partidas elegíveis anteriores da mesma série.
- **Evento Competitivo**: Contexto opcional que organiza confrontos, incluindo eventos com quatro times.
- **Agendamento de Confronto**: Compromisso entre lados com horário, natureza e regulamento, independente de presença recorrente.
- **Registro de Auditoria Competitiva**: Evidência de decisão ou correção com ator, justificativa e estado anterior/posterior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Em 100% das listagens e agregados sazonais sem filtro explícito, exceto o catálogo administrativo de Seasons, a Season ativa é aplicada e identificada; detalhes por ID e auditoria preservam seu vínculo histórico sem filtro implícito.
- **SC-002**: Em teste moderado com ator previamente autenticado e capacidades necessárias, o participante conclui o cadastro de uma Season planejada, sua competição/rodada e uma série MD3 em até 5 minutos, medidos do início do formulário à confirmação.
- **SC-003**: Em 100% das partidas Fearless confirmadas, nenhum campeão já presente no bloqueio efetivo da série pode ser confirmado novamente.
- **SC-004**: Em 100% dos eventos com quatro times, o regulamento geral e todas as séries pertencentes impedem Fearless.
- **SC-005**: Em 100% dos amistosos, nenhuma projeção oficial de Score, Rating, MVP/SVP, ranking ou recorde é alterada.
- **SC-006**: Em 100% das correções competitivas, ator, justificativa e estados anterior/posterior permanecem auditáveis.
- **SC-007**: Duas tentativas simultâneas de ativar Seasons diferentes resultam em uma única Season ativa e informam o conflito ao outro ator.
- **SC-008**: Em teste de usabilidade, membros conseguem aplicar Season atual, duas Seasons e histórico completo usando no máximo três ações de controle e uma confirmação por recorte.
- **SC-009**: Todas as regras críticas de Season, MD3/MD5, Fearless, amistosos, autorização e concorrência possuem cenários automatizados antes da implementação ser considerada concluída.

## Assumptions

- A Rinha acompanha cada Season temática da Riot como uma Season competitiva própria, mas as datas permanecem administráveis e não se sobrepõem.
- `America/Sao_Paulo` é o fuso de negócio para calendário, série diária e agendamentos.
- O fluxo manual é obrigatório e suficiente para o primeiro incremento.
- `DraftMontagem` é o modelo canônico do draft diário e não será substituído por esta feature.
- Times oficiais existentes podem ser referenciados; a expansão para oito integrantes, escalações e transferências pertence à feature 024.
- Estatísticas detalhadas, Rinha Score, Rinha Rating e votação usam esta fundação, mas pertencem a features posteriores.
- Presidente é a autoridade esportiva principal; SuperAdmin mantém a autoridade técnica máxima e pode gerir Seasons; poderes esportivos finos do VicePresidente serão formalizados antes das respectivas operações serem implementadas.

## Out of Scope

- Coleta LCU, Live Client Data, Riot API, Tournament API, ROFL ou OCR.
- Estatísticas detalhadas de jogador e time.
- Cálculo de Rinha Score ou movimentação de Rating.
- Votação de MVP e SVP.
- Expansão de elenco, escalação diária e janelas de transferência.
- Páginas completas de perfil, time, comparação e rankings.
- Balanceamento automático, scout e sugestões de draft.
- Definição visual das faixas de nota.

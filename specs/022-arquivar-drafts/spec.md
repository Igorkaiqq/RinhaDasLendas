# Feature Specification: Arquivamento Administrativo de Drafts

**Feature Branch**: `feature/022-arquivar-drafts`

**Created**: 2026-07-26

**Status**: Draft

**Input**: Permitir que Admin e SuperAdmin arquivem qualquer draft com motivo obrigatório; drafts ativos são cancelados e publicam o cancelamento no Discord; arquivados ficam ocultos da listagem normal e podem ser visualizados e restaurados por filtro administrativo, preservando histórico e auditoria.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Arquivar qualquer draft com segurança (Priority: P1)

Como Admin ou SuperAdmin, quero arquivar um draft que não deve mais aparecer na operação cotidiana, para manter a tela organizada sem apagar seu histórico.

**Why this priority**: Remover drafts obsoletos da navegação normal é o valor central da feature e precisa preservar dados e interromper corretamente qualquer fluxo ainda ativo.

**Independent Test**: Um Admin arquiva, com motivo, um draft em cada estado suportado e confirma que ele desaparece da lista normal, preserva seus dados e não continua com ações operacionais ativas.

**Acceptance Scenarios**:

1. **Given** um draft finalizado ou cancelado não arquivado, **When** um Admin ou SuperAdmin informa um motivo válido e confirma o arquivamento, **Then** o draft é arquivado sem alteração de seu status terminal e desaparece da listagem normal.
2. **Given** um draft em qualquer estado não terminal, **When** um Admin ou SuperAdmin confirma o arquivamento com motivo válido, **Then** o draft é cancelado e arquivado na mesma operação e não continua sujeito a presença, turno, escolha ou finalização.
3. **Given** um draft ativo arquivado, **When** a operação termina, **Then** o cancelamento é destinado à publicação no Discord e uma falha temporária dessa integração não reativa nem desarquiva o draft.
4. **Given** um arquivamento concluído, **When** seus dados são consultados administrativamente, **Then** participantes, presenças, capitães, ordem, escolhas, times, reservas, substituições, publicações e ações anteriores permanecem íntegros.
5. **Given** motivo vazio ou composto apenas por espaços, **When** o arquivamento é solicitado, **Then** nenhuma alteração ocorre e uma validação localizada orienta o usuário.
6. **Given** uma falha antes da confirmação do cancelamento e arquivamento, **When** a operação é encerrada, **Then** status, arquivamento, auditoria e intenção de publicação permanecem todos no estado anterior.

---

### User Story 2 - Manter arquivados fora da operação normal (Priority: P1)

Como usuário do sistema, quero ver apenas drafts relevantes na navegação normal, para encontrar rapidamente o fluxo em que preciso atuar sem registros administrativos antigos.

**Why this priority**: Apenas registrar o arquivamento sem aplicar a ocultação em todos os acessos deixaria a tela poluída e permitiria operações indevidas por links antigos.

**Independent Test**: Após arquivar um draft, um usuário comum pesquisa, filtra, atualiza a página e tenta abrir seu link anterior sem encontrar nem operar o registro.

**Acceptance Scenarios**:

1. **Given** um ou mais drafts arquivados, **When** qualquer usuário abre ou filtra a listagem normal, **Then** nenhum draft arquivado é incluído nos resultados ou contagens.
2. **Given** o identificador de um draft arquivado, **When** um usuário sem permissão administrativa tenta acessá-lo diretamente, **Then** o sistema não revela o registro nem permite qualquer ação sobre ele.
3. **Given** um draft arquivado que possua cancelamento ainda não publicado no Discord, **When** os processos operacionais são executados, **Then** somente a entrega pendente do cancelamento pode prosseguir e nenhuma etapa do draft é retomada.
4. **Given** que o draft selecionado é arquivado, **When** a listagem é atualizada, **Then** a seleção avança para um item visível ou apresenta o estado vazio apropriado sem manter o draft arquivado aberto.

---

### User Story 3 - Consultar e restaurar drafts arquivados (Priority: P2)

Como Admin ou SuperAdmin, quero incluir drafts arquivados na navegação e restaurá-los quando necessário, para corrigir arquivamentos indevidos sem reconstruir dados.

**Why this priority**: A reversibilidade reduz o risco de uma ação administrativa destrutiva, mas depende do arquivamento e da ocultação já funcionarem.

**Independent Test**: Um Admin ativa o filtro de arquivados, identifica um registro pelo badge, restaura-o e confirma seu retorno à listagem normal com status e histórico preservados.

**Acceptance Scenarios**:

1. **Given** um Admin ou SuperAdmin na tela de drafts, **When** ativa o filtro para incluir arquivados, **Then** os registros arquivados aparecem identificados sem confundir arquivamento com o status operacional.
2. **Given** um draft arquivado que já estava finalizado ou cancelado, **When** é restaurado, **Then** volta à listagem normal com o mesmo status e os mesmos dados anteriores.
3. **Given** um draft que foi cancelado automaticamente ao ser arquivado, **When** é restaurado, **Then** volta à listagem como cancelado e não retoma presença, cronômetros, turnos ou escolhas.
4. **Given** uma restauração concluída, **When** o histórico administrativo é consultado, **Then** os eventos anteriores de arquivamento e restauração continuam registrados.

---

### User Story 4 - Restringir e auditar ações administrativas (Priority: P2)

Como responsável pela plataforma, quero que somente Admin e SuperAdmin arquivem ou restaurem drafts e que cada ação seja rastreável, para impedir ocultações indevidas e permitir prestação de contas.

**Why this priority**: Arquivar altera a visibilidade operacional e pode cancelar um draft ativo, portanto exige autorização mais restrita que a gestão cotidiana de drafts.

**Independent Test**: As ações são tentadas com cada papel do sistema e o histórico é conferido após operações permitidas, comprovando autorização e rastreabilidade.

**Acceptance Scenarios**:

1. **Given** um Moderador, Capitão, Jogador ou integração automatizada, **When** tenta arquivar, restaurar ou incluir arquivados, **Then** a operação é recusada mesmo que esse ator possua outras permissões de gestão de drafts.
2. **Given** um Admin ou SuperAdmin, **When** arquiva um draft, **Then** responsável, instante, motivo e cancelamento decorrente, quando aplicável, ficam registrados.
3. **Given** um Admin ou SuperAdmin, **When** restaura um draft, **Then** responsável e instante ficam registrados sem apagar a justificativa ou os eventos anteriores.
4. **Given** solicitações repetidas ou concorrentes para atingir o mesmo estado, **When** são processadas, **Then** o draft converge para o estado solicitado sem duplicar eventos administrativos equivalentes.
5. **Given** solicitações concorrentes de arquivamento e restauração, **When** uma delas confirma primeiro, **Then** a operação oposta não sobrescreve silenciosamente o estado confirmado e recebe um conflito compreensível.

### Edge Cases

- Um draft pode ser arquivado enquanto outro usuário tenta avançar sua etapa; apenas uma alteração coerente deve prevalecer, sem deixar o registro arquivado e operacional ao mesmo tempo.
- Uma publicação Discord pode estar pendente, em andamento ou em reconciliação no momento do arquivamento; o cancelamento final deve substituir a progressão operacional sem perder capacidade de nova tentativa.
- A integração Discord pode estar indisponível; o arquivamento continua válido e a falha é apresentada como estado de publicação recuperável.
- Um draft pode ser arquivado ou restaurado por duas sessões administrativas simultâneas; somente um evento equivalente deve ser registrado para cada mudança real.
- Dois arquivamentos simultâneos podem informar motivos diferentes; o primeiro confirmado prevalece e o segundo não altera motivo, responsável ou auditoria.
- A página pode conter somente drafts arquivados; a visão normal informa que não há drafts visíveis, enquanto o filtro administrativo permite encontrá-los.
- O filtro de arquivados pode ser removido por perda de permissão durante a sessão; os registros deixam imediatamente de ser apresentados ou acessíveis.
- Links antigos, atualizações em tempo real e dados mantidos em memória não podem reexibir ou permitir ações em um draft arquivado para usuários não autorizados.
- A restauração de um draft cancelado durante o arquivamento não pode reconstruir estado ativo, prazo, turno ou seleção anterior.
- Nomes e motivos longos devem permanecer legíveis, validados e sem quebrar a interface em português ou inglês.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir que somente usuários com papel Admin ou SuperAdmin arquivem drafts.
- **FR-002**: O sistema MUST permitir o arquivamento nos estados Presença Aberta, Presença Encerrada, Capitães Definidos, Ordem Definida, Aberta, Finalizada e Cancelada.
- **FR-003**: Todo arquivamento MUST exigir motivo que, após remoção de espaços nas extremidades, contenha de 1 a 500 caracteres; 500 caracteres MUST ser aceitos e 501 MUST ser recusado.
- **FR-004**: Ao arquivar um draft em Presença Aberta, Presença Encerrada, Capitães Definidos, Ordem Definida ou Aberta, o sistema MUST cancelá-lo e arquivá-lo como uma única alteração lógica, usando o motivo administrativo também como justificativa do cancelamento.
- **FR-005**: Ao arquivar um draft finalizado ou cancelado, o sistema MUST preservar seu status operacional.
- **FR-006**: O arquivamento MUST interromper presença, prazos, turnos, escolhas e demais avanços operacionais do draft.
- **FR-007**: O cancelamento decorrente do arquivamento de um draft ativo MUST registrar, na mesma alteração lógica, uma intenção durável de publicação no Discord, processada somente após a confirmação do arquivamento.
- **FR-008**: Falha ou indisponibilidade do Discord MUST NOT impedir, reverter ou ocultar o resultado do arquivamento e MUST manter a publicação do cancelamento disponível para nova tentativa autorizada.
- **FR-009**: Drafts arquivados MUST ser excluídos de listagens, pesquisas, filtros, contagens, atualizações em tempo real e acessos diretos normais.
- **FR-010**: Usuários sem a permissão específica MUST NOT consultar, incluir em filtros, arquivar, restaurar ou operar drafts arquivados.
- **FR-011**: Tentativas autenticadas de incluir arquivados sem papel Admin ou SuperAdmin MUST ser recusadas explicitamente; usuários não autenticados MUST continuar sujeitos à autenticação obrigatória.
- **FR-012**: Admin e SuperAdmin MUST poder incluir drafts arquivados por meio de um filtro administrativo desativado por padrão.
- **FR-013**: A interface MUST identificar um draft arquivado separadamente de seu status finalizado ou cancelado.
- **FR-014**: Admin e SuperAdmin MUST poder restaurar um draft arquivado sem informar novo motivo.
- **FR-015**: A restauração MUST preservar o status atual, todo o conteúdo do draft e todo o histórico administrativo.
- **FR-016**: Um draft cancelado em decorrência do arquivamento MUST permanecer cancelado após restauração e MUST NOT retomar automaticamente qualquer etapa, prazo, turno ou publicação operacional anterior.
- **FR-017**: Arquivamento e restauração MUST registrar responsável e instante; arquivamento MUST também registrar o motivo. Quando o draft estiver ativo, a mesma operação MUST registrar uma ação de Cancelamento e uma ação de Arquivamento distintas, sem duplicação.
- **FR-018**: Eventos administrativos já registrados MUST ser imutáveis e permanecer disponíveis após restauração ou novo arquivamento.
- **FR-019**: Somente Admin e SuperAdmin MUST poder consultar motivo, responsável e histórico administrativo de arquivamento ou restauração.
- **FR-020**: Repetir uma solicitação válida quando o draft já estiver no estado solicitado MUST retornar o estado atual sem novo evento; entre arquivamentos concorrentes, o primeiro confirmado MUST preservar seu motivo e responsável; operações opostas concorrentes MUST impedir sobrescrita silenciosa e retornar conflito para a operação perdedora.
- **FR-021**: Se a alteração lógica de cancelamento e arquivamento falhar antes da confirmação, status, metadados de arquivamento, ações administrativas e intenção de publicação MUST permanecer inalterados e nenhuma mensagem de cancelamento MUST ser enviada.
- **FR-022**: O arquivamento MUST NOT excluir fisicamente nem alterar indevidamente presenças, participantes, capitães, ordem, escolhas, times, reservas, substituições, publicações ou relações externas do draft.
- **FR-023**: Após arquivar o draft selecionado, a tela MUST selecionar outro item visível ou apresentar o estado vazio correspondente, sem manter ações do registro arquivado disponíveis.
- **FR-024**: Após restaurar um draft, a tela MUST atualizar a navegação sem exigir recarregamento completo da página.
- **FR-025**: Controles de arquivar, incluir arquivados e restaurar MUST aparecer somente para Admin e SuperAdmin, sem substituir a validação de autorização do sistema.
- **FR-026**: Títulos, botões, filtros, badges, confirmações, motivos, validações, estados vazios, notificações e erros da feature MUST possuir conteúdo equivalente em português e inglês.
- **FR-027**: Mensagens de impedimento e falha MUST ser compreensíveis, localizadas e não revelar detalhes internos nem a existência de drafts inacessíveis.
- **FR-028**: Quando disponibilizada aos usuários, a feature MUST possuir uma entrada localizada no histórico de Atualizações que explique arquivamento, restauração e preservação do histórico sem detalhes técnicos internos.

### Key Entities

- **Draft**: Fluxo existente com status operacional, participantes, presença, capitães, ordem, escolhas, times, reservas, substituições e publicações; passa a possuir condição administrativa arquivada independente de seu status.
- **Estado de Arquivamento**: Condição atual que informa se o draft está arquivado, quando foi arquivado, por qual usuário e com qual motivo.
- **Ação Administrativa**: Registro imutável de arquivamento, restauração e eventual cancelamento, com responsável, instante, motivo quando aplicável e vínculo com o draft.
- **Publicação de Cancelamento**: Comunicação destinada ao Discord quando um draft ativo é cancelado por arquivamento, com estado recuperável caso a integração esteja indisponível.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Admin e SuperAdmin conseguem arquivar drafts nos sete estados Presença Aberta, Presença Encerrada, Capitães Definidos, Ordem Definida, Aberta, Finalizada e Cancelada informando um motivo válido; Moderador, Capitão, Jogador e integração automatizada falham em todas as tentativas equivalentes.
- **SC-002**: Em 100% dos arquivamentos de drafts ativos, o status final observado é cancelado e nenhuma presença, prazo, turno, escolha ou finalização posterior permanece executável.
- **SC-003**: Na primeira resposta de listagem solicitada após o arquivamento, o draft deixa de aparecer em todas as visões normais, pesquisas e contagens e deixa de ser acessível diretamente por usuários não administrativos.
- **SC-004**: Admin e SuperAdmin encontram qualquer draft arquivado ao ativar o filtro administrativo e concluem sua restauração usando seleção, ação de restaurar e confirmação, sem recarregar a página inteira.
- **SC-005**: Após arquivamento e restauração, 100% dos participantes, presenças, capitães, escolhas, times, reservas, substituições, publicações e relações existentes permanecem inalterados.
- **SC-006**: Drafts cancelados pelo arquivamento permanecem cancelados em 100% das restaurações e nunca retomam automaticamente o estado ativo anterior.
- **SC-007**: Cada mudança real produz exatamente um evento administrativo de Arquivamento ou Restauração com responsável e instante; arquivar um draft ativo produz também exatamente um evento de Cancelamento, inclusive sob solicitações repetidas ou concorrentes.
- **SC-008**: Indisponibilidade do Discord não impede o arquivamento em 100% dos cenários testados e deixa o cancelamento identificável para nova tentativa sem retomar o draft.
- **SC-009**: Em português e inglês, 100% dos novos controles, estados e mensagens apresentam conteúdo compreensível e equivalente, sem chaves técnicas ou texto sem tradução visível.
- **SC-010**: Nenhum cenário de arquivamento ou restauração remove fisicamente registros do draft ou torna seu histórico administrativo anterior indisponível.

## Assumptions

- A autenticação, os papéis existentes e o histórico administrativo atual serão reutilizados, com uma autorização específica mais restrita que a gestão cotidiana de drafts.
- Arquivamento é uma ação administrativa reversível e independente do status operacional; não representa uma nova etapa do draft.
- O motivo de até 500 caracteres segue o limite das justificativas administrativas já usadas no produto.
- Publicações de cancelamento podem ser concluídas de forma assíncrona e repetidas com segurança quando o Discord estiver indisponível.
- A restauração corrige a visibilidade do registro, mas não desfaz o cancelamento de um draft que estava ativo no momento do arquivamento.
- A listagem normal continua sendo a experiência de todos os usuários; a visualização de arquivados é uma opção explícita e exclusiva de Admin e SuperAdmin.

## Out of Scope

- Exclusão física de drafts ou de qualquer registro relacionado.
- Retomada do estado ativo anterior de um draft cancelado durante o arquivamento.
- Arquivamento automático por idade, quantidade ou status.
- Arquivamento ou restauração em massa.
- Permissão de arquivamento para Moderador, Capitão, Jogador ou integrações automatizadas.
- Alteração ou remoção retroativa de mensagens já publicadas no Discord.
- Redesenho geral do fluxo visual de Draft além dos controles, filtros, badges, estados e mensagens necessários para esta feature.

# Feature Specification: Reabertura de Presença do Draft

**Feature Branch**: `feature/024-reabrir-presenca-draft`

**Created**: 2026-07-27

**Status**: Implemented / Verified

**Input**: Permitir que um organizador reabra uma presença encerrada por engano, mantenha os participantes confirmados e prossiga normalmente com capitães, ordem e início do draft, inclusive quando o primeiro encerramento ocorreu com 19 jogadores.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reabrir presença encerrada por engano (Priority: P1)

Como organizador autorizado, quero reabrir uma lista de presença encerrada antes da definição dos capitães, para permitir novas confirmações sem recriar o draft ou perder os participantes existentes.

**Why this priority**: Um encerramento acidental hoje é irreversível e impede corrigir a lista usada na partida.

**Independent Test**: Encerrar uma presença com 19 participantes, reabri-la, confirmar o vigésimo participante e verificar que os 20 participantes permanecem disponíveis para um novo encerramento.

**Acceptance Scenarios**:

1. **Given** um draft com presença encerrada e capitães ainda não definidos, **When** um organizador autorizado reabre a presença, **Then** o draft retorna à presença aberta e mantém todas as confirmações existentes.
2. **Given** uma presença reaberta, **When** jogadores confirmam ou o organizador altera presenças manualmente, **Then** as mesmas ações disponíveis antes do primeiro encerramento voltam a funcionar.
3. **Given** uma presença reaberta após o prazo automático original, **When** nenhum organizador a encerra, **Then** ela permanece aberta até um encerramento manual.
4. **Given** uma presença reaberta com novos participantes, **When** o organizador encerra novamente, **Then** quantidades de times e reservas são calculadas com a lista confirmada naquele momento.
5. **Given** um draft que já avançou para capitães, ordem, escolhas ou estado terminal, **When** alguém tenta reabrir a presença, **Then** a operação é recusada sem alterar o draft.

---

### User Story 2 - Prosseguir com dezenove participantes (Priority: P1)

Como organizador, quero entender e concluir o fluxo válido de um draft encerrado com 19 participantes, para não ficar bloqueado ao definir capitães, ordem e início.

**Why this priority**: O incidente mostrou que controles desabilitados não comunicam adequadamente a estrutura calculada e geram a impressão de que nenhum draft pode ser iniciado.

**Independent Test**: Encerrar uma presença de times de cinco com 19 participantes, selecionar os três capitães exigidos e concluir ordem e início sem adicionar um vigésimo participante.

**Acceptance Scenarios**:

1. **Given** 19 participantes e times de cinco, **When** a presença é encerrada, **Then** o sistema informa três times, quatro reservas e a necessidade de exatamente três capitães.
2. **Given** esse draft com presença encerrada, **When** o organizador seleciona exatamente três participantes elegíveis como capitães, **Then** a definição de capitães fica disponível e pode ser concluída.
3. **Given** os três capitães definidos, **When** o organizador define uma ordem válida, **Then** o draft avança para a etapa em que pode ser iniciado conforme o modo escolhido.
4. **Given** que o organizador prefere quatro times, **When** reabre a presença, obtém a vigésima confirmação e encerra novamente, **Then** o sistema informa quatro times, nenhuma reserva e exige quatro capitães.

---

### User Story 3 - Restringir e registrar a reabertura (Priority: P1)

Como responsável pela operação, quero que somente pessoas com permissão de gerenciar drafts possam reabrir presenças e que a ação seja rastreável, para evitar alterações indevidas.

**Why this priority**: A reabertura altera a etapa operacional e precisa seguir a mesma governança das demais ações administrativas do draft.

**Independent Test**: Exercitar a reabertura como anônimo, jogador e organizador autorizado e verificar negação, sucesso e registro correto da autoria.

**Acceptance Scenarios**:

1. **Given** um usuário sem permissão de gerenciar drafts, **When** tenta reabrir uma presença, **Then** o acesso é negado sem revelar dados administrativos nem alterar o draft.
2. **Given** um organizador autorizado, **When** reabre uma presença, **Then** a autoria e o instante da ação são registrados a partir da identidade autenticada.
3. **Given** outros usuários acompanhando o draft, **When** a presença é reaberta, **Then** o novo estado fica disponível sem exigir que recriem ou selecionem outro draft.
4. **Given** português ou inglês como idioma ativo, **When** a ação, confirmação, sucesso ou erro são apresentados, **Then** todo o conteúdo aparece localizado e equivalente.

### Edge Cases

- Repetir a reabertura depois que a presença já está aberta não altera dados e retorna impedimento claro.
- Reabrir uma presença sem confirmações preserva a lista vazia e permite novas confirmações.
- Reabrir não restaura presenças canceladas antes ou depois do primeiro encerramento.
- Um prazo automático já vencido não pode encerrar imediatamente uma presença reaberta.
- Falha durante a reabertura não pode deixar status, prazo e totais derivados parcialmente alterados.
- Atualizações simultâneas devem resultar em um único estado consistente e nunca remover confirmações.
- Integrações externas indisponíveis não impedem a reabertura nem o encerramento manual posterior.
- Draft arquivado não pode ser reaberto pelo fluxo operacional normal.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer a ação de reabrir presença somente a usuários autenticados com permissão de gerenciar drafts.
- **FR-002**: A reabertura MUST ser permitida somente quando o draft estiver com a presença encerrada e antes da definição dos capitães.
- **FR-003**: A reabertura MUST preservar todas as presenças e respectivos estados, origens e ordens existentes.
- **FR-004**: A reabertura MUST retornar o draft ao estado de presença aberta.
- **FR-005**: A reabertura MUST remover o prazo de encerramento automático da ocorrência, mantendo-a aberta até novo encerramento manual.
- **FR-006**: Totais de times, reservas e indicação de continuação excepcional MUST deixar de representar uma estrutura fechada durante a reabertura e MUST ser determinados novamente no próximo encerramento.
- **FR-007**: Após a reabertura, confirmação, cancelamento e gestão manual de presenças MUST obedecer às regras já existentes para presença aberta.
- **FR-008**: A ação MUST registrar responsável autenticado e instante, sem aceitar autoria fornecida pelo solicitante.
- **FR-009**: Usuários que acompanham o draft MUST receber o estado reaberto pelo mecanismo de atualização já disponível no produto.
- **FR-010**: O sistema MUST impedir reabertura de draft arquivado, cancelado, finalizado ou que já possua capitães, ordem ou escolhas em andamento.
- **FR-011**: Com 19 participantes e times de cinco, o sistema MUST manter a regra de três times completos e quatro reservas.
- **FR-012**: Para uma presença encerrada, a interface MUST informar quantos capitães são necessários e MUST habilitar a definição quando exatamente essa quantidade de participantes elegíveis estiver selecionada.
- **FR-013**: Um draft válido com 19 participantes MUST permitir concluir capitães, ordem e início sem exigir um vigésimo participante.
- **FR-014**: Após reabrir, confirmar o vigésimo participante e encerrar novamente, o sistema MUST calcular quatro times, nenhuma reserva e quatro capitães necessários.
- **FR-015**: A reabertura MUST ser confirmada explicitamente para reduzir novos acionamentos acidentais.
- **FR-016**: Falhas e impedimentos MUST preservar o estado anterior e apresentar mensagem compreensível ao usuário.
- **FR-017**: Textos, ações, confirmações, estados e mensagens novos ou alterados MUST possuir conteúdo equivalente em português e inglês, com acentuação portuguesa revisada.
- **FR-018**: A reabertura MUST funcionar sem depender da disponibilidade do Discord ou de outra integração externa.
- **FR-019**: A entrega MUST preservar as regras atuais de tamanho de equipe, cálculo de reservas, elegibilidade de capitães, ordem e início do draft.

### Key Entities

- **Draft**: Fluxo operacional que contém estado, prazo de presença, estrutura de times, participantes, capitães, ordem e escolhas.
- **Presença do Draft**: Participação confirmada ou cancelada, com jogador, origem e ordem preservadas durante a reabertura.
- **Ação Administrativa do Draft**: Registro da reabertura com responsável autenticado e instante da ação.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos cenários válidos, o organizador reabre a presença sem perder ou duplicar qualquer confirmação existente.
- **SC-002**: Em 100% dos cenários após o prazo original, a presença reaberta permanece disponível até o organizador encerrá-la manualmente.
- **SC-003**: Em 100% dos cenários com 19 participantes e times de cinco, três capitães válidos permitem concluir capitães, ordem e início do draft.
- **SC-004**: Em 100% dos cenários de reabertura seguidos da vigésima confirmação, o novo encerramento resulta em quatro times e nenhuma reserva.
- **SC-005**: Em 100% das tentativas feitas sem permissão ou após a definição dos capitães, o draft permanece inalterado.
- **SC-006**: Organizadores conseguem identificar a quantidade exigida de capitães e a ação de reabertura sem orientação externa.
- **SC-007**: Português e inglês apresentam estrutura e significado equivalentes para 100% dos conteúdos novos ou alterados.
- **SC-008**: A jornada de reabertura e novo encerramento continua utilizável quando integrações externas estão indisponíveis.

## Assumptions

- A permissão existente de gerenciamento de drafts é a autorização adequada para reabrir presença.
- O encerramento posterior à reabertura será exclusivamente manual; definir um novo prazo não faz parte desta correção.
- A ação só retrocede a etapa imediatamente posterior à presença aberta e não descarta capitães, times ou escolhas.
- A fórmula vigente de times completos e reservas está correta e não será alterada.
- O mecanismo existente de atualização do draft e a auditoria administrativa serão reutilizados.

## Out of Scope

- Reabrir presença depois que capitães, ordem ou escolhas já foram definidos.
- Alterar o tamanho dos times ou formar um time incompleto para eliminar reservas.
- Definir novo horário automático durante a reabertura.
- Corrigir bloqueios globais de permissão da interface que não foram reproduzidos neste incidente.
- Alterar publicação, mensagens ou interações do Discord além de refletir o estado operacional vigente.

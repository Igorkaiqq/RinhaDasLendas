# Feature Specification: Draft em Tempo Real na Montagem de Times

**Feature Branch**: `012-draft-montagem-tempo-real`

**Created**: 2026-06-24

**Status**: Draft

**Input**: User description: "Implementar draft em tempo real com capitães ativos e ordem de escolha na DraftMontagem. Usar sincronização em tempo real, backend como fonte oficial, capitães ativos, apenas capitão da vez escolhe, timer de 30 segundos controlado pelo backend, reservas não podem ser escolhidos e servem apenas como complemento emergencial, mensagens e textos com i18n/resources, sem texto hardcoded."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capitão escolhe jogadores no seu turno (Priority: P1)

Como capitão ativo de um time em uma montagem de draft, quero escolher jogadores disponíveis somente quando for minha vez, para formar meu time de forma clara, justa e auditável.

**Why this priority**: Esta é a experiência central do draft em tempo real. Sem ela, a feature não entrega valor.

**Independent Test**: Pode ser testada com uma montagem aberta contendo dois ou mais times, capitães ativos e jogadores livres; o capitão da vez escolhe um jogador e o estado do draft avança para o próximo capitão.

**Acceptance Scenarios**:

1. **Given** uma montagem aberta em modo de draft em tempo real com jogadores livres, **When** o capitão da vez escolhe um jogador livre antes do fim do turno, **Then** o jogador entra no time desse capitão, a escolha é registrada na ordem do draft e a vez avança para o próximo capitão elegível.
2. **Given** uma montagem aberta em modo de draft em tempo real, **When** um capitão fora da vez tenta escolher um jogador, **Then** a escolha é recusada e o estado oficial do draft não muda.
3. **Given** uma montagem aberta em modo de draft em tempo real, **When** um usuário que não é capitão ativo tenta escolher um jogador, **Then** a escolha é recusada e o usuário continua podendo apenas visualizar informações.

---

### User Story 2 - Participantes acompanham o draft sincronizado (Priority: P1)

Como participante ou espectador autenticado, quero ver em tempo real os times, capitães, jogadores disponíveis, reservas e a ordem de escolha, para acompanhar o andamento do draft sem atualizar a página manualmente.

**Why this priority**: A sincronização de estado é essencial para evitar decisões paralelas, confusão entre capitães e divergência visual.

**Independent Test**: Pode ser testada abrindo a mesma montagem em duas sessões; quando uma escolha acontece em uma sessão, a outra recebe o estado atualizado sem recarregar a página.

**Acceptance Scenarios**:

1. **Given** dois usuários visualizando a mesma montagem, **When** o capitão da vez realiza uma escolha válida, **Then** todos os usuários conectados veem a atualização de times, disponíveis, histórico e próximo turno.
2. **Given** um usuário entra em uma montagem com draft em andamento, **When** a tela é carregada, **Then** ele visualiza o estado oficial atual do draft, incluindo turno e tempo restante aproximado.
3. **Given** um capitão fora da vez visualiza jogadores disponíveis, **When** ele abre detalhes de um jogador, **Then** ele consegue consultar as informações do jogador, mas não consegue escolher.

---

### User Story 3 - Turno expira automaticamente (Priority: P1)

Como organizador, quero que cada capitão tenha no máximo 30 segundos para escolher, para o draft não travar quando alguém demora ou fica ausente.

**Why this priority**: O limite de tempo mantém o fluxo do draft previsível e impede bloqueios operacionais.

**Independent Test**: Pode ser testada iniciando um turno, aguardando 30 segundos sem escolha e confirmando que a vez passa automaticamente para o próximo capitão elegível.

**Acceptance Scenarios**:

1. **Given** um turno ativo, **When** o capitão da vez não escolhe em até 30 segundos, **Then** o turno é registrado como expirado e a vez passa automaticamente.
2. **Given** o timer expira enquanto o capitão tenta escolher, **When** a escolha chega depois da expiração oficial, **Then** a escolha é recusada e prevalece o estado de timeout.
3. **Given** um time já está completo, **When** a ordem de escolha avança, **Then** esse time é ignorado e o próximo capitão com vaga recebe a vez.

---

### User Story 4 - Reserva atua como complemento emergencial (Priority: P2)

Como organizador, quero que reservas não participem da escolha dos capitães e fiquem disponíveis para substituição emergencial, para preencher vaga caso um participante confirmado não compareça ou precise sair.

**Why this priority**: Esta regra preserva o significado de reserva e evita que capitães escolham jogadores fora do pool confirmado.

**Independent Test**: Pode ser testada com uma montagem contendo jogadores livres e reservas; capitães conseguem escolher apenas livres, enquanto reservas permanecem separados e bloqueados para escolha.

**Acceptance Scenarios**:

1. **Given** uma montagem em tempo real com reservas, **When** um capitão tenta escolher um reserva, **Then** a escolha é recusada e o reserva permanece fora dos times.
2. **Given** uma montagem com reservas, **When** usuários visualizam a tela, **Then** os reservas aparecem em área separada com indicação de uso emergencial.
3. **Given** um jogador confirmado precisa sair, **When** um organizador executa uma substituição emergencial por reserva, **Then** o reserva ocupa a vaga no time e a alteração fica registrada.

---

### User Story 5 - Organizadores gerenciam ciclo do draft (Priority: P2)

Como organizador com permissão, quero iniciar, cancelar e finalizar uma montagem de draft em tempo real, para controlar o ciclo oficial da formação de times.

**Why this priority**: A operação do draft precisa ter início e encerramento claros, mas pode ser implementada após o fluxo principal de escolha.

**Independent Test**: Pode ser testada criando uma montagem, iniciando o modo em tempo real, acompanhando escolhas e concluindo ou cancelando a montagem.

**Acceptance Scenarios**:

1. **Given** uma montagem aberta com capitães definidos e jogadores livres, **When** o organizador inicia o draft em tempo real, **Then** o primeiro turno é criado e todos veem o estado inicial sincronizado.
2. **Given** uma montagem em tempo real aberta, **When** o organizador cancela a montagem, **Then** novas escolhas são bloqueadas e todos veem o estado cancelado.
3. **Given** todos os times estão completos ou não existem jogadores livres elegíveis, **When** o draft chega ao fim, **Then** a montagem é marcada como concluída ou pronta para finalização conforme regra do produto.

---

### Edge Cases

- Se dois comandos de escolha forem enviados quase ao mesmo tempo para o mesmo turno, apenas o primeiro válido deve alterar o estado oficial.
- Se o capitão perder conexão durante o turno, o timer continua e a vez passa automaticamente após 30 segundos.
- Se um usuário entrar após vários turnos, ele deve receber o estado oficial atual, não uma reconstrução local.
- Se todos os times elegíveis estiverem completos, o draft deve encerrar sem criar novos turnos.
- Se restarem jogadores livres mas nenhum time tiver vaga, esses jogadores não podem ser escolhidos e o draft deve encerrar.
- Se um capitão for inativado ou perder vínculo de jogador antes de escolher, ele não pode escolher e o sistema deve impedir uso indevido da vez.
- Se uma montagem estiver em modo manual, regras de escolha em tempo real não devem se aplicar.
- Se uma reserva for enviada por engano como escolha, o sistema deve recusar sem alterar times, turno ou histórico.
- Se mensagens de validação ou tela forem exibidas, devem usar catálogos de mensagem e tradução, sem texto hardcoded.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir iniciar o modo de draft em tempo real em uma montagem aberta que tenha capitães definidos e jogadores elegíveis.
- **FR-002**: O sistema MUST manter o backend como fonte oficial do estado da montagem em tempo real, incluindo times, jogadores livres, reservas, capitão da vez, ordem de escolha e tempo do turno.
- **FR-003**: O sistema MUST sincronizar alterações do estado do draft para todos os usuários conectados à mesma montagem.
- **FR-004**: O sistema MUST permitir que apenas capitães ativos vinculados à montagem escolham jogadores.
- **FR-005**: O sistema MUST permitir que apenas o capitão do turno atual escolha jogador.
- **FR-006**: O sistema MUST impedir que capitães fora da vez, participantes comuns e espectadores alterem o estado do draft.
- **FR-007**: O sistema MUST permitir que capitães fora da vez, participantes comuns e espectadores visualizem informações dos jogadores disponíveis e reservas.
- **FR-008**: O sistema MUST permitir escolha apenas de jogadores com estado livre e elegíveis para o pool de escolha.
- **FR-009**: O sistema MUST impedir que jogadores reservas sejam escolhidos por capitães durante o draft.
- **FR-010**: O sistema MUST manter reservas separados dos jogadores livres e identificados como complemento emergencial.
- **FR-011**: O sistema MUST controlar cada turno com duração oficial de 30 segundos.
- **FR-012**: O sistema MUST avançar automaticamente a vez quando o capitão não escolher dentro de 30 segundos.
- **FR-013**: O sistema MUST registrar a ordem de escolhas realizadas e turnos expirados.
- **FR-014**: O sistema MUST avançar a vez seguindo a ordem dos times da montagem e ignorando times completos.
- **FR-015**: O sistema MUST mover o jogador escolhido para o time do capitão da vez e remover esse jogador do pool de livres.
- **FR-016**: O sistema MUST encerrar o draft quando todos os times estiverem completos ou quando não houver mais jogadores livres elegíveis para preencher vagas.
- **FR-017**: O sistema MUST bloquear escolhas em montagens canceladas, finalizadas ou fora do modo em tempo real.
- **FR-018**: O sistema MUST permitir substituição emergencial de um jogador de time por um reserva somente para usuários organizadores autorizados.
- **FR-019**: O sistema MUST registrar substituições emergenciais com time, jogador que saiu, reserva que entrou, motivo e responsável.
- **FR-020**: O sistema MUST impedir substituição emergencial que exceda o tamanho máximo do time ou use um jogador que não esteja como reserva.
- **FR-021**: O sistema MUST apresentar na interface o capitão da vez, o tempo restante aproximado, jogadores livres, reservas, times e histórico do draft.
- **FR-022**: O sistema MUST evitar que a interface dependa de validações locais para regras críticas de escolha, turno, reserva, capitão ativo ou tempo.
- **FR-023**: O sistema MUST exibir todos os textos, rótulos, botões, placeholders, mensagens, erros, confirmações, badges, estados vazios e toasts por meio de tradução.
- **FR-024**: O sistema MUST emitir mensagens do backend por catálogo de resources ou estrutura equivalente de localização.
- **FR-025**: O sistema MUST manter português e inglês sincronizados para toda nova mensagem ou texto visível.

### Key Entities *(include if feature involves data)*

- **Montagem de Draft**: Formação de times existente que passa a poder operar em modo manual ou em tempo real, com status, times, participantes, reservas e estado do turno atual.
- **Time da Montagem**: Time dentro da montagem, com nome, ordem, capitão, cor, limite de jogadores e membros escolhidos.
- **Participante da Montagem**: Jogador incluído na montagem, podendo estar livre, em time ou como reserva.
- **Capitão Ativo**: Participante vinculado a usuário ativo, com papel de capitão e associado a um time da montagem.
- **Turno de Escolha**: Janela oficial em que um capitão específico pode escolher um jogador livre para seu time.
- **Escolha do Draft**: Registro da decisão do capitão ou da expiração do turno, preservando a sequência do draft.
- **Reserva Emergencial**: Participante fora do pool de escolha, usado apenas para substituição de ausência ou saída posterior.
- **Substituição Emergencial**: Registro da troca de um jogador de time por um reserva, com motivo e responsável.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em uma montagem com duas sessões abertas, 95% das escolhas válidas aparecem para todos os usuários conectados em até 2 segundos após a confirmação oficial.
- **SC-002**: 100% das tentativas de escolha por usuário que não é capitão da vez são recusadas sem alterar o estado do draft.
- **SC-003**: 100% dos turnos sem escolha avançam automaticamente após 30 segundos oficiais, com tolerância operacional de até 2 segundos.
- **SC-004**: 100% das tentativas de escolher reservas são recusadas e não modificam times, turno ou histórico.
- **SC-005**: Usuários conseguem identificar em até 5 segundos quem é o capitão da vez, quanto tempo resta e quais jogadores estão livres.
- **SC-006**: Organizadores conseguem concluir um draft completo de 10 jogadores em menos de 8 minutos sem atualização manual da página.
- **SC-007**: Todas as novas mensagens visíveis ao usuário possuem versão em português e inglês antes da entrega.

## Assumptions

- A montagem visual de draft existente é o fluxo principal da rota de drafts e será a base da feature.
- Usuários precisam estar autenticados para participar ou visualizar montagens protegidas.
- Capitães elegíveis são jogadores ativos vinculados a usuários ativos com papel apropriado de capitão.
- Reservas são determinados na criação ou organização da montagem e não participam do pool de escolha.
- A substituição emergencial é uma ação de organizador, não uma escolha de capitão.
- O tempo oficial do turno é controlado pelo backend; o frontend pode apenas exibir contagem aproximada.
- A primeira versão pode ser otimizada para uma única instância de backend, desde que preserve consistência no estado oficial.

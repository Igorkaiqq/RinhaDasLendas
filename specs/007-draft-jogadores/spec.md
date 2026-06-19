# Feature Specification: Draft de Jogadores

**Feature Branch**: `feature/007-draft-jogadores`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "implementar a etapa 7 inteira, a etapa 7 se caracteriza pelo draft de jogadores, utilize o speckit para isso"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar Sessao de Draft (Priority: P1)

Como organizador da rinha, quero iniciar uma sessao de draft com uma lista de jogadores elegiveis para que dois capitaes possam formar times de forma controlada e transparente.

**Why this priority**: Sem uma sessao de draft nao existe fluxo principal para organizar a partida.

**Independent Test**: Pode ser testado criando uma sessao com pelo menos dois capitaes e jogadores disponiveis, validando que a sessao fica pronta para escolhas.

**Acceptance Scenarios**:

1. **Given** existem jogadores ativos suficientes, **When** o organizador cria uma sessao informando nome, capitaes e jogadores disponiveis, **Then** o sistema registra a sessao como aberta e lista os jogadores ainda disponiveis.
2. **Given** a lista contem jogador duplicado ou inativo, **When** o organizador tenta criar a sessao, **Then** o sistema rejeita a criacao com uma mensagem clara.

---

### User Story 2 - Registrar Escolhas Alternadas (Priority: P1)

Como organizador, quero registrar cada escolha de jogador seguindo a ordem do draft para que os times sejam formados sem jogador duplicado e com historico das escolhas.

**Why this priority**: O valor central do draft e controlar a ordem de escolha e impedir inconsistencias na composicao.

**Independent Test**: Pode ser testado criando uma sessao e realizando escolhas ate concluir os dois times.

**Acceptance Scenarios**:

1. **Given** uma sessao aberta com jogadores disponiveis, **When** o organizador escolhe um jogador para o capitao da vez, **Then** o sistema registra a escolha, remove o jogador da lista disponivel e indica o proximo capitao.
2. **Given** um jogador ja foi escolhido, **When** o organizador tenta escolher o mesmo jogador novamente, **Then** o sistema impede a escolha duplicada.
3. **Given** os dois times atingiram o limite configurado da sessao, **When** a ultima escolha valida e registrada, **Then** o sistema marca a sessao como concluida.

---

### User Story 3 - Sortear Capitaes e Ordem (Priority: P2)

Como organizador, quero sortear capitaes e ordem inicial quando o grupo nao decidir manualmente para que o draft possa comecar sem discussao externa.

**Why this priority**: A constituicao exige suporte a capitaes definidos manualmente ou sorteados, e o sorteio reduz atrito no uso real.

**Independent Test**: Pode ser testado criando uma sessao sem capitaes definidos manualmente e verificando que o sistema seleciona dois capitaes distintos e uma ordem de escolha transparente.

**Acceptance Scenarios**:

1. **Given** ha pelo menos quatro jogadores ativos elegiveis, **When** o organizador solicita sorteio de capitaes, **Then** o sistema escolhe dois capitaes distintos e informa que o criterio foi sorteio.
2. **Given** capitaes ja foram definidos manualmente, **When** a sessao e exibida, **Then** o sistema informa que o criterio foi manual.

---

### User Story 4 - Consultar Resultado do Draft (Priority: P3)

Como participante, quero visualizar os times, a ordem das escolhas e o status da sessao para entender como a formacao aconteceu.

**Why this priority**: Transparencia aumenta confianca no draft e evita contestacao do resultado.

**Independent Test**: Pode ser testado consultando uma sessao com escolhas registradas e validando que as informacoes aparecem em ordem correta.

**Acceptance Scenarios**:

1. **Given** uma sessao possui escolhas registradas, **When** o usuario consulta o draft, **Then** o sistema mostra capitaes, jogadores de cada time, escolhas em ordem e jogadores restantes.
2. **Given** a sessao foi concluida, **When** o usuario consulta o draft, **Then** o sistema mostra o status concluido e impede novas escolhas.

---

### Edge Cases

- Criacao deve falhar quando houver menos jogadores elegiveis do que o minimo necessario para dois capitaes e pelo menos uma escolha.
- Criacao deve falhar quando os capitaes manuais forem iguais, inativos ou nao estiverem na lista de jogadores elegiveis.
- Uma escolha deve falhar quando a sessao estiver concluida ou cancelada.
- Uma escolha deve falhar quando o jogador escolhido nao estiver na lista de disponiveis.
- O sistema deve preservar a ordem historica das escolhas mesmo quando a tela for recarregada.
- O sistema deve deixar claro se capitaes e ordem foram definidos manualmente ou por sorteio.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an organizer to create a draft session with a display name, optional notes, maximum players per team, eligible players, and either manual captains or captain draw.
- **FR-002**: System MUST validate that all eligible players are active and unique before creating a draft session.
- **FR-003**: System MUST require two distinct captains for every draft session, selected manually or by draw.
- **FR-004**: System MUST record whether captains and first pick order were chosen manually or by draw.
- **FR-005**: System MUST expose the current captain/team that must pick next.
- **FR-006**: System MUST allow a valid available player to be picked for the current captain/team.
- **FR-007**: System MUST prevent the same player from being selected more than once in the same draft session.
- **FR-008**: System MUST prevent picks after a draft session is concluded or canceled.
- **FR-009**: System MUST automatically conclude a draft session when both teams reach the configured player limit or there are no valid players left to pick.
- **FR-010**: System MUST allow an organizer to cancel an open draft session with optional reason.
- **FR-011**: System MUST allow users to list draft sessions and filter by status.
- **FR-012**: System MUST allow users to view a draft session with captains, teams, pick history, available players, status, and criteria used.
- **FR-013**: System MUST provide clear validation messages for invalid players, duplicate picks, inactive players, invalid captains, and invalid session status.
- **FR-014**: System MUST keep all critical draft rules outside user interface-only behavior so manual API or page refresh cannot corrupt a draft.

### Key Entities

- **Draft Session**: Represents one player draft, including name, status, team size, selection criteria, captains, available players, pick history, notes, creation date and update date.
- **Draft Participant**: Represents a player included in a draft session and whether the player is captain, available, or already assigned to a team.
- **Draft Pick**: Represents one choice in the draft order, including sequence number, team side, captain, selected player and timestamp.
- **Draft Team**: Represents the current composition for each side of the draft, anchored by one captain and filled through picks.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An organizer can create a valid draft session and register the first pick in under 2 minutes.
- **SC-002**: 100% of attempted duplicate player picks are blocked with a clear message.
- **SC-003**: 100% of completed draft sessions display both final teams and the full ordered pick history.
- **SC-004**: Users can identify whether captains and first pick were manual or drawn in every draft session view.
- **SC-005**: The primary draft flow works without any Discord or Riot integration.

## Assumptions

- The feature is for internal organizers using the existing player base.
- Authentication and authorization are not expanded in this step; the UI assumes trusted internal usage already established by the app.
- A standard draft creates two teams with up to five players each, but the session stores the configured team size to support smaller internal games.
- Captains are also players in their teams and count toward the team size.
- Draft choices alternate between Team A and Team B after the first pick; snake draft is out of scope for this MVP step unless added later.
- Riot and Discord integrations are out of scope; all inputs are manual or based on registered players.

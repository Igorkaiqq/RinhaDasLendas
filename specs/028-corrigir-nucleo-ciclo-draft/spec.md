# Feature Specification: Corrigir Núcleo do Ciclo de Draft

**Feature Branch**: `feature/028-corrigir-nucleo-ciclo-draft`

**Created**: 2026-07-29

**Status**: Concluída

**Input**: Corrigir as regras centrais do ciclo de draft, distinguindo montagem manual de draft em tempo real, aplicando corretamente o cargo global e a designação diária de capitão e protegendo finalização, timeout e substituições.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Escolher o modo depois da presença (Priority: P1)

Como Admin+, quero escolher entre montagem manual e draft em tempo real depois de encerrar a presença, para que cada fluxo solicite somente as etapas necessárias ao seu modo.

**Why this priority**: O fluxo atual trata todo draft como manual antes de opcionalmente iniciar tempo real, obrigando capitães e ordem mesmo quando Admin+ montará os times diretamente.

**Independent Test**: Encerrar uma presença válida e verificar que nenhuma montagem ou seleção de capitães avança antes da escolha do modo; selecionar cada modo e confirmar seu próximo estado independente.

**Acceptance Scenarios**:

1. **Given** uma presença encerrada de um draft novo, **When** Admin+ consulta a preparação, **Then** o sistema exige a escolha entre `Manual` e `TempoReal`.
2. **Given** uma presença encerrada, **When** Admin+ escolhe `Manual`, **Then** o draft abre o board administrativo sem exigir capitães ou ordem de picks.
3. **Given** uma presença encerrada, **When** Admin+ escolhe `TempoReal`, **Then** o draft avança para a seleção dos capitães diários antes da ordem e do início.
4. **Given** um usuário abaixo de Admin, **When** tenta escolher ou alterar o modo, **Then** a operação é recusada sem mutação.
5. **Given** uma criação direta com jogadores previamente selecionados, **When** Admin+ confirma a montagem, **Then** o draft nasce em modo `Manual` sem passar por presença, escolha de modo, capitães ou ordem.

---

### User Story 2 - Montar times manualmente sem capitães (Priority: P1)

Como Admin+, quero distribuir todos os titulares manualmente sem definir capitães, para concluir uma montagem administrativa sem regras exclusivas do tempo real.

**Why this priority**: Capitão e autoridade de pick só possuem função no draft em tempo real.

**Independent Test**: Selecionar `Manual`, distribuir todos os titulares em times completos sem marcar capitães e finalizar com sucesso.

**Acceptance Scenarios**:

1. **Given** um draft manual aberto, **When** Admin+ salva um layout completo sem capitães, **Then** o layout é aceito.
2. **Given** titulares ainda livres ou time incompleto, **When** Admin+ tenta finalizar, **Then** a operação é recusada com mensagem localizada.
3. **Given** todos os titulares distribuídos e todos os times completos, **When** Admin+ finaliza, **Then** o draft se torna `Finalizada`.
4. **Given** um draft manual finalizado, **When** qualquer usuário tenta alterar layout, capitão, pick ou substituição, **Then** nenhuma alteração é aceita.

---

### User Story 3 - Definir capitães específicos do draft em tempo real (Priority: P1)

Como Admin+, quero selecionar os capitães daquele dia somente entre titulares elegíveis com cargo global `Capitão`, para conceder autoridade de escolha apenas a quem exercerá a função neste draft.

**Why this priority**: Cargo global representa elegibilidade; autoridade de escolha pertence a uma designação específica por draft.

**Independent Test**: Encerrar uma presença com titulares e reservas que possuam ou não o cargo global, selecionar capitães válidos e comprovar que outros jogadores com o mesmo cargo continuam jogadores comuns draftáveis.

**Acceptance Scenarios**:

1. **Given** jogadores ordenados pela presença, **When** o recorte titular é calculado, **Then** confirmados fora da capacidade permanecem reservas independentemente de possuírem cargo `Capitão`.
2. **Given** um titular ativo com cargo global `Capitão`, **When** Admin+ o seleciona, **Then** ele se torna capitão somente daquele draft.
3. **Given** um titular sem cargo global `Capitão`, **When** Admin+ tenta selecioná-lo como capitão diário, **Then** a operação é recusada.
4. **Given** uma reserva com cargo global `Capitão`, **When** Admin+ tenta selecioná-la antes do início, **Then** a operação é recusada porque ela não pertence ao recorte titular.
5. **Given** um jogador com cargo global `Capitão` não selecionado para o draft, **When** outro capitão o escolhe, **Then** ele entra no time como jogador comum e não recebe autoridade de pick.
6. **Given** um capitão diário inativado antes do início ou do próprio turno, **When** a ação depende de sua elegibilidade, **Then** o sistema exige substituição por capitão elegível.

---

### User Story 4 - Iniciar e concluir o tempo real com invariantes consistentes (Priority: P1)

Como participante do draft, quero que ordem, turnos, timeout, substituições e finalização preservem a autoridade correta, para que o ciclo não trave nem permita alterações indevidas.

**Why this priority**: A troca incorreta de capitão e estados permissivos podem bloquear o novo responsável ou alterar resultados já finalizados.

**Independent Test**: Definir capitães, definir ordem, iniciar explicitamente, executar picks e timeout, substituir o capitão da vez e concluir com times completos.

**Acceptance Scenarios**:

1. **Given** capitães diários definidos, **When** Admin+ define uma ordem válida, **Then** o draft entra em `OrdemDefinida` sem iniciar o primeiro turno.
2. **Given** um draft em `OrdemDefinida`, **When** Admin+ inicia o tempo real, **Then** o primeiro turno é criado uma única vez para o primeiro capitão da ordem.
3. **Given** um turno expirado sem escolha, **When** o timeout é processado, **Then** a tentativa entra no histórico, a sequência avança e o time recebe nova oportunidade em rodada posterior se ainda tiver vaga.
4. **Given** o capitão do turno atual precisa ser substituído, **When** Admin+ escolhe explicitamente um novo capitão elegível, **Then** time, participante e autoridade do turno mudam atomicamente.
5. **Given** uma reserva com cargo `Capitão` entrando por outro jogador, **When** Admin+ não a designa como novo capitão, **Then** ela entra somente como jogadora.
6. **Given** times completos, **When** a última escolha válida é registrada, **Then** o draft finaliza e limpa o turno ativo.
7. **Given** um draft em tempo real ou finalizado, **When** uma chamada tenta sortear capitães ou alterar dados proibidos para o estado, **Then** a operação é recusada no backend independentemente da interface.

---

### User Story 5 - Preservar drafts ativos anteriores (Priority: P1)

Como organizador, quero concluir drafts ativos criados antes da mudança sem reiniciar sua preparação, para evitar perda de trabalho ou mudança inesperada em eventos em andamento.

**Why this priority**: O sistema possui dados persistidos em produção e não pode reinterpretar silenciosamente modo, capitães ou ordem existentes.

**Independent Test**: Restaurar drafts legados em cada estado não terminal suportado e confirmar que seguem suas transições anteriores, enquanto drafts novos exigem escolha explícita de modo.

**Acceptance Scenarios**:

1. **Given** um draft ativo criado antes da ativação da feature, **When** ele é carregado, **Then** preserva modo, estado, capitães, ordem e próximas ações do fluxo legado.
2. **Given** um draft novo criado depois da ativação, **When** sua presença é encerrada, **Then** ele exige escolha explícita de modo.
3. **Given** um draft legado terminal, **When** a migração é aplicada, **Then** seu resultado e histórico permanecem inalterados.

### Edge Cases

- A quantidade de titulares é definida pela ordem final, manual ou de confirmação já persistida, limitada a `QuantidadeTimes * TamanhoEquipe`.
- Não haver titulares suficientes com cargo global `Capitão` impede iniciar o modo tempo real, mas não impede escolher o modo manual.
- Perda do cargo global, inativação ou ausência de um capitão diário exige nova designação antes de sua próxima ação.
- A entrada de uma reserva nunca concede autoridade de capitão implicitamente.
- Timeout pode fazer a quantidade de tentativas superar a quantidade de vagas, sem aumentar a capacidade dos times.
- Se um time permanecer incompleto sem titular livre elegível, o draft aguarda intervenção Admin+ e não finaliza automaticamente.
- Uma requisição repetida de escolha de modo ou início não cria times, turnos ou histórico duplicados.
- Alterações concorrentes de pick, timeout, substituição e finalização preservam uma única versão válida do agregado.
- Criação direta continua restrita a Admin+ e não pode iniciar implicitamente um draft em tempo real.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Drafts novos originados de lista de presença MUST permanecer sem modo operacional definido até Admin+ escolher `Manual` ou `TempoReal` depois do encerramento da presença.
- **FR-002**: Somente usuários `Admin` ou `SuperAdmin` MUST poder escolher modo, montar manualmente, definir capitães e ordem, iniciar, substituir ou finalizar.
- **FR-003**: O modo `Manual` MUST abrir o board sem exigir capitães diários nem ordem de picks.
- **FR-004**: Layout manual MUST aceitar times sem capitão e MUST exigir todos os titulares atribuídos exatamente uma vez.
- **FR-005**: Finalização manual MUST exigir todos os times com `TamanhoEquipe` jogadores e nenhum titular livre.
- **FR-006**: O modo `TempoReal` MUST exigir exatamente um capitão diário por time antes da ordem.
- **FR-007**: Capitão diário MUST ser jogador ativo, confirmado, pertencente ao recorte titular e associado a usuário com cargo global `Capitão`.
- **FR-008**: Cargo global `Capitão` MUST NOT conceder autoridade de pick sem designação explícita naquele draft.
- **FR-009**: Jogador com cargo global `Capitão` não designado no draft MUST permanecer elegível como jogador comum.
- **FR-010**: Reserva MUST NOT ser selecionada como capitão diário antes do início, ainda que possua cargo global `Capitão`.
- **FR-011**: Definir ordem MUST produzir o estado `OrdemDefinida` sem iniciar turnos.
- **FR-012**: Iniciar tempo real MUST ser uma transição explícita de `OrdemDefinida` para `Aberta` em modo `TempoReal`.
- **FR-013**: Timeout MUST registrar a tentativa, avançar a sequência e manter o time elegível para nova rodada enquanto possuir vaga e houver titular livre.
- **FR-014**: Substituição MUST exigir seleção explícita da reserva e MUST NOT transferir o papel de capitão automaticamente.
- **FR-015**: Substituição do capitão atual MUST exigir novo capitão elegível e atualizar atomicamente a autoridade do turno.
- **FR-016**: Draft `Finalizada` ou `Cancelada` MUST rejeitar layout, sorteio ou definição de capitães, picks e substituições.
- **FR-017**: Modo tempo real MUST finalizar automaticamente somente quando todos os times estiverem completos; ausência de titular livre elegível com vaga restante MUST manter o draft aberto para intervenção Admin+.
- **FR-018**: Drafts ativos anteriores à feature MUST preservar o fluxo legado; drafts novos MUST usar o novo ciclo.
- **FR-019**: Todas as novas validações MUST usar mensagens localizadas em português e inglês.
- **FR-020**: O backend MUST permanecer a fonte de verdade para cargo, elegibilidade, estado, capacidade e autorização.
- **FR-021**: Operações concorrentes MUST preservar controle otimista e impedir mais de uma transição vencedora para o mesmo estado ou turno.
- **FR-022**: Criação direta com jogadores selecionados MUST permanecer disponível somente para Admin+ e MUST produzir uma montagem `Manual` sem presença, escolha de modo, capitães ou ordem.

### Key Entities

- **Modo Operacional**: Escolha posterior à presença que direciona o draft para montagem `Manual` ou `TempoReal`; permanece indefinido em drafts novos até decisão de Admin+.
- **Cargo Global de Capitão**: Papel de usuário que torna o jogador elegível a ser selecionado como capitão diário, sem conceder autoridade automática em drafts.
- **Capitão Diário**: Titular elegível designado para comandar um time e exercer picks somente naquele draft.
- **Recorte Titular**: Primeiros jogadores confirmados segundo a ordem persistida, limitado pela capacidade total dos times.
- **Reserva**: Confirmado fora do recorte titular, inelegível a capitão inicial e disponível para substituição.
- **Tentativa de Escolha**: Registro sequencial de pick ou timeout; timeout consome a tentativa, mas não preenche vaga.
- **Draft Legado**: Draft criado antes da ativação do novo ciclo e autorizado a concluir pelas regras operacionais anteriores.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos drafts novos originados de lista de presença exigem escolha explícita de modo depois do encerramento da presença.
- **SC-002**: Draft manual completo finaliza sem capitães, e 100% das tentativas de finalizar layout incompleto são recusadas.
- **SC-003**: 100% dos capitães diários selecionados pertencem ao recorte titular, estão ativos e possuem cargo global `Capitão`.
- **SC-004**: Jogadores com cargo `Capitão` não selecionados permanecem draftáveis e não recebem autoridade em 100% dos cenários cobertos.
- **SC-005**: Substituir o capitão da vez permite ao novo capitão agir e impede o removido na mesma versão persistida.
- **SC-006**: Timeout registrado mantém o time elegível até completar sua vaga sem ultrapassar o tamanho do time.
- **SC-007**: Nenhuma operação de layout, capitão, pick ou substituição altera um draft terminal.
- **SC-008**: Drafts legados não terminais concluem sem redefinir modo, capitães ou ordem existentes.
- **SC-009**: Testes de domínio, validadores, handlers, integração e frontend cobrem os dois modos e todas as negativas de cargo, recorte, estado e autorização.

## Assumptions

- O cargo global `Capitão` já existe no RBAC e o jogador elegível possui vínculo com um usuário desse cargo.
- Admin+ significa exclusivamente `Admin` e `SuperAdmin`; `Moderador` não administra o novo ciclo.
- A ordem de presença existente continua sendo a fonte de verdade do recorte titular.
- O tamanho de time permanece entre os limites de domínio existentes.
- A compatibilidade de drafts legados será explícita e removível apenas depois que não houver registros ativos anteriores à feature.
- A criação direta existente representa exclusivamente uma montagem manual administrativa.

## Out of Scope

- Entrega das correções de SignalR, timers, refresh e proteção de layout previstas para a feature 029.
- Entrega das correções de publicação, polling, guild boundary e credenciais Discord previstas para a feature 030.
- Promoção automática de usuários ao cargo global `Capitão`.
- Alteração da ordem de presença para incluir capitães confirmados fora do recorte titular.
- Publicação automática dos times finalizados nesta feature.

## Evidências de Conclusão

- Revisão independente final: aprovada sem findings críticos, importantes ou menores.
- Backend: `724/724` testes aprovados e build Release sem warnings ou erros.
- Frontend: `525/525` testes aprovados, lint e build de produção concluídos.
- Concorrência: cinco rodadas focadas, `25/25`, sem resposta HTTP 500; deadlock PostgreSQL `40P01` convertido em conflito `409`.
- Chromium: fluxo Admin+ validado em desktop e mobile, incluindo escolha de modo, rail responsivo e criação direta manual sem capitães.
- Internacionalização: catálogos PT/EN sincronizados, resources backend equivalentes e ausência de novos textos visíveis hardcoded.

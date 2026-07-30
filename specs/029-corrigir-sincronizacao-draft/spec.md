# Feature Specification: Corrigir Sincronização e Operação do Draft

**Feature Branch**: `feature/029-corrigir-sincronizacao-draft`

**Created**: 2026-07-30

**Status**: Draft

**Input**: Corrigir sincronização, timers, refresh, estado obsoleto e operação multicliente do ciclo de draft sem alterar as regras centrais entregues pela feature 028.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manter todos os clientes no mesmo estado (Priority: P1)

Como participante de um draft, quero receber cada mudança relevante na ordem correta, para acompanhar presença, preparação, escolhas, substituições e finalização sem recarregar a página.

**Why this priority**: Divergência entre clientes pode conceder ações indevidas, esconder a vez atual e comprometer a execução da rinha.

**Independent Test**: Abrir o mesmo draft em duas sessões, executar cada transição do ciclo em uma delas e comprovar que ambas convergem para a maior versão persistida sem aplicar estado antigo ou capacidade de outro usuário.

**Acceptance Scenarios**:

1. **Given** dois usuários no mesmo draft, **When** presença, modo, capitães, ordem, início, pick, timeout, substituição, layout, finalização, cancelamento, arquivamento ou restauração altera informação exibida, **Then** ambos recebem ou reconciliam o novo estado sem reload manual.
2. **Given** respostas e notificações resolvidas fora de ordem, **When** uma versão antiga chega depois de uma versão nova, **Then** a versão antiga é descartada.
3. **Given** uma informação que depende do usuário atual, **When** o estado compartilhado é distribuído, **Then** essa informação não é reutilizada por outros usuários e cada sessão preserva sua própria capacidade.
4. **Given** uma operação concorrente rejeitada, **When** o cliente recebe conflito, **Then** ele busca o estado canônico antes de permitir nova ação.

---

### User Story 2 - Recuperar sincronização após perda de conexão (Priority: P1)

Como participante, quero que a tela se recupere automaticamente de interrupções temporárias, para continuar o draft sem precisar descobrir que o estado ficou desatualizado.

**Why this priority**: Uma queda curta de conexão é comum e não pode deixar o cliente silenciosamente desconectado durante um turno com prazo.

**Independent Test**: Interromper e restaurar o canal em tempo real durante um turno, produzir alterações enquanto ele está indisponível e comprovar reconexão, reconciliação e retomada do estado correto.

**Acceptance Scenarios**:

1. **Given** um draft aberto, **When** a sessão inicia a sincronização, **Then** ela começa a escutar mudanças antes de buscar o estado canônico inicial.
2. **Given** perda temporária do canal, **When** a sessão está reconectando, **Then** consultas periódicas mantêm a tela atualizada até a conexão saudável retornar.
3. **Given** esgotamento das tentativas automáticas, **When** a tela permanece aberta, **Then** novas tentativas controladas continuam sem duplicar consultas ou callbacks.
4. **Given** troca rápida de draft ou saída da tela, **When** respostas da sessão anterior chegam, **Then** callbacks, consultas e timers antigos são descartados.
5. **Given** sincronização degradada, **When** seu estado muda, **Then** o usuário recebe indicação discreta, acessível e localizada; conexão saudável não adiciona ruído visual.

---

### User Story 3 - Preservar sucesso e continuidade dos processamentos automáticos (Priority: P1)

Como organizador, quero que operações persistidas continuem bem-sucedidas mesmo quando a notificação falha e que um draft defeituoso não interrompa os demais, para evitar repetição, duplicação e atraso do lote.

**Why this priority**: Retornar erro depois de persistir induz repetição perigosa, enquanto uma falha isolada em worker pode bloquear timeout ou fechamento dos outros drafts.

**Independent Test**: Forçar falha de notificação depois do commit e conflito no primeiro item de um lote, comprovando sucesso funcional único, autoria auditável e processamento dos itens seguintes.

**Acceptance Scenarios**:

1. **Given** uma mutação persistida, **When** a notificação posterior falha, **Then** a resposta funcional permanece de sucesso e a falha fica observável para recuperação.
2. **Given** múltiplos drafts elegíveis a timeout ou fechamento automático, **When** um deles falha, **Then** os demais são processados em contextos independentes.
3. **Given** uma ação automática que exige autoria, **When** o worker a executa, **Then** a auditoria identifica autoria sistêmica válida sem simular usuário humano.
4. **Given** conflito ou repetição de uma ação automática, **When** o estado já avançou, **Then** não há histórico, timeout, cancelamento ou notificação duplicada.

---

### User Story 4 - Proteger montagem manual não salva (Priority: P1)

Como Admin+, quero ser avisado antes que uma atualização remota ou navegação descarte meu layout manual ainda não salvo, para não perder trabalho silenciosamente.

**Why this priority**: O board atual substitui o estado local em qualquer refresh, causando perda invisível durante colaboração administrativa.

**Independent Test**: Alterar um layout local, receber versão remota maior e tentar trocar de draft, sair da rota ou fechar a página, verificando que nenhuma ação descarta a edição sem decisão explícita.

**Acceptance Scenarios**:

1. **Given** layout local alterado, **When** chega atualização remota, **Then** a edição permanece protegida e o usuário escolhe entre continuar editando ou aplicar o estado canônico.
2. **Given** layout local alterado, **When** o usuário troca de draft, muda de rota, filtra o draft atual, arquiva ou fecha a página, **Then** existe confirmação antes do descarte.
3. **Given** confirmação para descartar, **When** o usuário aceita, **Then** o estado canônico mais recente é aplicado.
4. **Given** salvamento bem-sucedido, **When** a versão persistida correspondente é aplicada, **Then** o estado deixa de ser marcado como não salvo.
5. **Given** conflito ao salvar layout, **When** o estado canônico é recarregado, **Then** a edição local não é apagada antes da escolha do usuário.

---

### User Story 5 - Operar timers e enriquecimentos sem degradar o ciclo (Priority: P2)

Como operador da plataforma, quero que verificações frequentes consultem apenas o necessário e que dados auxiliares não bloqueiem o draft, para manter resposta previsível e logs úteis.

**Why this priority**: Consultas completas a cada segundo e falhas auxiliares aumentam custo e podem impedir a sincronização principal sem agregar valor ao ciclo.

**Independent Test**: Executar o timer com drafts ativos e falhar uma busca auxiliar, comprovando consulta inicial mínima, carregamento completo somente para candidatos e continuidade da tela principal.

**Acceptance Scenarios**:

1. **Given** muitos drafts não expirados, **When** o timer procura candidatos, **Then** consulta somente identificadores e instantes necessários, sem carregar coleções completas.
2. **Given** candidatos realmente expirados, **When** são processados, **Then** apenas esses drafts têm o estado completo carregado.
3. **Given** falha em busca auxiliar de presença, **When** o detalhe principal está disponível, **Then** o draft abre, sincroniza e restringe o erro ao controle dependente dessa busca.
4. **Given** operação normal em produção, **When** o timer executa sem erro, **Then** consultas rotineiras não dominam os logs e falhas relevantes permanecem visíveis.

### Edge Cases

- Uma notificação pode chegar entre a entrada no canal e a primeira consulta canônica.
- Duas respostas da mesma versão podem conter metadados personalizados diferentes; o agregado compartilhado não deve regredir.
- A conexão pode cair novamente enquanto o fallback ou uma reconexão anterior ainda está em andamento.
- O draft pode ser cancelado, arquivado ou removido da lista enquanto existe layout local não salvo.
- O navegador pode estar com horário local adiantado ou atrasado; prazos continuam baseados no horário oficial.
- Um worker pode sofrer conflito, cancelamento solicitado, falha de domínio ou falha de notificação em itens diferentes do mesmo lote.
- O usuário pode perder permissão ou elegibilidade durante a desconexão; a reconciliação personalizada deve refletir a condição atual.
- Uma busca auxiliar antiga pode terminar depois da troca de draft e não deve preencher o novo contexto.
- A notificação pode falhar permanentemente depois do commit; a operação não é revertida nem repetida automaticamente pelo cliente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST distribuir somente estado compartilhado e versionado nas notificações destinadas a todos os clientes de um draft.
- **FR-002**: O sistema MUST resolver capacidades dependentes da identidade separadamente para cada sessão e MUST NOT reutilizar a capacidade calculada para o autor de uma operação.
- **FR-003**: Toda transição persistida que altere informação exibida no draft MUST produzir uma tentativa de atualização após a persistência bem-sucedida.
- **FR-004**: Falha de atualização posterior à persistência MUST NOT converter uma operação já confirmada em falha funcional.
- **FR-005**: Falhas de atualização MUST ser registradas de forma observável, sem dados sensíveis, para diagnóstico e recuperação por reconciliação.
- **FR-006**: Operações rejeitadas ou conflitantes MUST NOT produzir notificação de sucesso.
- **FR-007**: A sessão MUST iniciar a escuta e ingressar no contexto do draft antes de consultar o estado canônico inicial.
- **FR-008**: O ingresso no contexto de sincronização MUST validar existência do draft e autorização de visualização.
- **FR-009**: O cliente MUST tentar reconexão com intervalos limitados e explícitos enquanto a tela permanecer ativa.
- **FR-010**: Durante conexão degradada, o cliente MUST consultar periodicamente o estado personalizado e MUST interromper esse fallback quando a conexão estiver saudável ou a tela for encerrada.
- **FR-011**: Cada abertura de draft MUST possuir uma geração que invalide callbacks, consultas, retries e timers das gerações anteriores.
- **FR-012**: O cliente MUST aplicar estado compartilhado por identificador do draft, geração atual e versão monotônica persistida.
- **FR-013**: Uma versão inferior MUST NOT substituir uma versão superior, independentemente da ordem de chegada de resposta HTTP, notificação ou refresh.
- **FR-014**: Respostas da mesma versão MAY atualizar somente informações personalizadas ou temporais sem regredir o estado compartilhado.
- **FR-015**: O controle de respostas de mutação MUST ser independente do controle de refresh passivo para que uma consulta antiga não invalide um sucesso mais novo.
- **FR-016**: Após conflito em pick, timeout, capitães, ordem, substituição, layout ou outra mutação do ciclo, o cliente MUST reconciliar imediatamente o estado canônico antes de liberar nova tentativa.
- **FR-017**: O relógio do turno MUST continuar usando o deslocamento do horário oficial recalculado em cada reconciliação.
- **FR-018**: O cliente MUST representar conexão saudável, reconectando, fallback e desconectada, exibindo somente estados degradados em região acessível e localizada.
- **FR-019**: Busca auxiliar MUST ser tratada como enriquecimento opcional e sua falha MUST NOT impedir carregamento do detalhe nem sincronização principal.
- **FR-020**: Respostas auxiliares de outro draft, geração ou requisição obsoleta MUST ser descartadas.
- **FR-021**: O board manual MUST comunicar à tela quando possui layout alterado e ainda não persistido.
- **FR-022**: Atualização remota, troca de draft, mudança de rota, remoção do draft atual, arquivamento ou fechamento da página MUST NOT descartar layout não salvo sem confirmação explícita.
- **FR-023**: Ao manter a edição após versão remota maior, o sistema MUST preservar layout local e versão-base e MUST exigir reconciliação antes do salvamento.
- **FR-024**: Ao descartar a edição, o sistema MUST aplicar a maior versão canônica disponível.
- **FR-025**: Salvamento de layout MUST limpar o estado não salvo somente depois que a versão persistida correspondente for aplicada.
- **FR-026**: Cada item processado por timer ou fechamento automático MUST usar contexto independente para que falha de um draft não interrompa os demais.
- **FR-027**: Ações automáticas MUST registrar autoria sistêmica explícita e auditável quando não houver usuário humano.
- **FR-028**: O timer MUST localizar candidatos usando somente identificador e instantes necessários e carregar o agregado completo apenas para drafts realmente processados.
- **FR-029**: O sistema MUST evitar que conflito, retry ou repetição automática duplique histórico, turno, cancelamento, auditoria ou notificação.
- **FR-030**: Logs rotineiros de consulta do timer em produção MUST ser reduzidos sem ocultar falhas, conflitos e métricas operacionais.
- **FR-031**: Novos estados, conflitos, confirmações e orientações MUST estar disponíveis em português e inglês, com acentuação correta e sem texto visível hardcoded.
- **FR-032**: O fluxo MUST permanecer funcional com uma única réplica e sem depender de infraestrutura distribuída adicional.

### Key Entities

- **Snapshot Compartilhado do Draft**: Estado canônico visível a todos, identificado pelo draft e por uma versão monotônica, sem capacidades específicas de usuário.
- **Estado Personalizado da Sessão**: Informações calculadas para a identidade atual, incluindo capacidades e referência do horário oficial.
- **Geração de Sincronização**: Identifica uma abertura específica do draft e delimita callbacks, consultas, retries e timers válidos.
- **Edição Local de Layout**: Layout ainda não persistido, sua versão-base e a decisão pendente quando existe estado remoto mais novo.
- **Candidato de Processamento Automático**: Referência mínima de um draft potencialmente expirado ou encerrável, carregado por completo somente durante seu processamento isolado.
- **Autoria Sistêmica**: Identidade auditável reservada a ações automáticas sem atribuição falsa a usuário humano.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em condições normais, 100% das mudanças de turno e estado aparecem nas demais sessões conectadas em até 2 segundos.
- **SC-002**: Após o backend voltar a ficar disponível, uma sessão degradada converge para o estado canônico em até 5 segundos sem reload manual.
- **SC-003**: Em testes com respostas fora de ordem, 100% dos snapshots inferiores à maior versão aplicada são descartados.
- **SC-004**: Todas as transições relevantes do ciclo possuem cobertura que comprova tentativa de atualização pós-persistência e ausência de evento em mutações rejeitadas.
- **SC-005**: Nenhuma falha exclusiva de notificação transforma uma mutação persistida em resposta funcional de erro ou duplica a operação em nova tentativa.
- **SC-006**: Em lote com um item defeituoso, 100% dos demais drafts elegíveis continuam processados e auditados independentemente.
- **SC-007**: Nenhum cenário coberto de atualização remota, navegação ou fechamento perde layout local não salvo sem confirmação explícita.
- **SC-008**: A busca periódica de candidatos não carrega times, participantes, presenças, escolhas, substituições, publicações ou auditorias completas.
- **SC-009**: Falha de busca auxiliar não impede detalhe principal, sincronização nem ações que não dependam do enriquecimento.
- **SC-010**: Estados degradados e decisões de reconciliação são compreensíveis por teclado e leitor de tela em português e inglês nos viewports suportados.
- **SC-011**: Duas sessões completam a jornada da presença à finalização, incluindo timeout, substituição e reconexão, sem divergência persistente ou reload manual.

## Assumptions

- A feature 028 permanece como fonte das regras de modo, capitães, ordem, timeout, substituição e finalização; esta feature não redefine essas invariantes.
- A versão persistida do draft é monotônica e continua sendo a referência de precedência entre snapshots.
- O endpoint personalizado existente continua disponível para reconciliação autenticada.
- Produção utiliza uma única réplica do backend durante esta entrega.
- O intervalo operacional atual do timer continua adequado à meta de atualização em até 2 segundos.
- O histórico de ações já existente pode representar autoria sistêmica sem migração destrutiva de dados anteriores.
- Confirmações de descarte seguem o padrão visual e acessível já adotado no projeto.

## Out of Scope

- Redis, backplane ou escalonamento horizontal do backend.
- Event sourcing, eventos incrementais por entidade ou outbox transacional genérico.
- Redesenho visual da mesa de draft.
- Alteração de duração, ordem, capacidade, elegibilidade ou demais regras centrais da feature 028.
- Correções de publicação, polling, guild boundary e credenciais Discord reservadas à feature 030.

# Feature Specification: Agendamento Recorrente de Listas de Presença

**Feature Branch**: `feature/020-agendamento-listas-presenca`

**Created**: 2026-07-23

**Status**: Draft - aguardando aprovação dos artefatos

**Input**: Agendas semanais administradas por Moderador+ que criam drafts com presença aberta e os encaminham ao fluxo existente de publicação no Discord sem duplicações.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar e manter uma agenda semanal (Priority: P1)

Como usuário com permissão de gerenciamento de drafts, quero criar, editar, pausar, reativar e excluir logicamente agendas semanais, para automatizar listas de presença sem acessar configurações sensíveis do Discord.

**Why this priority**: A agenda configurável é a origem do valor da feature e precisa existir antes de qualquer execução automática.

**Independent Test**: Um Moderador acessa `/configuracoes`, cria uma agenda com nome, observação opcional, dias e horários válidos, edita os dados futuros, pausa, reativa e arquiva a agenda sem visualizar ou alterar guild, canais, token ou ativação global do bot.

**Acceptance Scenarios**:

1. **Given** um Moderador com `CanManageDrafts`, **When** cria uma agenda com ao menos um dia, publicação e encerramento no mesmo dia, **Then** a agenda fica ativa no fuso `America/Sao_Paulo` e registra autoria e auditoria.
2. **Given** uma agenda ativa, **When** o Moderador edita nome, observação, dias ou horários, **Then** somente ocorrências ainda não criadas usam a nova configuração.
3. **Given** uma agenda ativa ou pausada, **When** o Moderador pausa ou reativa repetidamente, **Then** a operação é idempotente e auditada.
4. **Given** uma agenda existente, **When** o Moderador a exclui, **Then** ela é arquivada logicamente, deixa de aparecer nas consultas usuais e não altera drafts já criados.

---

### User Story 2 - Criar e publicar uma ocorrência exatamente uma vez (Priority: P1)

Como organizador, quero que cada data de uma agenda ativa produza no máximo um draft e uma publicação principal, para evitar listas duplicadas mesmo com múltiplos processadores ou reinícios.

**Why this priority**: A execução confiável é a finalidade da agenda e duplicações afetam diretamente jogadores e operação no Discord.

**Independent Test**: Duas execuções concorrentes avaliam a mesma agenda e data dentro da janela; ao final existe exatamente uma ocorrência, um draft com times de cinco e uma publicação de presença pendente para o fluxo existente do bot.

**Acceptance Scenarios**:

1. **Given** uma agenda ativa, configurada e devida, **When** ela é avaliada dentro da janela, **Then** uma ocorrência, um draft nomeado `Nome configurado - dd/MM/yyyy` e uma publicação pendente tornam-se visíveis juntos.
2. **Given** processadores concorrentes para a mesma agenda e data local, **When** ambos tentam executar, **Then** somente um claim expirável vence e no máximo um draft é criado.
3. **Given** um processador interrompido após adquirir o claim sem concluir, **When** cinco minutos expiram e a janela continua aberta, **Then** outro processador pode recuperar a ocorrência.
4. **Given** uma ocorrência já criada, **When** ciclos posteriores avaliam a mesma agenda e data, **Then** nenhum draft ou publicação compensatória adicional é criado.

---

### User Story 3 - Recuperar execuções atrasadas dentro da janela (Priority: P1)

Como organizador, quero que agendas permaneçam recuperáveis após indisponibilidade temporária, para que listas ainda úteis sejam criadas e datas vencidas sejam registradas sem duplicação.

**Why this priority**: Reinícios, indisponibilidade de vários dias e configuração incompleta são situações operacionais esperadas e não podem deixar o estado silenciosamente inconsistente.

**Independent Test**: Após três dias de indisponibilidade, o sistema percorre todas as datas posteriores a `UltimaDataAvaliada`, registra cada data selecionada vencida como perdida, cria a ocorrência do dia ainda dentro da janela e só então avança o marcador diário.

**Acceptance Scenarios**:

1. **Given** uma agenda que permaneceu ativa durante vários dias de indisponibilidade, **When** o sistema retorna, **Then** avalia sem horizonte arbitrário todas as datas posteriores a `UltimaDataAvaliada` até a data local atual.
2. **Given** uma data selecionada cuja publicação passou e cujo encerramento ainda não ocorreu, **When** ela é recuperada, **Then** o draft atrasado é criado exatamente uma vez.
3. **Given** uma data selecionada cujo encerramento já passou, **When** ela é recuperada, **Then** a ocorrência é registrada como perdida e nenhum draft é criado.
4. **Given** bot desativado ou configuração Discord incompleta, **When** a agenda vence, **Then** a ocorrência fica bloqueada sem draft e é reavaliada até ser criada dentro da janela ou marcada perdida após o encerramento.
5. **Given** uma agenda reativada depois do horário de publicação, **When** o ciclo avalia a data da reativação, **Then** essa ocorrência não é recuperada porque a agenda não permaneceu ativa no horário previsto.

---

### User Story 4 - Acompanhar próxima execução e resultados recentes (Priority: P2)

Como Moderador+, quero visualizar agendas, próxima execução e ocorrências recentes, para acompanhar a automação e agir diante de bloqueios, perdas ou falhas.

**Why this priority**: A visibilidade operacional torna a automação compreensível e administrável, mas depende das agendas e ocorrências das histórias anteriores.

**Independent Test**: Um Moderador consulta cards ordenados por próxima execução e abre o histórico paginado de uma agenda, vendo somente status e códigos públicos localizados, sem claims, IDs de mensagens ou configuração sensível.

**Acceptance Scenarios**:

1. **Given** agendas não arquivadas, **When** o Moderador abre a central, **Then** vê uma página de agendas ativas e pausadas, fuso de Brasília, próxima execução e resultado recente em ordem útil, com ação localizada para carregar mais quando houver outra página.
2. **Given** ocorrências de uma agenda, **When** o Moderador aciona `Ver histórico`, **Then** abre um painel ou modal acessível e paginado com data, janela, status, draft relacionado quando houver e mensagem localizada segura.
3. **Given** nenhum agendamento, **When** a central é aberta, **Then** um estado vazio localizado orienta a criação da primeira agenda.
4. **Given** viewport de 320px ou maior, **When** a central e os diálogos são operados por teclado ou toque, **Then** não há overflow obrigatório e foco, ações e confirmações permanecem acessíveis.

---

### User Story 5 - Impedir acesso sem permissão (Priority: P1)

Como responsável pela plataforma, quero restringir agendas e dados operacionais a usuários autorizados, para preservar separação de funções e informações administrativas.

**Why this priority**: Autorização é condição obrigatória para todos os demais fluxos e não pode depender somente da interface.

**Independent Test**: A matriz de acesso confirma `401` para anônimo, `403` para Jogador, sucesso para Moderador/Admin/SuperAdmin e ausência da seção para usuário comum, sem ampliar acesso à configuração sensível.

**Acceptance Scenarios**:

1. **Given** uma requisição sem autenticação, **When** qualquer operação de agenda é solicitada, **Then** o acesso é negado sem retornar dados administrativos.
2. **Given** um usuário autenticado sem `CanManageDrafts`, **When** solicita listagem ou alteração, **Then** recebe negação consistente e não vê a seção na interface.
3. **Given** um Moderador com `CanManageDrafts` e sem `CanManageUsers`, **When** acessa `/configuracoes`, **Then** gerencia agendas sem visualizar guild, canais, token ou ativação global do bot.
4. **Given** uma operação de alteração autorizada, **When** ela é processada, **Then** a autoria vem da identidade autenticada e nunca de um campo controlado pelo corpo da requisição.

### Edge Cases

- Nome ausente, fora de 3 a 100 caracteres, observação acima de 500 caracteres, nenhum dia ou dias duplicados são rejeitados com mensagem localizada.
- Horários têm precisão de minuto; encerramento igual ou anterior à publicação, ou fora do mesmo dia local, é rejeitado.
- Horário local inválido ou ambíguo não é ajustado silenciosamente: a ocorrência falha com código público localizado e sem draft.
- Pausar, editar ou arquivar durante uma execução não modifica ocorrência ou draft já confirmado.
- Claim divergente não pode concluir ou falhar uma ocorrência; claim expirado pode ser retomado somente enquanto a janela estiver aberta.
- Falha transitória antes da confirmação não avança indevidamente `UltimaDataAvaliada`; uma falha em uma agenda não interrompe as demais.
- Uma ocorrência `Bloqueada` continua sendo reavaliada em todos os ciclos mesmo depois de `UltimaDataAvaliada` ter avançado além de sua data.
- Ao criar ou reativar antes do horário local de publicação, o dia atual permanece elegível; ao criar ou reativar no horário ou depois dele, o dia atual não é recuperado.
- Falha conhecida de publicação, resultado incerto e CTA seguem estados independentes do protocolo existente e nunca provocam novo draft.
- Logs e métricas não incluem nome, observação, usuário, token, payload Discord, IDs de mensagem ou motivo administrativo livre.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir listar agendas com `page` e `pageSize`, criar, detalhar, editar, pausar, reativar, arquivar e consultar ocorrências paginadas somente a usuários autenticados com `CanManageDrafts`.
- **FR-002**: O sistema MUST manter a configuração sensível de guild, canais, token e ativação global separada e protegida por `CanManageUsers`; `CanManageDrafts` MUST NOT conceder acesso a esses dados.
- **FR-003**: Uma agenda MUST possuir nome normalizado de 3 a 100 caracteres, observação opcional de até 500 caracteres, ao menos um dia ISO único e horários locais com precisão de minuto.
- **FR-004**: Publicação e encerramento MUST ocorrer no mesmo dia em `America/Sao_Paulo`, e o encerramento MUST ser estritamente posterior à publicação.
- **FR-005**: A agenda MUST repetir semanalmente até ser pausada ou arquivada e MUST usar os estados `Ativo`, `Pausado` e `Arquivado`.
- **FR-006**: Exclusão MUST ser arquivamento lógico; agenda arquivada MUST NOT ser editada, reativada ou retornada pelas consultas usuais.
- **FR-007**: Criação, edição, pausa, reativação e arquivamento MUST registrar responsável autenticado, instante e resumo estrutural sem dados sensíveis.
- **FR-008**: Alterações de agenda MUST afetar somente ocorrências ainda não criadas e MUST NOT alterar ou cancelar drafts existentes.
- **FR-009**: Para cada combinação agenda e data local, o sistema MUST persistir no máximo uma ocorrência e criar no máximo um draft e uma publicação principal.
- **FR-010**: A aquisição de ocorrência MUST ser atômica e usar claim único expirável em cinco minutos, permitindo retomada após interrupção somente dentro da janela.
- **FR-011**: A conclusão MUST confirmar ocorrência, draft e publicação pendente como uma única unidade; falha antes da confirmação MUST permitir nova tentativa sem estado parcial.
- **FR-012**: O draft agendado MUST usar times de cinco, os padrões operacionais atuais, observação configurada e nome `Nome configurado - dd/MM/yyyy` sem depender da cultura do processo.
- **FR-013**: O backend MUST ser a fonte de verdade da recorrência; o bot MUST NOT consultar agendas, calcular horários ou consumir endpoint novo.
- **FR-014**: Drafts agendados MUST entrar sem campo novo no polling existente do bot e reutilizar claims, conclusão, falha, reconciliação e CTA atuais.
- **FR-015**: Bot desativado ou configuração Discord incompleta MUST bloquear a ocorrência sem criar draft invisível; todo ciclo MUST reavaliar ocorrências bloqueadas independentemente de `UltimaDataAvaliada`, criando o draft após readquirir claim se a configuração voltar dentro da janela ou marcando a ocorrência perdida após o encerramento.
- **FR-016**: O sistema MUST interpretar horários com `America/Sao_Paulo`, persistir instantes calculados em UTC e MUST NOT usar deslocamento fixo como regra.
- **FR-017**: O sistema MUST percorrer todas as datas posteriores a `UltimaDataAvaliada` até a data local atual, inclusive após indisponibilidade de múltiplos dias, sem horizonte arbitrário.
- **FR-018**: `UltimaDataAvaliada` MUST avançar somente depois que todas as ocorrências esperadas da data forem confirmadas ou classificadas; data atual antes da publicação MUST permanecer pendente.
- **FR-019**: Uma ocorrência atrasada MUST ser criada antes do encerramento somente quando a agenda permaneceu ativa no horário previsto; após o encerramento MUST ser perdida sem draft.
- **FR-020**: A interface em `/configuracoes` MUST apresentar central responsiva com resumo, cards, próxima execução, resultado recente, paginação ou ação localizada para carregar mais agendas, estado vazio, formulário e confirmações para `CanManageDrafts`.
- **FR-021**: Consultas MUST expor somente DTOs administrativos seguros e códigos públicos `messageCode`; claims, tokens, IDs de mensagem, payloads e falhas técnicas Discord MUST NOT ser retornados.
- **FR-022**: Todos os textos visíveis do frontend MUST usar chaves equivalentes em `pt.json` e `en.json`, e todas as mensagens backend MUST usar recursos equivalentes em português e inglês.
- **FR-023**: Datas, horários e dias da semana MUST respeitar o locale ativo; português MUST ter acentuação revisada e valores inseridos pelo usuário MUST NOT ser traduzidos.
- **FR-024**: O sistema MUST registrar métricas de agendas avaliadas, ocorrências criadas, bloqueadas, perdidas, falhas, conflitos e duração sem dados pessoais ou sensíveis.
- **FR-025**: A entrega MUST adicionar a release `2026.07.2` ao histórico localizado do produto, descrevendo agendamento, horários, gestão Moderador+, recuperação e proteção contra duplicidade em linguagem de usuário.
- **FR-026**: A interface MUST oferecer a ação localizada `Ver histórico` e abrir um painel ou modal acessível que consome ocorrências paginadas, permite navegar entre páginas e preserva foco, teclado e leitura por tecnologia assistiva.
- **FR-027**: Ao criar uma agenda, `UltimaDataAvaliada` MUST iniciar no dia local anterior quando o horário atual em `America/Sao_Paulo` for anterior ao horário de publicação, e na data local atual quando for igual ou posterior; ao reativar, MUST usar o maior valor entre o marcador existente e essa data calculada.

### Key Entities

- **AgendamentoPresenca**: Agregado semanal com configuração, estado, autoria, datas de ciclo e coleções de dias, ocorrências e histórico.
- **AgendamentoPresencaDiaSemana**: Dia ISO selecionado, único dentro de uma agenda e persistido relacionalmente.
- **OcorrenciaAgendamentoPresenca**: Execução esperada de uma agenda em uma data local, com janela, status, claim expirável, draft opcional e código público de falha.
- **HistoricoAgendamentoPresenca**: Auditoria administrativa de criação, edição e mudanças de estado, sem conteúdo sensível.
- **DraftMontagem**: Draft existente criado com times de cinco e publicação de presença pendente para o protocolo atual do bot.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% da matriz de autorização retorna negação para anônimo e usuário sem `CanManageDrafts`, permite Moderador+ e preserva a restrição `CanManageUsers` sobre configuração sensível.
- **SC-002**: Em 100% dos cenários concorrentes exercitados, uma combinação agenda/data produz exatamente uma ocorrência e no máximo um draft e uma publicação principal.
- **SC-003**: Após indisponibilidade simulada de três ou mais dias, 100% das datas selecionadas posteriores a `UltimaDataAvaliada` são classificadas em ordem, sem lacunas e sem avanço prematuro do marcador.
- **SC-004**: 100% das ocorrências recuperadas antes do encerramento e elegíveis criam um draft; 100% das ocorrências avaliadas após o encerramento são perdidas sem draft.
- **SC-005**: 100% dos cenários com bot desativado ou configuração incompleta permanecem sem draft enquanto bloqueados e retomam somente dentro da janela.
- **SC-006**: 100% das operações administrativas exercitadas registram autoria e ação, sem alterar drafts já criados ou expor dados operacionais proibidos.
- **SC-007**: A central pode ser operada por teclado e toque em desktop, tablet, mobile e 320px sem overflow horizontal obrigatório, perda de foco ou ação inacessível.
- **SC-008**: Português e inglês apresentam estrutura equivalente no frontend, backend e conteúdo da release `2026.07.2`, sem texto visível hardcoded nos novos fluxos.
- **SC-009**: 100% dos cenários de paginação exercitados retornam metadados e itens coerentes para agendas e ocorrências, sem duplicar ou omitir itens ao avançar páginas na interface.
- **SC-010**: 100% das ocorrências bloqueadas exercitadas são reavaliadas em ciclos posteriores mesmo com `UltimaDataAvaliada` avançada, terminando criadas dentro da janela ou perdidas após o encerramento.
- **SC-011**: 100% dos cenários no mesmo dia inicializam ou atualizam `UltimaDataAvaliada` no dia anterior antes da publicação e no dia atual a partir do horário de publicação, sem retroceder o marcador ao reativar.

## Assumptions

- Autenticação JWT, permissões `CanManageDrafts` e `CanManageUsers`, fluxo de `DraftMontagem`, polling e claims de publicação do bot já existem e serão reutilizados.
- O intervalo operacional padrão do avaliador será de 30 segundos; ele não altera a precisão de minuto da agenda.
- Nome e observação são os únicos parâmetros de draft configuráveis nesta feature; demais opções continuam nos padrões atuais do bot.
- Agenda criada inicia ativa e aplica a regra determinística de `UltimaDataAvaliada` do FR-027 no horário local de São Paulo.
- Falhas de envio posteriores à criação do draft pertencem ao protocolo existente de publicação e reconciliação.

## Out of Scope

- Timezone configurável, calendário mensal, período final da recorrência ou expressão cron arbitrária.
- Tamanho de equipe configurável, múltiplos canais por agenda ou criação automática de capitães e jogadores.
- Alteração ou cancelamento de drafts já criados por edição, pausa ou arquivamento da agenda.
- Quartz, Hangfire, novo serviço de agendamento ou regra de recorrência no bot ou frontend.
- Sincronização com calendários externos ou notificações adicionais fora da lista e CTA atuais.

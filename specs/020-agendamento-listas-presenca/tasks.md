---

description: "Tarefas TDD do agendamento recorrente de listas de presença"
---

# Tasks: Agendamento Recorrente de Listas de Presença

**Input**: `specs/020-agendamento-listas-presenca/spec.md`, `plan.md`, `data-model.md`, `contracts/` e design aprovado

**Prerequisites**: estes artefatos devem receber aprovação explícita antes de T001; o baseline .NET indisponível nesta sessão bloqueia qualquer implementação backend

**Tests**: cada comportamento deve ter teste RED escrito e executado antes da implementação correspondente; não aceitar falha por ambiente como evidência RED do comportamento

**Organization**: as sete fases abaixo equivalem às Tasks 2-8 do plano aprovado e preservam rastreabilidade para as cinco histórias.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: somente quando as tarefas alteram arquivos distintos e não dependem de trabalho ainda incompleto.
- **[Story]**: história atendida (`US1` a `US5`); fases fundacionais não usam rótulo de história.
- Toda tarefa identifica os arquivos que cria ou altera.
- O marcador só muda para `[X]` após evidência reproduzível da ação e dos testes associados.

## Phase 1: Domínio de Agendas e Ocorrências (equivale à Task 2)

**Purpose**: estabelecer agregado, enums, invariantes, transições e auditoria sem dependência de infraestrutura.

- [X] T001 Escrever testes RED para nome, observação, ao menos um dia, dias únicos, janela no mesmo dia e precisão de minuto em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [X] T002 Acrescentar testes RED para `OcorreEm`, normalização, `UltimaDataAvaliada`, edição sem alterar ocorrências e pausa/reativação idempotentes em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [X] T003 Acrescentar testes RED para histórico com `CamposAlterados` contendo somente nomes estáveis, arquivamento imutável e transições de ocorrência em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [X] T004 Executar o filtro `AgendamentoPresencaTests` e registrar RED causado pelos tipos/comportamentos ausentes em `specs/020-agendamento-listas-presenca/tasks.md`. RED confirmado em 2026-07-24: `CS0246` para `AgendamentoPresenca`, `DiaSemanaIso` e `OcorrenciaAgendamentoPresenca`; segundo RED comportamental confirmou auditoria incorreta do no-op idempotente (1 falha, 33 aprovados). Revisão TDD posterior confirmou 30 falhas e 33 aprovações para coleções mutáveis, dias ISO fora da faixa, histórico sem whitelist/limite e códigos públicos inseguros.
- [X] T005 Adicionar as constantes localizáveis `MV089` a `MV100` em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [X] T006 Criar `DiaSemanaIso`, `AgendamentoPresencaStatus`, `OcorrenciaAgendamentoPresencaStatus` e `AgendamentoPresencaAcao` em `BackEnd/src/RinhaDasLendas.Domain/Enums/`
- [X] T007 Implementar agregado, dias e histórico com backing fields, invariantes, `CamposAlterados` sem valores e instantes explícitos em `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresenca.cs`, `AgendamentoPresencaDiaSemana.cs` e `HistoricoAgendamentoPresenca.cs`
- [X] T008 Implementar factories e transições de ocorrência sem relógio estático em `BackEnd/src/RinhaDasLendas.Domain/Entities/OcorrenciaAgendamentoPresenca.cs`
- [X] T009 Executar `AgendamentoPresencaTests` e registrar GREEN de invariantes, auditoria e transições em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN da revisão confirmado em 2026-07-24: 63 aprovados, 0 falhas, 0 ignorados.

**Checkpoint**: domínio executável sem EF, PostgreSQL, HTTP, DTOs ou Discord.

---

## Phase 2: Persistência, Timezone, Claims e Deduplicação (equivale à Task 3)

**Purpose**: proteger uma ocorrência/draft por agenda/data, fechar o schema e garantir timezone/retomada seguros.

- [X] T010 Escrever testes PostgreSQL RED para enums `smallint`, dias relacionais, histórico exato e índices únicos de agenda/data e `draft_montagem_id` não nulo em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [X] T011 Acrescentar testes PostgreSQL RED para duas páginas de agendas em `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC` com empate e pausadas sem duplicação/omissão, counts, concorrência e claims em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [X] T012 Acrescentar testes RED para conclusão atômica com draft/publicação e rollback sem estado parcial em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [X] T013 Escrever testes RED de `SaoPauloAgendamentoPresencaTimeZone` para conversão São Paulo/UTC e horário local inválido ou ambíguo em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [X] T014 Executar `AgendamentoPresencaBehaviorIntegrationTests` e registrar RED por mappings, repositório e timezone ausentes em `specs/020-agendamento-listas-presenca/tasks.md`. RED confirmado em 2026-07-24 no container PostgreSQL disponível: compilação falhou com `CS0234` porque `RinhaDasLendas.Infrastructure.Time` e os tipos atribuídos da Task 3 ainda não existiam; antes do RED, 63 testes de domínio e o teste PostgreSQL isolado existente passaram.
- [X] T015 Criar `AgendamentoPresencaOcorrenciaClaim` e `IAgendamentoPresencaRepository` com `ListAsync`/`CountAsync`, `ListOccurrencesAsync`/`CountOccurrencesAsync` e `ListBlockedAsync` em `BackEnd/src/RinhaDasLendas.Domain/Models/AgendamentoPresencaOcorrenciaClaim.cs` e `BackEnd/src/RinhaDasLendas.Domain/Repositories/IAgendamentoPresencaRepository.cs`
- [X] T016 Criar `IAgendamentoPresencaTimeZone` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaTimeZone.cs`
- [X] T017 Implementar `SaoPauloAgendamentoPresencaTimeZone` com `TimeZoneInfo` e `America/Sao_Paulo`, fazendo os testes RED de T013 passarem em `BackEnd/src/RinhaDasLendas.Infrastructure/Time/SaoPauloAgendamentoPresencaTimeZone.cs`
- [X] T018 Mapear enums `smallint`, `campos_alterados varchar(200)`, checks, relações, claims e índices únicos obrigatórios em `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [X] T019 Implementar paginação/count com `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC`, `ListBlockedAsync`, advisory lock, upserts, claim expirável e conclusão transacional em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/AgendamentoPresencaRepository.cs`
- [X] T020 Registrar repositório e timezone em `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- [X] T021 Criar migration `20260723090000_AddAgendamentosPresenca` e atualizar snapshot em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.cs`, `20260723090000_AddAgendamentosPresenca.Designer.cs` e `RinhaDasLendasDbContextModelSnapshot.cs`
- [X] T022 Executar testes PostgreSQL e script idempotente, registrando GREEN de schema, paginação/count, timezone, concorrência, claim e rollback em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN corrigido após terceira revisão em 2026-07-24: 52 testes PostgreSQL e 125 testes focados de domínio/PostgreSQL aprovados, incluindo terminais com claim nulo, janela persistida, TTL exato, constraints, publicação pelo agregado, rollback/reuso, conclusões concorrentes, configuração obsoleta nos três caminhos de criação, agenda pausada/arquivada, dia removido, retomada de bloqueada, expiração de claim e fronteiras de encerramento; suíte backend com 414 aprovados; build Release com 0 avisos/erros; script idempotente gerado; `has-pending-model-changes` informou ausência de drift; migration `20260723090000_AddAgendamentosPresenca` alinhada com designer/snapshot.

**Checkpoint**: banco e timezone garantem integridade, paginação e retomada segura independentemente da quantidade de réplicas.

---

## Phase 3: Gestão, Paginação, API e Autorização (equivale à Task 4; US1 e US5)

**Story Goal**: Moderador+ gerencia agendas por contratos paginados e seguros, enquanto usuários sem permissão não acessam dados administrativos.

**Independent Test**: matriz HTTP comprova paginação de agendas/ocorrências, `401`, `403`, CRUD para Moderador, autoria do JWT, marcador determinístico e ausência de claims/IDs Discord.

- [X] T023 [US1] Escrever testes RED de `SaveAgendamentoPresencaRequestDto` e códigos `MV089`-`MV094` em `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaValidatorTests.cs`
- [X] T024 [US1] Escrever testes RED dos handlers de CRUD e duas páginas com empate/pausadas em `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC`, counts, detalhe e próxima execução em `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaHandlersTests.cs`
- [X] T025 [US1] Acrescentar três cenários RED de criação e reativação antes, exatamente no horário e depois da publicação, provando marcador no dia anterior para menor/igual, data atual somente para maior e bloqueio apenas com `AtivadoEm > PublicacaoPrevistaEm`, em `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaHandlersTests.cs`
- [X] T026 [US5] Escrever matriz HTTP RED para anônimo, Jogador, Moderador e Admin, `page`/`pageSize` e projeções sem campos operacionais em `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- [X] T027 [US1] Executar validators/handlers/endpoints e registrar RED pelos contratos e comportamentos ausentes em `specs/020-agendamento-listas-presenca/tasks.md`. RED confirmado em 2026-07-24 no devcontainer: compilação falhou com `CS0234`/`CS0246` para DTOs, commands, queries, handlers, validator e relógio ainda ausentes; o ambiente e o baseline de 414 testes estavam íntegros.
- [X] T028 [US1] Criar `SaveAgendamentoPresencaRequestDto`, `AgendamentoPresencaSummaryDto` e `OcorrenciaAgendamentoPresencaSummaryDto` e reutilizar `PaginatedResponseDto<T>` em `BackEnd/src/RinhaDasLendas.Application/Dtos/AgendamentoPresencaDtos.cs`
- [X] T029 [US1] Criar commands de criar, editar, pausar, reativar e arquivar em `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/`
- [X] T030 [US1] Criar queries paginadas de agendas com ordem total declarada, ocorrências e detalhe em `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/`
- [X] T031 [US1] Implementar handlers CQRS com counts, `PaginatedResponseDto`, ordem `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC`, autoria e fronteira `AtivadoEm > PublicacaoPrevistaEm` em `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/`
- [X] T032 [US1] Implementar `AgendamentoPresencaRequestValidator` sem duplicar invariantes de domínio em `BackEnd/src/RinhaDasLendas.Application/Validators/AgendamentoPresencaRequestValidator.cs`
- [X] T033 [US1] Criar `ISystemClock` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/ISystemClock.cs` e `SystemClock` em `BackEnd/src/RinhaDasLendas.Api/Services/SystemClock.cs`
- [X] T034 [US5] Acrescentar clientes autenticados Moderador e Jogador em `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/SecurityApiFactory.cs`
- [X] T035 [US5] Implementar os oito endpoints finos com `CanManageDrafts`, paginação, `ISender`, autoria do claim e respostas padrão em `BackEnd/src/RinhaDasLendas.Api/Controllers/AgendamentosPresencaController.cs`
- [X] T036 [US1] Adicionar `MV089`-`MV100` sincronizados em `docs/messages/message-catalog.md`, `docs/messages/message-codes.md`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx`
- [X] T037 [US1] Registrar `ISystemClock` e dependências CQRS em `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [X] T038 [US1] Executar validators/handlers e registrar GREEN de CRUD, duas páginas estáveis com empate/pausadas, counts e três fronteiras temporais em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN reforçado em 2026-07-24: 99 testes focados aprovados, incluindo criação/reativação nas três fronteiras, ordenação e `ProximaExecucaoEm` pela mesma projeção SQL, quatro comandos constantes para listagem, três para ocorrências, coleções limitadas e concorrência otimista PostgreSQL por `xmin` com um vencedor e um `MV097`.
- [X] T039 [US5] Executar a matriz HTTP e registrar GREEN de paginação, `401`, `403`, `400`, `404`, `409` e sucessos em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN reforçado em 2026-07-24: todos os oito endpoints permanecem exclusivamente em `CanManageDrafts`; configuração Discord GET/PUT exige `CanManageUsers`; model binding inválido retorna envelope localizado; concorrência HTTP retorna um `200` e um `409/MV097`; autoria forjada é ignorada e todas as respostas com body foram auditadas contra dados operacionais.

**Checkpoint**: gestão da US1 funciona por API paginada e a fronteira da US5 é comprovada independentemente do scheduler e frontend.

---

## Phase 4: Execução, Bloqueadas, Recuperação e Métricas (equivale à Task 5; US2 e US3)

**Story Goal**: criar uma ocorrência/draft por data, recuperar atrasos e reavaliar bloqueadas mesmo após avanço de `UltimaDataAvaliada`.

**Independent Test**: duas execuções criam um draft; após três dias cada data é classificada; bloqueada com marcador avançado é criada após retorno da configuração ou perdida após encerramento.

- [X] T040 [US2] Escrever testes RED para antes da publicação, dentro da janela, configuração indisponível, retomada, claim expirado e concorrência em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [X] T041 [US3] Acrescentar testes RED para indisponibilidade de três dias, avanço seguro de `UltimaDataAvaliada`, perda e reativação tardia em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [X] T042 [US3] Escrever testes RED da fase independente `ListBlockedAsync` com marcador avançado para manter bloqueada, readquirir/criar dentro da janela e marcar perdida após encerramento em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [X] T043 [US3] Acrescentar testes RED para falha isolada por agenda, ciclo sem sobreposição e cancelamento do host em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [X] T044 [US2] Escrever testes RED específicos dos contadores de avaliadas/criadas/bloqueadas/perdidas/falhas/conflitos, histograma e rejeição de tags sensíveis em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaMetricsTests.cs`
- [X] T045 [US2] Executar testes de ciclo, bloqueadas e métricas e registrar RED pelos handlers/serviços ausentes em `specs/020-agendamento-listas-presenca/tasks.md`. RED confirmado em 2026-07-24 no devcontainer: baseline de 461 testes aprovado; filtro da Task 5 falhou com `CS0246` exclusivamente para `IAgendamentoPresencaMetrics` e `ProcessarAgendamentosPresencaDevidosCommandHandler` ainda ausentes.
- [X] T046 [US2] Criar `IAgendamentoPresencaMetrics` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaMetrics.cs`
- [X] T047 [US2] Criar `ProcessarAgendamentosPresencaDevidosCommand` e `AgendamentoPresencaCycleResult` em `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommand.cs`
- [X] T048 [US3] Implementar varredura de agendas/datas posteriores a `UltimaDataAvaliada`, bloqueio inicial, perda e criação transacional em `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- [X] T049 [US3] Implementar fase independente que chama `ListBlockedAsync` em todo ciclo e mantém, readquire claim/conclui draft ou marca `Perdida` sem consultar `UltimaDataAvaliada` em `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- [X] T050 [US2] Implementar `AgendamentoPresencaExecutionService` com escopo, `PeriodicTimer`, ciclo sequencial e cancelamento em `BackEnd/src/RinhaDasLendas.Api/Services/AgendamentoPresencaExecutionService.cs`
- [X] T051 [US2] Implementar contadores/histograma com tags limitadas a status/código estável, fazendo T044 passar, em `BackEnd/src/RinhaDasLendas.Api/Observability/AgendamentoPresencaMetrics.cs`
- [X] T052 [US2] Registrar hosted service, métricas e intervalo default 30 em `BackEnd/src/RinhaDasLendas.Api/Program.cs` e `BackEnd/src/RinhaDasLendas.Api/appsettings.json`
- [X] T053 [US2] Executar testes do ciclo e registrar GREEN para exactly-once, claim expirável e configuração indisponível em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN em 2026-07-24: 72 testes focados de serviço, métricas e PostgreSQL aprovados; dois ciclos produziram uma ocorrência, um draft e uma publicação pendente; claim de cinco minutos, terminal concorrente e processamento interrompido preservaram deduplicação e retomada.
- [X] T054 [US3] Executar cenários de múltiplos dias e bloqueadas com marcador avançado e registrar GREEN de recuperação/perda em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN em 2026-07-24: recuperação sem horizonte classificou três dias selecionados, avançou datas não selecionadas monotonicamente, reavaliou bloqueada com marcador avançado e cobriu crash entre commit de `Perdida` e gravação do marcador.
- [X] T055 [US2] Executar `AgendamentoPresencaMetricsTests` e registrar GREEN de contadores, duração e tags seguras em `specs/020-agendamento-listas-presenca/tasks.md`. GREEN em 2026-07-24: contadores de avaliadas, criadas, bloqueadas, perdidas, falhas e conflitos e histograma de duração aprovados; valor não permitido foi normalizado para `MV097` sem entrar em tags. Suíte backend: 478 aprovados; build Release: 0 avisos e 0 erros.

### Revisão obrigatória da Task 5

- [X] T055A Persistir `Falha/MV096` idempotente, snapshots imutáveis e janela técnica determinística com migration e testes PostgreSQL.
- [X] T055B Usar relógio fresco, lotes configuráveis, no-op de bloqueadas e configuração transitória distinta de indisponibilidade.
- [X] T055C Isolar tracking/conflitos por agenda, adicionar diagnóstico seguro e corrigir semântica de métricas.
- [X] T055D Testar lifecycle real do hosted service, claim expirado, handlers concorrentes, backlog/cancelamento e EF `xmin` no mesmo ciclo.
- [X] T055E Executar GREEN focado/PostgreSQL/backend/build/EF/i18n/diff, atualizar relatório e commit sem amend. GREEN da revisão em 2026-07-24: 88 testes focados e 494 testes backend aprovados; build Release sem avisos/erros; migration idempotente gerada com snapshots; `has-pending-model-changes` sem drift; paridade i18n com 778 chaves frontend e 218 resources backend; `git diff --check` aprovado.
- [X] T055F Escrever RED para candidatas futuras, rotação por cursor após falhas persistentes e processamento da agenda de janela curta antes do encerramento.
- [X] T055G Escrever RED PostgreSQL para CAS de falha timezone contra `xmin`/configuração observada e conclusão aguardando lock além do encerramento.
- [X] T055H Remover controle de tracking do contrato Domain/Application e tornar snapshots explícitos, normalizados e validados no Domain com teste de persistência.
- [X] T055I Executar GREEN focado/PostgreSQL/backend/build/EF/i18n/diff, atualizar relatório e criar segundo commit sem amend. GREEN da segunda revisão em 2026-07-24: 104 testes focados de Domain/serviço e 66 testes PostgreSQL aprovados; suíte backend com 504 aprovados; build Release sem avisos/erros; script idempotente gerado e `has-pending-model-changes` sem drift; paridade i18n mantida em 778 chaves frontend e 218 resources backend; `git diff --check` aprovado.
- [X] T055J Escrever RED PostgreSQL para aquisição/conclusão após espera em lock, expiração baseada no banco e recusa após encerramento/claim expirado.
- [X] T055K Substituir expansão histórica por cálculo relacional da próxima data em 1-7 dias e limpar tracking em toda exceção de persistência, com regressões de décadas e falha não concorrencial.
- [X] T055L Executar GREEN focado/PostgreSQL/backend/build/EF/i18n/diff, atualizar relatório e criar terceiro commit sem amend. GREEN da terceira revisão em 2026-07-24: cinco regressões RED/GREEN e 71 testes PostgreSQL aprovados; suíte backend com 509 aprovados; build Release sem avisos/erros; script idempotente gerado e `has-pending-model-changes` sem drift; ausência de `generate_series` no backend; paridade i18n mantida em 778 chaves frontend e 218 resources backend; `git diff --check` aprovado.
- [X] T055M Validar `TryMarkFailedAsync` pelo relógio PostgreSQL, tornar a fixture de claim perdido determinística e executar o gate final. RED confirmou CAS com `@now`; GREEN isolado com 2 testes, PostgreSQL focado com 72 testes e backend Release com 510 testes aprovados; build sem avisos/erros, i18n em paridade e `git diff --check` aprovado.

RED da revisão confirmado em 2026-07-24: baseline de 478 testes aprovado; novos testes falharam na compilação com `CS0246` para `IAgendamentoPresencaDiagnostics` e `AgendamentoPresencaProcessingOptions`, portas ausentes que representam os requisitos rejeitados.

RED da segunda revisão confirmado em 2026-07-24: snapshots aceitaram valores inválidos e sem normalização; contratos não possuíam cursor, versão/configuração observada ou recarga de candidata; Application ainda dependia de `DiscardTrackedChanges`; testes PostgreSQL expuseram seleção de futuras, ausência de CAS por `xmin` e conclusão baseada somente em `@now`.

RED da terceira revisão confirmado em 2026-07-24: cinco testes PostgreSQL falharam porque aquisição/conclusão usavam `@now` após espera em locks, a expiração persistida vinha do chamador, `ListCandidatesAsync` executava `generate_series` histórico e falha `DbUpdateException` não concorrencial mantinha a primeira agenda dirty para o save seguinte.

RED da correção final confirmado em 2026-07-24: `TryMarkFailedAsync` aceitou claim expirado quando recebeu instante stale; a fixture de perda foi alterada para comprovar `Processando`, expirar o claim diretamente no PostgreSQL e somente então persistir `Perdida`.

**Checkpoint**: US2/US3 preservam exactly-once e nenhuma ocorrência bloqueada fica invisível após o avanço do marcador.

---

## Phase 5: Central, Paginação e Histórico (equivale à Task 6; US1, US4 e US5)

**Story Goal**: Moderador+ gerencia agendas paginadas e consulta histórico paginado acessível sem acessar configuração sensível.

**Independent Test**: Moderador carrega mais agendas, abre `Ver histórico`, pagina ocorrências em PT/EN; Jogador não vê a seção; modal funciona por teclado em 320px.

- [ ] T056 [US1] Escrever testes RED de duas páginas de agendas com empate/pausadas na ordem backend, métodos, payload `HH:mm`, `messageCode` e propagação de `403`/`500` em `FrontEnd/src/services/presenceSchedules.spec.ts`
- [ ] T057 [US4] Acrescentar testes RED de `listPresenceScheduleOccurrences(id,page,pageSize)` e envelope `PaginatedResponse` em `FrontEnd/src/services/presenceSchedules.spec.ts`
- [ ] T058 [US1] Escrever testes RED de campos, chips `aria-pressed`, validação, loading, `Escape` e foco em `FrontEnd/src/components/settings/PresenceScheduleFormDialog.spec.ts`
- [ ] T059 [US4] Escrever testes RED de cards em duas páginas com empate de próxima execução/nome, pausadas e desempate por ID, provando carregar mais sem duplicação/omissão em `FrontEnd/src/components/settings/PresenceScheduleSection.spec.ts`
- [ ] T060 [US4] Escrever testes RED de `Ver histórico`, paginação, loading, erro, vazio, região viva, foco e `Escape` em `FrontEnd/src/components/settings/PresenceScheduleOccurrenceHistoryDialog.spec.ts`
- [ ] T061 [US1] Escrever testes RED de pausa/exclusão contextual e submissão única em `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.spec.ts`
- [ ] T062 [US5] Escrever testes RED de visibilidade Jogador/Moderador/Admin e separação de permissões em `FrontEnd/src/views/SettingsView.spec.ts`
- [ ] T063 [US1] Executar os testes frontend focados e registrar RED pelos tipos, serviço e componentes ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T064 [US1] Criar `PaginatedResponse<T>`, tipos fechados e contratos de agenda/ocorrência em `FrontEnd/src/types/presenceSchedule.ts`
- [ ] T065 [US1] Implementar listagem paginada preservando a ordem total do backend sem reordenar no cliente e mutações sem fallback silencioso em `FrontEnd/src/services/presenceSchedules.ts`
- [ ] T066 [US4] Implementar `listPresenceScheduleOccurrences(id,page,pageSize)` preservando metadados em `FrontEnd/src/services/presenceSchedules.ts`
- [ ] T067 [US1] Implementar formulário acessível de criação/edição em `FrontEnd/src/components/settings/PresenceScheduleFormDialog.vue`
- [ ] T068 [US1] Implementar confirmações acessíveis de pausa e arquivamento em `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.vue`
- [ ] T069 [US4] Implementar resumo, cards, `Ver histórico` e concatenação paginada na ordem backend sem duplicar/omitir agendas em `FrontEnd/src/components/settings/PresenceScheduleSection.vue`
- [ ] T070 [US4] Implementar painel/modal acessível com histórico paginado, controles, região viva e restauração de foco em `FrontEnd/src/components/settings/PresenceScheduleOccurrenceHistoryDialog.vue`
- [ ] T071 [US5] Integrar agendas por `CanManageDrafts` e manter configuração sensível por `CanManageUsers` em `FrontEnd/src/views/SettingsView.vue`
- [ ] T072 [P] [US1] Adicionar `settings.presenceSchedules` completo, incluindo paginação e `Ver histórico`, em `FrontEnd/src/i18n/locales/pt.json`
- [ ] T073 [P] [US1] Adicionar estrutura equivalente em inglês em `FrontEnd/src/i18n/locales/en.json`
- [ ] T074 [US4] Aplicar cards, paginação e modal responsivos com tokens existentes e sem overflow em 320px em `FrontEnd/src/styles/main.css`
- [ ] T075 [US1] Executar testes de serviço/formulários e registrar GREEN de duas páginas estáveis com empate/pausadas, CRUD e confirmações em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T076 [US4] Executar testes de seção/histórico e registrar GREEN de carregar mais, ocorrências paginadas, acessibilidade e responsividade em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T077 [US5] Executar `SettingsView.spec.ts` e registrar GREEN da matriz de visibilidade e separação de permissões em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: US1/US4 e a visibilidade da US5 funcionam com paginação sem deslocar regras do backend.

---

## Phase 6: Compatibilidade do Bot, Release e Documentação (equivale à Task 7; US2 e US4)

**Story Goal**: comprovar polling existente sem endpoint novo e tornar a entrega `2026.07.2` compreensível e operável.

**Independent Test**: fixture de draft agendado passa por dois ciclos com uma publicação; release localizada é a mais recente e documentação cobre recuperação sem segredos.

- [ ] T078 [US2] Escrever fixture/teste RED ou de caracterização para draft agendado em dois ciclos de `runDraftPollingCycle` em `discord-bot/src/modules/drafts/draftInteractions.spec.ts`
- [ ] T079 [US2] Executar o teste do bot e, se falhar por contrato real, corrigir somente a adaptação existente sem endpoint/regra de agenda em `discord-bot/src/modules/drafts/draftInteractions.spec.ts` e arquivos de produção estritamente necessários
- [ ] T080 [US4] Escrever testes RED para `2026.07.2`, ID `presence-scheduling-2026-07`, posição latest e paridade localizada em `FrontEnd/src/constants/systemUpdates.spec.ts`, `FrontEnd/src/services/systemUpdates.spec.ts` e `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T081 [US4] Adicionar release `2026.07.2` e remover destaque de `2026.07.1` em `FrontEnd/src/constants/systemUpdates.ts`
- [ ] T082 [P] [US4] Adicionar conteúdo de produto da release em português em `FrontEnd/src/i18n/locales/pt.json`
- [ ] T083 [P] [US4] Adicionar conteúdo equivalente da release em inglês em `FrontEnd/src/i18n/locales/en.json`
- [ ] T084 [P] [US4] Atualizar fluxo existente de drafts Discord em `docs/domain/DRAFT_DISCORD_OPERATIONS.md`
- [ ] T085 [P] [US4] Documentar agenda, bloqueadas independentes, recuperação, perda, métricas e runbook em `docs/domain/AGENDAMENTO_LISTAS_PRESENCA.md`
- [ ] T086 [US2] Executar suíte/build do bot e registrar ausência de endpoint novo ou regra de recorrência em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T087 [US4] Executar testes do histórico/i18n e registrar GREEN de `2026.07.2`, paridade e linguagem segura em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: bot permanece adaptador e a entrega fica documentada e localizada.

---

## Phase 7: Verificação Integrada, Segurança e Browser Real (equivale à Task 8)

**Purpose**: produzir evidência final reproduzível sem ampliar escopo.

- [ ] T088 Executar testes e build Release do backend pelo devcontainer e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T089 Executar testes, build e lint sem fix do frontend e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T090 Executar testes e build completos do bot e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T091 Aplicar migration em PostgreSQL descartável e comprovar enums `smallint`, histórico, índices únicos, rollback e reaplicação em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T092 Executar matriz HTTP real com duas páginas ordenadas, empate, pausadas e paginação de ocorrências para anônimo, Jogador, Moderador e Admin em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T093 Disparar dois ciclos simultâneos e comprovar uma ocorrência, um draft, uma publicação pendente e um claim vencedor em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T094 Validar recuperação de múltiplos dias, bloqueada com marcador avançado, claim expirado e inicialização antes/no/depois da publicação em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T095 Validar `/configuracoes`, carregar mais e `Ver histórico` com browser real para Jogador/Moderador/Admin em 1440x900, 768x1024, 390x844 e 320px em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T096 Auditar autorização, autoria, DTOs, rate limiting, logs, contadores/tags de métricas e ausência de segredos em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T097 Auditar textos hardcoded frontend/backend, paginação, `Ver histórico`, paridade `pt.json`/`en.json`, resources PT/EN, acentuação e validações em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T098 Executar `git diff --check`, revisar somente arquivos da feature e marcar tarefas comprovadas em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: nenhuma tarefa é concluída com `Não` na auditoria de internacionalização ou sem evidência de paginação, segurança, concorrência e recuperação.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: começa somente após aprovação explícita deste `tasks.md` e ambiente backend disponível.
- **Phase 2**: depende das entidades/transições da Phase 1; T013-T014 precedem obrigatoriamente a implementação de timezone em T017.
- **Phase 3**: depende de repositório paginado/count e timezone da Phase 2.
- **Phase 4**: depende do domínio, operações atômicas e recursos/API das Phases 1-3.
- **Phase 5**: depende do contrato HTTP paginado da Phase 3, mas pode começar sem aguardar o scheduler completo.
- **Phase 6**: depende da criação de draft/publicação da Phase 4 e da estrutura i18n da Phase 5.
- **Phase 7**: depende de todas as fases anteriores.

### User Story Dependencies

- **US1 (P1)**: domínio, persistência, API paginada e UI de gestão.
- **US2 (P1)**: depende do domínio/persistência e entrega exactly-once independentemente da UI.
- **US3 (P1)**: amplia US2 com recuperação de múltiplos dias e fase independente de bloqueadas.
- **US4 (P2)**: usa paginação de agendas/ocorrências e estados da US2/US3 para acompanhamento.
- **US5 (P1)**: autorização backend é testável na Phase 3; visibilidade frontend é concluída na Phase 5.

### Red-Green Order

- T001-T004 precedem T005-T009.
- T010-T014 precedem T015-T022; especificamente T013-T014 precedem T017.
- T023-T027 precedem T028-T039.
- T040-T045 precedem T046-T055; especificamente T042 precede T049 e T044 precede T051.
- T056-T063 precedem T064-T077; especificamente T057 precede T066 e T060 precede T070.
- T078 precede qualquer adaptação em T079; T080 precede T081-T083.
- Cada teste novo deve falhar pelo comportamento ausente, não por indisponibilidade de SDK, banco ou serviço.

### Parallel Opportunities

- T072 e T073 podem ocorrer em paralelo após os testes i18n porque alteram catálogos distintos.
- T082 e T083 podem ocorrer em paralelo após T080 porque alteram catálogos distintos.
- T084 e T085 podem ocorrer em paralelo após o comportamento estar consolidado porque alteram documentos distintos.
- Nenhuma outra tarefa recebe `[P]`; pares RED/GREEN e arquivos compartilhados exigem ordem.

## Parallel Examples

```text
T072: Adicionar settings.presenceSchedules em FrontEnd/src/i18n/locales/pt.json
T073: Adicionar settings.presenceSchedules em FrontEnd/src/i18n/locales/en.json

T084: Atualizar docs/domain/DRAFT_DISCORD_OPERATIONS.md
T085: Criar docs/domain/AGENDAMENTO_LISTAS_PRESENCA.md
```

## Implementation Strategy

### MVP First

1. Aprovar os artefatos e disponibilizar o devcontainer/.NET antes de T001.
2. Concluir Phases 1-3 para gestão segura, persistida e paginada.
3. Validar US1 e autorização backend da US5 antes do scheduler.
4. Concluir Phase 4 para US2/US3, incluindo bloqueadas independentes e métricas testadas.

### Incremental Delivery

1. Domínio e schema fechados.
2. Timezone testado antes da implementação e API paginada.
3. Exactly-once, recuperação e fase independente de bloqueadas.
4. Central paginada e histórico acessível.
5. Regressão do bot, release e documentação.
6. Verificação integrada, browser, segurança e i18n.

## Traceability

| Requirement/contract | Tasks |
|----------------------|-------|
| FR-001, FR-002, backend auth/paginação | T011, T024, T026, T030-T039, T056, T059, T062, T065, T069, T071, T075, T077, T092, T096 |
| FR-003-FR-008, domínio/auditoria | T001-T010, T023-T032 |
| FR-009-FR-011, exactly-once/claim | T011-T022, T040, T048-T054, T091, T093-T094 |
| FR-012-FR-015, draft/bot/bloqueadas | T012, T019, T040, T042, T048-T049, T078-T079, T086, T094 |
| FR-016-FR-019, timezone/recuperação | T013-T017, T025, T031, T041-T049, T054, T094 |
| FR-020, frontend paginado | T056, T059, T064-T075, T095 |
| FR-021, DTOs seguros | T026, T028, T035, T039, T092, T096 |
| FR-022-FR-023, i18n | T036, T056-T077, T080-T083, T087, T097 |
| FR-024, observabilidade | T043-T055, T096 |
| FR-025, release `2026.07.2` | T080-T087 |
| FR-026, histórico acessível | T057, T060, T064, T066, T069-T070, T072-T076, T095, T097 |
| FR-027, marcador determinístico | T002, T025, T031, T038, T041, T054, T094 |
| FR-028, ordenação paginada estável | T011, T019, T024, T030-T031, T038, T056, T059, T065, T069, T075, T092 |
| Backend API contract | T011, T024, T026, T028-T039, T092 |
| Frontend UI contract | T056-T077, T095, T097 |
| Discord bot contract | T078-T079, T086, T093 |
| SC-001-SC-008 | T039, T053-T055, T075-T077, T086-T098 |
| SC-009-SC-012 | T011, T019, T024-T025, T030-T031, T038, T042, T049, T054, T056-T076, T092, T094-T095 |

## Gate

- Não iniciar T001 até aprovação explícita de `spec.md`, `plan.md`, `data-model.md`, `contracts/` e `tasks.md`.
- Não executar backend nesta sessão: o baseline .NET está indisponível.
- Preservar `docs/prompts/`, `specs/018-importacao-partidas-lcu/` e qualquer mudança não relacionada.

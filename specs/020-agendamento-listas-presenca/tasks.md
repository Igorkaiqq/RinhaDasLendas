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

- [ ] T001 Escrever testes RED para nome, observação, ao menos um dia, dias únicos, janela no mesmo dia e precisão de minuto em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [ ] T002 Acrescentar testes RED para `OcorreEm`, normalização, `UltimaDataAvaliada`, edição sem alterar ocorrências e pausa/reativação idempotentes em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [ ] T003 Acrescentar testes RED para histórico com autoria, arquivamento imutável e todas as transições válidas/inválidas de ocorrência em `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- [ ] T004 Executar o filtro `AgendamentoPresencaTests` e registrar RED causado pelos tipos/comportamentos ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T005 Adicionar as constantes localizáveis `MV089` a `MV100` em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [ ] T006 Criar `DiaSemanaIso`, `AgendamentoPresencaStatus`, `OcorrenciaAgendamentoPresencaStatus` e `AgendamentoPresencaAcao` em `BackEnd/src/RinhaDasLendas.Domain/Enums/`
- [ ] T007 Implementar agregado, dias e histórico com backing fields, invariantes e instantes explícitos em `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresenca.cs`, `AgendamentoPresencaDiaSemana.cs` e `HistoricoAgendamentoPresenca.cs`
- [ ] T008 Implementar factories e transições de ocorrência sem relógio estático em `BackEnd/src/RinhaDasLendas.Domain/Entities/OcorrenciaAgendamentoPresenca.cs`
- [ ] T009 Executar `AgendamentoPresencaTests` e registrar GREEN de todas as invariantes e transições em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: domínio executável sem EF, PostgreSQL, HTTP, DTOs ou Discord.

---

## Phase 2: Persistência, Claims e Deduplicação (equivale à Task 3)

**Purpose**: proteger uma ocorrência/draft por agenda/data e garantir recuperação de processador interrompido.

- [ ] T010 Escrever testes PostgreSQL RED de dias relacionais, arquivamento e constraints únicas em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [ ] T011 Acrescentar testes PostgreSQL RED para dois processadores, claim de cinco minutos, claim expirado recuperável e claim divergente em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [ ] T012 Acrescentar testes RED para conclusão atômica com draft/publicação e rollback sem estado parcial em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [ ] T013 Executar `AgendamentoPresencaBehaviorIntegrationTests` e registrar RED por mappings/repositório ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T014 Criar `AgendamentoPresencaOcorrenciaClaim` em `BackEnd/src/RinhaDasLendas.Domain/Models/AgendamentoPresencaOcorrenciaClaim.cs` e `IAgendamentoPresencaRepository` em `BackEnd/src/RinhaDasLendas.Domain/Repositories/IAgendamentoPresencaRepository.cs`
- [ ] T015 Criar `IAgendamentoPresencaTimeZone` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaTimeZone.cs`
- [ ] T016 Mapear DbSets, relações, checks, índices, claims e constraints únicas em `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T017 Implementar `SaoPauloAgendamentoPresencaTimeZone` com `TimeZoneInfo` e `America/Sao_Paulo` em `BackEnd/src/RinhaDasLendas.Infrastructure/Time/SaoPauloAgendamentoPresencaTimeZone.cs`
- [ ] T018 Implementar advisory lock, upserts, claim expirável e conclusão transacional em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/AgendamentoPresencaRepository.cs`
- [ ] T019 Registrar repositório e timezone em `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- [ ] T020 Criar migration `20260723090000_AddAgendamentosPresenca` e atualizar snapshot em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.cs`, `20260723090000_AddAgendamentosPresenca.Designer.cs` e `RinhaDasLendasDbContextModelSnapshot.cs`
- [ ] T021 Executar os testes PostgreSQL e o script idempotente da migration, registrando GREEN para concorrência, claim, atomicidade e rollback em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: banco garante unicidade e retomada segura, independentemente da quantidade de réplicas.

---

## Phase 3: Gestão, API e Autorização (equivale à Task 4; US1 e US5)

**Story Goal**: Moderador+ gerencia agendas por contratos seguros, enquanto usuários sem permissão não acessam agenda nem configuração sensível.

**Independent Test**: matriz HTTP comprova `401`, `403`, CRUD para Moderador, autoria do JWT, `404` para arquivada e ausência de claims/IDs Discord.

- [ ] T022 [US1] Escrever testes RED de `SaveAgendamentoPresencaRequestDto` e códigos `MV089`-`MV094` em `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaValidatorTests.cs`
- [ ] T023 [US1] Escrever testes RED dos handlers de criar, editar, pausar, reativar, arquivar, listar, detalhar e paginar ocorrências em `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaHandlersTests.cs`
- [ ] T024 [US5] Escrever matriz HTTP RED para anônimo, Jogador, Moderador e Admin e projeções sem campos operacionais em `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- [ ] T025 [US1] Executar validators/handlers/endpoints e registrar RED pelos contratos ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T026 [US1] Criar `SaveAgendamentoPresencaRequestDto`, `AgendamentoPresencaSummaryDto` e `OcorrenciaAgendamentoPresencaSummaryDto` em `BackEnd/src/RinhaDasLendas.Application/Dtos/AgendamentoPresencaDtos.cs`
- [ ] T027 [US1] Criar commands de criar, editar, pausar, reativar e arquivar em `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/`
- [ ] T028 [US1] Criar queries de listar, detalhar e listar ocorrências em `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/`
- [ ] T029 [US1] Implementar handlers CQRS com autoria autenticada, idempotência e cálculo de próxima execução em `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/`
- [ ] T030 [US1] Implementar `AgendamentoPresencaRequestValidator` sem duplicar invariantes de domínio em `BackEnd/src/RinhaDasLendas.Application/Validators/AgendamentoPresencaRequestValidator.cs`
- [ ] T031 [US1] Criar `ISystemClock` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/ISystemClock.cs` e `SystemClock` em `BackEnd/src/RinhaDasLendas.Api/Services/SystemClock.cs`
- [ ] T032 [US5] Acrescentar clientes autenticados Moderador e Jogador em `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/SecurityApiFactory.cs`
- [ ] T033 [US5] Implementar os oito endpoints finos com `CanManageDrafts`, `ISender`, autoria do claim e respostas padrão em `BackEnd/src/RinhaDasLendas.Api/Controllers/AgendamentosPresencaController.cs`
- [ ] T034 [US1] Adicionar `MV089`-`MV100` sincronizados em `docs/messages/message-catalog.md`, `docs/messages/message-codes.md`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx`
- [ ] T035 [US1] Registrar `ISystemClock` e dependências CQRS em `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T036 [US1] Executar validators/handlers/endpoints e registrar GREEN de CRUD, paginação e projeções em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T037 [US5] Executar a matriz HTTP e registrar GREEN de `401`, `403`, `400`, `404`, `409` e sucessos em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: gestão da US1 funciona por API e a fronteira da US5 é comprovada independentemente do scheduler e frontend.

---

## Phase 4: Execução Exatamente Uma Vez e Recuperação (equivale à Task 5; US2 e US3)

**Story Goal**: criar uma ocorrência/draft por data e recuperar todas as datas atrasadas sem avanço prematuro de `UltimaDataAvaliada`.

**Independent Test**: duas execuções concorrentes criam um único draft; após três dias indisponíveis, cada data é classificada e a data atual é criada somente se ainda estiver na janela.

- [ ] T038 [US2] Escrever testes RED de conversão São Paulo/UTC e horário local inválido ou ambíguo em `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`
- [ ] T039 [US2] Escrever testes RED para antes da publicação, dentro da janela, configuração indisponível, retomada e concorrência em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [ ] T040 [US3] Acrescentar testes RED para indisponibilidade de três dias, `UltimaDataAvaliada`, perda após encerramento e reativação tardia em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [ ] T041 [US3] Acrescentar testes RED para falha isolada por agenda, ciclo sem sobreposição e cancelamento do host em `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- [ ] T042 [US2] Executar testes do ciclo/timezone e registrar RED pelos handlers/serviço ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T043 [US2] Criar `IAgendamentoPresencaMetrics` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaMetrics.cs`
- [ ] T044 [US2] Criar `ProcessarAgendamentosPresencaDevidosCommand` e `AgendamentoPresencaCycleResult` em `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommand.cs`
- [ ] T045 [US3] Implementar percurso sem horizonte das datas após `UltimaDataAvaliada`, bloqueio, perda, reativação e criação transacional em `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- [ ] T046 [US2] Implementar `AgendamentoPresencaExecutionService` com escopo, `PeriodicTimer`, ciclo sequencial e cancelamento em `BackEnd/src/RinhaDasLendas.Api/Services/AgendamentoPresencaExecutionService.cs`
- [ ] T047 [US2] Implementar contadores/histograma sem nome, observação, usuário ou IDs Discord em `BackEnd/src/RinhaDasLendas.Api/Observability/AgendamentoPresencaMetrics.cs`
- [ ] T048 [US2] Registrar hosted service, métricas e intervalo default 30 em `BackEnd/src/RinhaDasLendas.Api/Program.cs` e `BackEnd/src/RinhaDasLendas.Api/appsettings.json`
- [ ] T049 [US2] Executar testes do ciclo e registrar GREEN para exactly-once, claim expirável e configuração indisponível em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T050 [US3] Executar cenários de múltiplos dias e registrar GREEN para recuperação, perda, reativação tardia e avanço seguro de `UltimaDataAvaliada` em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: US2 e US3 são testáveis sem interface e preservam uma ocorrência/draft/publicação por agenda/data.

---

## Phase 5: Central de Automações (equivale à Task 6; US1, US4 e US5)

**Story Goal**: Moderador+ gerencia e acompanha agendas em `/configuracoes`, sem acesso à configuração sensível.

**Independent Test**: Moderador vê cards e executa CRUD em PT/EN; Jogador não vê a seção; Admin vê agendas e configuração sensível; modal funciona por teclado e em 320px.

- [ ] T051 [US1] Escrever testes RED de URLs, métodos, payload `HH:mm`, `messageCode` e propagação de `403`/`500` em `FrontEnd/src/services/presenceSchedules.spec.ts`
- [ ] T052 [US1] Escrever testes RED de campos, chips `aria-pressed`, validação, loading, `Escape` e foco em `FrontEnd/src/components/settings/PresenceScheduleFormDialog.spec.ts`
- [ ] T053 [US4] Escrever testes RED de cards, próxima execução, vazio, loading, erro e status em `FrontEnd/src/components/settings/PresenceScheduleSection.spec.ts`
- [ ] T054 [US1] Escrever testes RED de pausa/exclusão contextual e submissão única em `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.spec.ts`
- [ ] T055 [US5] Escrever testes RED de visibilidade Jogador/Moderador/Admin e separação de permissões em `FrontEnd/src/views/SettingsView.spec.ts`
- [ ] T056 [US1] Executar os testes frontend focados e registrar RED pelos componentes/serviço ausentes em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T057 [US1] Criar tipos fechados e contratos de request/summary em `FrontEnd/src/types/presenceSchedule.ts`
- [ ] T058 [US1] Implementar as seis funções de serviço sem fallback silencioso em `FrontEnd/src/services/presenceSchedules.ts`
- [ ] T059 [US1] Implementar formulário acessível de criação/edição em `FrontEnd/src/components/settings/PresenceScheduleFormDialog.vue`
- [ ] T060 [US1] Implementar confirmações acessíveis de pausa e arquivamento em `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.vue`
- [ ] T061 [US4] Implementar resumo, cards, status, próxima execução, ações e estados em `FrontEnd/src/components/settings/PresenceScheduleSection.vue`
- [ ] T062 [US5] Integrar agendas por `CanManageDrafts` e manter configuração sensível por `CanManageUsers` em `FrontEnd/src/views/SettingsView.vue`
- [ ] T063 [P] [US1] Adicionar `settings.presenceSchedules` completo e acentuado em `FrontEnd/src/i18n/locales/pt.json`
- [ ] T064 [P] [US1] Adicionar estrutura equivalente em inglês em `FrontEnd/src/i18n/locales/en.json`
- [ ] T065 [US4] Aplicar cards responsivos, chips, ações e modal com tokens existentes e sem overflow em 320px em `FrontEnd/src/styles/main.css`
- [ ] T066 [US1] Executar testes frontend focados e registrar GREEN de serviço, CRUD, formulário e confirmações em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T067 [US4] Executar testes de seção e registrar GREEN de acompanhamento, estados e responsividade contratual em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T068 [US5] Executar `SettingsView.spec.ts` e registrar GREEN da matriz de visibilidade e separação de permissões em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: US1, US4 e a visibilidade da US5 funcionam em frontend sem deslocar regras do backend.

---

## Phase 6: Compatibilidade do Bot, Release e Documentação (equivale à Task 7; US2 e US4)

**Story Goal**: comprovar polling existente sem endpoint novo e tornar a entrega `2026.07.2` compreensível e operável.

**Independent Test**: fixture de draft agendado passa por dois ciclos com uma publicação; release localizada é a mais recente e documentação cobre recuperação sem segredos.

- [ ] T069 [US2] Escrever fixture/teste RED ou de caracterização para draft agendado em dois ciclos de `runDraftPollingCycle` em `discord-bot/src/modules/drafts/draftInteractions.spec.ts`
- [ ] T070 [US2] Executar o teste do bot e, se falhar por contrato real, corrigir somente a adaptação existente sem endpoint/regra de agenda em `discord-bot/src/modules/drafts/draftInteractions.spec.ts` e arquivos de produção estritamente necessários
- [ ] T071 [US4] Escrever testes RED para `2026.07.2`, ID `presence-scheduling-2026-07`, posição latest e paridade localizada em `FrontEnd/src/constants/systemUpdates.spec.ts`, `FrontEnd/src/services/systemUpdates.spec.ts` e `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T072 [US4] Adicionar release `2026.07.2` e remover destaque de `2026.07.1` em `FrontEnd/src/constants/systemUpdates.ts`
- [ ] T073 [P] [US4] Adicionar conteúdo de produto da release em português em `FrontEnd/src/i18n/locales/pt.json`
- [ ] T074 [P] [US4] Adicionar conteúdo equivalente da release em inglês em `FrontEnd/src/i18n/locales/en.json`
- [ ] T075 [P] [US4] Atualizar fluxo existente de drafts Discord em `docs/domain/DRAFT_DISCORD_OPERATIONS.md`
- [ ] T076 [P] [US4] Documentar agenda, bloqueio, recuperação, perda, métricas e runbook em `docs/domain/AGENDAMENTO_LISTAS_PRESENCA.md`
- [ ] T077 [US2] Executar suíte/build do bot e registrar que nenhum endpoint novo ou regra de recorrência foi introduzido em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T078 [US4] Executar testes do histórico/i18n e registrar GREEN de `2026.07.2`, paridade e linguagem segura em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: bot permanece adaptador e a entrega fica documentada e localizada.

---

## Phase 7: Verificação Integrada, Segurança e Browser Real (equivale à Task 8)

**Purpose**: produzir evidência final reproduzível sem ampliar escopo.

- [ ] T079 Executar testes e build Release do backend pelo devcontainer e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T080 Executar testes, build e lint sem fix do frontend e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T081 Executar testes e build completos do bot e registrar resultados em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T082 Aplicar migration em PostgreSQL descartável e comprovar constraints de agenda/data, dias, rollback e reaplicação em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T083 Executar matriz HTTP real para anônimo, Jogador, Moderador e Admin e registrar ausência de dados operacionais em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T084 Disparar dois ciclos simultâneos e comprovar uma ocorrência, um draft, uma publicação pendente e um claim vencedor em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T085 Validar recuperação real de múltiplos dias, claim expirado e `UltimaDataAvaliada` sem lacunas em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T086 Validar `/configuracoes` com browser real para Jogador/Moderador/Admin em 1440x900, 768x1024, 390x844 e 320px e registrar evidências em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T087 Auditar autorização, autoria, DTOs, rate limiting, logs, métricas e ausência de segredos e registrar resultado em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T088 Auditar textos hardcoded frontend/backend, paridade `pt.json`/`en.json`, resources PT/EN, bot, acentuação, placeholders, botões, títulos, badges, toasts, confirmações, estados vazios e validações em `specs/020-agendamento-listas-presenca/tasks.md`
- [ ] T089 Executar `git diff --check`, revisar somente arquivos da feature e marcar tarefas comprovadas em `specs/020-agendamento-listas-presenca/tasks.md`

**Checkpoint**: nenhuma tarefa é concluída com `Não` na auditoria de internacionalização ou sem evidência de segurança, concorrência e recuperação.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: começa somente após aprovação explícita deste `tasks.md` e ambiente backend disponível.
- **Phase 2**: depende das entidades e transições da Phase 1.
- **Phase 3**: depende do repositório e timezone da Phase 2.
- **Phase 4**: depende do domínio, operações atômicas e recursos/API estabelecidos nas Phases 1-3.
- **Phase 5**: depende do contrato HTTP funcional da Phase 3, mas pode começar após esse checkpoint sem aguardar o scheduler completo.
- **Phase 6**: depende da criação de draft/publicação da Phase 4 e da estrutura i18n frontend da Phase 5.
- **Phase 7**: depende de todas as fases anteriores.

### User Story Dependencies

- **US1 (P1)**: domínio, persistência, API e UI de gestão; entrega administração semanal independente do scheduler.
- **US2 (P1)**: depende do domínio/persistência e entrega exactly-once independentemente da UI.
- **US3 (P1)**: amplia o ciclo da US2 com recuperação de múltiplos dias e marcador seguro.
- **US4 (P2)**: usa projeções da API e ocorrências da US2/US3 para acompanhamento.
- **US5 (P1)**: autorização backend é testável na Phase 3; visibilidade frontend é concluída na Phase 5.

### Red-Green Order

- T001-T004 precedem T005-T009.
- T010-T013 precedem T014-T021.
- T022-T025 precedem T026-T037.
- T038-T042 precedem T043-T050.
- T051-T056 precedem T057-T068.
- T069 precede qualquer adaptação permitida em T070; T071 precede T072-T074.
- Cada teste novo deve falhar pelo comportamento ausente, não apenas por indisponibilidade de SDK, banco ou serviço.

### Parallel Opportunities

- T063 e T064 podem ocorrer em paralelo após os testes i18n porque alteram catálogos distintos.
- T073 e T074 podem ocorrer em paralelo após T071 porque alteram catálogos distintos.
- T075 e T076 podem ocorrer em paralelo após o comportamento estar consolidado porque alteram documentos distintos.
- Nenhuma outra tarefa recebe `[P]`; pares de teste/implementação, migrations, `Program.cs`, `tasks.md` e catálogos compartilhados exigem ordem.

## Parallel Examples

```text
T063: Adicionar settings.presenceSchedules em FrontEnd/src/i18n/locales/pt.json
T064: Adicionar settings.presenceSchedules em FrontEnd/src/i18n/locales/en.json

T075: Atualizar docs/domain/DRAFT_DISCORD_OPERATIONS.md
T076: Criar docs/domain/AGENDAMENTO_LISTAS_PRESENCA.md
```

## Implementation Strategy

### MVP First

1. Aprovar os artefatos e disponibilizar o devcontainer/.NET antes de T001.
2. Concluir Phases 1-3 para permitir gestão segura e persistida de agendas como primeiro incremento.
3. Validar US1 e a autorização backend da US5 independentemente antes do scheduler.
4. Concluir Phase 4 para entregar US2/US3 sem depender da interface.

### Incremental Delivery

1. Domínio e banco íntegros.
2. CRUD autorizado e localizado.
3. Execução exatamente uma vez e recuperação de múltiplos dias.
4. Central responsiva e acompanhamento.
5. Regressão do bot, release e documentação.
6. Verificação integrada, browser, segurança e i18n.

## Traceability

| Requirement/contract | Tasks |
|----------------------|-------|
| FR-001, FR-002, backend auth | T024, T032-T037, T055, T062, T068, T083, T087 |
| FR-003-FR-008, domain | T001-T009, T022-T030 |
| FR-009-FR-011, exactly-once/claim | T010-T021, T038-T049, T082, T084-T085 |
| FR-012-FR-015, draft/bot/Discord indisponível | T012, T018, T039, T045, T069-T070, T077 |
| FR-016-FR-019, timezone/recuperação/`UltimaDataAvaliada` | T017, T038, T040, T045, T050, T085 |
| FR-020, frontend UI | T051-T068, T086 |
| FR-021, DTOs seguros | T024, T026, T033, T037, T083, T087 |
| FR-022-FR-023, i18n | T034, T051-T068, T071-T074, T078, T088 |
| FR-024, observabilidade | T041, T043, T047-T050, T087 |
| FR-025, release `2026.07.2` | T071-T078 |
| Backend API contract | T022-T037, T083 |
| Frontend UI contract | T051-T068, T086, T088 |
| Discord bot contract | T069-T070, T077, T084 |
| SC-001-SC-008 | T037, T049-T050, T066-T068, T077-T089 |

## Gate

- Não iniciar T001 até aprovação explícita de `spec.md`, `plan.md`, `data-model.md`, `contracts/` e `tasks.md`.
- Não executar backend nesta sessão: o baseline .NET está indisponível.
- Preservar `docs/prompts/`, `specs/018-importacao-partidas-lcu/` e qualquer mudança não relacionada.

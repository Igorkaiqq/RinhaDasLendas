---
description: "Lista de tarefas para implementar a Fundação Competitiva Sazonal"
---

# Tasks: Fundação Competitiva Sazonal

**Input**: Documentos de design em `/specs/023-fundacao-competitiva-sazonal/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/competitive-foundation.openapi.yaml` e `contracts/ui-contracts.md`

**Tests**: Obrigatórios por SC-009. Em cada fase, escrever os testes primeiro, confirmar que falham pelo motivo esperado e somente então implementar.

**Organization**: As tarefas são agrupadas por user story. Todas as fases P1 precedem o Evento P2.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Pode executar em paralelo por atuar em arquivos distintos e não depender de tarefa incompleta.
- **[Story]**: User story atendida pela tarefa.
- Todas as tarefas possuem caminho exato de arquivo.

## Phase 1: Setup

**Purpose**: Preparar infraestrutura de teste e validação contratual reutilizável.

- [x] T001 [P] Criar builders e fixtures competitivas para Seasons, competições, regras, Séries, Partidas, Times, DraftMontagem e atores autenticados em `BackEnd/tests/RinhaDasLendas.Tests/Fixtures/CompetitiveFoundationFixtures.cs`
- [x] T002 [P] Criar fábrica PostgreSQL isolada para testes concorrentes, de migration e constraints em `BackEnd/tests/RinhaDasLendas.Tests/Fixtures/CompetitivePostgresFixture.cs`
- [x] T003 [P] Criar teste-base que carregue e valide o contrato OpenAPI da feature em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveFoundationOpenApiContractTests.cs`
- [x] T004 [P] Criar fixtures tipadas de Season, competição, Evento, Série, Partida e capabilities para testes Vue em `FrontEnd/src/test/competitiveFixtures.ts`

## Phase 2: Foundational

**Purpose**: Implementar contratos, persistência, autorização, concorrência, idempotência e localização que bloqueiam todas as user stories.

**Critical**: Nenhuma user story começa antes desta fase estar concluída.

### Tests

- [x] T005 [P] Criar testes automatizados da matriz completa das cinco capabilities para SuperAdmin, Presidente, VicePresidente, Admin, Moderador, Capitão e Jogador, incluindo todas as condições de recurso e negação padrão do VicePresidente, em `BackEnd/tests/RinhaDasLendas.Tests/Security/CompetitiveFoundationAuthorizationTests.cs`
- [x] T006 [P] Criar testes de replay idempotente, conflito por conteúdo divergente, retenção de 90 dias, ETag obsoleta, duas confirmações concorrentes e correção concorrente com confirmação em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveFoundationConcurrencyTests.cs`
- [x] T007 [P] Criar testes da migration para schema vazio e atual, downgrade, UUIDs, `snake_case`, FKs, exclusões, unicidades, sobreposição temporal, Season ativa única e Partida em uma única Série em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveFoundationMigrationTests.cs`
- [x] T008 [P] Criar testes de paridade dos resources PT-BR/EN-US e envelopes localizados de validação, conflito, proibição e inexistência em `BackEnd/tests/RinhaDasLendas.Tests/Messages/CompetitiveFoundationLocalizationTests.cs`

### Implementation

- [x] T009 [P] Adicionar Presidente e VicePresidente, as cinco capabilities esportivas e os códigos estáveis de mensagens competitivas em `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthRoles.cs`, `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthPermissions.cs` e `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- [x] T010 [P] Criar enums estáveis de Season, Série, draft, Partida, remake e término em `BackEnd/src/RinhaDasLendas.Domain/Enums/SeasonEstado.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/SerieTipo.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/SerieEstado.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/SerieFormato.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/ModoDraft.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/PartidaEstado.cs`, `BackEnd/src/RinhaDasLendas.Domain/Enums/DecisaoPicksRemake.cs` e `BackEnd/src/RinhaDasLendas.Domain/Enums/MotivoTerminoPartida.cs`
- [ ] T011 [P] Definir eventos de domínio e contratos de repositório para calendário, competição, Evento, Série, auditoria e idempotência em `BackEnd/src/RinhaDasLendas.Domain/Events/Competitive/CompetitiveDomainEvents.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/ICalendarioCompetitivoRepository.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/ICompeticaoRepository.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/IEventoCompetitivoRepository.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/ISerieRepository.cs`, `BackEnd/src/RinhaDasLendas.Domain/Repositories/ICompetitiveAuditRepository.cs` e `BackEnd/src/RinhaDasLendas.Domain/Repositories/IIdempotencyRepository.cs`
- [ ] T012 [P] Criar o modelo estrutural de calendário, Season, competição, rodada e versão imutável de regras em `BackEnd/src/RinhaDasLendas.Domain/Entities/CalendarioCompetitivo.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/Season.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/Competicao.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/Rodada.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/VersaoRegras.cs`
- [ ] T013 [P] Criar o modelo estrutural de Série, lados, participantes, Partidas e picks versionados em `BackEnd/src/RinhaDasLendas.Domain/Entities/Serie.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/LadoSerie.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/ParticipanteEsperadoSerie.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/Partida.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/PickPartida.cs`
- [ ] T014 [P] Criar estruturas append-only de Evento, auditoria e idempotência sem payload bruto ou texto localizado em `BackEnd/src/RinhaDasLendas.Domain/Entities/EventoCompetitivo.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/EventoTime.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/RegistroAuditoriaCompetitiva.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/OperacaoIdempotente.cs`
- [ ] T015 Configurar DbSets, relacionamentos, conversões, índices, constraints temporais, concorrência otimista e exclusões restritivas em `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T016 Implementar idempotência vinculada a ator, método, rota e hash, replay da resposta mínima e header `Idempotency-Replayed` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IIdempotencyService.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Services/IdempotencyService.cs` e `BackEnd/src/RinhaDasLendas.Api/Middleware/IdempotencyMiddleware.cs`
- [ ] T017 Implementar identidade do ator autenticado, policies por capability, condições de SuperAdmin/Admin/Moderador e cálculo servidor-side de `acoesPermitidas` em `BackEnd/src/RinhaDasLendas.Application/Interfaces/ICurrentActor.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/CurrentActor.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/AuthService.cs` e `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- [ ] T018 Adicionar mensagens competitivas sincronizadas e localizadas em `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx` e `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- [ ] T019 Criar a migration única da fundação competitiva, incluindo singleton do calendário, índices parciais, constraints e FKs, em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260728000000_AddCompetitiveSeasonFoundation.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260728000000_AddCompetitiveSeasonFoundation.Designer.cs`

## Phase 3: User Story 1 - Administrar a Season atual (Priority: P1) MVP

**Goal**: Cadastrar, ativar e encerrar Seasons; criar competições, rodadas e regras; aplicar seleção sazonal explícita e preservar histórico.

**Independent Test**: Criar Seasons consecutivas, ativar uma com concorrência, criar competição/rodada/regra e validar os recortes atual, múltiplos e todas sem reatribuir fatos históricos.

### Tests

- [ ] T020 [P] [US1] Criar testes de período `[início, fim)`, limites inicial/final, não sobreposição, ordem anual, estados e preservação histórica da Season em `BackEnd/tests/RinhaDasLendas.Tests/Domain/SeasonTests.cs`
- [ ] T021 [P] [US1] Criar testes de competição, circuito diário único, rodadas ordenadas, publicação imutável MD3/MD5 e regra geral versus regra de competição em `BackEnd/tests/RinhaDasLendas.Tests/Domain/CompeticaoTests.cs`
- [ ] T022 [P] [US1] Criar testes de handlers para ativação atômica concorrente, alteração de período, encerramento, seleção padrão/múltipla/todas e catálogo administrativo sem filtro implícito em `BackEnd/tests/RinhaDasLendas.Tests/Application/SeasonSelectionContractTests.cs`
- [ ] T023 [P] [US1] Criar testes HTTP dos endpoints de Seasons, competições, rodadas e regras, incluindo paginação, DTOs, ETags, idempotência e respostas localizadas em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveSeasonApiTests.cs`
- [ ] T024 [P] [US1] Criar testes Vue do seletor sazonal, formulários, conflito 409 e fluxo Season → competição → rodada em `FrontEnd/src/components/competitive/SeasonManagement.spec.ts`

### Implementation

- [ ] T025 [US1] Implementar transições, validação temporal, ativação atômica e value object de seleção sazonal em `BackEnd/src/RinhaDasLendas.Domain/Rules/SeasonRules.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/CalendarioCompetitivo.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/Season.cs`
- [ ] T026 [US1] Implementar comandos, validators e handlers de criação, edição, ativação e encerramento de Season com auditoria, idempotência e ETag do calendário em `BackEnd/src/RinhaDasLendas.Application/Commands/Seasons/SeasonCommands.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/SeasonValidators.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/Seasons/SeasonCommandHandlers.cs`
- [ ] T027 [US1] Implementar criação/edição de competição, criação/reordenação de rodadas e publicação imutável de regras com condições de recurso em `BackEnd/src/RinhaDasLendas.Application/Commands/Competicoes/CompetitionCommands.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/CompetitionValidators.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/Competicoes/CompetitionCommandHandlers.cs`
- [ ] T028 [US1] Implementar DTOs e queries paginadas de Season e competição com `calendarioConfigurado`, `temporadaAtual`, `seasonsIncluidas` e `acoesPermitidas` em `BackEnd/src/RinhaDasLendas.Application/Dtos/SeasonDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/CompeticaoDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Queries/Seasons/SeasonQueries.cs` e `BackEnd/src/RinhaDasLendas.Application/Queries/Competicoes/CompetitionQueries.cs`
- [ ] T029 [US1] Implementar persistência transacional do calendário, Seasons, competições, rodadas e versões de regras em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/CalendarioCompetitivoRepository.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/CompeticaoRepository.cs`
- [ ] T030 [US1] Expor contratos REST de Seasons, competições, rodadas e regras com Swagger, DTOs e códigos HTTP do OpenAPI em `BackEnd/src/RinhaDasLendas.Api/Controllers/TemporadasController.cs` e `BackEnd/src/RinhaDasLendas.Api/Controllers/CompeticoesController.cs`
- [ ] T031 [P] [US1] Criar tipos, serviços Axios e serialização URL dos recortes `current`, `selected` e `all` em `FrontEnd/src/types/season.ts`, `FrontEnd/src/types/competition.ts`, `FrontEnd/src/services/seasons.ts` e `FrontEnd/src/services/competitions.ts`
- [ ] T032 [US1] Implementar `SeasonsView`, seletor, drawers, diálogo de transição e painel de competição com estados acessíveis e textos PT/EN em `FrontEnd/src/views/SeasonsView.vue`, `FrontEnd/src/components/competitive/SeasonScopeSelector.vue`, `FrontEnd/src/components/competitive/SeasonFormDrawer.vue`, `FrontEnd/src/components/competitive/SeasonTransitionDialog.vue`, `FrontEnd/src/components/competitive/CompetitionPanel.vue`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

## Phase 4: User Story 2 - Operar uma série diária com Fearless (Priority: P1)

**Goal**: Operar Séries diárias MD3/MD5 originadas de DraftMontagem finalizado, com snapshots, placar, remakes, Fearless reconstruível e conclusão segura.

**Independent Test**: Criar MD3 Fearless a partir de draft finalizado, confirmar partidas sem repetir campeões, tratar remake e concluir em duas vitórias sem permitir partidas excedentes.

### Tests

- [ ] T033 [P] [US2] Criar testes de Série MD3/MD5, dois lados, snapshots, estados, placar, surrender, cancelamento, conclusão e bloqueio de partidas excedentes em `BackEnd/tests/RinhaDasLendas.Tests/Domain/SerieTests.cs`
- [ ] T034 [P] [US2] Criar testes de Fearless bilateral, picks confirmados, remakes preservados/desconsiderados, isolamento entre Séries e reconstrução após correção em `BackEnd/tests/RinhaDasLendas.Tests/Domain/FearlessTests.cs`
- [ ] T035 [P] [US2] Criar testes de correção de Série concluída para `2-0 → 1-1` com anulação explícita, inversão de vencedor e anulação da Partida decisiva em `BackEnd/tests/RinhaDasLendas.Tests/Application/CompetitiveCorrectionHandlerTests.cs`
- [ ] T036 [P] [US2] Criar testes de handlers para DraftMontagem finalizado/arquivado, capitães, data local de São Paulo, Season ativa, ETags compartilhadas e concorrência entre Partidas em `BackEnd/tests/RinhaDasLendas.Tests/Application/DailySeriesHandlerTests.cs`
- [ ] T037 [P] [US2] Criar testes HTTP de criação/início/cancelamento de Série, Partidas, picks, resultados, remakes, anulações e correções conforme OpenAPI em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DailySeriesApiTests.cs`
- [ ] T038 [P] [US2] Criar testes Vue para placar, Fearless, operação de Partida, remake, correção, conflito 409 e ausência de textos hardcoded em `FrontEnd/src/components/competitive/DailySeriesOperation.spec.ts`

### Implementation

- [ ] T039 [US2] Implementar regras de formato, estados, placar derivado, elegibilidade diária e data competitiva em `America/Sao_Paulo` em `BackEnd/src/RinhaDasLendas.Domain/Rules/SerieRules.cs`
- [ ] T040 [US2] Implementar reconstrução Fearless por ordem, validação dos dez picks, remake e sinalização de conflitos posteriores em `BackEnd/src/RinhaDasLendas.Domain/Rules/FearlessRules.cs`
- [ ] T041 [US2] Implementar comportamento agregado de Série, Partida, picks versionados e snapshots imutáveis sem alterar o ciclo do DraftMontagem em `BackEnd/src/RinhaDasLendas.Domain/Entities/Serie.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/Partida.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/PickPartida.cs`, `BackEnd/src/RinhaDasLendas.Domain/Entities/LadoSerie.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/ParticipanteEsperadoSerie.cs`
- [ ] T042 [US2] Implementar comandos e validators de Série diária, transições, Partidas, picks, resultado, remake, cancelamento, anulação e correção em `BackEnd/src/RinhaDasLendas.Application/Commands/Series/SeriesCommands.cs`, `BackEnd/src/RinhaDasLendas.Application/Commands/Partidas/MatchCommands.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/SerieValidators.cs` e `BackEnd/src/RinhaDasLendas.Application/Validators/PartidaValidators.cs`
- [ ] T043 [US2] Implementar handlers transacionais com ETag da Série, idempotência, ator autenticado, auditoria antes/depois e conclusão automática MD3/MD5 em `BackEnd/src/RinhaDasLendas.Application/Handlers/Series/SeriesCommandHandlers.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/Partidas/MatchCommandHandlers.cs`
- [ ] T044 [US2] Implementar DTOs e queries de Série, Partida, placar, Fearless, revisão necessária e elegibilidade futura em `BackEnd/src/RinhaDasLendas.Application/Dtos/SerieDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/PartidaDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Queries/Series/SeriesQueries.cs` e `BackEnd/src/RinhaDasLendas.Application/Queries/Partidas/MatchQueries.cs`
- [ ] T045 [US2] Implementar persistência do agregado Série, carregamento do DraftMontagem e gravação atômica de Partidas, picks e versões em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/SerieRepository.cs`
- [ ] T046 [US2] Expor endpoints REST de Séries e Partidas, ETags da Série e contratos Swagger sem expor entidades em `BackEnd/src/RinhaDasLendas.Api/Controllers/SeriesController.cs` e `BackEnd/src/RinhaDasLendas.Api/Controllers/PartidasController.cs`
- [ ] T047 [P] [US2] Criar tipos e serviços frontend para Séries, Partidas, idempotência, ETags e invalidação conjunta de placar/Fearless em `FrontEnd/src/types/series.ts`, `FrontEnd/src/types/match.ts`, `FrontEnd/src/services/series.ts` e `FrontEnd/src/services/matches.ts`
- [ ] T048 [US2] Implementar listagem/detalhe de Séries, placar, Fearless e operação de Partida com estados localizados em `FrontEnd/src/views/SeriesView.vue`, `FrontEnd/src/views/SeriesDetailView.vue`, `FrontEnd/src/views/MatchDetailView.vue`, `FrontEnd/src/components/competitive/SeriesScoreboard.vue`, `FrontEnd/src/components/competitive/FearlessPanel.vue`, `FrontEnd/src/components/competitive/MatchOperationPanel.vue`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

## Phase 5: User Story 3 - Agendar confrontos entre times oficiais (Priority: P1)

**Goal**: Agendar confrontos oficiais e amistosos entre Times ativos, sem presença, preservando snapshots e elegibilidade correta.

**Independent Test**: Agendar um confronto oficial e um amistoso entre os mesmos Times e confirmar que somente o oficial alimenta projeções competitivas futuras.

### Tests

- [ ] T049 [P] [US3] Criar testes de domínio para Times oficiais distintos, snapshots históricos, confronto sem presença, regras da mesma Season e inelegibilidade absoluta de amistosos em `BackEnd/tests/RinhaDasLendas.Tests/Domain/OfficialAndFriendlySeriesTests.cs`
- [ ] T050 [P] [US3] Criar testes de autorização e API para Presidente, Admin, Moderador e SuperAdmin em ConfrontoOficial/Amistoso e validar projeções oficiais inalteradas pelo amistoso em `BackEnd/tests/RinhaDasLendas.Tests/Integration/OfficialAndFriendlySeriesApiTests.cs`
- [ ] T051 [P] [US3] Criar testes Vue do formulário adaptativo, cards oficiais/amistosos, aviso permanente e informação competitiva preservada no mobile em `FrontEnd/src/components/competitive/OfficialSeriesScheduling.spec.ts`

### Implementation

- [ ] T052 [US3] Estender as regras de Série para `ConfrontoOficial` e `Amistoso`, lados compatíveis, Season/competição/rodada coerentes e elegibilidade derivada em `BackEnd/src/RinhaDasLendas.Domain/Rules/SerieRules.cs`
- [ ] T053 [US3] Implementar criação de confrontos com Times ativos, snapshots de nome/tag/membros e ausência de dependência da lista de presença em `BackEnd/src/RinhaDasLendas.Application/Handlers/Series/SeriesCommandHandlers.cs`
- [ ] T054 [US3] Implementar consultas separadas de histórico/agregados amistosos e indicadores de Score, Rating, MVP/SVP, ranking, recordes e qualificação em `BackEnd/src/RinhaDasLendas.Application/Queries/Series/SeriesQueries.cs` e `BackEnd/src/RinhaDasLendas.Application/Dtos/SerieDtos.cs`
- [ ] T055 [US3] Completar contratos HTTP e testes de DTO/OpenAPI para filtros de tipo, Season histórica e detalhes sem filtro implícito em `BackEnd/src/RinhaDasLendas.Api/Controllers/SeriesController.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveFoundationOpenApiContractTests.cs`
- [ ] T056 [P] [US3] Implementar formulário adaptativo, card responsivo e avisos de amistoso em `FrontEnd/src/components/competitive/SeriesFormDrawer.vue`, `FrontEnd/src/components/competitive/SeriesCard.vue` e `FrontEnd/src/views/SeriesView.vue`
- [ ] T057 [US3] Adicionar tipos, filtros, serviços e textos PT/EN para natureza, elegibilidade e histórico amistoso em `FrontEnd/src/types/series.ts`, `FrontEnd/src/services/series.ts`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

## Phase 6: User Story 4 - Registrar evento com quatro times sem Fearless (Priority: P2)

**Goal**: Criar Eventos com exatamente quatro Times oficiais e impedir Fearless no regulamento e em todas as Séries associadas.

**Independent Test**: Criar Evento com quatro Times, tentar ativar Fearless e confirmar que cada confronto usa draft padrão com bloqueios isolados.

### Tests

- [ ] T058 [P] [US4] Criar testes de domínio para exatamente quatro Times ativos/distintos, snapshots, mesma Season, modo padrão fixo e associação válida de Séries em `BackEnd/tests/RinhaDasLendas.Tests/Domain/EventoCompetitivoTests.cs`
- [ ] T059 [P] [US4] Criar testes HTTP e de autorização para recusar Fearless global ou por Série, impedir Times externos e garantir bloqueios não compartilhados em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveEventApiTests.cs`
- [ ] T060 [P] [US4] Criar testes Vue para seleção de quatro Times, ausência de toggle Fearless e apresentação acessível do draft padrão em `FrontEnd/src/components/competitive/CompetitiveEvent.spec.ts`

### Implementation

- [ ] T061 [US4] Implementar invariantes e snapshots do Evento e seus quatro Times em `BackEnd/src/RinhaDasLendas.Domain/Entities/EventoCompetitivo.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/EventoTime.cs`
- [ ] T062 [US4] Implementar comando, validator, handler, DTO, query e repositório do Evento com auditoria e idempotência em `BackEnd/src/RinhaDasLendas.Application/Commands/Eventos/EventCommands.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/EventoValidators.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/Eventos/EventHandlers.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/EventoDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Queries/Eventos/EventQueries.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/EventoCompetitivoRepository.cs`
- [ ] T063 [US4] Impor draft padrão e isolamento Fearless ao associar Série a Evento e expor os endpoints OpenAPI em `BackEnd/src/RinhaDasLendas.Application/Handlers/Series/SeriesCommandHandlers.cs` e `BackEnd/src/RinhaDasLendas.Api/Controllers/EventosController.cs`
- [ ] T064 [US4] Implementar tipos, serviço e controles de Evento no formulário de Série com textos sincronizados PT/EN em `FrontEnd/src/types/event.ts`, `FrontEnd/src/services/events.ts`, `FrontEnd/src/components/competitive/SeriesFormDrawer.vue`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

## Phase 7: User Story 5 - Auditar decisões competitivas (Priority: P2)

**Goal**: Registrar e consultar alterações competitivas append-only com ator, capability, instante, justificativa e valores anterior/posterior.

**Independent Test**: Corrigir resultado com justificativa e recuperar o histórico por recurso, preservando versões de regras e negando metadados a usuários não autorizados.

### Tests

- [ ] T065 [P] [US5] Criar testes de auditoria append-only para calendário, Season, competição, rodada, regra, Evento, Série e Partida, incluindo ator autenticado e redaction de dados sensíveis, em `BackEnd/tests/RinhaDasLendas.Tests/Application/CompetitiveAuditTests.cs`
- [ ] T066 [P] [US5] Criar testes HTTP de auditoria contextual/global, paginação, ausência de filtro sazonal implícito e matriz negativa de acesso em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveAuditApiTests.cs`
- [ ] T067 [P] [US5] Criar testes Vue do painel de auditoria, estados antes/depois, justificativa, acesso negado e localização em `FrontEnd/src/components/competitive/CompetitiveAuditPanel.spec.ts`

### Implementation

- [ ] T068 [US5] Implementar projeção append-only, snapshots mínimos redigidos, consulta por tipo/ID e autorização por recurso em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/CompetitiveAuditRepository.cs`, `BackEnd/src/RinhaDasLendas.Application/Queries/CompetitiveAudit/CompetitiveAuditQueries.cs` e `BackEnd/src/RinhaDasLendas.Application/Dtos/CompetitiveAuditDtos.cs`
- [ ] T069 [US5] Expor auditoria global e contextual com DTOs paginados, respostas localizadas e proteção `CanViewCompetitiveAudit` em `BackEnd/src/RinhaDasLendas.Api/Controllers/AuditoriaCompetitivaController.cs` e `BackEnd/src/RinhaDasLendas.Api/Controllers/SeriesController.cs`
- [ ] T070 [US5] Implementar painel autorizado de auditoria e dialogs de correção/anulação com antes/depois e textos PT/EN em `FrontEnd/src/components/competitive/CompetitiveAuditPanel.vue`, `FrontEnd/src/components/competitive/MatchOperationPanel.vue`, `FrontEnd/src/views/SeriesDetailView.vue`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validar contratos, rotas, qualidade, segurança, i18n, acessibilidade e critérios mensuráveis.

- [ ] T071 [P] Atualizar navegação, permissions e rotas autenticadas de Seasons, Séries e Partidas, substituindo o placeholder sem criar estado duplicado, em `FrontEnd/src/constants/permissions.ts`, `FrontEnd/src/constants/appRoutes.ts`, `FrontEnd/src/router/index.ts` e `FrontEnd/src/router/index.spec.ts`
- [ ] T072 [P] Criar testes estruturais de paridade `pt.json`/`en.json`, ausência de texto hardcoded em componentes competitivos e nomes acessíveis localizados em `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T073 Validar todos os paths, headers, DTOs, paginação, schemas e respostas implementados contra `specs/023-fundacao-competitiva-sazonal/contracts/competitive-foundation.openapi.yaml` em `BackEnd/tests/RinhaDasLendas.Tests/Integration/CompetitiveFoundationOpenApiContractTests.cs`
- [ ] T074 Executar testes, build, lint, audit, verificação da migration e todos os cenários funcionais, registrando resultados e eventuais ajustes em `specs/023-fundacao-competitiva-sazonal/quickstart.md`
- [ ] T075 Validar em Chromium autenticado PT-BR/EN-US nas larguras 1280 px e 320 px, incluindo teclado, foco, movimento reduzido, controles de 44 px, cards mobile, fluxo em cinco minutos e seleção sazonal em até três ações, registrando evidências em `specs/023-fundacao-competitiva-sazonal/quickstart.md`

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: Sem dependências.
- **Foundational**: Depende da conclusão de Setup e bloqueia todas as user stories.
- **US1 (P1)**: Depende de Foundational.
- **US2 (P1)**: Depende de US1 para Season ativa, competição, rodada e regras publicadas.
- **US3 (P1)**: Depende de US1 e reutiliza o agregado Série estabilizado por US2.
- **US4 (P2)**: Depende da conclusão de todas as histórias P1, especialmente US3 e as regras de Série.
- **US5 (P2)**: Depende das mutações auditáveis de US1-US4.
- **Polish**: Depende de todas as histórias incluídas na entrega.

### User Story Dependencies

- **US1**: Base sazonal e primeiro incremento implantável.
- **US2**: Usa Season ativa, circuito diário, rodada e regra publicada de US1.
- **US3**: Usa a fundação sazonal e operacional de US1-US2, mas mantém cenários próprios de confronto oficial e amistoso.
- **US4**: Só inicia após P1; associa Eventos às Séries sem alterar os fluxos P1.
- **US5**: Consolida consulta e apresentação da auditoria produzida pelas histórias anteriores.

### Within Each User Story

- Testes devem ser escritos e falhar antes da implementação correspondente.
- Regras e agregados precedem handlers.
- Validators e handlers precedem controllers.
- DTOs e contratos precedem integração frontend.
- Backend permanece fonte de verdade para autorização e regras competitivas.
- Cada checkpoint exige execução dos testes da história antes de avançar.

## Parallel Opportunities

- T001-T004 podem executar em paralelo.
- T005-T008 podem executar em paralelo antes da implementação foundational.
- T009-T014 podem ser distribuídas por arquivos distintos.
- Os testes marcados `[P]` em cada história podem ser escritos em paralelo.
- Tipos e serviços frontend marcados `[P]` podem avançar após estabilização dos DTOs.
- US4 não deve ser antecipada enquanto US1-US3 P1 não estiverem validadas.

## Parallel Examples

```text
US1: T020, T021, T022, T023 e T024
US2: T033, T034, T035, T036, T037 e T038
US3: T049, T050 e T051
US4: T058, T059 e T060
US5: T065, T066 e T067
```

## Implementation Strategy

### MVP First

1. Completar Setup.
2. Completar Foundational.
3. Completar US1.
4. Executar os testes de US1 e validar Season → competição → rodada → regra.
5. Entregar US1 como primeiro incremento operacional.

### Incremental Delivery

1. Setup + Foundational estabelecem persistência, segurança, concorrência e i18n.
2. US1 entrega calendário, governança e seleção sazonal.
3. US2 entrega o fluxo diário MD3/MD5 com Fearless.
4. US3 completa os fluxos P1 com confrontos oficiais e amistosos.
5. US4 adiciona Evento de quatro Times somente após P1 estar estável.
6. US5 disponibiliza a trilha consolidada para resolução de disputas.
7. Polish valida contratos, migration, segurança, i18n, responsividade e acessibilidade.

### TDD Strategy

1. Escrever os testes da fase.
2. Executá-los e confirmar falha pelo comportamento ausente.
3. Implementar a menor alteração que satisfaça os testes.
4. Executar testes de domínio, aplicação, integração, segurança e frontend.
5. Refatorar sem alterar contratos.
6. Executar regressão completa antes do checkpoint.

## Requirements Traceability

- **FR-001 a FR-008**: T020, T022, T025-T026, T028-T030 e T032.
- **FR-009 a FR-018**: T033, T036, T039, T041-T046.
- **FR-019 a FR-024**: T034-T035, T040-T046 e T048.
- **FR-025 a FR-027**: T058-T064.
- **FR-028 a FR-034**: T049-T057.
- **FR-035 a FR-041**: T005-T006, T009, T016-T017, T022, T035-T037, T043, T065-T070.
- **FR-042 a FR-044**: T008, T018, T024, T031-T032, T038, T047-T048, T051, T056-T057, T060, T064, T067, T070-T075.
- **FR-045 a FR-046**: T021, T023, T027-T030.
- **FR-047 a FR-049**: T033, T036, T039, T041-T046.
- **SC-001**: T022-T023 e T028-T032.
- **SC-002**: T024, T032, T038, T048 e T075.
- **SC-003**: T034, T037, T040-T046.
- **SC-004**: T058-T064.
- **SC-005**: T049-T057.
- **SC-006**: T035, T043, T065-T070.
- **SC-007**: T006, T022, T026 e T029.
- **SC-008**: T024, T032 e T075.
- **SC-009**: T005-T008, T020-T024, T033-T038, T049-T051, T058-T060, T065-T067 e T072-T074.

## Final Verification

- Total de tarefas: **75**
- Setup: **4**
- Foundational: **15**
- US1: **13**
- US2: **16**
- US3: **9**
- US4: **7**
- US5: **6**
- Polish: **5**
- Escopo MVP sugerido: **Setup + Foundational + US1**
- Todas as tarefas seguem o formato obrigatório com checkbox, ID, labels e caminhos.
- Todas as regras críticas possuem testes anteriores à implementação.
- Todas as histórias P1 precedem o Evento P2.

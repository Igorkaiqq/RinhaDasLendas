---

description: "Tarefas de implementação do arquivamento administrativo de drafts"
---

# Tasks: Arquivamento Administrativo de Drafts

**Input**: Design documents from `/specs/022-arquivar-drafts/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Obrigatórios. Toda tarefa de produção depende da tarefa RED correspondente; executar o teste, confirmar que falha pela ausência do comportamento e somente então implementar.

**Organization**: Tarefas agrupadas por jornada, com infraestrutura compartilhada mínima antes das histórias.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode executar em paralelo porque altera arquivos diferentes e não depende de tarefa incompleta.
- **[Story]**: Jornada da especificação atendida.
- Todos os caminhos são relativos à raiz do repositório.

## Phase 1: Setup e Baseline

**Purpose**: Registrar o estado anterior e preparar evidências sem alterar comportamento.

- [ ] T001 Criar `specs/022-arquivar-drafts/verification-report.md` com branch, commits de specify/plan/tasks, versões de .NET/Node, comandos baseline e seções para RED/GREEN, migration, browser e auditoria i18n
- [ ] T002 [P] Executar a suíte backend baseline conforme `specs/022-arquivar-drafts/quickstart.md` e registrar totais e ambiente em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T003 Executar testes/build baseline de `FrontEnd/` e `discord-bot/` após T002 e registrar totais, warnings e auditoria npm em `specs/022-arquivar-drafts/verification-report.md`

---

## Phase 2: Fundação Compartilhada

**Purpose**: Estabelecer modelo, contratos, autorização e schema exigidos por todas as jornadas.

**CRITICAL**: Nenhuma história começa antes deste checkpoint.

### RED: contratos fundamentais

- [ ] T004 [P] Escrever e executar testes de domínio inicialmente falhos para metadados, `Arquivado`, motivo 1/500/501, tipos de ação e `Cancelamento` em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T005 [P] Escrever e executar testes inicialmente falhos para requests, `versaoEstado`, projeções públicas/Admin+, códigos MV101-MV104/MSIS029-MSIS030 e paridade dos resources base/pt-BR/en-US em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemValidatorTests.cs`, `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemProjectionContractTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Messages/ResourceMessageProviderTests.cs`
- [ ] T006 [P] Escrever e executar testes inicialmente falhos da policy `CanArchiveDrafts` e da permissão retornada somente a Admin/SuperAdmin em `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- [ ] T007 [P] Criar testes PostgreSQL inicialmente falhos para os três campos todos nulos ou todos preenchidos, motivo trim de 1-500, rejeição de 501, FK `Restrict`, predicados exatos dos índices parciais, ausência de backfill arquivado, cascades inalterados, upgrade, banco vazio e `Down` em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingMigrationTests.cs`
- [ ] T008 [P] Escrever e executar testes inicialmente falhos dos tipos `arquivado`, `versaoEstado`, `Cancelamento`, requests e parâmetro `includeArchived` em `FrontEnd/src/services/draftMontagens.spec.ts` e `discord-bot/src/shared/api/rinhaApi.spec.ts`

### GREEN: modelo e contratos fundamentais

- [ ] T009 Implementar metadados, propriedade derivada, `Arquivamento`, `Restauracao`, `CancelamentoPorArquivamento` e timestamp determinístico em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` e `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemAcaoAdministrativa.cs` até T004 passar
- [ ] T010 [P] Adicionar `Cancelamento` em `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemPublicacaoDiscordTipo.cs`, `FrontEnd/src/types/draftMontagem.ts` e `discord-bot/src/shared/api/types.ts` até a parte de tipos de T004/T008 passar
- [ ] T011 Implementar DTOs de requests/resultado/arquivamento e expor `arquivado`/`versaoEstado` em `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemArquivamentoDtos.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemResponseDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemResumoDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs` e `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemDiscordOperationalDto.cs` até T005 passar
- [ ] T012 [P] Implementar validators e códigos MV101-MV104/MSIS029-MSIS030 em `BackEnd/src/RinhaDasLendas.Application/Validators/ArquivarDraftMontagemValidator.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/RestaurarDraftMontagemValidator.cs`, `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx` e `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx` até T005 passar
- [ ] T013 Implementar `CanArchiveDrafts` somente para Admin/SuperAdmin nos ambientes normal/Testing e em `/api/v1/auth/me/permissions` alterando `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthPermissions.cs`, `BackEnd/src/RinhaDasLendas.Api/Program.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Identity/AuthService.cs` até T006 passar
- [ ] T014 Alterar somente as assinaturas de `IDraftMontagemRepository` para `includeArchived` e carregamentos/reloads internos, sem implementar filtros, e atualizar o fake concreto em `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- [ ] T015 Implementar os três campos sem backfill, constraint todos-nulos/todos-válidos, motivo trim 1-500, FK `Restrict`, índices parciais com predicados exatos e cascades existentes inalterados em `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`, gerar a migration em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/`, atualizar `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs` e fazer T007 passar
- [ ] T016 Implementar tipos, requests, resultado reduzido, filtro e codificação de IDs em `FrontEnd/src/types/draftMontagem.ts`, `FrontEnd/src/constants/permissions.ts` e `FrontEnd/src/services/draftMontagens.ts` até T008 passar

**Checkpoint**: Modelo, schema, contratos, resources e policy disponíveis; testes fundamentais verdes.

---

## Phase 3: User Story 1 - Arquivar qualquer draft com segurança (Priority: P1) MVP

**Goal**: Admin+ arquiva qualquer estado; estados ativos cancelam atomicamente, criam auditoria e intenção Discord sem remover histórico.

**Independent Test**: Arquivar os sete estados, forçar falha antes do commit e indisponibilidade Discord; observar estado coerente, dados preservados e publicação pendente somente nos cinco estados ativos.

### RED: User Story 1

- [ ] T017 [P] [US1] Escrever e executar testes inicialmente falhos dos sete estados, atomicidade, idempotência, primeiro motivo/autor, uso do mesmo motivo normalizado em `CancelamentoPorArquivamento` e `Arquivamento` e preservação de coleções em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T018 [P] [US1] Criar e executar testes inicialmente falhos do command/handler e endpoint de arquivar, uma persistência, versão observada, conflito/reload, notifier somente após commit, 401/403 e equivalência `ME035` entre arquivado inacessível e inexistente em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemArchivingHandlerTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- [ ] T019 [P] [US1] Criar e executar testes PostgreSQL inicialmente falhos de persistência atômica, rollback completo, dois arquivamentos concorrentes, corrida com avanço operacional e handlers backend de claim/conclusão/falha para `Cancelamento` arquivado em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemPublicationRealtimeTests.cs`
- [ ] T020 [P] [US1] Escrever e executar testes inicialmente falhos do evento ID-only e ausência de broadcast completo após arquivar em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeNotificationPublisherTests.cs`
- [ ] T021 [P] [US1] Escrever e executar testes inicialmente falhos de candidato/embed localizado, canal, claim, revalidação pré-envio, invalidação entre claim e envio, janela residual de envio já iniciado, recusa de conclusão obsoleta, cancelamento compensatório prioritário, falha pré-envio e resultado incerto em `discord-bot/src/modules/drafts/draftInteractions.spec.ts` e `discord-bot/src/discord/embeds/draftEmbeds.spec.ts`
- [ ] T022 [P] [US1] Escrever e executar testes inicialmente falhos do dialog de arquivamento ativo/terminal, trim, 500/501, foco e ação destrutiva em `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts`
- [ ] T023 [P] [US1] Escrever e executar testes inicialmente falhos da chamada de arquivo, bloqueio duplicado, seleção próximo/anterior/vazio e erros 401/409 em `FrontEnd/src/views/DraftsView.spec.ts`

### GREEN: User Story 1

- [ ] T024 [US1] Implementar `DraftMontagem.Arquivar` como transição indivisível com cancelamento ativo, limpeza operacional, mesmo motivo normalizado nas duas ações e publicação `Cancelamento/Pendente` em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` até T017 passar
- [ ] T025 [US1] Implementar command e handler com validação, autoria do `ICurrentUser`, versão, `TrySaveChangesAsync`, convergência após reload e `MV103` em `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/ArquivarDraftMontagemCommand.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ArquivarDraftMontagemCommandHandler.cs` até T018 passar
- [ ] T026 [US1] Implementar classificação de conflito da publicação, tracker/reload incluindo arquivados e persistência atômica em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemSaveConflictClassifier.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs` até T019 passar
- [ ] T027 [US1] Adicionar `PATCH /api/v1/draft-montagens/{id}/arquivar` protegido por `CanArchiveDrafts` e respostas 200/400/401/403/404/409 em `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs` até T018/T019 passarem
- [ ] T028 [US1] Implementar `DraftMontagemArchived` com payload apenas ID em `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs` e `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs` até T020 passar
- [ ] T029 [US1] Implementar somente o carregamento interno e o fluxo claim/conclusão/falha do novo tipo `Cancelamento`, sem alterar ainda os tipos operacionais legados, e suprimir notifier completo no arquivado em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs` até T019/T020 passarem
- [ ] T030 [US1] Implementar candidato, revalidação pré-envio, embed e mensagens PT/EN de cancelamento em `discord-bot/src/modules/drafts/draftInteractions.ts`, `discord-bot/src/discord/embeds/draftEmbeds.ts`, `discord-bot/src/shared/messages/pt-BR.ts` e `discord-bot/src/shared/messages/en-US.ts` até T021 passar
- [ ] T031 [US1] Implementar ações `archiveDraft`/`restoreDraft`, motivo obrigatório e foco no dialog em `FrontEnd/src/components/drafts/DraftReasonDialog.vue` até T022 passar
- [ ] T032 [US1] Implementar orquestração Admin+ de arquivar, motivo/versão, limpeza imediata, seleção reconciliada e tratamento 401/409 em `FrontEnd/src/views/DraftsView.vue` até T023 passar

**Checkpoint**: Arquivamento funciona end-to-end nos sete estados, sem depender do Discord e sem exclusão física.

---

## Phase 4: User Story 2 - Manter arquivados fora da operação normal (Priority: P1)

**Goal**: Arquivados desaparecem de listas, detalhes, timers, elegibilidade, realtime e comandos normais; somente cancelamento Discord pendente permanece operacional.

**Independent Test**: Usuário comum pesquisa, filtra, acessa link antigo e tenta mutações após arquivamento sem localizar nem operar o draft; bot ainda conclui exclusivamente o cancelamento.

### RED: User Story 2

- [ ] T033 [P] [US2] Escrever e executar testes PostgreSQL inicialmente falhos de lista/count/detalhe/timers/elegibilidade/comandos ocultando arquivados, `includeArchived=true` retornando 403 sem Admin+ e polling incluindo somente cancelamento em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- [ ] T034 [US2] Após T033, escrever e executar testes inicialmente falhos que rejeitam claim/conclusão/falha de presença/chamada/times em arquivado e reconciliam cancelamento expirado em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPublicationReconciliationServiceTests.cs`
- [ ] T035 [P] [US2] Escrever e executar testes inicialmente falhos do filtro/duplo badge/empty state em `FrontEnd/src/components/drafts/DraftNavigator.spec.ts` e badge do header em `FrontEnd/src/components/drafts/DraftWorkspaceHeader.spec.ts`
- [ ] T036 [P] [US2] Escrever e executar testes inicialmente falhos de registro/entrega do evento realtime ID-only, `includeArchived=false`, remoção da memória e desmarcação do filtro com seleção arquivada em `FrontEnd/src/services/draftMontagemRealtime.spec.ts` e `FrontEnd/src/views/DraftsView.spec.ts`

### GREEN: User Story 2

- [ ] T037 [US2] Aplicar `ArquivadoEm == null` em `GetByIdAsync`, reload normal, listas/count, timers e elegibilidade; preservar métodos internos explícitos em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs` até T033 passar
- [ ] T038 [US2] Depois do RED T034, restringir `ListActiveForDiscordAsync` e SQLs de claim/conclusão/falha/reconciliação ao tipo/estado permitido e invalidar claims operacionais antigos em `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs` até T033/T034 passarem
- [ ] T039 [US2] Implementar autorização condicional com `IAuthorizationService` para `includeArchived=true`, mantendo listagem normal autenticada, em `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs` até T033 passar
- [ ] T040 [P] [US2] Implementar checkbox Admin+, badge neutro separado e vazios de arquivados em `FrontEnd/src/components/drafts/DraftNavigator.vue` e `FrontEnd/src/components/drafts/DraftWorkspaceHeader.vue` até T035 passar
- [ ] T041 [US2] Implementar listagem padrão explícita, filtro, perda de permissão, registro/entrega do evento ID-only e reconciliação da seleção em `FrontEnd/src/views/DraftsView.vue` e `FrontEnd/src/services/draftMontagemRealtime.ts` até T036 passar

**Checkpoint**: Arquivados ficam invisíveis e inoperantes para o fluxo normal em API, realtime, interface e bot.

---

## Phase 5: User Story 3 - Consultar e restaurar drafts arquivados (Priority: P2)

**Goal**: Admin+ encontra arquivados, consulta detalhe autorizado e restaura visibilidade sem retomar estado ativo.

**Independent Test**: Admin ativa filtro, abre item arquivado, restaura e confirma retorno com status/histórico preservados e sem reload completo.

### RED: User Story 3

- [ ] T042 [P] [US3] Escrever e executar testes inicialmente falhos de restauração idempotente, limpeza dos metadados atuais, status cancelado preservado e ações imutáveis em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- [ ] T043 [P] [US3] Escrever e executar testes inicialmente falhos do handler/endpoint de restauração, reload, concorrência restore/restore, conflito archive/restore e equivalência `ME035` para restaurar arquivado inacessível/inexistente em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemArchivingHandlerTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs`
- [ ] T044 [US3] Após T043, escrever e executar testes inicialmente falhos do detalhe `/arquivamento`, metadados atuais, histórico e equivalência `ME035` para arquivado inacessível/inexistente em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemProjectionContractTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs`
- [ ] T045 [P] [US3] Escrever e executar testes inicialmente falhos de filtro Admin+, seleção de arquivado, confirmação sem textarea, restauração mantendo status/badges/foco em `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts` e `FrontEnd/src/views/DraftsView.spec.ts`

### GREEN: User Story 3

- [ ] T046 [US3] Implementar `DraftMontagem.Restaurar` limpando metadados atuais, preservando status/histórico e sem recriar operação em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` até T042 passar
- [ ] T047 [US3] Implementar command/handler de restaurar com versão, convergência e conflito em `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/RestaurarDraftMontagemCommand.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RestaurarDraftMontagemCommandHandler.cs` até T043 passar
- [ ] T048 [US3] Implementar query/handler Admin+ de detalhe e histórico em `BackEnd/src/RinhaDasLendas.Application/Queries/DraftMontagens/GetDraftMontagemArquivamentoQuery.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/GetDraftMontagemArquivamentoQueryHandler.cs` até T044 passar
- [ ] T049 [US3] Adicionar `PATCH /{id}/restaurar` e `GET /{id}/arquivamento` com contratos/status documentados em `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs` até T043/T044 passarem
- [ ] T050 [US3] Implementar carregamento de detalhe arquivado, confirmação de restauração, recarga sem reload completo, status preservado e foco em `FrontEnd/src/services/draftMontagens.ts`, `FrontEnd/src/components/drafts/DraftReasonDialog.vue` e `FrontEnd/src/views/DraftsView.vue` até T045 passar

**Checkpoint**: Arquivamento é reversível, auditável e nunca retoma um draft anteriormente ativo.

---

## Phase 6: User Story 4 - Restringir e auditar ações administrativas (Priority: P2)

**Goal**: Garantir matriz de papéis, não vazamento do histórico e republicação contextual segura.

**Independent Test**: Executar arquivar/restaurar/incluir/consultar/republicar com cada papel e comprovar 401/403/200, autoria do principal e projeções sem vazamento.

### RED: User Story 4

- [ ] T051 [P] [US4] Escrever e executar matriz JWT inicialmente falha para anônimo, Jogador, Capitão, Moderador, Admin, SuperAdmin e bot, incluindo equivalência de envelope/código `ME035` entre draft arquivado inacessível e inexistente em detalhe normal, detalhe de arquivo, mutações e republicação, em `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- [ ] T052 [P] [US4] Escrever e executar testes inicialmente falhos de autoria não controlável pelo body, ocultação dos três tipos administrativos da projeção Moderador e não enumeração por contagens/realtime em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs`
- [ ] T053 [US4] Após T052, escrever e executar testes inicialmente falhos de republicação `Cancelamento` somente Admin+, mantendo republicações normais para Moderador, em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemArchivingIntegrationTests.cs`
- [ ] T054 [P] [US4] Escrever e executar testes inicialmente falhos de controles Admin/SuperAdmin versus Moderador/Jogador, tratamento 403 sem perder `canManageDrafts` e painel permitindo somente `Cancelamento` em `Falha`/`RequerReconciliacao` para arquivado em `FrontEnd/src/views/DraftsView.spec.ts` e `FrontEnd/src/components/drafts/DraftDiscordPublicationPanel.spec.ts`

### GREEN: User Story 4

- [ ] T055 [US4] Filtrar ações de arquivamento da projeção Moderador e incluir histórico completo somente na projeção Admin+ em `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs` e `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemArquivamentoDtos.cs` até T052 passar
- [ ] T056 [US4] Adicionar command/handler e endpoint Admin+ separado para republicar `Cancelamento` arquivado em `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/RepublicarCancelamentoDraftArquivadoCommand.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarCancelamentoDraftArquivadoCommandHandler.cs` e `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs` até T053 passar
- [ ] T057 [US4] Completar atributos, autorização condicional, metadata OpenAPI e matriz 401/403/404/409 em `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs` e `BackEnd/src/RinhaDasLendas.Api/Program.cs` até T051 passar
- [ ] T058 [US4] Implementar capacidade independente Admin+, ocultação por 403, regras do painel e republicação administrativa separada em `FrontEnd/src/views/DraftsView.vue`, `FrontEnd/src/services/draftMontagens.ts` e `FrontEnd/src/components/drafts/DraftDiscordPublicationPanel.vue` até T054 passar

**Checkpoint**: Somente Admin/SuperAdmin operam ou consultam arquivamento; Moderador mantém apenas suas capacidades preexistentes.

---

## Phase 7: Internacionalização, Atualizações e Gates Finais

**Purpose**: Fechar textos, documentação editorial, qualidade e evidências antes de considerar a implementação pronta.

- [ ] T059 [P] Escrever e executar testes inicialmente falhos de todas as chaves de filtro, badges, dialogs, validações, toasts, erros 401/403/409 e publicação de cancelamento em `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T060 Implementar conteúdo equivalente e acentuação revisada em `FrontEnd/src/i18n/locales/pt.json`, `FrontEnd/src/i18n/locales/en.json`, `discord-bot/src/shared/messages/pt-BR.ts` e `discord-bot/src/shared/messages/en-US.ts` até T059 e testes do bot passarem
- [ ] T061 [P] Escrever e executar testes inicialmente falhos da próxima release, ordem e destaque único em `FrontEnd/src/constants/systemUpdates.spec.ts`, `FrontEnd/src/services/systemUpdates.spec.ts` e `FrontEnd/src/views/SystemUpdatesView.spec.ts`
- [ ] T062 Adicionar a próxima versão disponível sobre arquivar/restaurar drafts em `FrontEnd/src/constants/systemUpdates.ts`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json` somente após os gates locais, até T061 passar
- [ ] T063 Executar `dotnet test`/`dotnet build` completos no devcontainer e testes de migration; registrar totais e zero falhas em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T064 Executar testes/build completos do bot após T063 e registrar totais, comportamento da janela Discord em voo e zero falhas em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T065 Executar testes/build/lint/audit completos do frontend após T064 e registrar totais e zero falhas em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T066 Validar com `agent-browser` Admin, Moderador, viewports 1440/768/320, foco, teclado, overflow, PT/EN, console e jornadas de arquivo/restauração; anexar evidências em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T067 Auditar textos hardcoded, resources backend, sincronização PT/EN, mensagens do bot, acentuação, placeholders, botões, títulos, badges, dialogs, toasts, vazios e validações; marcar todos como conformes em `specs/022-arquivar-drafts/verification-report.md`
- [ ] T068 Executar `git diff --check`, revisar migration sem exclusão/cascade, confirmar FR-001 a FR-028 e SC-001 a SC-010 no `specs/022-arquivar-drafts/verification-report.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: sem dependências.
- **Phase 2**: depende do baseline; bloqueia todas as histórias.
- **US1**: depende da fundação e entrega o MVP de arquivamento seguro.
- **US2**: depende do estado arquivado produzido pela US1.
- **US3**: depende de US1 e dos carregamentos/filtros internos de US2.
- **US4**: depende dos endpoints de US1/US3 e fecha autorização/projeções.
- **Phase 7**: depende de todas as histórias e não publica Atualizações antes dos gates locais.

### User Story Dependencies

```text
Foundation
   └── US1 Arquivar
         └── US2 Ocultar
               └── US3 Restaurar
                     └── US4 Autorizar/Auditar
                           └── Gates finais
```

### Within Each User Story

- Executar todas as tarefas RED da história e confirmar falha esperada.
- Implementar somente o mínimo para GREEN.
- Rodar testes focados após cada mudança e suíte relacionada no checkpoint.
- Não corrigir teste para acomodar implementação divergente da especificação.
- Refatorar somente após GREEN e manter a suíte verde.

### Parallel Opportunities

- T002 precede T003 porque ambos atualizam o mesmo relatório.
- T004-T008 podem executar em paralelo por projeto/arquivo.
- T010 e T012 podem executar em paralelo após T009 definir o modelo.
- Em US1, T017-T023 são RED paralelos; bot, frontend e backend podem avançar em trilhas separadas após os contratos.
- Em US2, T033, T035 e T036 são RED paralelos; T034 começa somente após T033, e T040 pode avançar paralelamente aos filtros backend.
- Em US3, T042, T043 e T045 são RED paralelos; T044 começa somente após T043.
- Em US4, T051, T052 e T054 são RED paralelos; T053 começa somente após T052.
- T063, T064 e T065 executam em sequência porque atualizam o mesmo relatório.

## Parallel Examples

### User Story 1

```text
Backend: T017 → T024; T018/T019/T020 → T025-T029
Bot: T021 → T030
Frontend: T022/T023 → T031/T032
```

### User Story 2

```text
Backend: T033 → T034 → T037-T039
Frontend: T035/T036 → T040/T041
```

### User Story 3

```text
Backend: T042/T043 → T044 → T046-T049
Frontend: T045 → T050
```

### User Story 4

```text
Backend: T051/T052 → T053 → T055-T057
Frontend: T054 → T058
```

## Implementation Strategy

### MVP First

1. Completar Setup e Fundação.
2. Completar US1 em RED-GREEN-REFACTOR.
3. Validar os sete estados e atomicidade antes de avançar.
4. Não implantar o MVP isolado: ocultação e autorização completa são necessárias para produção.

### Incremental Delivery

1. Fundação: schema, contratos, resources e policy.
2. US1: transição segura e cancelamento Discord.
3. US2: invisibilidade operacional completa.
4. US3: reversibilidade.
5. US4: matriz de autorização e histórico restrito.
6. Gates, Atualizações e validação autenticada.

## Notes

- `[P]` significa arquivos/trilhas sem conflito direto naquele checkpoint.
- Commits de implementação devem usar português e agrupar ciclos lógicos verdes, nunca estados RED quebrados.
- Não usar `DELETE`, `Remove`, `ExecuteDelete` ou cascade novo para drafts.
- Não enviar motivo de arquivamento no Discord nem em realtime público.
- A janela de mensagem Discord já em voo é uma limitação externa aceita; revalidação e cancelamento compensatório são obrigatórios.
- `docs/prompts/` e `specs/018-importacao-partidas-lcu/` são mudanças preexistentes e não pertencem à feature.

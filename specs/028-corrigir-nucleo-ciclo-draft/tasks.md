# Tasks: Corrigir Núcleo do Ciclo de Draft

**Input**: `spec.md`, `plan.md` e `docs/superpowers/plans/2026-07-29-corrigir-nucleo-ciclo-draft.md`

**Tests**: TDD obrigatório em domínio, validators, handlers, integração, segurança, migration e frontend.

## Phase 1: Setup

**Purpose**: Alinhar contexto e contratos compartilhados antes das regras.

- [ ] T001 Atualizar o contexto gerenciado da feature 028 em `AGENTS.md` e `.specify/feature.json`
- [ ] T002 [P] Adicionar códigos do ciclo em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs` e traduções em `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx`
- [ ] T003 [P] Adicionar os códigos equivalentes em `FrontEnd/src/constants/messageCode.ts` e `FrontEnd/src/services/messageService.ts`

---

## Phase 2: Foundational

**Purpose**: Versionamento persistido e contratos que bloqueiam todas as histórias.

- [ ] T004 Escrever testes RED de migration e compatibilidade em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleMigrationTests.cs`
- [ ] T005 Criar `DraftMontagemCicloVersao` em `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemCicloVersao.cs` e tornar `Modo` anulável em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T006 Mapear `ciclo_versao` e `modo` anulável em `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- [ ] T007 Gerar e revisar a migration `CorrigirNucleoCicloDraft` em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/` com backfill legado sem alterar estado, times, participantes ou histórico
- [ ] T008 Executar GREEN de migration e atualizar o snapshot em `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs`
- [ ] T009 [P] Atualizar `Modo`, `CicloVersao` e payload de substituição em `BackEnd/src/RinhaDasLendas.Application/Dtos/` e `FrontEnd/src/types/draftMontagem.ts`
- [ ] T010 [P] Criar `CanManageDraftCycle` para Admin+ e `CanCreateDraftPresenceOrManageCycle` para Admin+/bot em `BackEnd/src/RinhaDasLendas.Domain/Constants/AuthPermissions.cs` e `BackEnd/src/RinhaDasLendas.Api/Program.cs`

**Checkpoint**: Dados existentes classificados como legado; contratos compilam com modo anulável.

---

## Phase 3: User Story 1 - Escolher o modo depois da presença

**Goal**: Draft de presença aguarda modo; criação direta continua Manual.

**Independent Test**: Fechar presença v2, escolher cada modo e criar montagem direta sem capitães.

- [ ] T011 [US1] Escrever testes RED de fábricas e seleção idempotente em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- [ ] T012 [US1] Implementar `CriarPorPresenca`, `CriarManualDireto` e `SelecionarModo` em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T013 [US1] Migrar criação web e agendada em `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CreateDraftMontagemCommandHandler.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- [ ] T014 [P] [US1] Criar DTO, validator e command em `BackEnd/src/RinhaDasLendas.Application/Dtos/SelecionarModoDraftMontagemRequestDto.cs`, `BackEnd/src/RinhaDasLendas.Application/Validators/SelecionarModoDraftMontagemValidator.cs` e `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/SelecionarModoDraftMontagemCommand.cs`
- [ ] T015 [US1] Escrever RED e implementar `SelecionarModoDraftMontagemCommandHandler` em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SelecionarModoDraftMontagemCommandHandler.cs`
- [ ] T016 [US1] Expor `PATCH /api/v1/draft-montagens/{id}/modo` com `CanManageDraftCycle` em `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- [ ] T017 [P] [US1] Escrever RED do serviço de modo e criação direta em `FrontEnd/src/services/draftMontagens.spec.ts`
- [ ] T018 [US1] Implementar `chooseDraftMontagemMode` e normalizar criação direta em `FrontEnd/src/services/draftMontagens.ts`
- [ ] T019 [P] [US1] Escrever RED dos controles de modo em `FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts` e `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T020 [US1] Implementar escolha Admin+ de modo em `FrontEnd/src/components/drafts/DraftPreparationPanel.vue` e `FrontEnd/src/views/DraftsView.vue`
- [ ] T021 [P] [US1] Remover configuração de capitães da criação direta em `FrontEnd/src/components/drafts/visual/DraftVisualSetup.vue` e `FrontEnd/src/components/drafts/visual/DraftVisualSetup.spec.ts`
- [ ] T022 [US1] Adicionar textos PT/EN de modo e criação direta em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

**Checkpoint**: US1 funciona sem capitães e sem reinterpretar drafts legados.

---

## Phase 4: User Story 2 - Montar times manualmente sem capitães

**Goal**: Manual aceita board sem capitães e só finaliza completo.

**Independent Test**: Distribuir todos os titulares sem capitães, rejeitar incompleto e finalizar completo.

- [ ] T023 [US2] Escrever RED de layout/finalização manual em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- [ ] T024 [US2] Permitir layout manual sem capitães e exigir completude na finalização em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- [ ] T025 [US2] Ajustar validators e handlers de layout/finalização em `BackEnd/src/RinhaDasLendas.Application/Validators/SalvarLayoutDraftMontagemValidator.cs`, `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SalvarLayoutDraftMontagemCommandHandler.cs` e `FinalizarDraftMontagemCommandHandler.cs`
- [ ] T026 [P] [US2] Escrever RED do board manual em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- [ ] T027 [US2] Remover capitães, ordem e início realtime do board manual em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [ ] T028 [P] [US2] Implementar rail mode-aware manual em `FrontEnd/src/components/drafts/DraftStateRail.vue`, `DraftStateRail.spec.ts`, `DraftWorkspaceHeader.vue` e `DraftWorkspaceHeader.spec.ts`
- [ ] T029 [US2] Adicionar mensagens localizadas de layout incompleto em resources backend e `FrontEnd/src/i18n/locales/pt.json`/`en.json`

**Checkpoint**: US2 finaliza somente times completos e sem capitães obrigatórios.

---

## Phase 5: User Story 3 - Definir capitães específicos do draft

**Goal**: Cargo global habilita seleção; autoridade continua específica por draft.

**Independent Test**: Aceitar somente titular ativo com role, rejeitar reserva e manter capitão global não designado como jogador comum.

- [ ] T030 [US3] Escrever RED de cargo, atividade, vínculo e recorte titular em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs` e `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`
- [ ] T031 [US3] Adicionar `GetCapitaesElegiveisIdsAsync` em `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs` e `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- [ ] T032 [US3] Aplicar elegibilidade e designação diária em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DefinirCapitaesDraftMontagemCommandHandler.cs`
- [ ] T033 [US3] Projetar `CapitaesElegiveisIds` no contrato admin em `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs` e `GetDraftMontagemAdminQueryHandler.cs`
- [ ] T034 [P] [US3] Escrever RED de titulares/reservas/elegibilidade em `FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts` e `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T035 [US3] Restringir seleção de capitães no painel e view em `FrontEnd/src/components/drafts/DraftPreparationPanel.vue` e `FrontEnd/src/views/DraftsView.vue`
- [ ] T036 [US3] Adicionar textos PT/EN de elegibilidade, titular e reserva em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

**Checkpoint**: US3 distingue cargo global de autoridade diária.

---

## Phase 6: User Story 4 - Iniciar e concluir realtime com invariantes

**Goal**: OrdemDefinida real, início explícito, timeout com nova rodada, substituição explícita e guardas terminais.

**Independent Test**: Executar jornada realtime completa, substituir capitão da vez e bloquear mutações terminais.

- [ ] T037 [US4] Escrever RED de `OrdemDefinida`, início único e timeout em nova rodada em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- [ ] T038 [US4] Implementar ordem e início v2 preservando legado em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` e handlers `DefinirOrdemEscolhaDraftMontagemCommandHandler.cs`/`IniciarDraftMontagemTempoRealCommandHandler.cs`
- [ ] T039 [US4] Revalidar capitão no pick e no estado realtime em `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPickDraftMontagemCommandHandler.cs` e `DraftMontagemRealtimeStateFactory.cs`
- [ ] T040 [US4] Escrever RED de substituição explícita e terminalidade em `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- [ ] T041 [US4] Implementar `NovoCapitaoId`, troca atômica de turno e guardas terminais em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`, `SubstituirReservaDraftMontagemRequestDto.cs`, validator e handler correspondentes
- [ ] T042 [P] [US4] Escrever RED do diálogo em `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.spec.ts`
- [ ] T043 [US4] Criar `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.vue` com reserva e novo capitão explícitos
- [ ] T044 [US4] Integrar início de `OrdemDefinida`, substituição e guardas terminais em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue` e `FrontEnd/src/views/DraftsView.vue`
- [ ] T045 [P] [US4] Atualizar testes do board/view em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts` e `FrontEnd/src/views/DraftsView.spec.ts`
- [ ] T046 [US4] Adicionar textos PT/EN de início, substituição e terminalidade em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

**Checkpoint**: US4 conclui realtime completo e não permite mutação terminal.

---

## Phase 7: User Story 5 - Preservar drafts ativos anteriores

**Goal**: Dados e transições v1 permanecem funcionais até estado terminal.

**Independent Test**: Migrar e concluir drafts v1 em estados ativos e terminais sem redefinir modo/capitães/ordem.

- [ ] T047 [US5] Escrever compatibilidade v1 em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemLegacyCompatibilityIntegrationTests.cs`
- [ ] T048 [US5] Ajustar ramificações legadas no agregado e handlers em `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs` e `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/`
- [ ] T049 [P] [US5] Escrever regressão de UI legada em `FrontEnd/src/views/DraftsView.spec.ts` e `FrontEnd/src/components/drafts/DraftStateRail.spec.ts`
- [ ] T050 [US5] Preservar apresentação de drafts legados em `FrontEnd/src/views/DraftsView.vue` e `FrontEnd/src/components/drafts/DraftStateRail.vue`

**Checkpoint**: US5 comprova compatibilidade persistida e visual.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T051 [P] Criar matriz de autorização Admin/SuperAdmin/Moderador/Jogador/Bot em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleAuthorizationIntegrationTests.cs`
- [ ] T052 Criar jornadas manual e realtime em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- [ ] T053 [P] Cobrir concorrência de modo/início/pick/timeout/substituição em `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- [ ] T054 Atualizar contratos e regressões existentes em `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemProjectionContractTests.cs`, `Integration/EndpointCoverageIntegrationTests.cs` e `Security/SecurityHardeningTests.cs`
- [ ] T055 [P] Auditar chaves, acentuação e hardcodes em `FrontEnd/src/i18n/i18n.spec.ts`, `FrontEnd/src/i18n/locales/pt.json`, `en.json` e resources backend
- [ ] T056 Executar backend completo e build via devcontainer em `BackEnd/RinhaDasLendas.sln`
- [ ] T057 Executar `lint:check`, testes e build completos em `FrontEnd/package.json`
- [ ] T058 Validar desktop/tablet/mobile e fluxo Admin+/Capitão em produção local com Chromium
- [ ] T059 Executar revisão independente do diff completo entre `origin/main...HEAD` e corrigir findings
- [ ] T060 Atualizar evidências e status em `specs/028-corrigir-nucleo-ciclo-draft/spec.md` e `tasks.md`

---

## Dependencies

```text
Setup -> Foundational -> US1
US1 -> US2
US1 -> US3
US3 -> US4
US1 + US2 + US3 + US4 -> US5
US1..US5 -> Polish -> Deploy
```

## Parallel Opportunities

- T002 e T003 podem avançar em paralelo.
- T009 e T010 podem avançar após o modelo básico.
- Testes frontend de cada história podem ser escritos enquanto o backend da mesma história entra em GREEN.
- T026/T028, T034 e T042 são independentes em arquivos distintos.
- T051, T053 e T055 podem ser preparados em paralelo antes da verificação completa.

## Implementation Strategy

1. Entregar primeiro versionamento e US1 sem publicar.
2. Completar manual e capitães antes de alterar realtime.
3. Integrar realtime/substituição somente após contratos backend estáveis.
4. Executar compatibilidade e matriz de autorização antes das suítes completas.
5. Fazer um único deploy da feature 028 depois de todas as 60 tarefas concluídas e revisadas.

## Format Validation

Todas as tarefas usam checkbox, ID sequencial, marcador `[P]` somente quando paralelizável, label `[USn]` nas fases de história e caminho explícito de arquivo.

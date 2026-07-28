---

description: "Tarefas para reabertura de presença do draft"
---

# Tasks: Reabertura de Presença do Draft

**Input**: Design documents from `/specs/024-reabrir-presenca-draft/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: A correção seguirá TDD porque a especificação exige cobertura das regras críticas e das jornadas 19/20.

**Organization**: Tarefas agrupadas por história, mantendo cada incremento verificável.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode ser executada em paralelo sem editar os mesmos arquivos.
- **[Story]**: História coberta pela tarefa.
- Todos os caminhos são relativos à raiz do repositório.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirmar o baseline isolado antes da alteração.

- [x] T001 Registrar no início da execução os baselines de 579 testes backend e 465 testes frontend descritos em specs/024-reabrir-presenca-draft/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Definir o erro localizado compartilhado pela regra e pela interface.

- [x] T002 [P] Adicionar `DraftMontagemPresenceCannotBeReopened` como `MV106` em BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs e nos três arquivos BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages*.resx
- [x] T003 [P] Adicionar `MV106` aos catálogos em FrontEnd/src/constants/messageCode.ts e FrontEnd/src/services/messageService.ts

**Checkpoint**: Código e mensagens da invariante disponíveis em PT-BR e EN-US.

---

## Phase 3: User Story 1 - Reabrir presença encerrada por engano (Priority: P1) MVP

**Goal**: Reabrir somente `PresencaEncerrada`, preservar presenças, remover o prazo e recalcular estrutura apenas no próximo fechamento.

**Independent Test**: Encerrar com 19, reabrir, manter 19, esperar além do ciclo automático, adicionar o vigésimo e encerrar como 4+0.

### Tests for User Story 1

- [x] T004 [P] [US1] Escrever testes de domínio falhando para transição, preservação, limpeza, auditoria e estados inválidos em BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs
- [x] T005 [P] [US1] Escrever testes de handler falhando para autoria, persistência, retorno, notificação e not-found em BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCommandHandlerTests.cs
- [x] T006 [P] [US1] Escrever teste de serviço frontend falhando para `PATCH /reabrir-presenca` e propagação de erro em FrontEnd/src/services/draftMontagens.spec.ts
- [x] T007 [P] [US1] Escrever testes de componentes falhando para ação secundária e confirmação sem motivo em FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts e FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts
- [x] T008 [US1] Executar os testes focados de T004-T007 nos projetos BackEnd/ e FrontEnd/ e confirmar falhas causadas somente pelas interfaces ainda ausentes

### Implementation for User Story 1

- [x] T009 [US1] Implementar `DraftMontagem.ReabrirPresenca(Guid)` com a transição e auditoria definidas em BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs
- [x] T010 [US1] Criar ReabrirPresencaDraftMontagemCommand.cs e ReabrirPresencaDraftMontagemCommandHandler.cs em BackEnd/src/RinhaDasLendas.Application/{Commands,Handlers}/DraftMontagens/
- [x] T011 [US1] Expor `PATCH {id}/reabrir-presenca` com `CanManageDrafts` em BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs
- [x] T012 [P] [US1] Implementar `reopenDraftMontagemPresence` em FrontEnd/src/services/draftMontagens.ts
- [x] T013 [US1] Adicionar `canReopenPresence`, evento `reopen-presence` e ação localizada em FrontEnd/src/components/drafts/DraftPreparationPanel.vue
- [x] T014 [US1] Adicionar ação confirmatória `reopenPresence` sem motivo em FrontEnd/src/components/drafts/DraftReasonDialog.vue
- [x] T015 [US1] Integrar capability, confirmação, mutação e feedback em FrontEnd/src/views/DraftsView.vue
- [x] T016 [US1] Executar novamente os testes focados backend/frontend e confirmar 0 falhas

**Checkpoint**: Moderador+ reabre a lista sem perda e ela permanece aberta até fechamento manual.

---

## Phase 4: User Story 2 - Prosseguir com dezenove participantes (Priority: P1)

**Goal**: Explicitar três capitães e provar que 19 jogadores permitem ordem e início sem vigésimo jogador.

**Independent Test**: 19 jogadores geram 3 times/4 reservas; 3 capitães habilitam definição, ordem e início.

### Tests for User Story 2

- [x] T017 [P] [US2] Escrever teste de domínio/integrado para 19 → 3+4 → capitães → ordem → início e 19 → reabrir → 20 → 4+0 em BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs
- [x] T018 [P] [US2] Escrever testes da contagem e habilitação exata de capitães em FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts e FrontEnd/src/views/DraftsView.spec.ts
- [x] T019 [US2] Executar T017-T018 em BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs, FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts e FrontEnd/src/views/DraftsView.spec.ts e confirmar a falha específica antes da implementação

### Implementation for User Story 2

- [x] T020 [US2] Mostrar `{selected} / {total} capitães` e manter habilitação por igualdade exata em FrontEnd/src/components/drafts/DraftPreparationPanel.vue
- [x] T021 [US2] Ajustar somente o necessário na orquestração de seleção 19/20 em FrontEnd/src/views/DraftsView.vue sem alterar a fórmula do backend
- [x] T022 [US2] Executar os testes focados em BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs e FrontEnd/src/{components/drafts/DraftPreparationPanel.spec.ts,views/DraftsView.spec.ts} e confirmar 0 falhas

**Checkpoint**: O fluxo com 19 funciona e o requisito de três capitães fica inequívoco.

---

## Phase 5: User Story 3 - Restringir e registrar a reabertura (Priority: P1)

**Goal**: Garantir 401/403/200, autoria confiável, realtime e textos equivalentes.

**Independent Test**: Anônimo e jogador são negados; Moderador+ reabre, gera auditoria e observadores recebem o novo estado.

### Tests for User Story 3

- [x] T023 [P] [US3] Adicionar cobertura 401/403/200 e endpoint declarado em BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs e BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs
- [x] T024 [P] [US3] Adicionar cobertura de autoria e notificação em BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCommandHandlerTests.cs
- [x] T025 [P] [US3] Adicionar cobertura de capability, revalidação, envio único, foco e feedback em FrontEnd/src/views/DraftsView.spec.ts e FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts
- [x] T026 [P] [US3] Adicionar teste de sincronização das novas chaves em FrontEnd/src/i18n/i18n.spec.ts

### Implementation for User Story 3

- [x] T027 [US3] Completar pares de interface em FrontEnd/src/i18n/locales/pt.json e FrontEnd/src/i18n/locales/en.json com acentuação portuguesa revisada
- [x] T028 [US3] Garantir que o handler derive autoria de `ICurrentUser` e publique `StateUpdatedAsync` em BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ReabrirPresencaDraftMontagemCommandHandler.cs
- [x] T029 [US3] Executar testes em BackEnd/tests/RinhaDasLendas.Tests/{Security,Application}/ e FrontEnd/src/{views/DraftsView.spec.ts,i18n/i18n.spec.ts} e confirmar 0 falhas

**Checkpoint**: Reabertura autorizada, auditável, sincronizada e localizada.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validar regressão, padrões e preparar lançamento.

- [x] T030 [P] Executar `npm run lint:check`, `npm test` e `npm run build` em FrontEnd/ e corrigir somente regressões da feature
- [x] T031 [P] Executar a suíte completa .NET 10 pelo app devcontainer conforme specs/024-reabrir-presenca-draft/quickstart.md
- [x] T032 Auditar textos hardcoded, resources, sincronização pt/en, acentuação, botões, títulos, feedback e validações em BackEnd/src/ e FrontEnd/src/ somente nos arquivos alterados
- [x] T033 Executar cenários desktop/mobile e autorização de specs/024-reabrir-presenca-draft/quickstart.md usando o ambiente disponível
- [x] T034 Executar revisão de código final, `git diff --check` e inspeção de status/diff na raiz `./` sem modificar alterações externas
- [x] T035 Atualizar todas as caixas concluídas em specs/024-reabrir-presenca-draft/tasks.md e registrar o commit de implementação em português
- [x] T036 Enviar `feature/024-reabrir-presenca-draft` ao origin e executar o fluxo de integração/produção existente, verificando a saúde após o lançamento

---

## Phase 7: User Story 2 Addendum - Destacar capitães selecionados (Priority: P1)

**Goal**: Tornar inequívoca a seleção de cada capitão destacando simultaneamente seu botão e sua linha.

**Independent Test**: Com dois participantes elegíveis e somente o primeiro em `captainSelection`, o primeiro botão e sua linha têm as classes selecionadas e `aria-pressed=true`; o segundo não tem as classes e mantém `aria-pressed=false`.

### Tests for User Story 2 Addendum

- [x] T037 [US2] Estender o teste de seleção acessível para exigir destaque no botão e na linha somente do capitão selecionado em FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts
- [x] T038 [US2] Executar `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts` em FrontEnd/ e confirmar falha pelas classes visuais ausentes

### Implementation for User Story 2 Addendum

- [x] T039 [US2] Aplicar `draft-preparation__captain-toggle--selected` e `draft-preparation__player--captain` a partir de `captainSelection` e estilizar os estados com tokens existentes em FrontEnd/src/components/drafts/DraftPreparationPanel.vue
- [x] T040 [US2] Executar novamente `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts` em FrontEnd/ e confirmar 0 falhas
- [x] T041 [US2] Executar `npm run lint:check`, `npm test` e `npm run build` em FrontEnd/ e auditar textos hardcoded, sincronização pt/en, acentuação e controles de interface nos arquivos alterados
- [x] T042 [US2] Validar seleção e desmarcação no Chromium em viewport desktop e mobile, incluindo contraste, foco, overflow e coerência com `aria-pressed`
- [x] T043 [US2] Atualizar o status em specs/024-reabrir-presenca-draft/spec.md e registrar o cenário visual em specs/024-reabrir-presenca-draft/quickstart.md
- [x] T044 [US2] Revisar o diff de FrontEnd/src/components/drafts/DraftPreparationPanel.vue, FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts e specs/024-reabrir-presenca-draft/, commitar em português, enviar a branch, integrar no main e executar o deploy com verificações de saúde

**Checkpoint**: Seleção e desmarcação de capitães ficam imediatamente perceptíveis em desktop, mobile e tecnologia assistiva.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: concluído antes da implementação.
- **Foundational**: bloqueia todas as histórias.
- **US1**: depende de Foundational e entrega o MVP.
- **US2**: usa a transição de US1 para a jornada 19→20, mas sua jornada 19 sem reabrir é independente.
- **US3**: usa command/endpoint de US1 e pode ter testes preparados em paralelo.
- **Polish**: depende de US1, US2 e US3.
- **US2 Addendum**: depende da seleção de capitães já entregue em US2 e deve ser concluído antes do novo deploy.

### User Story Dependencies

```text
Foundational → US1 → US2
                 └→ US3
US2 + US3 → Polish → US2 Addendum → Push/Produção
```

### Parallel Opportunities

- T002 e T003 usam projetos diferentes.
- T004-T007 preparam testes em arquivos independentes.
- T017 e T018 validam backend e frontend separadamente.
- T023-T026 cobrem segurança, aplicação, interface e i18n em paralelo.
- T030 e T031 verificam frontend e backend em paralelo.
- O addendum é sequencial porque teste, implementação, verificação visual e deploy alteram ou validam o mesmo incremento frontend.

## Parallel Examples

```text
US1: T004 domínio | T005 handler | T006 serviço frontend | T007 componentes
US3: T023 endpoints/segurança | T024 handler | T025 interface | T026 i18n
Polish: T030 frontend | T031 backend
```

## Implementation Strategy

### MVP First

1. Concluir T001-T003.
2. Executar T004-T016 em TDD.
3. Validar reabertura independente antes de clareza e hardening adicionais.

### Incremental Delivery

1. US1 entrega recuperação do encerramento acidental.
2. US2 elimina a ambiguidade e cobre a progressão com 19/20.
3. US3 fecha autorização, autoria, realtime e localização.
4. Polish valida e lança uma única correção coesa.

## Notes

- Testes devem falhar pelo motivo esperado antes da implementação.
- Não criar migration, DTO vazio, novo diálogo, store ou publicação Discord.
- Não alterar fórmula de times, reservas ou requisitos de capitães.
- Commits devem usar português e conter somente arquivos desta worktree.

---

description: "Tarefas para redesenhar e publicar o fluxo completo de Draft"
---

# Tasks: Redesenho do Fluxo de Draft

**Input**: Documentos em `/specs/021-redesenho-fluxo-draft/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contracts.md, quickstart.md

**Tests**: Obrigatórios e escritos antes da implementação de cada jornada. Cada teste novo deve falhar pelo motivo esperado antes do código de produção correspondente.

**Approval**: Especificação, design, plano, tarefas e execução autônoma foram pré-aprovados pelo usuário em 2026-07-25. A implementação pode seguir sem novo checkpoint, salvo bloqueio ou alteração de regra de negócio.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode executar em paralelo sem conflito de arquivo ou dependência incompleta
- **[Story]**: História de usuário correspondente
- Todos os caminhos são relativos à raiz do repositório

## Phase 1: Setup

**Purpose**: Preparar gates não destrutivos e registrar baseline.

- [X] T001 Adicionar script não destrutivo `lint:check` em `FrontEnd/package.json`
- [X] T002 Executar e registrar o baseline focado de 58 testes em `specs/021-redesenho-fluxo-draft/verification-report.md`

---

## Phase 2: Foundational

**Purpose**: Fechar contratos compartilhados de status e internacionalização antes das jornadas.

**CRITICAL**: Nenhuma jornada começa antes desta fase.

- [X] T003 [P] Escrever testes falhos para todos os sete filtros de status, incluindo `OrdemDefinida`, em `FrontEnd/src/constants/draftMontagemStatus.spec.ts`
- [X] T004 [P] Ampliar o scanner de textos visíveis hardcoded para `DraftsView.vue` e `components/drafts/**/*.vue` em `FrontEnd/src/i18n/i18n.spec.ts`
- [X] T005 Incluir os sete status na ordem canônica em `FrontEnd/src/constants/draftMontagemStatus.ts`
- [X] T006 Adicionar chaves compartilhadas de estado desconhecido, cancelamento, progresso, ações e acessibilidade em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

**Checkpoint**: Contratos de status e i18n prontos.

---

## Phase 3: User Story 1 - Operar o draft com hierarquia clara (Priority: P1) MVP

**Goal**: Manter contexto, progresso e ação principal consistentes em todos os estados.

**Independent Test**: Os sete estados e o fallback desconhecido apresentam contexto correto, zero ou uma ação primária e nenhuma ação de avanço em estados terminais.

### Tests for User Story 1

- [X] T007 [P] [US1] Escrever matriz falha de progresso, cancelamento, desconhecido, Discord e `aria-current` em `FrontEnd/src/components/drafts/DraftStateRail.spec.ts`
- [X] T008 [P] [US1] Escrever testes falhos do cabeçalho, métricas, slots de ação e nomes longos em `FrontEnd/src/components/drafts/DraftWorkspaceHeader.spec.ts`
- [X] T009 [US1] Escrever testes integrados falhos da hierarquia nos sete estados em `FrontEnd/src/views/DraftsView.spec.ts`

### Implementation for User Story 1

- [X] T010 [P] [US1] Implementar estados `terminal` e `unknown` e `aria-current` em `FrontEnd/src/components/layout/DraftRail.vue`
- [X] T011 [US1] Implementar mapeamento canônico, cancelamento e fallback neutro em `FrontEnd/src/components/drafts/DraftStateRail.vue`
- [X] T012 [P] [US1] Criar contexto estável e grupos de ação em `FrontEnd/src/components/drafts/DraftWorkspaceHeader.vue`
- [X] T013 [US1] Integrar o shell operacional e remover landmarks/cabeçalhos duplicados em `FrontEnd/src/views/DraftsView.vue` e `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`

**Checkpoint**: Contexto e progresso funcionam independentemente do conteúdo das etapas.

---

## Phase 4: User Story 2 - Gerenciar a lista de presença sem controles quebrados (Priority: P1)

**Goal**: Oferecer roster legível, inclusão manual agrupada e Discord subordinado.

**Independent Test**: Listas com 0, 1, 10, 14 e 30 participantes permitem buscar, incluir, remover e acompanhar publicação sem cards comprimidos ou eventos incorretos.

### Tests for User Story 2

- [X] T014 [P] [US2] Escrever testes falhos da matriz de participantes, capitães, busca e eventos em `FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts`
- [X] T015 [P] [US2] Escrever testes falhos de status, fallback, permissão, envio duplicado e republicação em `FrontEnd/src/components/drafts/DraftDiscordPublicationPanel.spec.ts`
- [X] T016 [US2] Atualizar testes integrados falhos de presença, duplicidade, permissões e recuperação para confirmar, encerrar, remover e republicar em `FrontEnd/src/views/DraftsView.spec.ts`

### Implementation for User Story 2

- [X] T017 [P] [US2] Criar roster, seleção de capitães e toolbar manual em `FrontEnd/src/components/drafts/DraftPreparationPanel.vue`
- [X] T018 [P] [US2] Criar painel subordinado de publicações em `FrontEnd/src/components/drafts/DraftDiscordPublicationPanel.vue`
- [X] T019 [US2] Integrar painéis preservando handlers, IDs, motivos e proteções concorrentes em `FrontEnd/src/views/DraftsView.vue`

**Checkpoint**: A etapa de presença é funcional e testável sem o board de escolhas.

---

## Phase 5: User Story 3 - Conduzir capitães, ordem e escolhas (Priority: P1)

**Goal**: Tornar capitães, ordem, turno, preferências e resultado explícitos sem alterar regras ou payloads.

**Independent Test**: O fluxo percorre capitães, ordem, escolhas e finalização, preservando permissões, `jogadorId`, ordem salva e leitura terminal.

### Tests for User Story 3

- [X] T020 [US3] Criar testes falhos para clone imutável, ordem de times, progresso, preferências, pick, payload, terminal, permissão e bloqueio de eventos duplicados em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- [X] T021 [US3] Escrever matriz integrada falha de permissão, envio duplicado e resultado preservado para cancelar draft, definir capitães, definir ordem, escolher e finalizar, incluindo escolha inválida e atualização ao vivo, em `FrontEnd/src/views/DraftsView.spec.ts`

### Implementation for User Story 3

- [X] T022 [US3] Reorganizar `DraftVisualBoard` com ordem e progresso explícitos, preferências visíveis e estados terminais somente leitura em `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- [X] T023 [US3] Ajustar a integração de ações de capitães, ordem, escolha e finalização sem mudar serviços em `FrontEnd/src/views/DraftsView.vue`

**Checkpoint**: Todo o ciclo operacional do Draft funciona no novo shell.

---

## Phase 6: User Story 5 - Navegar entre drafts com contexto suficiente (Priority: P2)

**Goal**: Compactar filtros e drafts com seleção, status e recuperação claros.

**Independent Test**: Usuário filtra, reconhece e troca drafts com data ausente, nome longo, status desconhecido e lista vazia.

### Tests for User Story 5

- [ ] T024 [US5] Escrever testes falhos de filtros, sete status, seleção, fallback, loading, retry e vazio em `FrontEnd/src/components/drafts/DraftNavigator.spec.ts`

### Implementation for User Story 5

- [ ] T025 [US5] Criar navegador responsivo conforme contrato em `FrontEnd/src/components/drafts/DraftNavigator.vue`
- [ ] T026 [US5] Integrar filtros, falha de listagem, retry, seleção e criação em `FrontEnd/src/views/DraftsView.vue`

**Checkpoint**: Navegação é independente e mantém o workspace intacto.

---

## Phase 7: User Story 4 - Usar todo o fluxo em qualquer viewport (Priority: P1)

**Goal**: Garantir reflow, alvos de toque, foco, rolagem e movimento reduzido.

**Independent Test**: 1440px, 1024px, 768px e 320px não apresentam overflow horizontal, sobreposição ou segunda rolagem vertical concorrente.

### Tests for User Story 4

- [ ] T027 [US4] Escrever assertions estruturais falhas para ordem de leitura, regiões e acessibilidade em `FrontEnd/src/views/DraftsView.spec.ts`, `FrontEnd/src/components/drafts/DraftNavigator.spec.ts`, `FrontEnd/src/components/drafts/DraftWorkspaceHeader.spec.ts`, `FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts`, `FrontEnd/src/components/drafts/DraftDiscordPublicationPanel.spec.ts`, `FrontEnd/src/components/drafts/DraftStateRail.spec.ts` e `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`

### Implementation for User Story 4

- [ ] T028 [US4] Substituir seletores legados por estilos escopados do shell, roster, ações, rail e board em `FrontEnd/src/styles/main.css`
- [ ] T029 [US4] Implementar breakpoints 1024px e 768px, coluna única em 320px, alvos de 44px e redução de movimento em `FrontEnd/src/styles/main.css`
- [ ] T030 [US4] Validar teclado, overflow e screenshots nos quatro viewports com `agent-browser` e registrar evidências locais em `specs/021-redesenho-fluxo-draft/verification-report.md`

**Checkpoint**: Fluxo responsivo e acessível aprovado localmente.

---

## Phase 8: User Story 6 - Consultar a correção publicada nas Atualizações (Priority: P2)

**Goal**: Publicar `2026.07.3` sem anunciar antecipadamente o redesenho.

**Independent Test**: Atualizações mostra `.3` no topo, única destacada, em PT/EN e com link para Configurações.

### Tests for User Story 6

- [ ] T031 [P] [US6] Atualizar testes falhos do registro e serviço para `.3` em `FrontEnd/src/constants/systemUpdates.spec.ts` e `FrontEnd/src/services/systemUpdates.spec.ts`
- [ ] T032 [P] [US6] Atualizar testes falhos do card, hero e locales para `.3` em `FrontEnd/src/components/updates/SystemUpdateCard.spec.ts`, `FrontEnd/src/views/SystemUpdatesView.spec.ts` e `FrontEnd/src/i18n/i18n.spec.ts`

### Implementation for User Story 6

- [ ] T033 [US6] Adicionar `2026.07.3` e retirar destaque de `.2` em `FrontEnd/src/constants/systemUpdates.ts`
- [ ] T034 [US6] Adicionar conteúdo editorial equivalente de `.3` em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`

**Checkpoint**: Correção já publicada aparece no histórico oficial.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Fechar gates, publicar o redesenho e preparar entrega.

- [ ] T035 Executar suíte focada, suíte frontend completa, `build`, `lint:check` e auditoria de dependências conforme `specs/021-redesenho-fluxo-draft/quickstart.md`
- [ ] T036 Executar auditoria completa de internacionalização e registrar todos os itens como conformes em `specs/021-redesenho-fluxo-draft/verification-report.md`
- [ ] T037 Revisar limites de responsabilidade, duplicações, regressões e conformidade visual em `FrontEnd/src/views/DraftsView.vue`, `FrontEnd/src/components/drafts/` e `FrontEnd/src/styles/main.css`
- [ ] T038 Após SC-001 a SC-010 aprovados localmente, escrever testes falhos para a release posterior do redesenho em `FrontEnd/src/constants/systemUpdates.spec.ts`, `FrontEnd/src/services/systemUpdates.spec.ts`, `FrontEnd/src/views/SystemUpdatesView.spec.ts` e `FrontEnd/src/i18n/i18n.spec.ts`
- [ ] T039 Adicionar a próxima versão editorial disponível do redesenho em `FrontEnd/src/constants/systemUpdates.ts`, `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`
- [ ] T040 Reexecutar todos os gates e finalizar evidências locais de release em `specs/021-redesenho-fluxo-draft/verification-report.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup precede Foundation.
- Foundation bloqueia todas as histórias.
- US1 cria o shell exigido por US2 e US3.
- US2, US3 e US5 executam em sequência depois de US1 por compartilharem `DraftsView.vue` e seus testes; todas devem terminar antes de US4.
- US6 depende apenas de Foundation e pode avançar em paralelo com US1-US5.
- Polish depende de todas as histórias; T038-T039 dependem explicitamente de SC-001 a SC-010 aprovados.

### User Story Dependencies

- **US1**: Foundation.
- **US2**: US1.
- **US3**: US2; integra o painel de preparação nos status iniciais e o board nos status posteriores.
- **US4**: US1, US2, US3 e US5 para validar o layout final.
- **US5**: US3; conclui a composição da view antes da validação responsiva.
- **US6**: Foundation; independente do redesenho.

### Within Each User Story

- Escrever testes e confirmar falha antes da implementação.
- Implementar o menor comportamento que torne os testes verdes.
- Executar testes focados antes de avançar.
- Não alterar serviços, backend ou regras de domínio para satisfazer uma expectativa visual.

### Parallel Opportunities

- T003 e T004.
- T007 e T008.
- T010 e T012.
- T014 e T015.
- T017 e T018.
- US6 em paralelo com US1-US5.
- T031 e T032.

## Parallel Examples

```text
US1: DraftStateRail.spec.ts em paralelo com DraftWorkspaceHeader.spec.ts
US2: DraftPreparationPanel.spec.ts em paralelo com DraftDiscordPublicationPanel.spec.ts
US6: contratos do registro/serviço em paralelo com card/view/i18n
```

## Implementation Strategy

### MVP

1. Setup e Foundation.
2. US1 para shell, progresso e ações.
3. Validar isoladamente os sete estados.

### Incremental Delivery

1. US2 entrega presença organizada.
2. US3 completa capitães, ordem, escolhas e resultado.
3. US5 substitui a navegação antiga.
4. US4 fecha responsividade sobre a composição final.
5. US6 publica a correção `.3` independentemente.
6. Polish comprova os critérios e só então publica a release do redesenho.

## Notes

- Preservar `docs/prompts/` e `specs/018-importacao-partidas-lcu/` não rastreados.
- Não modificar componentes legados do agregado `Draft`.
- Não criar novos tokens, dependências, endpoints ou regras de negócio.
- Commits das fases e da implementação devem ser em português.

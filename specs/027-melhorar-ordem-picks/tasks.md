---

description: "Tarefas para melhorar a exibição da ordem de picks"
---

# Tasks: Melhorar Exibição da Ordem de Picks

**Input**: Design documents from `/specs/027-melhorar-ordem-picks/`

**Prerequisites**: `plan.md`, `spec.md`

**Tests**: A implementação seguirá TDD e deve comprovar ordem global, ordinal por time, timeout, associação ausente, 10+ times, 40+ escolhas, três dígitos e responsividade.

**Organization**: Tarefas agrupadas por história, com dependências explícitas e cada incremento verificável.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode ser executada em paralelo sem editar os mesmos arquivos.
- **[Story]**: História coberta pela tarefa.
- Todos os caminhos são relativos à raiz do repositório.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirmar branch, arquivos e baseline antes da alteração.

- [ ] T001 Registrar branch, status e baseline de FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts em specs/027-melhorar-ordem-picks/tasks.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fixar contratos visuais e limites antes de editar produção.

- [ ] T002 Confirmar em specs/027-melhorar-ordem-picks/plan.md que a projeção usa `timeId`, conta timeout, preserva empates e não altera backend

**Checkpoint**: Escopo e algoritmo prontos para TDD sem dependência backend.

---

## Phase 3: User Story 1 - Entender a sequência geral e por time (Priority: P1) MVP

**Goal**: Mostrar em cada card número geral, jogador, time e ordinal acumulado daquele time.

**Independent Test**: Sequência snake fora de ordem resulta em `#06`, jogador, `Rona` e `2ª escolha`, com timeout consumindo ordinal.

### Tests for User Story 1

- [ ] T003 [US1] Estender o teste de sequência para exigir número, jogador, time, ordinal e timeout em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T004 [US1] Adicionar teste de estabilidade em empates, associação ausente e atualização PT/EN em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T005 [US1] Executar `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts` em FrontEnd/ e confirmar RED pelos seletores e traduções ausentes

### Implementation for User Story 1

- [ ] T006 [P] [US1] Adicionar `teamPickOrder` e `unknownTeam` em FrontEnd/src/i18n/locales/pt.json com acentuação revisada
- [ ] T007 [P] [US1] Adicionar pares equivalentes de `teamPickOrder` e `unknownTeam` em FrontEnd/src/i18n/locales/en.json
- [ ] T008 [US1] Criar `presentedChoices` com ordenação estável, contador por `timeId`, chave única e resolução de time em FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue
- [ ] T009 [US1] Renderizar `presentedChoices` como cards dentro do `<ol>` com `data-pick-sequence-number`, `data-pick-player` e `data-pick-team-order` em FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue
- [ ] T010 [US1] Executar testes de FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts e FrontEnd/src/i18n/i18n.spec.ts e confirmar 0 falhas

**Checkpoint**: A ordem global e a ordem específica de cada time ficam identificáveis e localizadas.

---

## Phase 4: User Story 2 - Consultar drafts com muitos times (Priority: P1)

**Goal**: Exibir todas as escolhas de dez ou mais times em grade responsiva sem expansão ou rolagem interna.

**Independent Test**: Dez times, quarenta escolhas snake e uma escolha `#100` geram 41 cards, ordinais corretos e nenhum limite visual fixo.

### Tests for User Story 2

- [ ] T011 [US2] Criar fixture snake com dez times e quarenta escolhas em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T012 [US2] Adicionar teste para 41 cards, ordinal por time, sequência `#100` e filhos diretos do `<ol>` em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T013 [US2] Adicionar teste estrutural da grade `auto-fit`, número com `min-width` e ausência de rolagem interna em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T014 [US2] Executar `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts` em FrontEnd/ e confirmar RED apenas pelos estilos ausentes

### Implementation for User Story 2

- [ ] T015 [US2] Estilizar `.draft-pick-overview`, header e `<ol>` como grade auto-fit sem altura máxima em FrontEnd/src/styles/main.css
- [ ] T016 [US2] Estilizar `.draft-pick-card`, número flexível e cópia com quebra de nomes usando somente tokens existentes em FrontEnd/src/styles/main.css
- [ ] T017 [US2] Executar novamente FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts e confirmar 0 falhas com todos os 41 cards

**Checkpoint**: A grade funciona com 10+ times, 40+ escolhas e sequências de três dígitos.

---

## Phase 5: User Story 3 - Preservar estados e acessibilidade (Priority: P1)

**Goal**: Manter progresso, estado vazio, semântica ordenada, fallback e equivalência PT/EN.

**Independent Test**: Com escolhas, sem escolhas e time ausente, a seção preserva `<ol>/<li>`, progresso e textos equivalentes nos dois idiomas.

### Tests for User Story 3

- [ ] T018 [US3] Adicionar asserções para `<ol>` rotulado, filhos `<li>`, progresso e estado vazio em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
- [ ] T019 [US3] Validar fallback e ordinal em português e inglês após troca reativa de locale em FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts

### Implementation for User Story 3

- [ ] T020 [US3] Ajustar somente o necessário no markup semântico e no fallback localizado em FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue
- [ ] T021 [US3] Executar FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts e FrontEnd/src/i18n/i18n.spec.ts e confirmar 0 falhas

**Checkpoint**: Conteúdo visual, semântico e localizado permanece equivalente em todos os estados.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validar regressão, responsividade, internacionalização e preparar integração.

- [ ] T022 Executar `npm run lint:check`, `npm test` e `npm run build` em FrontEnd/ e corrigir somente regressões desta feature
- [ ] T023 Auditar textos hardcoded, pares pt/en, acentuação, progresso, fallback, botões, títulos, badges, toasts, vazios e validações nos arquivos alterados de FrontEnd/src/
- [ ] T024 Validar 40 escolhas e 10 times no Chromium em 1440×1000, 768×1024 e 390×844, confirmando uma coluna mobile, `#100`, nomes longos e ausência de overflow em FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue
- [ ] T025 Executar revisão independente do diff de FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue, FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts, FrontEnd/src/styles/main.css e FrontEnd/src/i18n/locales/
- [ ] T026 Executar `git diff --check origin/main...HEAD`, inspecionar status/log de specs/027-melhorar-ordem-picks/ e confirmar que não existem alterações backend
- [ ] T027 Atualizar o status em specs/027-melhorar-ordem-picks/spec.md e marcar somente tarefas comprovadas em specs/027-melhorar-ordem-picks/tasks.md
- [ ] T028 Commitar specs/027-melhorar-ordem-picks/spec.md e specs/027-melhorar-ordem-picks/tasks.md em português, enviar feature/027-melhorar-ordem-picks, integrar em main e executar o fluxo de deploy com verificações de saúde

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: inicia imediatamente.
- **Foundational**: depende de Setup e bloqueia implementação.
- **US1**: depende de Foundational e entrega a projeção/markup usados pelas demais histórias.
- **US2**: depende do markup de US1 para aplicar escala e grade.
- **US3**: depende de US1, pode preparar testes junto de US2, mas sua validação final usa ambos.
- **Polish**: depende de US1, US2 e US3.

### User Story Dependencies

```text
Foundational → US1 → US2
                  └→ US3
US2 + US3 → Polish → Integração/Deploy
```

### Parallel Opportunities

- T006 e T007 alteram catálogos diferentes e podem ser preparados em paralelo após RED.
- T011-T013 podem ser escritos juntos antes dos estilos, mas editam o mesmo teste e devem ser coordenados por um único implementador.
- Auditoria documental de T023 pode ocorrer em paralelo com preparação da validação visual T024 depois das suítes verdes.

## Parallel Examples

```text
US1: T006 pt.json | T007 en.json
Polish: T023 auditoria i18n | preparação de cenários T024
```

## Implementation Strategy

### MVP First

1. Concluir T001-T002.
2. Executar T003-T010 em TDD.
3. Validar número geral + ordinal por time antes de adicionar a grade.

### Incremental Delivery

1. US1 torna a informação correta e compreensível.
2. US2 torna a apresentação escalável e responsiva.
3. US3 fecha semântica, fallback e idiomas.
4. Polish valida a jornada completa e prepara lançamento.

## Notes

- Timeout incrementa o ordinal do time porque consumiu sua vez.
- Nome textual do time é a identificação principal; não criar paleta para 10+ times.
- Não adicionar avatar, elo, rota, agrupamento, paginação, virtualização ou rolagem interna.
- Commits devem usar português e incluir somente arquivos da feature.

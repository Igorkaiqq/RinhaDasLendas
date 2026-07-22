---

description: "Tarefas de implementação do histórico de atualizações"
---

# Tasks: Histórico de Atualizações

**Input**: `specs/019-historico-atualizacoes/spec.md`, `specs/019-historico-atualizacoes/plan.md` e `docs/superpowers/specs/2026-07-22-historico-atualizacoes-design.md`

**Prerequisites**: especificação e plano aprovados antes de qualquer código de aplicação

**Tests**: cada teste de comportamento deve ser escrito e executado com falha esperada antes da implementação correspondente.

**Organization**: as tarefas seguem os seis incrementos aprovados e mantêm rastreabilidade para as quatro histórias de usuário.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: somente traduções ou documentação em arquivos distintos e sem dependência de edição concorrente.
- **[Story]**: história atendida pela tarefa (`US1`, `US2`, `US3` ou `US4`).
- Todas as tarefas identificam os arquivos que serão criados ou alterados.

## Phase 1: Setup E Contratos Do Registro

**Purpose**: estabelecer o contrato tipado e testes inicialmente vermelhos que bloqueiam o registro e suas operações.

- [ ] T001 Criar os tipos fechados de categoria, área, detalhe, link e release em `FrontEnd/src/types/systemUpdate.ts`
- [ ] T002 [US4] Escrever e executar testes vermelhos do contrato do registro para IDs e versões únicos, formato `AAAA.MM.N`, datas ISO, ordem decrescente, oito releases, 15 detalhes recentes, categorias, áreas, detalhes e links internos em `FrontEnd/src/constants/systemUpdates.spec.ts`
- [ ] T003 [US1] Escrever e executar testes vermelhos das operações de ordenação, release mais recente, agrupamento temporal e validação de links em `FrontEnd/src/services/systemUpdates.spec.ts`
- [ ] T004 [US2] Acrescentar e executar testes vermelhos de normalização localizada sem distinção de caixa ou acentuação, busca em título, resumo e detalhes, combinação de categorias e limpeza lógica em `FrontEnd/src/services/systemUpdates.spec.ts`
- [ ] T005 [US3] Acrescentar e executar testes vermelhos de leitura, gravação, versão divergente e fallback em memória quando `localStorage` lança erro ou está indisponível em `FrontEnd/src/services/systemUpdates.spec.ts`

**Checkpoint**: contratos definidos e comportamentos centrais descritos por testes que falham pela ausência da implementação.

---

## Phase 2: Registro Histórico, I18n E Validações Automatizadas

**Purpose**: entregar o catálogo inicial completo, operações puras e paridade localizada verificável.

- [ ] T006 [US4] Implementar a coleção imutável com oito marcos, versões e datas históricas coerentes, cinco categorias, oito áreas, links internos e os 15 detalhes individualizados da release mais recente em `FrontEnd/src/constants/systemUpdates.ts`, fazendo `FrontEnd/src/constants/systemUpdates.spec.ts` passar
- [ ] T007 [US1] Implementar ordenação, release mais recente, agrupamento e validação contra `AppRoutes` em `FrontEnd/src/services/systemUpdates.ts`, fazendo os testes correspondentes de `FrontEnd/src/services/systemUpdates.spec.ts` passar
- [ ] T008 [US2] Implementar normalização, busca sobre mensagens traduzidas e combinação de filtros em `FrontEnd/src/services/systemUpdates.ts`, fazendo os testes correspondentes de `FrontEnd/src/services/systemUpdates.spec.ts` passar
- [ ] T009 [US3] Implementar leitura e gravação de `rinha:last-seen-system-update`, detecção de conteúdo novo e fallback da sessão em `FrontEnd/src/services/systemUpdates.ts`, fazendo os testes correspondentes de `FrontEnd/src/services/systemUpdates.spec.ts` passar
- [ ] T010 [US4] Estender os testes de paridade para exigir todas as chaves de releases, detalhes, categorias, áreas, filtros, badge, estados e acessibilidade nos dois idiomas em `FrontEnd/src/i18n/i18n.spec.ts` e confirmar a falha antes dos catálogos
- [ ] T011 [P] [US4] Adicionar títulos, resumos, 15 detalhes recentes, demais marcos, categorias, áreas, filtros, badge, estados e nomes acessíveis em português com acentuação revisada em `FrontEnd/src/i18n/locales/pt.json`
- [ ] T012 [P] [US4] Adicionar estrutura e conteúdo equivalentes em inglês em `FrontEnd/src/i18n/locales/en.json`
- [ ] T013 [US4] Executar `npm test -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/i18n/i18n.spec.ts` em `FrontEnd/` e corrigir somente os arquivos desta fase até todos os contratos passarem

**Checkpoint**: o registro completo é válido, pesquisável e integralmente localizado sem backend ou textos editoriais hardcoded.

---

## Phase 3: Rota, Navegação E Badge Novo

**Purpose**: tornar o histórico descobrível e autenticado, com estado visualizado local e tolerante a falhas.

- [ ] T014 [US1] Escrever e executar teste vermelho para nome e caminho `/atualizacoes` em `FrontEnd/src/constants/appRoutes.spec.ts`
- [ ] T015 [US1] Escrever e executar teste vermelho da rota `requiresAuth`, sem restrição por papel, em `FrontEnd/src/router/index.spec.ts`
- [ ] T016 [US3] Escrever e executar testes vermelhos do item Atualizações na navegação desktop e mobile, badge localizado para versão ausente ou divergente, remoção sem recarga e fallback de storage em `FrontEnd/src/components/layout/SidebarNav.spec.ts`
- [ ] T017 [US1] Adicionar `Updates` a `AppRouteNames` e `/atualizacoes` a `AppRoutes` em `FrontEnd/src/constants/appRoutes.ts`, fazendo `FrontEnd/src/constants/appRoutes.spec.ts` passar
- [ ] T018 [US1] Registrar `SystemUpdatesView` com `requiresAuth: true` e título localizado em `FrontEnd/src/router/index.ts`, fazendo `FrontEnd/src/router/index.spec.ts` passar
- [ ] T019 [US3] Integrar o item Atualizações e o badge reativo com a versão visualizada em `FrontEnd/src/components/layout/SidebarNav.vue`, fazendo `FrontEnd/src/components/layout/SidebarNav.spec.ts` passar sem interromper navegação quando o storage falhar

**Checkpoint**: usuários autenticados encontram a rota em qualquer navegação e o badge reflete a visualização no navegador atual.

---

## Phase 4: Card Editorial Acessível

**Purpose**: entregar a unidade visual e semântica reutilizável da timeline.

- [ ] T020 [US1] Escrever e executar testes vermelhos para versão, `time`, título, resumo, categorias, áreas, destaque da release recente, grupos por categoria, `aria-expanded`, expansão por teclado e links internos opcionais em `FrontEnd/src/components/updates/SystemUpdateCard.spec.ts`
- [ ] T021 [US1] Implementar o card editorial com componentes e tokens existentes em `FrontEnd/src/components/updates/SystemUpdateCard.vue`, fazendo `FrontEnd/src/components/updates/SystemUpdateCard.spec.ts` passar sem texto visível hardcoded

**Checkpoint**: uma release pode ser lida, expandida e navegada por mouse, teclado ou toque de forma independente da página completa.

---

## Phase 5: Hero, Busca, Filtros, Timeline E Estado Vazio

**Purpose**: compor a experiência principal e a localização rápida de mudanças.

- [ ] T022 [US1] Escrever e executar testes vermelhos do hero da release mais recente, oito releases em ordem decrescente, agrupamento por ano e mês, índice lateral quando aplicável e registro da versão visualizada ao abrir em `FrontEnd/src/views/SystemUpdatesView.spec.ts`
- [ ] T023 [US2] Acrescentar e executar testes vermelhos de busca no idioma ativo, recálculo ao trocar idioma, chips combináveis, quantidade, limpeza e estado vazio localizado em `FrontEnd/src/views/SystemUpdatesView.spec.ts`
- [ ] T024 [US1] Acrescentar e executar testes vermelhos de semântica de lista, foco e nomes acessíveis, filtros roláveis e ausência de ações inacessíveis nas composições desktop e mobile em `FrontEnd/src/views/SystemUpdatesView.spec.ts`
- [ ] T025 [US1] Implementar hero, índice responsivo, timeline semântica e composição dos cards em `FrontEnd/src/views/SystemUpdatesView.vue`, fazendo os testes de consulta e acessibilidade em `FrontEnd/src/views/SystemUpdatesView.spec.ts` passar
- [ ] T026 [US2] Implementar estado de busca, categorias combináveis, quantidade, limpeza, recálculo por idioma e estado vazio em `FrontEnd/src/views/SystemUpdatesView.vue`, fazendo os testes de localização em `FrontEnd/src/views/SystemUpdatesView.spec.ts` passar

**Checkpoint**: consulta, busca, filtros, expansão, links e estado vazio formam um incremento funcional em português e inglês.

---

## Phase 6: Responsividade, Documentação E Auditoria Final

**Purpose**: validar qualidade transversal, registrar a manutenção futura e concluir o gate sem ampliar escopo.

- [ ] T027 [P] [US4] Documentar versionamento mensal, inclusão no registro, traduções, categorias, áreas, links, testes e commit em `docs/guides/ATUALIZAR_HISTORICO.md`
- [ ] T028 [P] [US4] Adicionar a revisão explícita do histórico para mudanças visíveis em `docs/standards/FEATURE_CHECKLIST.md`
- [ ] T029 Revisar responsividade e ajustar somente `FrontEnd/src/views/SystemUpdatesView.vue` e `FrontEnd/src/components/updates/SystemUpdateCard.vue` para uma coluna mobile, filtros roláveis, timeline à esquerda, largura disponível e ausência de overflow, preservando os testes escritos em T024
- [ ] T030 Executar `npm test` e `npm run build` em `FrontEnd/` e corrigir falhas relacionadas à feature nos arquivos listados neste documento
- [ ] T031 Auditar `FrontEnd/src/constants/systemUpdates.ts`, `FrontEnd/src/components/updates/SystemUpdateCard.vue` e `FrontEnd/src/views/SystemUpdatesView.vue` para confirmar ausência de texto visível hardcoded
- [ ] T032 Auditar sincronização de `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`, acentuação portuguesa, placeholders, botões, títulos, badges, estados vazios, nomes acessíveis e validações localizadas
- [ ] T033 Validar manualmente `/atualizacoes` autenticado em português e inglês por mouse, teclado e toque, em desktop e mobile, confirmando foco visível, badge, expansão, links, busca, filtros, estado vazio e ausência de overflow
- [ ] T034 Confirmar que nenhum arquivo de `BackEnd/`, migration, endpoint, painel administrativo, integração externa ou geração por commits foi adicionado e registrar o resultado da auditoria de internacionalização no relatório de implementação

**Checkpoint**: testes, build, responsividade, acessibilidade, documentação, escopo e internacionalização estão validados para aprovação.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: inicia após aprovação explícita destes artefatos e não altera código antes desse gate.
- **Phase 2**: depende dos tipos e testes vermelhos da Phase 1.
- **Phase 3**: depende do registro e serviço da Phase 2 para calcular a release mais recente e o badge.
- **Phase 4**: depende dos tipos, registro e traduções da Phase 2.
- **Phase 5**: depende do card da Phase 4 e da rota registrada na Phase 3.
- **Phase 6**: depende de todas as histórias implementadas e encerra com testes, build e auditorias.

### User Story Dependencies

- **User Story 1 (P1)**: usa o contrato e registro compartilhados; entrega consulta independente com rota, card e timeline.
- **User Story 2 (P2)**: usa o registro da US1, mas suas operações puras e critérios podem ser testados independentemente.
- **User Story 3 (P2)**: usa somente a versão mais recente do registro e a navegação existente; não depende de busca ou expansão.
- **User Story 4 (P2)**: estabelece o contrato e manutenção que suportam as demais histórias, com validação independente da interface.

### Red-Green Order

- T002 precede T006.
- T003 precede T007.
- T004 precede T008.
- T005 precede T009.
- T010 precede T011 e T012.
- T014 precede T017; T015 precede T018; T016 precede T019.
- T020 precede T021.
- T022 e T024 precedem T025; T023 precede T026.
- Cada teste novo MUST falhar pelo comportamento ausente antes do início da implementação correspondente.

### Parallel Opportunities

- T011 e T012 podem ser executadas em paralelo depois de T010 porque alteram catálogos distintos.
- T027 e T028 podem ser executadas em paralelo depois da implementação porque alteram documentos distintos.
- Nenhuma outra tarefa recebe `[P]`; testes e implementações compartilham arquivos ou possuem dependência direta.

## Parallel Examples

```text
T011: Traduzir o catálogo português em FrontEnd/src/i18n/locales/pt.json
T012: Traduzir o catálogo inglês em FrontEnd/src/i18n/locales/en.json

T027: Criar docs/guides/ATUALIZAR_HISTORICO.md
T028: Atualizar docs/standards/FEATURE_CHECKLIST.md
```

## Implementation Strategy

### MVP First

1. Concluir Phase 1 e Phase 2 para obter conteúdo válido e localizado.
2. Concluir a rota da Phase 3 sem depender do badge para entregar acesso autenticado.
3. Concluir Phase 4 e a parte US1 da Phase 5.
4. Parar e validar a User Story 1 de forma independente antes dos incrementos P2.

### Incremental Delivery

1. Entregar consulta cronológica e expansão como MVP da US1.
2. Adicionar busca, categorias e estado vazio da US2.
3. Adicionar badge local e fallback da US3.
4. Validar o fluxo de manutenção da US4 com guia, checklist e contratos automatizados.
5. Executar toda a Phase 6 antes de considerar a feature concluída.

## Traceability

| Requirement | Tasks |
|-------------|-------|
| FR-001 | T015, T018 |
| FR-002, FR-003 | T002, T006, T010-T012 |
| FR-004-FR-007 | T001-T003, T006-T007 |
| FR-008, FR-009 | T020-T022, T025 |
| FR-010-FR-012 | T004, T008, T023, T026 |
| FR-013 | T002-T003, T006-T007, T020-T021 |
| FR-014-FR-016 | T005, T009, T014-T019, T022, T025 |
| FR-017 | T010-T013, T021, T025-T026, T031-T032 |
| FR-018, FR-019 | T020-T025, T029, T033 |
| FR-020 | T027-T028, T034 |

## Notes

- Não iniciar T001 antes da aprovação explícita de `spec.md`, `plan.md` e `tasks.md`.
- Não adicionar dependências sem demonstrar que os recursos existentes são insuficientes.
- Não criar arquivos de aplicação fora de `FrontEnd/` nem persistência de domínio para esta feature.
- Commits de implementação devem seguir mensagens em português brasileiro e os gates do `AGENTS.md`.

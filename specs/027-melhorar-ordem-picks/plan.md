# Melhorar Exibição da Ordem de Picks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir a lista corrida da ordem de picks por cards responsivos que exibam sequência geral, jogador, time e ordinal da escolha dentro do time, sem limite visual fixo de times ou escolhas.

**Architecture:** `DraftVisualBoard.vue` cria uma projeção somente de apresentação a partir de `montagem.escolhas` e `montagem.times`: primeiro estabiliza a ordenação por sequência e depois calcula os ordinais em uma única passagem com contador por `timeId`. O template mantém o `<ol>` semântico e o CSS global transforma seus itens em uma grade autoajustável; nenhum contrato, regra ou persistência backend muda.

**Tech Stack:** Vue 3.5, TypeScript 5.9, Composition API, Vue I18n 11, Vitest 4, Vue Test Utils, happy-dom e CSS com tokens do design system existente.

## Global Constraints

- Implementar somente no frontend; não alterar API, banco de dados, domínio, SignalR ou contratos.
- Ordenar por `sequencia` antes de calcular o ordinal por time e preservar estabilidade em empates.
- Contar cada registro, inclusive timeout, como uma escolha consumida pelo respectivo `timeId`.
- Renderizar todas as escolhas sem paginação, expansão, virtualização ou rolagem interna.
- Não assumir quantidade máxima de times, escolhas, colunas ou dígitos na sequência.
- Identificar time por texto; cor não pode ser necessária nem exclusiva.
- Usar somente tokens visuais existentes e não criar cores, escalas, componentes ou dependências.
- Preservar progresso, estado vazio, `<ol>` semântico, nomes longos, foco, contraste e comportamento responsivo.
- Todo texto novo deve existir em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`, com significado equivalente e acentuação portuguesa revisada.
- Seguir TDD: observar a falha específica antes de alterar produção.

---

## File Structure

```text
FrontEnd/src/
├── components/drafts/visual/
│   ├── DraftVisualBoard.vue       # projeção ordenada e markup semântico dos cards
│   └── DraftVisualBoard.spec.ts   # ordinais, escala, fallback, i18n e regressão estrutural
├── i18n/locales/
│   ├── pt.json                    # formato PT-BR do ordinal e fallback de time
│   └── en.json                    # formato EN equivalente
└── styles/main.css                # grade e cards responsivos com tokens existentes
```

**Structure Decision:** Não extrair novo componente nem helper porque cálculo, markup e uso existem somente no histórico de picks do `DraftVisualBoard`. A projeção computada evita lógica no template sem criar uma abstração prematura.

---

### Task 1: Projeção ordenada e ordinal por time

**Files:**
- Modify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue:103-108,392-394,467-479`
- Test: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts:151-165`
- Modify: `FrontEnd/src/i18n/locales/pt.json:886-910`
- Modify: `FrontEnd/src/i18n/locales/en.json:886-910`

**Interfaces:**
- Consumes: `DraftMontagem['escolhas']`, `DraftMontagem['times']` e `choiceName(choice)` existentes.
- Produces: computed `presentedChoices` com itens `{ key, choice, teamName, teamPickOrder }` e chaves `drafts.visualBoard.teamPickOrder`/`drafts.visualBoard.unknownTeam`.

- [ ] **Step 1: Estender o teste existente para exigir sequência geral, time e ordinal por time**

No teste `shows explicit team order, captains, pick progress, ordered sequence, and preferred routes`, substituir a asserção genérica da sequência por:

```ts
const picks = wrapper.findAll('[data-pick-sequence]')

expect(picks).toHaveLength(3)
expect(picks[0]!.get('[data-pick-sequence-number]').text()).toBe('#1')
expect(picks[0]!.get('[data-pick-player]').text()).toBe('First Pick')
expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Team A · 1ª escolha')
expect(picks[1]!.get('[data-pick-sequence-number]').text()).toBe('#2')
expect(picks[1]!.get('[data-pick-team-order]').text()).toBe('Team B · 1ª escolha')
expect(picks[2]!.get('[data-pick-player]').text()).toBe('Tempo esgotado')
expect(picks[2]!.get('[data-pick-team-order]').text()).toBe('Team A · 2ª escolha')
```

O terceiro registro comprova que timeout consome a segunda escolha do `Team A`.

- [ ] **Step 2: Adicionar teste de estabilidade, associação ausente e idioma inglês**

```ts
it('keeps tied choices stable and localizes a missing team without breaking team ordinals', async () => {
  const draft = montagem()
  draft.escolhas = [
    ...draft.escolhas,
    {
      sequencia: 3,
      timeId: 'missing-team',
      capitaoId: 'missing-captain',
      jogadorId: 'picked-missing',
      jogadorNome: 'Missing Team Pick',
      tipo: 'Escolha',
      registradoEm: '2026-07-25T12:03:30Z',
    },
  ]
  const wrapper = mountBoard(draft)
  const picks = wrapper.findAll('[data-pick-sequence]')

  expect(picks.map((pick) => pick.get('[data-pick-player]').text())).toEqual([
    'First Pick',
    'Second Pick',
    'Tempo esgotado',
    'Missing Team Pick',
  ])
  expect(picks[2]!.get('[data-pick-team-order]').text()).toBe('Team A · 2ª escolha')
  expect(picks[3]!.get('[data-pick-team-order]').text()).toBe('Time não encontrado · 1ª escolha')

  await setLocale('en')
  await nextTick()
  expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Team A · pick 1')
  expect(picks[3]!.get('[data-pick-team-order]').text()).toBe('Unknown team · pick 1')
  wrapper.unmount()
})
```

- [ ] **Step 3: Executar o teste focado e confirmar RED**

Run: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`

Expected: FAIL porque `data-pick-sequence-number`, `data-pick-player`, `data-pick-team-order` e as traduções ainda não existem.

- [ ] **Step 4: Adicionar os pares de tradução**

Em `pt.json`, dentro de `drafts.visualBoard`:

```json
"teamPickOrder": "{team} · {order}ª escolha",
"unknownTeam": "Time não encontrado"
```

Em `en.json`, na mesma posição:

```json
"teamPickOrder": "{team} · pick {order}",
"unknownTeam": "Unknown team"
```

- [ ] **Step 5: Substituir `orderedChoices` pela projeção estável em uma passagem**

```ts
const presentedChoices = computed(() => {
  const teamsById = new Map(localMontagem.value.times.map((team) => [team.id, team]))
  const picksByTeam = new Map<string, number>()

  return localMontagem.value.escolhas
    .map((choice, originalIndex) => ({ choice, originalIndex }))
    .sort((current, next) => current.choice.sequencia - next.choice.sequencia || current.originalIndex - next.originalIndex)
    .map(({ choice, originalIndex }) => {
      const teamPickOrder = (picksByTeam.get(choice.timeId) ?? 0) + 1
      picksByTeam.set(choice.timeId, teamPickOrder)

      return {
        key: `${choice.sequencia}-${choice.timeId}-${originalIndex}`,
        choice,
        teamName: teamsById.get(choice.timeId)?.nome,
        teamPickOrder,
      }
    })
})
```

Remover o computed `orderedChoices`, que deixa de ter consumidores.

- [ ] **Step 6: Renderizar a projeção mantendo `<ol>` e estado vazio**

```vue
<ol v-if="presentedChoices.length" data-pick-sequence-list>
  <li v-for="item in presentedChoices" :key="item.key" data-pick-sequence class="draft-pick-card">
    <strong data-pick-sequence-number class="draft-pick-card__number">#{{ item.choice.sequencia }}</strong>
    <span class="draft-pick-card__copy">
      <strong data-pick-player>{{ choiceName(item.choice) }}</strong>
      <small data-pick-team-order>
        {{ t('drafts.visualBoard.teamPickOrder', {
          team: item.teamName ?? t('drafts.visualBoard.unknownTeam'),
          order: item.teamPickOrder,
        }) }}
      </small>
    </span>
  </li>
</ol>
<p v-else>{{ t('drafts.pickHistory.empty') }}</p>
```

- [ ] **Step 7: Executar teste focado e teste de sincronização i18n**

Run: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/i18n/i18n.spec.ts`

Expected: PASS, com 0 falhas e os pares PT/EN sincronizados.

- [ ] **Step 8: Commitar o incremento funcional**

```bash
git add FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts FrontEnd/src/i18n/locales/pt.json FrontEnd/src/i18n/locales/en.json
git commit -m "feat: identificar ordem de picks por time"
```

---

### Task 2: Grade responsiva e escala de dez ou mais times

**Files:**
- Modify: `FrontEnd/src/styles/main.css:2470-2518`
- Test: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`

**Interfaces:**
- Consumes: classes `draft-pick-overview`, `draft-pick-card`, `draft-pick-card__number` e `draft-pick-card__copy` produzidas pela Task 1.
- Produces: grade autoajustável sem limite de itens, rolagem interna ou largura fixa da numeração.

- [ ] **Step 1: Criar fixture de dez times e quarenta escolhas**

Adicionar ao teste:

```ts
function largeMontagem(): DraftMontagem {
  const draft = montagem()
  const teams = Array.from({ length: 10 }, (_, index) => ({
    id: `team-${index + 1}`,
    nome: `Time ${index + 1}`,
    ordem: index + 1,
    cor: index % 2 === 0 ? 'blue' : 'red',
    capitaoId: null,
    jogadores: [],
  }))
  const escolhas = Array.from({ length: 40 }, (_, index) => {
    const round = Math.floor(index / teams.length)
    const position = index % teams.length
    const teamIndex = round % 2 === 0 ? position : teams.length - position - 1

    return {
      sequencia: index + 1,
      timeId: teams[teamIndex]!.id,
      capitaoId: `captain-${teamIndex + 1}`,
      jogadorId: `player-${index + 1}`,
      jogadorNome: `Jogador ${index + 1} com nome competitivo longo`,
      tipo: 'Escolha' as const,
      registradoEm: `2026-07-25T13:${String(index).padStart(2, '0')}:00Z`,
    }
  })

  return {
    ...draft,
    tamanhoEquipe: 5,
    quantidadeTimes: teams.length,
    times: teams,
    escolhas,
  }
}
```

- [ ] **Step 2: Escrever teste falhando de escala, snake e três dígitos**

```ts
it('renders every choice for ten or more teams with snake ordinals and unbounded sequence digits', () => {
  const draft = largeMontagem()
  draft.escolhas.push({
    ...draft.escolhas[0]!,
    sequencia: 100,
    jogadorId: 'player-100',
    jogadorNome: 'Jogador 100',
  })
  const wrapper = mountBoard(draft)
  const picks = wrapper.findAll('[data-pick-sequence]')

  expect(picks).toHaveLength(41)
  expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 1ª escolha')
  expect(picks[19]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 2ª escolha')
  expect(picks[40]!.get('[data-pick-sequence-number]').text()).toBe('#100')
  expect(picks[40]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 5ª escolha')
  expect(wrapper.get('[data-pick-sequence-list]').element.children).toHaveLength(41)
  wrapper.unmount()
})
```

- [ ] **Step 3: Escrever teste estrutural falhando da grade sem rolagem interna**

```ts
it('uses an auto-fit pick grid without internal scrolling or fixed number width', () => {
  expect(MainCss).toMatch(/\.draft-pick-overview ol\s*{[\s\S]*?grid-template-columns:\s*repeat\(auto-fit,\s*minmax\(min\(220px,\s*100%\),\s*1fr\)\)/)
  expect(MainCss).toMatch(/\.draft-pick-card__number\s*{[\s\S]*?min-width:\s*36px/)

  const overviewRule = MainCss.match(/\.draft-pick-overview\s*{(?<declarations>[^}]*)}/)?.groups?.declarations ?? ''
  const listRule = MainCss.match(/\.draft-pick-overview ol\s*{(?<declarations>[^}]*)}/)?.groups?.declarations ?? ''
  expect(`${overviewRule}\n${listRule}`).not.toMatch(/max-height|overflow-y|overflow:\s*(auto|scroll)/)
})
```

- [ ] **Step 4: Executar teste focado e confirmar RED de CSS**

Run: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`

Expected: o teste de 41 itens passa após a Task 1, mas o teste estrutural FAIL porque os seletores da grade ainda não existem.

- [ ] **Step 5: Implementar a grade e os cards somente com tokens existentes**

Adicionar em `main.css`, imediatamente antes de `.draft-visual-history`:

```css
.draft-pick-overview {
  display: grid;
  gap: var(--space-sm);
  min-width: 0;
  margin-bottom: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  padding: var(--space-md);
  background: var(--color-surface-1);
}

.draft-pick-overview header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  flex-wrap: wrap;
}

.draft-pick-overview header :is(span, strong) {
  margin: 0;
}

.draft-pick-overview ol {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(220px, 100%), 1fr));
  gap: var(--space-xs);
  min-width: 0;
  margin: 0;
  padding: 0;
  list-style: none;
}

.draft-pick-card {
  display: grid;
  grid-template-columns: minmax(36px, auto) minmax(0, 1fr);
  align-items: center;
  gap: var(--space-xs);
  min-width: 0;
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  padding: var(--space-xs);
  background: var(--color-surface-2);
}

.draft-pick-card__number {
  display: inline-grid;
  place-items: center;
  min-width: 36px;
  min-height: 36px;
  border-radius: var(--radius-sm);
  padding-inline: var(--space-xxs);
  color: var(--color-ink);
  background: var(--color-primary);
  font-family: var(--font-data);
  font-size: 11px;
  font-variant-numeric: tabular-nums;
}

.draft-pick-card__copy {
  display: grid;
  gap: var(--space-xxs);
  min-width: 0;
}

.draft-pick-card__copy > strong,
.draft-pick-card__copy > small {
  min-width: 0;
  overflow-wrap: anywhere;
}

.draft-pick-card__copy > small {
  color: var(--color-ink-muted);
  font-size: 12px;
}
```

- [ ] **Step 6: Executar teste focado e confirmar GREEN**

Run: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`

Expected: PASS, incluindo todos os 41 cards, ordinais snake e regra sem rolagem interna.

- [ ] **Step 7: Commitar o incremento visual**

```bash
git add FrontEnd/src/styles/main.css FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts
git commit -m "feat: exibir ordem de picks em grade responsiva"
```

---

### Task 3: Verificação, acessibilidade e documentação de entrega

**Files:**
- Modify: `specs/027-melhorar-ordem-picks/spec.md`
- Modify: `specs/027-melhorar-ordem-picks/tasks.md`
- Verify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- Verify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- Verify: `FrontEnd/src/styles/main.css`
- Verify: `FrontEnd/src/i18n/locales/pt.json`
- Verify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Consumes: implementação concluída nas Tasks 1 e 2.
- Produces: evidência automatizada e visual pronta para revisão, integração e deploy.

- [ ] **Step 1: Executar a verificação frontend completa**

Run: `npm run lint:check && npm test && npm run build`

Expected: ESLint, todos os testes Vitest, `vue-tsc` e Vite terminam com código zero.

- [ ] **Step 2: Auditar internacionalização**

Confirmar explicitamente:

```text
Nenhum texto visível novo está hardcoded no componente.
teamPickOrder e unknownTeam existem em pt.json e en.json.
PT-BR usa “{order}ª escolha” com acentuação correta.
EN usa “pick {order}” com significado equivalente.
Backend e resources não foram alterados porque nenhum contrato mudou.
```

- [ ] **Step 3: Validar desktop, tablet e mobile no Chromium**

Exercitar pelo menos:

```text
1440×1000: grade com 40 escolhas e múltiplas colunas.
768×1024: redistribuição sem sobreposição.
390×844: uma coluna, nomes longos quebrando linha e scrollWidth igual ao viewport.
```

Em todos os tamanhos, confirmar: quarenta cards visíveis, nenhum `overflow-x`, nenhuma rolagem interna, progresso legível, `#100` sem corte, timeout, fallback de time e `<ol>` com filhos `<li>`.

- [ ] **Step 4: Revisar o diff completo**

Run: `git diff --check origin/main...HEAD && git status --short --branch && git diff --stat origin/main...HEAD`

Expected: sem erro de whitespace, somente arquivos especificados e nenhuma alteração backend.

- [ ] **Step 5: Atualizar status e tarefas após evidência real**

Alterar `spec.md` para `Implemented / Verified` e marcar em `tasks.md` apenas tarefas cuja execução e evidência estejam concluídas.

- [ ] **Step 6: Commitar a documentação de verificação**

```bash
git add specs/027-melhorar-ordem-picks/spec.md specs/027-melhorar-ordem-picks/tasks.md
git commit -m "docs: registrar validação da ordem de picks"
```

## Requirement Coverage

| Requirement | Plan coverage |
|---|---|
| FR-001 a FR-004 | Task 1, projeção estável e ordinais por `timeId` |
| FR-005, FR-006 e FR-008 | Task 2, fixture 40+ e grade auto-fit |
| FR-007 e FR-010 | Task 1, nome do time e fallback localizado |
| FR-009 e FR-011 | Tasks 1 e 2, `<ol>`, estado vazio, cards e nomes longos |
| FR-012 | Tasks 1 e 3, pares PT/EN e auditoria |
| FR-013 | Tasks 2 e 3, tokens e validação visual |
| FR-014 | Task 1, projeção exclusivamente frontend |

## Complexity Tracking

Nenhuma violação constitucional ou complexidade adicional requer justificativa. Não há componente, composable, store, endpoint, migration ou dependência nova.

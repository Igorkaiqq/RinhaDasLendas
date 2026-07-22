# Histórico de Atualizações do Sistema Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar uma página autenticada de atualizações com oito marcos históricos, release mais recente detalhada, busca, filtros, timeline responsiva e badge local de conteúdo novo.

**Architecture:** O conteúdo será um registro TypeScript imutável contendo apenas metadados e chaves i18n. Um serviço puro validará, pesquisará e controlará a versão visualizada; componentes Vue focados renderizarão o card editorial e a página, enquanto `AppShell` manterá o badge reativo sem backend.

**Tech Stack:** Vue 3.5, TypeScript 5.9, Composition API, Vue Router 4, Vue I18n 11, Vitest 4, Vue Test Utils, happy-dom e CSS com os tokens existentes.

## Global Constraints

- Seguir Constitution, Specify, Plan, Tasks e Implement; pausar após a Task 1 até `tasks.md` receber aprovação explícita.
- Trabalhar na branch `feature/019-historico-atualizacoes`; nunca implementar em `main`.
- Seguir `docs/design/DESIGN_SYSTEM.md`, `docs/design/DESIGN_TOKENS.md` e `docs/design/UI_GUIDELINES.md`.
- Não criar novos tokens de cor, espaçamento, tipografia, raio ou sombra.
- Não adicionar dependências, banco, migration, endpoint backend ou painel administrativo.
- A rota autenticada deve ser `/atualizacoes` e ficar disponível para qualquer usuário autenticado, sem exigência de role.
- Todo texto visível deve usar chaves equivalentes em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.
- Versões devem seguir `AAAA.MM.N`, datas devem ser ISO e releases devem permanecer em ordem cronológica decrescente.
- O histórico inicial deve conter exatamente os oito marcos aprovados; a release `2026.07.1` deve listar individualmente as 15 melhorias aprovadas.
- Segurança, rate limiting e concorrência devem ser descritos resumidamente e sem expor tokens, endpoints ou detalhes sensíveis.
- Usar TDD: observar cada teste falhar pelo comportamento ausente antes da implementação mínima.
- Commits devem ser pequenos, escritos em português brasileiro e incluir somente os arquivos da tarefa.
- Não modificar, incluir ou reverter `docs/prompts/`, `specs/018-importacao-partidas-lcu/` ou outras alterações não relacionadas.

---

## File Map

- Create `specs/019-historico-atualizacoes/spec.md`: histórias, requisitos e critérios mensuráveis aprovados.
- Create `specs/019-historico-atualizacoes/plan.md`: decisões técnicas e Constitution Check da feature.
- Create `specs/019-historico-atualizacoes/tasks.md`: tarefas Spec Kit aprováveis e ordenadas por dependência.
- Create `FrontEnd/src/types/systemUpdate.ts`: uniões fechadas e contratos imutáveis das releases.
- Create `FrontEnd/src/constants/systemUpdates.ts`: oito releases, metadados, chaves i18n e links internos.
- Create `FrontEnd/src/services/systemUpdates.ts`: ordenação, validação, busca e persistência segura da última versão vista.
- Create `FrontEnd/src/services/systemUpdates.spec.ts`: contrato do registro, filtros e fallback de storage.
- Create `FrontEnd/src/components/updates/SystemUpdateCard.vue`: card semântico, detalhes por categoria e links internos.
- Create `FrontEnd/src/components/updates/SystemUpdateCard.spec.ts`: renderização localizada, expansão e acessibilidade do card.
- Create `FrontEnd/src/views/SystemUpdatesView.vue`: hero, filtros, índice, agrupamento e estado vazio.
- Create `FrontEnd/src/views/SystemUpdatesView.spec.ts`: busca, filtros, agrupamento, locale e integração da página.
- Modify `FrontEnd/src/constants/appRoutes.ts`: nome e caminho estáveis da rota.
- Modify `FrontEnd/src/constants/appRoutes.spec.ts`: contrato de `/atualizacoes`.
- Modify `FrontEnd/src/router/index.ts`: rota autenticada e título localizado.
- Create `FrontEnd/src/router/index.spec.ts`: metadados de autenticação da nova rota.
- Modify `FrontEnd/src/types/layout.ts`: badge opcional e tipado no item de navegação.
- Modify `FrontEnd/src/components/layout/AppShell.vue`: item Atualizações e estado reativo de versão vista.
- Modify `FrontEnd/src/components/layout/SidebarNav.vue`: badge `Novo` localizado.
- Modify `FrontEnd/src/components/layout/SidebarNav.spec.ts`: badge e significado textual.
- Modify `FrontEnd/src/i18n/locales/pt.json`: navegação, página e histórico em português.
- Modify `FrontEnd/src/i18n/locales/en.json`: estrutura equivalente em inglês.
- Modify `FrontEnd/src/i18n/i18n.spec.ts`: paridade profunda e chaves do histórico.
- Modify `FrontEnd/src/styles/main.css`: layout editorial, timeline, estados e responsividade usando tokens existentes.
- Create `docs/standards/SYSTEM_UPDATES.md`: fluxo editorial para releases futuras.
- Modify `docs/standards/FEATURE_CHECKLIST.md`: revisão obrigatória do histórico.
- Modify `docs/standards/README.md`: link para o novo guia.

---

### Task 1: Materializar E Aprovar Os Artefatos Spec Kit

**Files:**
- Create: `specs/019-historico-atualizacoes/spec.md`
- Create: `specs/019-historico-atualizacoes/plan.md`
- Create: `specs/019-historico-atualizacoes/tasks.md`

**Interfaces:**
- Consumes: `docs/superpowers/specs/2026-07-22-historico-atualizacoes-design.md` e este plano.
- Produces: feature 019 formalmente especificada, planejada e dividida em tarefas aprovadas antes do código.

- [ ] **Step 1: Criar a especificação funcional**

Criar `spec.md` com estas histórias independentes:

```markdown
### User Story 1 - Consultar novidades do sistema (Priority: P1)
Como usuário autenticado, quero consultar atualizações em ordem cronológica e expandir seus detalhes, para entender cada mudança disponível na plataforma.

### User Story 2 - Encontrar uma mudança específica (Priority: P2)
Como usuário autenticado, quero buscar atualizações e filtrá-las por categoria, para localizar rapidamente uma novidade, melhoria ou correção.

### User Story 3 - Perceber conteúdo ainda não visualizado (Priority: P2)
Como usuário autenticado, quero ver um indicador de nova atualização na navegação, para saber quando há conteúdo que ainda não consultei neste navegador.

### User Story 4 - Manter o histórico junto das entregas (Priority: P2)
Como mantenedor, quero cadastrar releases em um contrato tipado e localizado, para publicar textos consistentes sem depender de backend ou geração por commits.
```

Registrar requisitos `FR-001` a `FR-020` cobrindo: rota autenticada, oito marcos, 15 itens na release recente, categorias fechadas, áreas tipadas, busca localizada, filtro combinável, detalhes expansíveis, links internos, badge local, fallback de storage, ordem/versão/data, i18n completo, responsividade, teclado e guia de manutenção.

- [ ] **Step 2: Criar o plano técnico Spec Kit**

Registrar em `plan.md`:

```markdown
**Branch**: `feature/019-historico-atualizacoes`
**Language/Version**: Vue 3.5 + TypeScript 5.9
**Primary Dependencies**: Vue Router, Vue I18n
**Storage**: registro compilado e `localStorage`; sem backend
**Testing**: Vitest, Vue Test Utils e happy-dom
**Structure Decision**: tipos em `types/`, dados em `constants/`, operações puras em `services/`, card em `components/updates/` e composição em `views/`.
```

O Constitution Check deve registrar `PASS` para simplicidade, uso interno, integrações não bloqueantes, i18n, testabilidade e ausência de persistência desnecessária.

- [ ] **Step 3: Criar tarefas Spec Kit ordenadas**

Criar `tasks.md` com as fases:

```markdown
1. Setup e contratos do registro.
2. Registro histórico, i18n e validações automatizadas.
3. Rota, navegação e badge Novo.
4. Card editorial acessível.
5. Hero, busca, filtros, timeline e estado vazio.
6. Responsividade, documentação e auditoria final.
```

Cada comportamento deve ter uma tarefa de teste anterior à implementação correspondente. Marcar como paralelas somente traduções e documentação que não toquem os mesmos arquivos.

- [ ] **Step 4: Validar os artefatos**

Run:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
```

Expected: JSON aponta para `specs/019-historico-atualizacoes` e lista `tasks.md` entre os documentos disponíveis.

- [ ] **Step 5: Commitar a fase documental**

```bash
git add specs/019-historico-atualizacoes/spec.md specs/019-historico-atualizacoes/plan.md specs/019-historico-atualizacoes/tasks.md
git commit -m "docs: planejar histórico de atualizações"
```

- [ ] **Step 6: Parar no gate de aprovação**

Apresentar `spec.md`, `plan.md` e `tasks.md` ao usuário. Não iniciar a Task 2 enquanto as tarefas não forem explicitamente aprovadas.

---

### Task 2: Implementar O Registro, Busca E Persistência Local

**Files:**
- Create: `FrontEnd/src/types/systemUpdate.ts`
- Create: `FrontEnd/src/constants/systemUpdates.ts`
- Create: `FrontEnd/src/services/systemUpdates.ts`
- Create: `FrontEnd/src/services/systemUpdates.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/i18n/i18n.spec.ts`

**Interfaces:**
- Consumes: `AppRoutePath`, função de tradução `(key: string) => string` e storage com `getItem`/`setItem`.
- Produces:

```ts
export type SystemUpdateCategory = 'feature' | 'improvement' | 'fix' | 'security' | 'infrastructure'
export type SystemUpdateArea = 'platform' | 'players' | 'teams' | 'users' | 'drafts' | 'discord' | 'security' | 'infrastructure'

export interface SystemUpdateDetail {
  readonly id: string
  readonly category: SystemUpdateCategory
  readonly titleKey: string
  readonly descriptionKey: string
  readonly link?: AppRoutePath
}

export interface SystemUpdateRelease {
  readonly id: string
  readonly version: string
  readonly publishedAt: string
  readonly featured: boolean
  readonly categories: readonly SystemUpdateCategory[]
  readonly areas: readonly SystemUpdateArea[]
  readonly titleKey: string
  readonly summaryKey: string
  readonly details: readonly SystemUpdateDetail[]
}

export const SYSTEM_UPDATES: readonly SystemUpdateRelease[]
export const LAST_SEEN_SYSTEM_UPDATE_KEY = 'rinha:last-seen-system-update'
export function getLatestSystemUpdate(releases?: readonly SystemUpdateRelease[]): SystemUpdateRelease
export function filterSystemUpdates(releases: readonly SystemUpdateRelease[], query: string, category: SystemUpdateCategory | 'all', translate: (key: string) => string): SystemUpdateRelease[]
export function getSystemUpdateValidationErrors(releases: readonly SystemUpdateRelease[], hasTranslation: (key: string) => boolean): string[]
export function readLastSeenSystemUpdate(storage?: Pick<Storage, 'getItem' | 'setItem'>): string | null
export function markLatestSystemUpdateSeen(version: string, storage?: Pick<Storage, 'getItem' | 'setItem'>): string
```

- [ ] **Step 1: Escrever os testes falhos do contrato**

Criar `systemUpdates.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import en from '@/i18n/locales/en.json'
import pt from '@/i18n/locales/pt.json'
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import {
  filterSystemUpdates,
  getLatestSystemUpdate,
  getSystemUpdateValidationErrors,
  markLatestSystemUpdateSeen,
  readLastSeenSystemUpdate,
} from './systemUpdates'

const hasPath = (source: object, path: string) => path.split('.').every((part) => {
  source = (source as Record<string, object>)[part]
  return source !== undefined
})

describe('system updates', () => {
  it('contains eight ordered and valid releases', () => {
    expect(SYSTEM_UPDATES).toHaveLength(8)
    expect(getLatestSystemUpdate().version).toBe('2026.07.1')
    expect(SYSTEM_UPDATES[0].details).toHaveLength(15)
    expect(getSystemUpdateValidationErrors(SYSTEM_UPDATES, (key) => hasPath(pt, key) && hasPath(en, key))).toEqual([])
  })

  it('filters localized content by normalized text and category', () => {
    const translate = (key: string) => key === 'updates.releases.2026_07_1.details.directDraft.description'
      ? 'Abre diretamente o draft correto pelo Discord'
      : key
    expect(filterSystemUpdates(SYSTEM_UPDATES, 'discord', 'improvement', translate)[0]?.version).toBe('2026.07.1')
  })

  it('uses in-memory fallback when localStorage throws', () => {
    const storage = { getItem: () => { throw new Error('blocked') }, setItem: () => { throw new Error('blocked') } }
    expect(markLatestSystemUpdateSeen('2026.07.1', storage)).toBe('2026.07.1')
    expect(readLastSeenSystemUpdate(storage)).toBe('2026.07.1')
  })
})
```

- [ ] **Step 2: Executar o teste para confirmar RED**

Run: `npm test --prefix FrontEnd -- src/services/systemUpdates.spec.ts`

Expected: FAIL porque tipos, registro e serviço ainda não existem.

- [ ] **Step 3: Criar os tipos e o registro imutável**

Criar `types/systemUpdate.ts` com as interfaces da seção **Interfaces**. Criar `constants/systemUpdates.ts` usando `as const satisfies readonly SystemUpdateRelease[]` e esta ordem:

```ts
export const SYSTEM_UPDATES = [
  { id: 'reliability-2026-07', version: '2026.07.1', publishedAt: '2026-07-22', featured: true },
  { id: 'security-visual-2026-06', version: '2026.06.7', publishedAt: '2026-06-30', featured: false },
  { id: 'discord-presence-2026-06', version: '2026.06.6', publishedAt: '2026-06-29', featured: false },
  { id: 'realtime-draft-2026-06', version: '2026.06.5', publishedAt: '2026-06-24', featured: false },
  { id: 'auth-player-link-2026-06', version: '2026.06.4', publishedAt: '2026-06-21', featured: false },
  { id: 'visual-draft-2026-06', version: '2026.06.3', publishedAt: '2026-06-20', featured: false },
  { id: 'players-teams-2026-06', version: '2026.06.2', publishedAt: '2026-06-19', featured: false },
  { id: 'foundation-i18n-2026-06', version: '2026.06.1', publishedAt: '2026-06-10', featured: false },
] as const satisfies readonly SystemUpdateRelease[]
```

Completar cada objeto com categorias, áreas e chaves sob `updates.releases.<version_normalizada>`. A primeira release deve possuir estes IDs de detalhe, nesta ordem:

```ts
['directDraft', 'invalidLinks', 'contextualDialogs', 'publicationStatuses', 'individualRecovery', 'duplicateProtection', 'realtimePresence', 'consistentPresence', 'eligibleSearch', 'adminTransparency', 'clearBotMessages', 'discordPermissions', 'independentCta', 'reliableQueue', 'securityStability']
```

Os sete marcos anteriores devem ter entre três e seis detalhes editoriais, extraídos de seus respectivos specs, sem reproduzir mensagens de commit.

- [ ] **Step 4: Implementar as funções puras e o fallback de storage**

Criar `services/systemUpdates.ts`:

```ts
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import type { SystemUpdateCategory, SystemUpdateRelease } from '@/types/systemUpdate'

export const LAST_SEEN_SYSTEM_UPDATE_KEY = 'rinha:last-seen-system-update'
let inMemoryLastSeen: string | null = null

export function getLatestSystemUpdate(releases: readonly SystemUpdateRelease[] = SYSTEM_UPDATES): SystemUpdateRelease {
  if (!releases.length) throw new Error('System update registry cannot be empty')
  return releases[0]
}

export function filterSystemUpdates(
  releases: readonly SystemUpdateRelease[],
  query: string,
  category: SystemUpdateCategory | 'all',
  translate: (key: string) => string,
) {
  const normalized = query.trim().toLocaleLowerCase()
  return releases.filter((release) => {
    if (category !== 'all' && !release.categories.includes(category)) return false
    if (!normalized) return true
    return [release.titleKey, release.summaryKey, ...release.details.flatMap((detail) => [detail.titleKey, detail.descriptionKey])]
      .some((key) => translate(key).toLocaleLowerCase().includes(normalized))
  })
}

export function readLastSeenSystemUpdate(storage = globalThis.localStorage): string | null {
  try { return storage.getItem(LAST_SEEN_SYSTEM_UPDATE_KEY) ?? inMemoryLastSeen }
  catch { return inMemoryLastSeen }
}

export function markLatestSystemUpdateSeen(version: string, storage = globalThis.localStorage): string {
  inMemoryLastSeen = version
  try { storage.setItem(LAST_SEEN_SYSTEM_UPDATE_KEY, version) } catch { /* session fallback already set */ }
  return version
}
```

Adicionar a validação completa no mesmo serviço:

```ts
export function getSystemUpdateValidationErrors(
  releases: readonly SystemUpdateRelease[],
  hasTranslation: (key: string) => boolean,
): string[] {
  const errors: string[] = []
  const ids = new Set<string>()
  const versions = new Set<string>()
  const detailIds = new Set<string>()
  const knownPaths = new Set<string>(Object.values(AppRoutes))

  if (releases.filter((release) => release.featured).length !== 1) errors.push('Exactly one release must be featured')

  releases.forEach((release, index) => {
    const parsedDate = new Date(`${release.publishedAt}T00:00:00Z`)
    if (ids.has(release.id)) errors.push(`Duplicate release id: ${release.id}`)
    if (versions.has(release.version)) errors.push(`Duplicate release version: ${release.version}`)
    if (!/^\d{4}\.\d{2}\.\d+$/.test(release.version)) errors.push(`Invalid version: ${release.version}`)
    if (!/^\d{4}-\d{2}-\d{2}$/.test(release.publishedAt)
      || Number.isNaN(parsedDate.getTime())
      || parsedDate.toISOString().slice(0, 10) !== release.publishedAt) {
      errors.push(`Invalid date: ${release.publishedAt}`)
    }
    if (index > 0 && releases[index - 1].publishedAt < release.publishedAt) errors.push('Releases must be newest first')
    if (!release.categories.length) errors.push(`Missing categories: ${release.id}`)
    if (!release.areas.length) errors.push(`Missing areas: ${release.id}`)
    if (!release.details.length) errors.push(`Missing details: ${release.id}`)
    for (const key of [release.titleKey, release.summaryKey]) {
      if (!hasTranslation(key)) errors.push(`Missing translation: ${key}`)
    }
    for (const detail of release.details) {
      const scopedDetailId = `${release.id}:${detail.id}`
      if (detailIds.has(scopedDetailId)) errors.push(`Duplicate detail id: ${scopedDetailId}`)
      detailIds.add(scopedDetailId)
      for (const key of [detail.titleKey, detail.descriptionKey]) {
        if (!hasTranslation(key)) errors.push(`Missing translation: ${key}`)
      }
      if (detail.link && !knownPaths.has(detail.link)) errors.push(`Unknown internal link: ${detail.link}`)
    }
    ids.add(release.id)
    versions.add(release.version)
  })

  return errors
}
```

Importar `AppRoutes` no topo do serviço. Os tipos fechados impedem categorias e áreas desconhecidas em compile time; o teste `vue-tsc` comprova esse contrato.

- [ ] **Step 5: Adicionar todo o conteúdo localizado**

Adicionar `navigation.updates`, `navigation.new`, `routes.updates.title` e a raiz `updates` nos dois JSON. A estrutura obrigatória é:

```json
{
  "updates": {
    "eyebrow": "Histórico do produto",
    "title": "Atualizações do sistema",
    "description": "Acompanhe novidades, melhorias e correções entregues para a comunidade.",
    "latest": "Última atualização",
    "searchLabel": "Buscar atualizações",
    "searchPlaceholder": "Busque por recurso ou melhoria...",
    "filterLabel": "Filtrar por categoria",
    "allCategories": "Todas",
    "resultCount": "{count} atualização | {count} atualizações",
    "emptyTitle": "Nenhuma atualização encontrada",
    "emptyDescription": "Tente outro termo ou remova os filtros selecionados.",
    "clearFilters": "Limpar filtros",
    "details": "Ver detalhes",
    "categories": {},
    "areas": {},
    "releases": {}
  }
}
```

Em inglês, usar `Product history`, `System updates`, `Follow new features, improvements, and fixes delivered to the community.`, `Latest update`, `Search updates`, `Search by feature or improvement...`, `Filter by category`, `All`, `{count} update | {count} updates`, `No updates found`, `Try another term or remove the selected filters.`, `Clear filters` e `View details`.

As categorias devem ser Novidade/Feature, Melhoria/Improvement, Correção/Fix, Segurança/Security e Infraestrutura/Infrastructure. As áreas devem traduzir os oito valores do contrato.

Para `2026_07_1`, usar os 15 títulos aprovados no design e descrições de uma a três frases; as versões inglesas devem transmitir o mesmo benefício, sem tradução automática literal que altere sentido. Para as outras releases, usar títulos editoriais: `Segurança e identidade visual`, `Discord e confirmação de presença`, `Draft em tempo real`, `Contas, permissões e perfis`, `Montagem visual de times`, `Jogadores, rotas e times` e `Fundação e internacionalização`.

- [ ] **Step 6: Reforçar a paridade i18n**

Atualizar `i18n.spec.ts` com função recursiva que coleta caminhos folha e teste:

```ts
expect(leafPaths(pt).sort()).toEqual(leafPaths(en).sort())
expect(i18n.global.t('updates.releases.2026_07_1.details.directDraft.title')).not.toContain('updates.')
```

- [ ] **Step 7: Executar os testes para confirmar GREEN**

Run: `npm test --prefix FrontEnd -- src/services/systemUpdates.spec.ts src/i18n/i18n.spec.ts`

Expected: PASS para contrato, oito releases, 15 detalhes recentes, filtros, storage e paridade de traduções.

- [ ] **Step 8: Commitar o registro e conteúdo**

```bash
git add FrontEnd/src/types/systemUpdate.ts FrontEnd/src/constants/systemUpdates.ts FrontEnd/src/services/systemUpdates.ts FrontEnd/src/services/systemUpdates.spec.ts FrontEnd/src/i18n/locales/pt.json FrontEnd/src/i18n/locales/en.json FrontEnd/src/i18n/i18n.spec.ts
git commit -m "feat: adicionar registro do histórico de atualizações"
```

---

### Task 3: Adicionar Rota, Navegação E Badge Novo

**Files:**
- Modify: `FrontEnd/src/constants/appRoutes.ts`
- Modify: `FrontEnd/src/constants/appRoutes.spec.ts`
- Modify: `FrontEnd/src/router/index.ts`
- Create: `FrontEnd/src/router/index.spec.ts`
- Create: `FrontEnd/src/views/SystemUpdatesView.vue`
- Modify: `FrontEnd/src/types/layout.ts`
- Modify: `FrontEnd/src/components/layout/AppShell.vue`
- Modify: `FrontEnd/src/components/layout/SidebarNav.vue`
- Modify: `FrontEnd/src/components/layout/SidebarNav.spec.ts`

**Interfaces:**
- Consumes: `getLatestSystemUpdate`, `readLastSeenSystemUpdate`, `markLatestSystemUpdateSeen` e `LAST_SEEN_SYSTEM_UPDATE_KEY` da Task 2.
- Produces: `AppRouteNames.Updates = 'updates'`, `AppRoutes.Updates = '/atualizacoes'` e `SidebarNavigationItem.badge?: 'new'`.

- [ ] **Step 1: Escrever testes falhos da rota e do badge**

Adicionar em `appRoutes.spec.ts`:

```ts
expect(AppRoutes.Updates).toBe('/atualizacoes')
expect(AppRouteNames.Updates).toBe('updates')
```

Criar `router/index.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import router from './index'
import { AppRouteNames } from '@/constants/appRoutes'

describe('updates route', () => {
  it('requires authentication and uses a localized title', () => {
    const route = router.getRoutes().find((candidate) => candidate.name === AppRouteNames.Updates)
    expect(route?.path).toBe('/atualizacoes')
    expect(route?.meta).toMatchObject({ requiresAuth: true, titleKey: 'routes.updates.title' })
  })
})
```

Adicionar a `SidebarNav.spec.ts` um item `updates` com `badge: 'new'` e verificar `Novo`, além de `aria-label` ou texto acessível equivalente.

- [ ] **Step 2: Executar os testes para confirmar RED**

Run: `npm test --prefix FrontEnd -- src/constants/appRoutes.spec.ts src/router/index.spec.ts src/components/layout/SidebarNav.spec.ts`

Expected: FAIL porque a rota, o campo `badge` e a renderização ainda não existem.

- [ ] **Step 3: Adicionar o shell da página, constantes e rota autenticada**

Criar primeiro `SystemUpdatesView.vue` como shell localizado, para que a rota tenha um alvo compilável antes da composição editorial da Task 5:

```vue
<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'

const { t } = useI18n()
</script>

<template>
  <PageFrame>
    <PageHeader :eyebrow="t('updates.eyebrow')" :title="t('updates.title')" :description="t('updates.description')" />
  </PageFrame>
</template>
```

Adicionar `Updates` aos dois objetos em `appRoutes.ts`. Importar `SystemUpdatesView` em `router/index.ts` e registrar:

```ts
{
  path: AppRoutes.Updates,
  name: AppRouteNames.Updates,
  component: SystemUpdatesView,
  meta: { titleKey: 'routes.updates.title', requiresAuth: true },
}
```

- [ ] **Step 4: Tornar o badge reativo no AppShell**

Adicionar `badge?: 'new'` a `SidebarNavigationItem`. Em `AppShell.vue`, manter:

```ts
const latestSystemUpdate = getLatestSystemUpdate()
const lastSeenSystemUpdate = ref(readLastSeenSystemUpdate())
const hasUnseenSystemUpdate = computed(() => lastSeenSystemUpdate.value !== latestSystemUpdate.version)

watch(
  () => route.name,
  (name) => {
    if (name === AppRouteNames.Updates) {
      lastSeenSystemUpdate.value = markLatestSystemUpdateSeen(latestSystemUpdate.version)
    }
  },
  { immediate: true },
)
```

Adicionar o item disponível após Draft:

```ts
{
  id: 'updates',
  label: t('navigation.updates'),
  icon: 'UP',
  routeName: AppRouteNames.Updates,
  path: AppRoutes.Updates,
  status: 'available',
  badge: hasUnseenSystemUpdate.value ? 'new' : undefined,
}
```

- [ ] **Step 5: Renderizar o badge localizado**

Em `SidebarNav.vue`, ao lado do status de placeholder:

```vue
<span v-if="item.badge === 'new'" class="sidebar__status sidebar__status--new">
  {{ t('navigation.new') }}
</span>
```

Usar o texto localizado como significado acessível; o badge não pode depender apenas de cor.

- [ ] **Step 6: Executar os testes para confirmar GREEN**

Run: `npm test --prefix FrontEnd -- src/constants/appRoutes.spec.ts src/router/index.spec.ts src/components/layout/SidebarNav.spec.ts`

Expected: PASS para rota autenticada, título, item de navegação e badge.

- [ ] **Step 7: Commitar navegação e rota**

```bash
git add FrontEnd/src/constants/appRoutes.ts FrontEnd/src/constants/appRoutes.spec.ts FrontEnd/src/router/index.ts FrontEnd/src/router/index.spec.ts FrontEnd/src/views/SystemUpdatesView.vue FrontEnd/src/types/layout.ts FrontEnd/src/components/layout/AppShell.vue FrontEnd/src/components/layout/SidebarNav.vue FrontEnd/src/components/layout/SidebarNav.spec.ts
git commit -m "feat: adicionar rota e indicador de atualizações"
```

---

### Task 4: Implementar O Card Editorial Acessível

**Files:**
- Create: `FrontEnd/src/components/updates/SystemUpdateCard.vue`
- Create: `FrontEnd/src/components/updates/SystemUpdateCard.spec.ts`

**Interfaces:**
- Consumes: `SystemUpdateRelease`, chaves i18n do registro e `AppRoutePath` dos links.
- Produces: componente com props `{ release: SystemUpdateRelease; latest: boolean }`, sem eventos obrigatórios.

- [ ] **Step 1: Escrever os testes falhos do card**

Criar `SystemUpdateCard.spec.ts`:

```ts
// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { i18n } from '@/i18n'
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import SystemUpdateCard from './SystemUpdateCard.vue'

describe('SystemUpdateCard', () => {
  it('renders semantic release data and all latest details', () => {
    const wrapper = mount(SystemUpdateCard, {
      props: { release: SYSTEM_UPDATES[0], latest: true },
      global: { plugins: [i18n], stubs: { RouterLink: { template: '<a><slot /></a>' } } },
    })
    expect(wrapper.get('time').attributes('datetime')).toBe('2026-07-22')
    expect(wrapper.findAll('[data-update-detail]')).toHaveLength(15)
    expect(wrapper.text()).toContain('Acesso direto ao draft pelo Discord')
  })

  it('groups details in native keyboard-accessible disclosure controls', () => {
    const wrapper = mount(SystemUpdateCard, { props: { release: SYSTEM_UPDATES[0], latest: true }, global: { plugins: [i18n] } })
    expect(wrapper.findAll('details').length).toBeGreaterThan(0)
    expect(wrapper.findAll('summary').every((summary) => summary.attributes('tabindex') === undefined)).toBe(true)
  })
})
```

- [ ] **Step 2: Executar o teste para confirmar RED**

Run: `npm test --prefix FrontEnd -- src/components/updates/SystemUpdateCard.spec.ts`

Expected: FAIL porque o componente ainda não existe.

- [ ] **Step 3: Implementar o componente**

O script deve agrupar detalhes pelas categorias declaradas na release e formatar a data com o locale ativo:

```ts
const { t, locale } = useI18n()
const groupedDetails = computed(() => props.release.categories
  .map((category) => ({ category, details: props.release.details.filter((detail) => detail.category === category) }))
  .filter((group) => group.details.length))
const formattedDate = computed(() => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'long', year: 'numeric', timeZone: 'UTC' })
  .format(new Date(`${props.release.publishedAt}T00:00:00Z`)))
```

O template deve usar `<article>`, `<time>`, heading, badges de categorias/áreas, `<details>` por grupo, `<ul>` para itens e `RouterLink` somente quando `detail.link` existir. A release mais recente recebe `system-update-card--latest`; ícones decorativos devem ter `aria-hidden="true"`.

- [ ] **Step 4: Executar o teste para confirmar GREEN**

Run: `npm test --prefix FrontEnd -- src/components/updates/SystemUpdateCard.spec.ts`

Expected: PASS com data semântica, 15 itens, agrupamento e conteúdo localizado.

- [ ] **Step 5: Commitar o card**

```bash
git add FrontEnd/src/components/updates/SystemUpdateCard.vue FrontEnd/src/components/updates/SystemUpdateCard.spec.ts
git commit -m "feat: adicionar cards editoriais de atualização"
```

---

### Task 5: Implementar Hero, Filtros E Timeline

**Files:**
- Modify: `FrontEnd/src/views/SystemUpdatesView.vue`
- Create: `FrontEnd/src/views/SystemUpdatesView.spec.ts`

**Interfaces:**
- Consumes: `SYSTEM_UPDATES`, `filterSystemUpdates`, `SystemUpdateCategory`, `SystemUpdateCard`, `PageFrame` e `PageHeader`.
- Produces: página autenticada renderizada pela rota da Task 3.

- [ ] **Step 1: Escrever testes falhos da página**

Criar `SystemUpdatesView.spec.ts` com happy-dom e i18n real:

```ts
it('renders latest hero and eight timeline releases', () => {
  const wrapper = mountView()
  expect(wrapper.get('h1').text()).toBe('Atualizações do sistema')
  expect(wrapper.findAll('[data-system-update]')).toHaveLength(8)
  expect(wrapper.get('[data-latest-update]').text()).toContain('2026.07.1')
})

it('combines search and category filters and clears them', async () => {
  const wrapper = mountView()
  await wrapper.get('input[type="search"]').setValue('Discord')
  await wrapper.get('[data-category="fix"]').trigger('click')
  expect(wrapper.findAll('[data-system-update]').length).toBeGreaterThan(0)
  await wrapper.get('[data-clear-filters]').trigger('click')
  expect(wrapper.findAll('[data-system-update]')).toHaveLength(8)
})

it('shows a localized empty state', async () => {
  const wrapper = mountView()
  await wrapper.get('input[type="search"]').setValue('conteudo inexistente 999')
  expect(wrapper.text()).toContain('Nenhuma atualização encontrada')
})
```

`mountView` deve montar `SystemUpdatesView` com `i18n`, stubs simples de `PageFrame`, `PageHeader` e `RouterLink` e limpar filtros entre testes por nova montagem.

- [ ] **Step 2: Executar o teste para confirmar RED**

Run: `npm test --prefix FrontEnd -- src/views/SystemUpdatesView.spec.ts`

Expected: FAIL porque a página ainda não existe.

- [ ] **Step 3: Implementar estado e agrupamento**

Usar somente estado local:

```ts
const query = ref('')
const activeCategory = ref<SystemUpdateCategory | 'all'>('all')
const filteredUpdates = computed(() => filterSystemUpdates(SYSTEM_UPDATES, query.value, activeCategory.value, t))
const latest = getLatestSystemUpdate()
const groupedUpdates = computed(() => Object.entries(Object.groupBy(filteredUpdates.value, (release) => release.publishedAt.slice(0, 7))))

function clearFilters() {
  query.value = ''
  activeCategory.value = 'all'
}
```

Se o target TypeScript não oferecer `Object.groupBy`, implementar `reduce<Record<string, SystemUpdateRelease[]>>` no próprio computed, sem criar dependência ou helper genérico.

- [ ] **Step 4: Implementar a composição visual e semântica**

O template deve conter:

```vue
<PageFrame>
  <PageHeader :eyebrow="t('updates.eyebrow')" :title="t('updates.title')" :description="t('updates.description')" />
  <section data-latest-update><!-- versão, data, resumo, categorias e áreas da latest --></section>
  <section :aria-label="t('updates.filterLabel')"><!-- search + chips + contador --></section>
  <div class="system-updates-layout">
    <nav class="system-updates-index" :aria-label="t('updates.versionIndex')"><!-- links #update-{id} --></nav>
    <ol class="system-updates-timeline"><!-- grupos por mês e SystemUpdateCard --></ol>
  </div>
  <section v-if="!filteredUpdates.length"><!-- empty title, description, clear button --></section>
</PageFrame>
```

Chips devem ser `<button type="button">` com `:aria-pressed`; busca deve ter `<label>` visível ou classe visualmente oculta já existente. A timeline deve usar `<ol>/<li>`, e cada card deve ter `:id="`update-${release.id}`"` e `data-system-update`.

- [ ] **Step 5: Executar o teste para confirmar GREEN**

Run: `npm test --prefix FrontEnd -- src/views/SystemUpdatesView.spec.ts src/components/updates/SystemUpdateCard.spec.ts`

Expected: PASS para hero, oito releases, busca, filtro, limpeza, agrupamento e estado vazio.

- [ ] **Step 6: Commitar a página**

```bash
git add FrontEnd/src/views/SystemUpdatesView.vue FrontEnd/src/views/SystemUpdatesView.spec.ts
git commit -m "feat: implementar timeline de atualizações"
```

---

### Task 6: Finalizar Estilos, Documentação E Verificação

**Files:**
- Modify: `FrontEnd/src/styles/main.css`
- Create: `docs/standards/SYSTEM_UPDATES.md`
- Modify: `docs/standards/FEATURE_CHECKLIST.md`
- Modify: `docs/standards/README.md`

**Interfaces:**
- Consumes: classes `system-updates-*`, `system-update-card*` e `sidebar__status--new` das Tasks 3 a 5.
- Produces: layout desktop/mobile final e processo editorial obrigatório para releases futuras.

- [ ] **Step 1: Adicionar estilos usando apenas tokens existentes**

Acrescentar um bloco dedicado ao fim de `main.css`. Usar:

```css
.system-updates-layout { display: grid; grid-template-columns: minmax(0, 1fr) 220px; gap: var(--space-xl); }
.system-updates-index { position: sticky; top: var(--space-lg); align-self: start; }
.system-updates-timeline { position: relative; display: grid; gap: var(--space-xl); margin: 0; padding: 0; list-style: none; }
.system-update-card { border: 1px solid var(--color-hairline); border-radius: var(--radius-xl); background: var(--color-surface-1); }
.system-update-card--latest { border-color: var(--color-primary); box-shadow: 0 0 0 1px var(--color-primary-soft); }
.sidebar__status--new { color: var(--color-secondary-hover); background: var(--color-secondary-soft); }
```

Completar hero, chips, eixo/marcador da timeline, badges, summaries, links, hover e `:focus-visible` usando `--color-*`, `--space-*`, `--radius-*`, `--duration-*` e `--ease-standard`. Não usar hex, pixels de espaçamento fora dos breakpoints existentes nem gradiente decorativo novo.

- [ ] **Step 2: Implementar os breakpoints aprovados**

Nos blocos existentes de `max-width: 1024px`, `760px` e `480px`:

```css
@media (max-width: 1024px) {
  .system-updates-layout { grid-template-columns: minmax(0, 1fr); }
  .system-updates-index { display: none; }
}

@media (max-width: 760px) {
  .system-updates-filters { overflow-x: auto; }
  .system-updates-timeline { padding-inline-start: var(--space-md); }
  .system-update-card { width: 100%; }
}

@media (max-width: 480px) {
  .system-updates-hero, .system-update-card { border-radius: var(--radius-lg); }
  .system-update-card__header { padding: var(--space-md); }
}
```

Confirmar que não há overflow horizontal a 320px e que alvos de toque usam ao menos `--control-height-md`.

- [ ] **Step 3: Criar o guia editorial**

Criar `docs/standards/SYSTEM_UPDATES.md` com o fluxo:

```markdown
1. Decidir se a mudança é visível ou relevante ao usuário.
2. Incrementar `AAAA.MM.N` dentro do mês da publicação.
3. Adicionar metadados e somente chaves i18n ao registro.
4. Escrever título curto e benefício prático em português e inglês.
5. Classificar cada detalhe em uma categoria e a release em áreas afetadas.
6. Usar link interno apenas quando houver uma ação útil e rota existente.
7. Executar testes de contrato, i18n, interface e build.
8. Commitar a entrada junto da mudança; agrupar alterações internas pequenas para evitar ruído.
```

Incluir exemplo completo de uma release com um detalhe e proibir segredos, IDs operacionais, payloads, mensagens de commit e jargão de infraestrutura sem benefício explicado.

- [ ] **Step 4: Integrar o guia ao workflow**

Adicionar em `FEATURE_CHECKLIST.md`, antes de “Antes do commit”:

```markdown
## Histórico de atualizações

- [ ] Avaliar se a mudança visível exige entrada em `FrontEnd/src/constants/systemUpdates.ts`.
- [ ] Confirmar que cada melhoria aparece individualmente quando usuários precisam identificá-la.
- [ ] Atualizar português e inglês seguindo `docs/standards/SYSTEM_UPDATES.md`.
- [ ] Agrupar mudanças exclusivamente internas quando uma entrada isolada gerar ruído editorial.
```

Adicionar `SYSTEM_UPDATES.md` ao índice de `docs/standards/README.md`.

- [ ] **Step 5: Executar verificação automatizada completa**

Run:

```bash
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint --prefix FrontEnd
git diff --check
```

Expected: todos os testes PASS, `vue-tsc` e Vite concluem sem erros, ESLint conclui sem erros e `git diff --check` não produz saída.

- [ ] **Step 6: Executar auditoria de internacionalização**

Verificar e registrar:

```text
Frontend hardcoded texts: Não encontrados.
Backend hardcoded messages: Não encontrados; backend não foi alterado.
pt.json e en.json sincronizados: Sim.
Backend resources atualizados: Não aplicável; backend não foi alterado.
Acentuação portuguesa revisada: Sim.
Placeholders, botões, títulos, badges, links e estados vazios revisados: Sim.
Validações frontend/backend usam i18n/resource: Sim; validações novas são estruturais e textos são i18n.
Novos arquivos respeitam o padrão: Sim.
```

Qualquer resposta diferente invalida a conclusão da tarefa.

- [ ] **Step 7: Verificar a interface em desktop e mobile**

Executar a aplicação, autenticar e validar `/atualizacoes` em 1440x900, 768x1024 e 390x844. Confirmar: oito releases, 15 detalhes recentes, índice desktop, timeline mobile, busca, chips, estado vazio, expansão por teclado, links, foco visível, ausência de overflow e remoção imediata do badge `Novo`.

- [ ] **Step 8: Commitar documentação e acabamento**

```bash
git add FrontEnd/src/styles/main.css docs/standards/SYSTEM_UPDATES.md docs/standards/FEATURE_CHECKLIST.md docs/standards/README.md
git commit -m "docs: adicionar processo editorial de atualizações"
```

- [ ] **Step 9: Revisar o conjunto final**

Run:

```bash
git status --short
git log --oneline -8
git diff HEAD~5..HEAD --check
```

Expected: somente alterações intencionais da feature 019 estão commitadas; `docs/prompts/` e `specs/018-importacao-partidas-lcu/` continuam fora dos commits; nenhum erro de whitespace.

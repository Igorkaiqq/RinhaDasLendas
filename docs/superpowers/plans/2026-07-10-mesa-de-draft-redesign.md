# Mesa de Draft Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the full frontend around the approved Mesa de Draft direction using shadcn-vue, while preserving existing behavior, i18n, accessibility, tests and the dark-first competitive SaaS identity.

**Architecture:** Use shadcn-vue as the component primitive layer and keep product-specific identity in `FrontEnd/src/styles/main.css`, design tokens and composed Vue views. Migrate incrementally: setup/token bridge first, then shared layout primitives, then Drafts as the identity anchor, then the remaining screens.

**Tech Stack:** Vue 3, TypeScript, Vite, Vue Router, Vue I18n, Vitest, shadcn-vue, Reka UI, Tailwind/shadcn CSS variables if introduced by shadcn-vue, existing CSS token file `FrontEnd/src/styles/main.css`.

## Global Constraints

- Work in `FrontEnd/` only unless documentation under `docs/` is explicitly mentioned.
- Do not change backend behavior for this redesign unless a UI migration exposes a missing API contract.
- Do not hardcode user-visible text in Vue components; add every new label/message to `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json`.
- Preserve Portuguese accents.
- Preserve existing tests and add/update tests for navigation, i18n parity and critical screen actions when markup changes.
- Run `npm test` and `npm run build` in `FrontEnd/` after each meaningful migration phase.
- Use shadcn-vue primitives before custom markup where practical.
- Do not overwrite the Mesa de Draft identity with a stock shadcn-vue preset.
- Body typography remains `Hanken Grotesk`.
- Display typography uses `Space Grotesk`.
- Data/status typography uses `JetBrains Mono` only for compact operational facts.
- Palette: `rift-void #070A12`, `panel-smoke #101522`, `lane-slate #1B2433`, `spell-violet #7C3AED`, `summoner-blue #38BDF8`, `ban-gold #C8A24A`.
- `ban-gold` is restricted to captain, pick/ban, publication attention or decisive moments.
- Commit steps are intentionally omitted because this repository is currently being worked without commits unless explicitly requested by the user.

---

## File Structure Map

### Created or Initialized

- `FrontEnd/components.json` - shadcn-vue project configuration.
- `FrontEnd/src/lib/utils.ts` - `cn()` utility generated/used by shadcn-vue.
- `FrontEnd/src/components/ui/*` - shadcn-vue primitives added by CLI.
- `FrontEnd/src/components/layout/DraftRail.vue` - reusable Mesa de Draft state rail.
- `FrontEnd/src/components/layout/PageFrame.vue` - shared page shell content frame for non-auth pages.
- `FrontEnd/src/components/layout/PageHeader.vue` - shared contextual header component.
- `FrontEnd/src/components/layout/StatusBadge.vue` - thin wrapper around shadcn `Badge` for product statuses.
- `FrontEnd/src/components/layout/EmptyState.vue` - product empty state wrapper using shadcn `Empty` if available or composed `Card`/`Button`.
- `FrontEnd/src/components/layout/MesaToastHost.vue` - toast host if `vue-sonner` is introduced.
- `FrontEnd/src/components/drafts/DraftStateRail.vue` - draft-specific adapter around `DraftRail.vue`.

### Modified

- `FrontEnd/package.json` - dependencies/scripts updated by shadcn-vue and optional icon/toast packages.
- `FrontEnd/src/styles/main.css` - token bridge, Mesa de Draft theme, legacy cleanup.
- `FrontEnd/src/main.ts` - imports toast styles/provider if needed.
- `FrontEnd/src/App.vue` - shell/toast host integration if needed.
- `FrontEnd/src/components/layout/AppShell.vue` - layout frame and responsive behavior.
- `FrontEnd/src/components/layout/SidebarNav.vue` - navigation visual redesign.
- `FrontEnd/src/views/DraftsView.vue` - anchor screen migration to Draft Rail and shadcn primitives.
- `FrontEnd/src/views/HomeView.vue` - hero and workflow redesign.
- `FrontEnd/src/views/LoginView.vue`, `RegisterView.vue`, `ForgotPasswordView.vue`, `ResetPasswordView.vue` - auth visual simplification.
- `FrontEnd/src/views/PlayersView.vue`, `ProfileView.vue`, `TeamsView.vue`, `UsersAdminView.vue`, `SettingsView.vue`, `ForbiddenView.vue`, `PlaceholderView.vue` - page migration to shared frame and shadcn primitives.
- `FrontEnd/src/i18n/locales/pt.json` and `FrontEnd/src/i18n/locales/en.json` - new UI copy.
- Existing component tests under `FrontEnd/src/**/*.spec.ts` - update selectors/expectations as markup changes.

---

### Task 1: Initialize shadcn-vue Safely

**Files:**
- Create: `FrontEnd/components.json`
- Create: `FrontEnd/src/lib/utils.ts`
- Create: `FrontEnd/src/components/ui/*`
- Modify: `FrontEnd/package.json`
- Modify: `FrontEnd/src/styles/main.css`

**Interfaces:**
- Produces: shadcn-vue import aliases under `@/components/ui/*` and `cn(...classes: ClassValue[]): string` from `@/lib/utils`.
- Consumes: current Vite/Vue app in `FrontEnd/`.

- [ ] **Step 1: Capture current baseline**

Run:

```bash
npm test
npm run build
```

Workdir: `FrontEnd/`

Expected: current frontend tests and build pass before any setup change.

- [ ] **Step 2: Initialize shadcn-vue without applying a stock identity**

Run from `FrontEnd/`:

```bash
npx shadcn-vue@latest init
```

Choices to use if prompted:

```text
TypeScript: yes
Framework: Vite
Style/preset: default/minimal option, then override tokens manually
Base color: neutral/zinc equivalent
CSS file: src/styles/main.css
Components alias: @/components
Utils alias: @/lib/utils
```

Expected: `components.json` and `src/lib/utils.ts` are created. Do not accept any option that overwrites all application CSS without review.

- [ ] **Step 3: Add initial shadcn-vue components**

Run from `FrontEnd/`:

```bash
npx shadcn-vue@latest add button card badge dialog sheet input select table tabs alert skeleton separator
```

Expected: component files appear under `FrontEnd/src/components/ui/`.

- [ ] **Step 4: Add toast support**

Run from `FrontEnd/`:

```bash
npm install vue-sonner
```

Expected: `vue-sonner` is listed in `dependencies`.

- [ ] **Step 5: Verify generated files**

Read `FrontEnd/src/lib/utils.ts` and a sample of generated components. Confirm imports use `@/` aliases and no component contains hardcoded app copy.

- [ ] **Step 6: Run setup verification**

Run from `FrontEnd/`:

```bash
npm test
npm run build
```

Expected: tests and build pass after setup. Fix only setup/import/type errors in this task.

---

### Task 2: Add Mesa de Draft Token Bridge

**Files:**
- Modify: `FrontEnd/src/styles/main.css`
- Modify: `docs/design/DESIGN_TOKENS.md`
- Test: `FrontEnd/src/i18n/i18n.spec.ts` remains passing

**Interfaces:**
- Consumes: shadcn-vue CSS variable expectations and current CSS custom properties.
- Produces: stable CSS tokens for Mesa de Draft and shadcn semantic variables.

- [ ] **Step 1: Add Mesa variables in `:root`**

Modify `FrontEnd/src/styles/main.css` near the existing token block:

```css
:root {
  --mesa-rift-void: #070a12;
  --mesa-panel-smoke: #101522;
  --mesa-lane-slate: #1b2433;
  --mesa-spell-violet: #7c3aed;
  --mesa-summoner-blue: #38bdf8;
  --mesa-ban-gold: #c8a24a;
  --mesa-ban-gold-soft: rgb(200 162 74 / 14%);
  --font-display: 'Space Grotesk', 'Hanken Grotesk', Inter, ui-sans-serif, system-ui, sans-serif;
  --font-body: 'Hanken Grotesk', Inter, ui-sans-serif, system-ui, sans-serif;
  --font-data: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
}
```

Then map existing variables:

```css
:root {
  --color-canvas: var(--mesa-rift-void);
  --color-canvas-raised: #0b1020;
  --color-surface-1: var(--mesa-panel-smoke);
  --color-surface-2: var(--mesa-lane-slate);
  --color-primary: var(--mesa-spell-violet);
  --color-secondary: var(--mesa-summoner-blue);
}
```

- [ ] **Step 2: Add shadcn semantic variable bridge**

If shadcn-vue generated CSS variables, make them point to the Mesa values. Use the format generated by the CLI. For Tailwind v4-style variables, add:

```css
:root {
  --background: var(--mesa-rift-void);
  --foreground: #fafafa;
  --card: var(--mesa-panel-smoke);
  --card-foreground: #fafafa;
  --popover: var(--mesa-panel-smoke);
  --popover-foreground: #fafafa;
  --primary: var(--mesa-spell-violet);
  --primary-foreground: #ffffff;
  --secondary: var(--mesa-lane-slate);
  --secondary-foreground: #fafafa;
  --muted: #151c2a;
  --muted-foreground: #9aa7b7;
  --accent: var(--mesa-ban-gold);
  --accent-foreground: #0b0e14;
  --destructive: #ef4444;
  --border: #263044;
  --input: #263044;
  --ring: rgb(124 58 237 / 42%);
  --radius: 0.875rem;
}
```

- [ ] **Step 3: Update design tokens doc**

Add a “Mesa de Draft extension” section to `docs/design/DESIGN_TOKENS.md` with the six approved colors and usage rule for `ban-gold`.

- [ ] **Step 4: Verify**

Run from `FrontEnd/`:

```bash
npm run build
```

Expected: build passes with no CSS syntax errors.

---

### Task 3: Build Shared Layout Primitives

**Files:**
- Create: `FrontEnd/src/components/layout/PageFrame.vue`
- Create: `FrontEnd/src/components/layout/PageHeader.vue`
- Create: `FrontEnd/src/components/layout/StatusBadge.vue`
- Create: `FrontEnd/src/components/layout/EmptyState.vue`
- Create: `FrontEnd/src/components/layout/MesaToastHost.vue`
- Modify: `FrontEnd/src/main.ts` or `FrontEnd/src/App.vue` if toast host requires app-level placement
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Produces: reusable components used by all later page migrations.
- Consumes: shadcn components from `@/components/ui/*` and Vue slots.

- [ ] **Step 1: Create `PageFrame.vue`**

Use this interface:

```vue
<script setup lang="ts">
defineProps<{
  rail?: boolean
}>()
</script>

<template>
  <main class="mesa-page-frame" :data-has-rail="rail || undefined">
    <slot />
  </main>
</template>
```

Add CSS in `main.css`:

```css
.mesa-page-frame {
  display: flex;
  flex-direction: column;
  gap: var(--space-xl);
  width: 100%;
  min-height: 100%;
}
```

- [ ] **Step 2: Create `PageHeader.vue`**

Use this interface:

```vue
<script setup lang="ts">
defineProps<{
  eyebrow?: string
  title: string
  description?: string
}>()
</script>

<template>
  <header class="mesa-page-header">
    <div class="mesa-page-header__copy">
      <p v-if="eyebrow" class="mesa-eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p v-if="description">{{ description }}</p>
    </div>
    <div v-if="$slots.actions" class="mesa-page-header__actions">
      <slot name="actions" />
    </div>
  </header>
</template>
```

- [ ] **Step 3: Create `StatusBadge.vue`**

Use shadcn `Badge` and this interface:

```vue
<script setup lang="ts">
import { Badge } from '@/components/ui/badge'

defineProps<{
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'gold'
}>()
</script>

<template>
  <Badge class="mesa-status-badge" :data-tone="tone || 'neutral'">
    <slot />
  </Badge>
</template>
```

Add CSS using semantic variables and `--mesa-ban-gold` for `data-tone='gold'`.

- [ ] **Step 4: Create `EmptyState.vue`**

Interface:

```vue
<script setup lang="ts">
defineProps<{
  title: string
  description: string
}>()
</script>

<template>
  <section class="mesa-empty-state">
    <div class="mesa-empty-state__mark" aria-hidden="true">◇</div>
    <h2>{{ title }}</h2>
    <p>{{ description }}</p>
    <div v-if="$slots.action" class="mesa-empty-state__action">
      <slot name="action" />
    </div>
  </section>
</template>
```

- [ ] **Step 5: Create `MesaToastHost.vue`**

Use `vue-sonner`:

```vue
<script setup lang="ts">
import { Toaster } from 'vue-sonner'
import 'vue-sonner/style.css'
</script>

<template>
  <Toaster rich-colors position="top-right" />
</template>
```

Mount it once in `App.vue` outside `RouterView`.

- [ ] **Step 6: Verify**

Run from `FrontEnd/`:

```bash
npm test
npm run build
```

Expected: tests and build pass.

---

### Task 4: Redesign App Shell and Sidebar

**Files:**
- Modify: `FrontEnd/src/components/layout/AppShell.vue`
- Modify: `FrontEnd/src/components/layout/SidebarNav.vue`
- Modify: `FrontEnd/src/components/layout/SidebarNav.spec.ts`
- Modify: `FrontEnd/src/styles/main.css`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Consumes: `PageFrame`, `StatusBadge`, existing `SidebarNavigationItem`.
- Produces: stable shell layout for all private routes.

- [ ] **Step 1: Update sidebar copy keys if needed**

Ensure these keys exist in both locale files:

```json
{
  "navigation": {
    "arena": "Arena",
    "operations": "Operações",
    "account": "Conta"
  }
}
```

Use English equivalents in `en.json`.

- [ ] **Step 2: Replace symbolic toggle treatment with a cleaner control**

Keep the current text toggle if no icon library exists yet, but restyle it as a compact control. Do not introduce icons in this task unless shadcn setup already selected a library.

- [ ] **Step 3: Restyle sidebar as arena console**

Add CSS classes:

```css
.sidebar {
  background:
    linear-gradient(180deg, rgb(56 189 248 / 7%), transparent 22rem),
    var(--color-sidebar);
  border-right: 1px solid var(--color-hairline-soft);
}

.sidebar__item--active {
  background: linear-gradient(90deg, rgb(124 58 237 / 18%), rgb(56 189 248 / 8%));
  border-color: rgb(124 58 237 / 36%);
}
```

- [ ] **Step 4: Update tests**

In `SidebarNav.spec.ts`, assert that navigation labels still render and active route state still applies. Do not assert fragile visual class internals except the active state class.

- [ ] **Step 5: Verify**

Run from `FrontEnd/`:

```bash
npm test -- src/components/layout/SidebarNav.spec.ts
npm run build
```

Expected: sidebar test and build pass.

---

### Task 5: Build Draft Rail Components

**Files:**
- Create: `FrontEnd/src/components/layout/DraftRail.vue`
- Create: `FrontEnd/src/components/drafts/DraftStateRail.vue`
- Create: `FrontEnd/src/components/drafts/DraftStateRail.spec.ts`
- Modify: `FrontEnd/src/constants/draftMontagem/draftMontagemStatusValues.ts` only if needed for exported status constants
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/styles/main.css`

**Interfaces:**
- Produces: `DraftRailStep` type and `DraftStateRail` component for `DraftsView.vue`.
- Consumes: `DraftMontagemStatus` and publication status from `DraftMontagem`.

- [ ] **Step 1: Write failing test**

Create `DraftStateRail.spec.ts`:

```ts
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import DraftStateRail from './DraftStateRail.vue'

describe('DraftStateRail', () => {
  it('marks the current draft status as active', () => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'CapitaesDefinidos', publicationStatus: 'Pendente' },
      global: { mocks: { $t: (key: string) => key } },
    })

    expect(wrapper.find('[data-state="active"]').text()).toContain('drafts.rail.captains')
  })
})
```

Run:

```bash
npm test -- src/components/drafts/DraftStateRail.spec.ts
```

Expected: fails because component does not exist.

- [ ] **Step 2: Create `DraftRail.vue`**

Interface:

```vue
<script setup lang="ts">
export interface DraftRailStep {
  id: string
  label: string
  state: 'done' | 'active' | 'pending' | 'attention'
}

defineProps<{ steps: DraftRailStep[] }>()
</script>

<template>
  <ol class="draft-rail" :aria-label="$attrs['aria-label'] as string">
    <li v-for="step in steps" :key="step.id" class="draft-rail__step" :data-state="step.state">
      <span class="draft-rail__node" aria-hidden="true" />
      <span>{{ step.label }}</span>
    </li>
  </ol>
</template>
```

- [ ] **Step 3: Create `DraftStateRail.vue`**

Interface:

```vue
<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import DraftRail, { type DraftRailStep } from '@/components/layout/DraftRail.vue'

const props = defineProps<{
  status: string
  publicationStatus?: string | null
}>()

const { t } = useI18n()

const order = ['PresencaAberta', 'PresencaEncerrada', 'CapitaesDefinidos', 'OrdemDefinida', 'Aberta', 'Finalizada']
const labels: Record<string, string> = {
  PresencaAberta: 'drafts.rail.presenceOpen',
  PresencaEncerrada: 'drafts.rail.presenceClosed',
  CapitaesDefinidos: 'drafts.rail.captains',
  OrdemDefinida: 'drafts.rail.order',
  Aberta: 'drafts.rail.picking',
  Finalizada: 'drafts.rail.finished',
}

const steps = computed<DraftRailStep[]>(() => {
  const activeIndex = Math.max(order.indexOf(props.status), 0)
  const base = order.map((status, index) => ({
    id: status,
    label: t(labels[status]),
    state: index < activeIndex ? 'done' : index === activeIndex ? 'active' : 'pending',
  }) satisfies DraftRailStep)

  base.push({
    id: 'discord',
    label: t('drafts.rail.discord'),
    state: props.publicationStatus === 'Falha' || props.publicationStatus === 'Pendente' ? 'attention' : props.publicationStatus === 'Publicada' ? 'done' : 'pending',
  })
  return base
})
</script>

<template>
  <DraftRail :steps="steps" :aria-label="t('drafts.rail.label')" />
</template>
```

- [ ] **Step 4: Add i18n keys**

Add to both locale files under `drafts.rail`:

```json
{
  "label": "Fluxo do draft",
  "presenceOpen": "Presença aberta",
  "presenceClosed": "Presença encerrada",
  "captains": "Capitães",
  "order": "Ordem",
  "picking": "Escolhas",
  "finished": "Finalizado",
  "discord": "Discord"
}
```

Use English equivalents in `en.json`.

- [ ] **Step 5: Add rail CSS**

Add CSS in `main.css`:

```css
.draft-rail {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
  padding: var(--space-sm);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-xl);
  background: rgb(16 21 34 / 78%);
}

.draft-rail__step {
  align-items: center;
  display: inline-flex;
  gap: var(--space-xs);
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.draft-rail__step[data-state='active'] { color: var(--color-ink); }
.draft-rail__step[data-state='done'] { color: var(--color-success); }
.draft-rail__step[data-state='attention'] { color: var(--mesa-ban-gold); }
.draft-rail__node { inline-size: 8px; block-size: 8px; border-radius: 999px; background: currentColor; }
```

- [ ] **Step 6: Verify**

Run:

```bash
npm test -- src/components/drafts/DraftStateRail.spec.ts
npm run build
```

Expected: test and build pass.

---

### Task 6: Migrate DraftsView as the Identity Anchor

**Files:**
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/services/draftMontagens.spec.ts` if selectors/copy expectations change
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/styles/main.css`

**Interfaces:**
- Consumes: `PageFrame`, `PageHeader`, `DraftStateRail`, shadcn `Button`, `Card`, `Badge`, `Input`, `Select`, `Alert`, `Separator`, `Skeleton`.
- Produces: redesigned Drafts page preserving all existing actions and data flows.

- [ ] **Step 1: Write/update critical tests before markup migration**

Ensure `draftMontagens.spec.ts` still covers:

```ts
expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1')
expect(api.post).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/discord/publicacoes/republicar', { tipo: 'TimesDefinidos', motivo: 'permissão corrigida' })
expect(api.delete).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/presencas/jogador-1', { data: { motivo: 'não poderá jogar' } })
```

Run:

```bash
npm test -- src/services/draftMontagens.spec.ts
```

Expected: tests pass before view migration.

- [ ] **Step 2: Add `DraftStateRail` to selected draft panel**

In `DraftsView.vue`, import:

```ts
import DraftStateRail from '@/components/drafts/DraftStateRail.vue'
```

Add computed publication status:

```ts
const finalTeamsPublicationStatus = computed(() => selectedMontagem.value?.publicacoesDiscord?.find((publication) => publication.tipo === 'TimesDefinidos')?.status ?? null)
```

Render after selected draft header:

```vue
<DraftStateRail
  v-if="selectedMontagem"
  :status="selectedMontagem.status"
  :publication-status="finalTeamsPublicationStatus"
/>
```

- [ ] **Step 3: Convert action buttons to shadcn `Button`**

Replace custom button classes for primary draft actions with `Button` variants. Keep action names unchanged:

```vue
<Button type="button" :disabled="saving" @click="confirmPresence">
  {{ t('drafts.presence.confirm') }}
</Button>
```

Use `variant="secondary"` for secondary actions and `variant="destructive"` for destructive actions if available.

- [ ] **Step 4: Convert status spans to `StatusBadge`**

Use `StatusBadge` for draft status and publication status. Map `Falha`/`Pendente` to `gold` or `warning` only when it requires attention.

- [ ] **Step 5: Reorganize layout into primary work surface and contextual side panel**

Keep existing data and methods. Structure template:

```vue
<PageFrame rail>
  <PageHeader ... />
  <section class="drafts-mesa-layout">
    <aside class="drafts-mesa-list">...</aside>
    <main class="drafts-mesa-board">...</main>
    <aside class="drafts-mesa-context">...</aside>
  </section>
</PageFrame>
```

On mobile, stack list, board and context.

- [ ] **Step 6: Preserve prompts and i18n**

Confirm no prompt text is hardcoded. Calls should remain:

```ts
t('drafts.cancelReasonPrompt')
t('drafts.presence.removeManualReasonPrompt')
t('drafts.publication.republishReasonPrompt')
```

- [ ] **Step 7: Verify**

Run:

```bash
npm test -- src/services/draftMontagens.spec.ts src/services/draftMontagemRealtime.spec.ts
npm run build
```

Expected: tests and build pass.

---

### Task 7: Redesign Home and Auth Screens

**Files:**
- Modify: `FrontEnd/src/views/HomeView.vue`
- Modify: `FrontEnd/src/views/LoginView.vue`
- Modify: `FrontEnd/src/views/RegisterView.vue`
- Modify: `FrontEnd/src/views/ForgotPasswordView.vue`
- Modify: `FrontEnd/src/views/ResetPasswordView.vue`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/styles/main.css`

**Interfaces:**
- Consumes: `PageFrame`, `PageHeader`, shadcn `Card`, `Button`, `Input`, `Alert`.
- Produces: entry/auth screens that introduce Mesa de Draft identity without visual noise.

- [ ] **Step 1: Preserve auth behavior tests/build baseline**

Run:

```bash
npm test
```

Expected: existing tests pass before migration.

- [ ] **Step 2: Rewrite Home hero around workflow**

Keep all user-visible copy in i18n. Add keys under `home.mesa`:

```json
{
  "thesis": "Organize presença, capitães e escolhas em uma mesa só.",
  "railLabel": "Fluxo da rinha",
  "presence": "Presença",
  "captains": "Capitães",
  "draft": "Draft",
  "discord": "Discord"
}
```

Use English equivalents in `en.json`.

- [ ] **Step 3: Simplify auth visual noise**

Reduce multiple gradients/glows to one table accent line. Keep logo and form behavior unchanged.

- [ ] **Step 4: Convert auth forms to shadcn primitives where low risk**

Use `Button`, `Input`, `Alert` first. Defer full form FieldGroup migration if it would require broad test rewrites.

- [ ] **Step 5: Verify**

Run:

```bash
npm test
npm run build
```

Expected: tests and build pass.

---

### Task 8: Migrate Players, Teams and Profile

**Files:**
- Modify: `FrontEnd/src/views/PlayersView.vue`
- Modify: `FrontEnd/src/views/TeamsView.vue`
- Modify: `FrontEnd/src/views/ProfileView.vue`
- Modify: `FrontEnd/src/components/players/*` as needed
- Modify: `FrontEnd/src/components/teams/*` as needed
- Modify: `FrontEnd/src/components/users/*` as needed
- Modify: related specs under `FrontEnd/src/components/**/*.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Consumes: shared layout primitives and shadcn `Card`, `Badge`, `Table`, `Button`, `Dialog`, `Sheet`, `Input`, `Select`.
- Produces: roster and lineup views aligned with Mesa de Draft.

- [ ] **Step 1: Update tests for stable behavior, not styling**

Before changing markup, identify tests that assert exact class names. Replace them with role/text/action assertions when possible.

- [ ] **Step 2: Players page migration**

Use page frame/header. Player rows/cards should foreground:

```text
display name | rank | preferred routes | status | actions
```

Use `StatusBadge` for player status and compact lane markers for route preferences.

- [ ] **Step 3: Teams page migration**

Team cards should foreground:

```text
team tag | team name | captain | active/inactive state | actions
```

Use shadcn `Dialog` for destructive confirmation and `Sheet` for edit/create if the existing modal can be migrated without changing behavior.

- [ ] **Step 4: Profile page migration**

Make account/player profile sections quieter. Use `Card` sections and `Alert` for pending profile notice.

- [ ] **Step 5: Verify targeted tests**

Run:

```bash
npm test -- src/components/teams/TeamFormModal.spec.ts src/components/teams/TeamDeleteDialog.spec.ts src/components/teams/TeamList.spec.ts src/services/meuJogador.spec.ts
npm run build
```

Expected: targeted tests and build pass.

---

### Task 9: Migrate Admin, Settings, Placeholder and Error Screens

**Files:**
- Modify: `FrontEnd/src/views/UsersAdminView.vue`
- Modify: `FrontEnd/src/views/SettingsView.vue`
- Modify: `FrontEnd/src/views/PlaceholderView.vue`
- Modify: `FrontEnd/src/views/ForbiddenView.vue`
- Modify: `FrontEnd/src/components/users/UserList.vue`
- Modify: `FrontEnd/src/components/users/UserFilters.vue`
- Modify: `FrontEnd/src/components/users/UserDetailsModal.vue`
- Modify: `FrontEnd/src/components/users/UserRolesEditor.vue`
- Modify: `FrontEnd/src/components/users/UserStatusConfirmDialog.vue`
- Modify: `FrontEnd/src/components/users/DiscordAdminConfigurationSection.vue`
- Modify: related i18n files

**Interfaces:**
- Consumes: `PageFrame`, `PageHeader`, `StatusBadge`, `EmptyState`, shadcn data/form primitives.
- Produces: quieter administrative surfaces aligned with the system identity.

- [ ] **Step 1: Convert admin lists to shadcn table/card patterns**

Use `Table` for user admin lists where tabular scanning matters. Use `Badge` for roles/status.

- [ ] **Step 2: Convert configuration sections to cards and alerts**

Use `Card` for each settings group and `Alert` for configuration guidance/errors.

- [ ] **Step 3: Convert placeholders and forbidden screen**

Use `EmptyState` for placeholders and forbidden access. Copy must be directional: explain what happened and where to go next.

- [ ] **Step 4: Verify**

Run:

```bash
npm test
npm run build
```

Expected: full frontend tests and build pass.

---

### Task 10: Legacy CSS Cleanup and Final Audit

**Files:**
- Modify: `FrontEnd/src/styles/main.css`
- Modify: `docs/design/DESIGN_SYSTEM.md`
- Modify: `docs/design/DESIGN_TOKENS.md`
- Modify: `docs/design/UI_GUIDELINES.md`
- Modify: `docs/superpowers/specs/2026-07-10-mesa-de-draft-redesign.md` only if implementation reveals a needed clarification

**Interfaces:**
- Consumes: all migrated screens and shared primitives.
- Produces: cleaner CSS and updated design documentation.

- [ ] **Step 1: Remove dead legacy CSS only after verifying usage**

Search for old classes before deleting. For each candidate, verify no Vue file uses it.

Run from repo root:

```bash
rg "old-class-name" FrontEnd/src
```

Expected: no results before deleting a class.

- [ ] **Step 2: Consolidate duplicated component CSS**

Move repeated page header/card/list styles into `.mesa-*` classes. Do not merge unrelated styles just because they look similar.

- [ ] **Step 3: Update design docs**

Add Mesa de Draft as the current visual direction in:

```text
docs/design/DESIGN_SYSTEM.md
docs/design/DESIGN_TOKENS.md
docs/design/UI_GUIDELINES.md
```

Include:

- Draft Rail as signature device;
- ban-gold usage limit;
- shadcn-vue as component primitive layer;
- Hanken Grotesk + Space Grotesk + JetBrains Mono roles.

- [ ] **Step 4: i18n audit**

Verify no new user-visible strings are hardcoded in migrated Vue files. Check likely patterns manually and with search:

```bash
rg ">[A-Za-zÀ-ÿ][^<{]*<|placeholder=\"[A-Za-zÀ-ÿ]|title=\"[A-Za-zÀ-ÿ]|aria-label=\"[A-Za-zÀ-ÿ]" FrontEnd/src
```

Expected: no new hardcoded user-facing copy; existing false positives must be inspected.

- [ ] **Step 5: Final verification**

Run from `FrontEnd/`:

```bash
npm test
npm run build
```

Expected: all frontend tests and build pass.

---

## Self-Review

### Spec Coverage

- Full-system scope: covered by Tasks 3-10.
- shadcn-vue adoption: covered by Tasks 1, 3 and all migration tasks.
- Mesa de Draft identity: covered by Tasks 2, 5, 6 and 10.
- Draft Rail: covered by Task 5 and Task 6.
- Cleaner/humanized interface: covered by page migrations and typography/token decisions.
- i18n: covered globally and in each task that adds copy.
- Accessibility/responsiveness: covered globally and in layout/shell tasks.
- Reduced motion: covered in Task 5 CSS and Task 10 audit.
- Testing/build verification: covered in every task.

### Placeholder Scan

No `TBD`, `TODO`, `implement later`, or unspecified placeholders are intentionally present. Conditional implementation is limited to CLI-generated shadcn-vue structure where the exact generated CSS format depends on the CLI output; the plan provides the exact semantic values that must be preserved.

### Type Consistency

- `DraftRailStep` is defined in Task 5 and consumed by `DraftRail.vue`/`DraftStateRail.vue`.
- `PageFrame`, `PageHeader`, `StatusBadge`, `EmptyState`, `MesaToastHost` are defined in Task 3 and consumed later.
- `DraftStateRail` props are `status: string` and `publicationStatus?: string | null`, matching `DraftMontagem.status` and publication statuses already used in `DraftsView.vue`.

---

## Execution Notes

- This plan intentionally does not include git commit steps because the current collaboration has been operating without commits unless explicitly requested.
- If implementation starts in this session, use `superpowers:subagent-driven-development` for task isolation or `superpowers:executing-plans` for inline execution.

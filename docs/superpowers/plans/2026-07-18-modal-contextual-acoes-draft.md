# Modal Contextual para Ações do Draft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir os quatro `window.prompt` da tela de drafts por um modal contextual reutilizável para cancelamento, remoção manual e republicações Discord.

**Architecture:** Um componente especializado `DraftReasonDialog.vue`, composto pelo Dialog existente de Reka UI, recebe uma união discriminada que determina conteúdo, contexto e variante. `DraftsView.vue` mantém uma única ação pendente e despacha a confirmação para os serviços já existentes, sem alterar contratos backend.

**Tech Stack:** Vue 3, TypeScript, Composition API, Vue I18n, Reka UI, shadcn-vue components, Vitest, Vue Test Utils, happy-dom.

## Global Constraints

- Seguir `docs/design/DESIGN_SYSTEM.md`, `docs/design/DESIGN_TOKENS.md` e `docs/design/UI_GUIDELINES.md`.
- Não criar novos tokens de cor, espaçamento, tipografia, raio ou sombra.
- Não adicionar dependências; reutilizar `FrontEnd/src/components/ui/dialog/` e `FrontEnd/src/components/ui/button/`.
- Não alterar contratos da API, regras de domínio ou permissões backend.
- Não manter qualquer chamada a `window.prompt` em `FrontEnd/src/views/DraftsView.vue`.
- Todo texto visível deve usar chaves equivalentes em `pt.json` e `en.json`, com acentuação portuguesa revisada.
- Usar TDD: observar cada teste falhar pelo comportamento ausente antes de implementar.
- Não modificar nem reverter alterações não relacionadas presentes no worktree.
- As etapas de commit são condicionais: não executar commits sem solicitação explícita do usuário.

---

## File Map

- Create `FrontEnd/src/components/drafts/DraftReasonDialog.vue`: renderização, estado temporário do motivo, validação e eventos do modal.
- Create `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts`: comportamento, acessibilidade básica e variantes dos quatro contextos.
- Create `FrontEnd/src/views/DraftsView.spec.ts`: integração dos botões, serviços e fechamento/manutenção do modal.
- Modify `FrontEnd/src/views/DraftsView.vue`: ação pendente única e remoção dos prompts nativos.
- Modify `FrontEnd/src/types/draftMontagem.ts`: tipo reutilizável do status de publicação Discord.
- Modify `FrontEnd/src/i18n/locales/pt.json`: textos do modal em português.
- Modify `FrontEnd/src/i18n/locales/en.json`: textos equivalentes em inglês.
- Modify `specs/017-robustecer-drafts-discord-jogadores/spec.md`: cenário e requisitos aprovados.
- Modify `specs/017-robustecer-drafts-discord-jogadores/plan.md`: decisão de UI e componentes afetados.
- Modify `specs/017-robustecer-drafts-discord-jogadores/tasks.md`: tarefas TDD e verificação rastreáveis.

---

### Task 1: Sincronizar os artefatos Spec Kit

**Files:**
- Modify: `specs/017-robustecer-drafts-discord-jogadores/spec.md`
- Modify: `specs/017-robustecer-drafts-discord-jogadores/plan.md`
- Modify: `specs/017-robustecer-drafts-discord-jogadores/tasks.md`

**Interfaces:**
- Consumes: design aprovado em `docs/superpowers/specs/2026-07-18-modal-contextual-acoes-draft-design.md`.
- Produces: requisitos `FR-021` a `FR-024`, critério `SC-010` e tarefas `T068` a `T074` aprováveis antes do código.

- [ ] **Step 1: Adicionar a história e os requisitos à especificação**

Adicionar após a User Story 5:

```markdown
### User Story 6 - Confirmar ações administrativas com contexto (Priority: P2)

Como administrador, quero confirmar cancelamento, remoção manual e republicações em um modal contextual, para entender o impacto da ação sem depender do prompt nativo do navegador.

**Independent Test**: Abrir cada uma das quatro ações e verificar título, contexto, motivo obrigatório, variante e serviço executado.

1. **Given** uma ação administrativa de draft, **When** o administrador a inicia, **Then** o site abre um modal contextual localizado em vez de `window.prompt`.
2. **Given** uma republicação Discord, **When** o modal abre, **Then** exibe o tipo e o status atual da publicação.
3. **Given** motivo vazio, **When** o administrador tenta confirmar, **Then** nenhum serviço é chamado.
```

Adicionar em Functional Requirements:

```markdown
- **FR-021**: O frontend MUST substituir prompts nativos de cancelamento, remoção manual e republicação por modal contextual.
- **FR-022**: O modal MUST exigir motivo não vazio e impedir envio duplicado durante processamento.
- **FR-023**: Republicações MUST mostrar tipo e status atual; remoção manual MUST mostrar o jogador afetado.
- **FR-024**: O modal MUST funcionar por teclado, controlar foco e usar somente textos internacionalizados.
```

Adicionar em Success Criteria:

```markdown
- **SC-010**: 100% das quatro ações com motivo na tela de drafts usam o modal contextual, sem chamadas a `window.prompt`.
```

- [ ] **Step 2: Atualizar o plano da feature**

Adicionar ao Phase Plan:

```markdown
5. P2 confirmação contextual: componente único baseado em Reka UI, integração dos quatro fluxos, i18n e testes responsivos.
```

Adicionar à decisão de estrutura:

```markdown
`DraftReasonDialog.vue` concentra apresentação e validação local do motivo; `DraftsView.vue` mantém somente a ação pendente e o despacho para serviços existentes.
```

- [ ] **Step 3: Registrar tarefas incrementais**

Adicionar antes da Final Phase:

```markdown
## Phase 8: User Story 6 - Confirmação contextual de ações

- [ ] T068 [P] [US6] Adicionar testes falhos do modal em `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts`.
- [ ] T069 [P] [US6] Adicionar chaves equivalentes em `pt.json` e `en.json`.
- [ ] T070 [US6] Implementar `DraftReasonDialog.vue` com Dialog Reka UI, validação e variantes.
- [ ] T071 [P] [US6] Adicionar testes falhos de integração em `FrontEnd/src/views/DraftsView.spec.ts`.
- [ ] T072 [US6] Integrar cancelamento, remoção manual e duas republicações em `DraftsView.vue`.
- [ ] T073 [US6] Remover todos os `window.prompt` de `DraftsView.vue`.
- [ ] T074 [US6] Executar testes, build, auditoria i18n e verificação desktop/mobile.
```

- [ ] **Step 4: Revisar consistência dos artefatos**

Run:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
```

Expected: JSON aponta para `specs/017-robustecer-drafts-discord-jogadores` e inclui `tasks.md`.

- [ ] **Step 5: Commit documental**

```bash
git add specs/017-robustecer-drafts-discord-jogadores/spec.md specs/017-robustecer-drafts-discord-jogadores/plan.md specs/017-robustecer-drafts-discord-jogadores/tasks.md docs/superpowers/specs/2026-07-18-modal-contextual-acoes-draft-design.md docs/superpowers/plans/2026-07-18-modal-contextual-acoes-draft.md
git commit -m "docs: planejar modal contextual de ações do draft"
```

---

### Task 2: Implementar o modal contextual com i18n

**Files:**
- Create: `FrontEnd/src/components/drafts/DraftReasonDialog.vue`
- Create: `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts`
- Modify: `FrontEnd/src/types/draftMontagem.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Consumes: `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogFooter` e `Button` existentes.
- Produces:

```ts
export type DraftReasonDialogAction =
  | { type: 'cancelDraft' }
  | { type: 'removeManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'republishPresence'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }
  | { type: 'republishTeams'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }

// Props: { open: boolean; action: DraftReasonDialogAction | null; saving: boolean }
// Emits: confirm(reason: string), cancel()
```

- [ ] **Step 1: Escrever testes falhos dos quatro contextos**

Criar `DraftReasonDialog.spec.ts` com happy-dom, Vue Test Utils e o i18n real:

```ts
// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { i18n } from '@/i18n'
import DraftReasonDialog, { type DraftReasonDialogAction } from './DraftReasonDialog.vue'

const mountDialog = (action: DraftReasonDialogAction, saving = false) =>
  mount(DraftReasonDialog, {
    attachTo: document.body,
    props: { open: true, action, saving },
    global: { plugins: [i18n], stubs: { teleport: true } },
  })

describe('DraftReasonDialog', () => {
  it.each([
    ['cancelDraft', { type: 'cancelDraft' }, 'Cancelar draft'],
    ['removeManualPresence', { type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' }, 'Remover presença'],
    ['republishPresence', { type: 'republishPresence', publicationStatus: 'Falha' }, 'Republicar lista de presença'],
    ['republishTeams', { type: 'republishTeams', publicationStatus: 'Pendente' }, 'Republicar times'],
  ] as const)('renders the %s context', async (_, action, title) => {
    const wrapper = mountDialog(action)
    expect(wrapper.text()).toContain(title)
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
    wrapper.unmount()
  })

  it('normalizes and emits a valid reason', async () => {
    const wrapper = mountDialog({ type: 'cancelDraft' })
    await wrapper.get('textarea').setValue('  motivo válido  ')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toEqual([['motivo válido']])
  })

  it('does not submit a blank reason', async () => {
    const wrapper = mountDialog({ type: 'cancelDraft' })
    await wrapper.get('textarea').setValue('   ')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toBeUndefined()
  })

  it('blocks actions while saving', async () => {
    const wrapper = mountDialog({ type: 'republishPresence', publicationStatus: 'Pendente' }, true)
    expect(wrapper.get('textarea').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').attributes('disabled')).toBeDefined()
  })
})
```

- [ ] **Step 2: Executar o teste para confirmar RED**

Run:

```bash
cd FrontEnd && npm test -- src/components/drafts/DraftReasonDialog.spec.ts
```

Expected: FAIL porque `DraftReasonDialog.vue` ainda não existe.

- [ ] **Step 3: Exportar o tipo do status Discord**

Adicionar após `DraftMontagemPublicacaoDiscordTipo` em `types/draftMontagem.ts`:

```ts
export type DraftMontagemPublicacaoDiscordStatus = 'Pendente' | 'Publicada' | 'Falha' | 'Ignorada'
```

Alterar a propriedade de `DraftMontagemPublicacaoDiscord`:

```ts
status: DraftMontagemPublicacaoDiscordStatus
```

- [ ] **Step 4: Adicionar chaves i18n equivalentes**

Adicionar em `drafts.reasonDialog` nos dois arquivos. Estrutura portuguesa:

```json
"reasonDialog": {
  "administrativeKicker": "Ação administrativa",
  "discordKicker": "Publicação no Discord",
  "reasonLabel": "Motivo",
  "reasonRequired": "Informe um motivo para continuar.",
  "back": "Voltar",
  "currentStatus": "Status atual: {status}",
  "affectedPlayer": "Jogador afetado: {name}",
  "cancelDraft": {
    "title": "Cancelar draft",
    "description": "O draft será cancelado e deixará de aparecer na listagem padrão.",
    "defaultReason": "Draft cancelado",
    "confirm": "Cancelar draft"
  },
  "removeManualPresence": {
    "title": "Remover presença",
    "description": "O jogador será removido da lista de presença deste draft.",
    "defaultReason": "Presença removida manualmente",
    "confirm": "Remover presença"
  },
  "republishPresence": {
    "title": "Republicar lista de presença",
    "description": "Uma nova mensagem da lista de presença será solicitada ao bot.",
    "defaultReason": "Republicação solicitada",
    "confirm": "Republicar presença",
    "context": "Lista de presença"
  },
  "republishTeams": {
    "title": "Republicar times",
    "description": "Uma nova mensagem dos times definidos será solicitada ao bot.",
    "defaultReason": "Republicação solicitada",
    "confirm": "Republicar times",
    "context": "Times definidos"
  }
}
```

O arquivo inglês deve manter a mesma árvore com traduções naturais: `Administrative action`, `Discord publication`, `Reason`, `Back`, `Cancel draft`, `Remove presence`, `Republish presence list` e `Republish teams`.

- [ ] **Step 5: Implementar o componente mínimo**

Criar `DraftReasonDialog.vue` com:

```vue
<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { DraftMontagemPublicacaoDiscordStatus } from '@/types/draftMontagem'

export type DraftReasonDialogAction =
  | { type: 'cancelDraft' }
  | { type: 'removeManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'republishPresence'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }
  | { type: 'republishTeams'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }

const props = defineProps<{ open: boolean; action: DraftReasonDialogAction | null; saving: boolean }>()
const emit = defineEmits<{ confirm: [reason: string]; cancel: [] }>()
const { t } = useI18n()
const reason = ref('')
const submitted = ref(false)
const key = computed(() => props.action ? `drafts.reasonDialog.${props.action.type}` : '')
const discordAction = computed(() => props.action?.type === 'republishPresence' || props.action?.type === 'republishTeams')
const publicationStatus = computed(() => {
  const action = props.action
  return action?.type === 'republishPresence' || action?.type === 'republishTeams' ? action.publicationStatus : null
})
const valid = computed(() => reason.value.trim().length > 0)

watch(() => [props.open, props.action] as const, ([open]) => {
  submitted.value = false
  reason.value = open && props.action ? t(`${key.value}.defaultReason`) : ''
}, { immediate: true })

function cancel() {
  if (!props.saving) emit('cancel')
}

function confirm() {
  submitted.value = true
  if (!valid.value || props.saving) return
  emit('confirm', reason.value.trim())
}
</script>

<template>
  <Dialog :open="open" @update:open="(value) => !value && cancel()">
    <DialogContent v-if="action" :show-close-button="!saving" class="border-border bg-popover sm:max-w-lg" @escape-key-down="saving && $event.preventDefault()">
      <form class="grid gap-5" @submit.prevent="confirm">
        <DialogHeader>
          <p class="page-kicker">{{ t(discordAction ? 'drafts.reasonDialog.discordKicker' : 'drafts.reasonDialog.administrativeKicker') }}</p>
          <DialogTitle>{{ t(`${key}.title`) }}</DialogTitle>
          <DialogDescription>{{ t(`${key}.description`) }}</DialogDescription>
        </DialogHeader>

        <div v-if="discordAction && publicationStatus" class="border-border bg-muted/40 grid gap-1 rounded-lg border p-3">
          <strong>{{ t(`${key}.context`) }}</strong>
          <span class="text-muted-foreground text-sm">{{ t('drafts.reasonDialog.currentStatus', { status: t(`drafts.publication.status.${publicationStatus}`) }) }}</span>
        </div>
        <div v-else-if="action.type === 'removeManualPresence'" class="border-border bg-muted/40 rounded-lg border p-3 text-sm">
          {{ t('drafts.reasonDialog.affectedPlayer', { name: action.jogadorNome }) }}
        </div>

        <label class="grid gap-2" for="draft-reason">
          <span class="text-sm font-medium">{{ t('drafts.reasonDialog.reasonLabel') }}</span>
          <textarea id="draft-reason" v-model="reason" class="border-input bg-background focus-visible:ring-ring min-h-24 rounded-md border p-3 outline-none focus-visible:ring-2" autofocus :disabled="saving" :aria-invalid="submitted && !valid" />
        </label>
        <p v-if="submitted && !valid" class="text-destructive text-sm" role="alert">{{ t('drafts.reasonDialog.reasonRequired') }}</p>

        <DialogFooter>
          <Button type="button" variant="outline" :disabled="saving" @click="cancel">{{ t('drafts.reasonDialog.back') }}</Button>
          <Button data-testid="draft-reason-confirm" type="submit" :variant="discordAction ? 'default' : 'destructive'" :disabled="saving">
            {{ t(`${key}.confirm`) }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
```

- [ ] **Step 6: Executar testes e confirmar GREEN**

Run:

```bash
cd FrontEnd && npm test -- src/components/drafts/DraftReasonDialog.spec.ts src/i18n/i18n.spec.ts
```

Expected: todos os testes passam e a paridade i18n permanece válida.

- [ ] **Step 7: Commit do componente**

```bash
git add FrontEnd/src/components/drafts/DraftReasonDialog.vue FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts FrontEnd/src/types/draftMontagem.ts FrontEnd/src/i18n/locales/pt.json FrontEnd/src/i18n/locales/en.json
git commit -m "feat: adicionar modal contextual de ações do draft"
```

---

### Task 3: Integrar os quatro fluxos da tela de drafts

**Files:**
- Create: `FrontEnd/src/views/DraftsView.spec.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`

**Interfaces:**
- Consumes: `DraftReasonDialog` e `DraftReasonDialogAction` produzidos na Task 2.
- Produces: nenhum `window.prompt`; um único `pendingReasonAction` e um único `confirmReasonAction(reason)`.

- [ ] **Step 1: Escrever teste de integração falho**

Criar `DraftsView.spec.ts` com mocks dos serviços, `useAuthState`, rota e SignalR. O cenário deve montar um draft com uma presença manual e publicações Discord, clicar nos quatro botões e validar o modal. Núcleo das asserções:

```ts
import DraftsViewSource from './DraftsView.vue?raw'

const confirmAction = async (buttonText: string, reason: string) => {
  const trigger = wrapper.findAll('button').find((button) => button.text().includes(buttonText))
  expect(trigger).toBeDefined()
  await trigger!.trigger('click')
  await flushPromises()
  expect(wrapper.get('[role="dialog"]').exists()).toBe(true)
  await wrapper.get('textarea').setValue(reason)
  await wrapper.get('form').trigger('submit')
  await flushPromises()
}

await confirmAction('Republicar presença', 'canal corrigido')
expect(republishDraftMontagemDiscordPublication).toHaveBeenCalledWith('montagem-1', 'Presenca', 'canal corrigido')

await confirmAction('Republicar times', 'mensagem dos times removida')
expect(republishDraftMontagemDiscordPublication).toHaveBeenCalledWith('montagem-1', 'TimesDefinidos', 'mensagem dos times removida')

await confirmAction('Cancelar', 'evento cancelado pelo organizador')
expect(cancelDraftMontagem).toHaveBeenCalledWith('montagem-1', 'evento cancelado pelo organizador')

await confirmAction('Remover', 'jogador avisou ausência')
expect(removeManualDraftMontagemPresence).toHaveBeenCalledWith('montagem-1', 'jogador-1', 'jogador avisou ausência')

expect(DraftsViewSource).not.toContain('window.prompt')
```

- [ ] **Step 2: Executar o teste para confirmar RED**

Run:

```bash
cd FrontEnd && npm test -- src/views/DraftsView.spec.ts
```

Expected: FAIL porque os botões ainda chamam `window.prompt` e o modal não está montado.

- [ ] **Step 3: Adicionar a ação pendente e os abridores**

No script de `DraftsView.vue`:

```ts
import DraftReasonDialog, { type DraftReasonDialogAction } from '@/components/drafts/DraftReasonDialog.vue'

const pendingReasonAction = ref<DraftReasonDialogAction | null>(null)

function requestDraftCancellation() {
  if (selectedMontagem.value && canManageDrafts.value) pendingReasonAction.value = { type: 'cancelDraft' }
}

function requestManualPresenceRemoval(jogadorId: string, jogadorNome: string) {
  if (selectedMontagem.value && canManageDrafts.value) {
    pendingReasonAction.value = { type: 'removeManualPresence', jogadorId, jogadorNome }
  }
}

function requestDiscordRepublish(tipo: DraftMontagemPublicacaoDiscordTipo) {
  if (!selectedMontagem.value || !canManageDrafts.value) return
  pendingReasonAction.value = tipo === 'Presenca'
    ? { type: 'republishPresence', publicationStatus: publicationStatus('Presenca') }
    : { type: 'republishTeams', publicationStatus: publicationStatus('TimesDefinidos') }
}
```

- [ ] **Step 4: Substituir os handlers por execuções com motivo recebido**

Remover os prompts de `cancelMontagem`, `removeManualPresence` e `republishDiscordPublication`. Criar:

```ts
async function confirmReasonAction(reason: string) {
  const action = pendingReasonAction.value
  if (!action || !selectedMontagem.value || !canManageDrafts.value) return

  saving.value = true
  try {
    if (action.type === 'cancelDraft') {
      selectedMontagem.value = await cancelDraftMontagem(selectedMontagem.value.id, reason)
      await loadVisualMontagens()
      notification.value = t('drafts.canceled', { name: selectedMontagem.value.nome })
    } else if (action.type === 'removeManualPresence') {
      selectedMontagem.value = await removeManualDraftMontagemPresence(selectedMontagem.value.id, action.jogadorId, reason)
      await loadEligibleManualPresencePlayers()
      notification.value = t('drafts.presence.manualRemoved')
    } else {
      const tipo = action.type === 'republishPresence' ? 'Presenca' : 'TimesDefinidos'
      selectedMontagem.value = await republishDraftMontagemDiscordPublication(selectedMontagem.value.id, tipo, reason)
      notification.value = t('drafts.publication.republishRequested')
    }
    pendingReasonAction.value = null
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}
```

Manter o modal aberto no `catch`; limpar `pendingReasonAction` apenas no caminho de sucesso ou cancelamento explícito.

- [ ] **Step 5: Conectar botões e modal no template**

Alterar os eventos:

```vue
@click="requestDraftCancellation"
@click.stop="requestManualPresenceRemoval(presence.jogadorId, presence.nomeExibicao)"
@click="requestDiscordRepublish('Presenca')"
@click="requestDiscordRepublish('TimesDefinidos')"
```

Montar antes do fechamento de `PageFrame`:

```vue
<DraftReasonDialog
  :open="pendingReasonAction !== null"
  :action="pendingReasonAction"
  :saving="saving"
  @cancel="pendingReasonAction = null"
  @confirm="confirmReasonAction"
/>
```

- [ ] **Step 6: Executar testes de integração e serviço**

Run:

```bash
cd FrontEnd && npm test -- src/views/DraftsView.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/services/draftMontagens.spec.ts
```

Expected: todos os testes passam; nenhum prompt nativo é encontrado.

- [ ] **Step 7: Commit da integração**

```bash
git add FrontEnd/src/views/DraftsView.vue FrontEnd/src/views/DraftsView.spec.ts
git commit -m "feat: integrar modal nas ações administrativas do draft"
```

---

### Task 4: Verificação final e auditoria

**Files:**
- Verify only: `FrontEnd/`

**Interfaces:**
- Consumes: Tasks 1 a 3 concluídas.
- Produces: evidência de testes, build, responsividade e i18n.

- [ ] **Step 1: Confirmar ausência de prompts nativos**

Run:

```bash
rg -n "window\.prompt" FrontEnd/src/views/DraftsView.vue
```

Expected: nenhum resultado.

- [ ] **Step 2: Executar a suíte completa do frontend**

Run:

```bash
cd FrontEnd && npm test
```

Expected: todos os arquivos e testes passam, sem falhas.

- [ ] **Step 3: Executar o build**

Run:

```bash
cd FrontEnd && npm run build
```

Expected: `vue-tsc` e Vite concluem com exit code 0.

- [ ] **Step 4: Verificar no navegador**

Com backend e frontend locais ativos, validar em 1440 px e 390 px:

```text
1. Abrir cancelamento e confirmar variante destrutiva.
2. Abrir remoção manual e confirmar nome do jogador.
3. Abrir republicação de presença e confirmar tipo/status.
4. Abrir republicação de times e confirmar tipo/status.
5. Testar Tab, Shift+Tab, Escape, motivo vazio e duplo clique.
6. Simular erro de API e confirmar que o modal permanece aberto.
```

- [ ] **Step 5: Auditoria de internacionalização**

Confirmar:

```text
- Nenhum texto visível hardcoded nos arquivos alterados.
- pt.json e en.json têm árvores idênticas.
- Acentuação portuguesa revisada.
- Títulos, botões, labels, validações, status e feedback usam i18n.
- Nenhuma alteração backend exige resources.
```

<script setup lang="ts">
import { computed } from 'vue'

import { LEAGUE_ROLES, type LeagueRoleValue } from '@/constants/leagueRoles'
import type { RoutePreference } from '@/services/players'

const props = defineProps<{
  modelValue: RoutePreference[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: RoutePreference[]]
}>()

const routes = LEAGUE_ROLES

const sortedRoutes = computed(() => {
  return [...routes].sort((left, right) => {
    const leftPriority = preferenceFor(left)?.prioridade ?? Number.MAX_SAFE_INTEGER
    const rightPriority = preferenceFor(right)?.prioridade ?? Number.MAX_SAFE_INTEGER

    return leftPriority - rightPriority
  })
})

const errors = computed(() => {
  const messages: string[] = []
  const priorities = props.modelValue.map((preference) => preference.prioridade)
  const blockedCount = props.modelValue.filter((preference) => preference.naoJogoNemLascando).length

  if (new Set(priorities).size !== 5) {
    messages.push('Cada prioridade deve ser unica.')
  }

  if (blockedCount > 1) {
    messages.push('Marque no maximo uma rota bloqueada.')
  }

  return messages
})

function updatePriority(rota: LeagueRoleValue, prioridade: number) {
  emit(
    'update:modelValue',
    props.modelValue.map((preference) =>
      preference.rota === rota ? { ...preference, prioridade } : preference,
    ),
  )
}

function updateBlocked(rota: LeagueRoleValue, checked: boolean) {
  emit(
    'update:modelValue',
    props.modelValue.map((preference) => ({
      ...preference,
      naoJogoNemLascando: preference.rota === rota ? checked : checked ? false : preference.naoJogoNemLascando,
    })),
  )
}

function preferenceFor(rota: LeagueRoleValue) {
  return props.modelValue.find((preference) => preference.rota === rota)
}
</script>

<template>
  <fieldset class="route-editor">
    <legend>Preferencias de rotas</legend>

    <p class="route-editor__hint">Ordene suas escolhas de 1 a 5 e marque no maximo uma rota para evitar.</p>

    <div class="route-editor__grid">
      <div v-for="route in sortedRoutes" :key="route" class="route-editor__card">
        <div class="route-editor__identity">
          <span class="route-editor__role" :class="`route-editor__role--${route.toLowerCase()}`">{{ route }}</span>
          <strong>#{{ preferenceFor(route)?.prioridade }}</strong>
        </div>

        <label class="route-editor__priority">
          <span>Prioridade</span>
          <select
            :value="preferenceFor(route)?.prioridade"
            @change="updatePriority(route, Number(($event.target as HTMLSelectElement).value))"
          >
            <option v-for="priority in 5" :key="priority" :value="priority">{{ priority }}</option>
          </select>
        </label>

        <label class="route-editor__blocked">
          <input
            type="checkbox"
            :checked="preferenceFor(route)?.naoJogoNemLascando"
            @change="updateBlocked(route, ($event.target as HTMLInputElement).checked)"
          />
          <span>Evitar</span>
        </label>
      </div>
    </div>

    <p v-for="error in errors" :key="error" class="route-editor__error">{{ error }}</p>
  </fieldset>
</template>

<style scoped>
.route-editor {
  grid-column: 1 / -1;
  display: grid;
  gap: var(--space-sm);
  min-width: 0;
  margin: 0;
  padding: var(--space-md);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-xl);
  background:
    radial-gradient(circle at top right, var(--color-primary-soft), transparent 38%),
    var(--color-surface-1);
}

.route-editor legend {
  padding: 0 var(--space-xs);
  color: var(--color-primary-hover);
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.route-editor__hint {
  margin: 0;
  color: var(--color-ink-muted);
  font-size: 13px;
  line-height: 1.5;
}

.route-editor__grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(128px, 1fr));
  gap: var(--space-xs);
}

.route-editor__card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: var(--space-xs);
  align-items: end;
  min-width: 0;
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-lg);
  padding: var(--space-xs);
  background:
    linear-gradient(145deg, rgb(255 255 255 / 4%), transparent 48%),
    var(--color-canvas-raised);
  transition:
    border-color var(--duration-base) var(--ease-standard),
    background var(--duration-base) var(--ease-standard),
    box-shadow var(--duration-base) var(--ease-standard);
}

.route-editor__card:focus-within {
  border-color: var(--color-primary-hover);
  background: var(--color-primary-soft);
  box-shadow: 0 0 0 1px rgb(139 92 246 / 16%);
}

.route-editor__identity {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.route-editor__role {
  overflow: hidden;
  font-size: 13px;
  font-weight: 800;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.route-editor__identity strong {
  color: var(--color-ink);
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 20px;
  line-height: 1;
}

.route-editor label {
  display: grid;
  gap: var(--space-xxs);
  color: var(--color-ink-muted);
  font-size: 12px;
}

.route-editor__priority span {
  color: var(--color-ink-subtle);
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.route-editor select {
  min-width: 58px;
  min-height: 34px;
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  padding: 0 var(--space-xs);
  color: var(--color-ink);
  background: var(--color-surface-2);
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-weight: 800;
}

.route-editor__blocked {
  grid-column: 1 / -1;
  display: inline-grid;
  justify-self: start;
  cursor: pointer;
}

.route-editor__blocked input {
  position: absolute;
  width: 1px !important;
  height: 1px !important;
  min-width: 1px !important;
  min-height: 1px !important;
  opacity: 0;
  pointer-events: none;
}

.route-editor__blocked span {
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-pill);
  padding: 5px 9px;
  color: var(--color-ink-muted);
  background: rgb(255 255 255 / 4%);
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  transition:
    border-color var(--duration-base) var(--ease-standard),
    background var(--duration-base) var(--ease-standard),
    color var(--duration-base) var(--ease-standard);
}

.route-editor__blocked input:checked + span {
  border-color: rgb(239 68 68 / 38%);
  color: var(--color-danger);
  background: rgb(239 68 68 / 12%);
}

.route-editor__blocked input:focus-visible + span {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.route-editor__error {
  margin: 0;
  color: var(--color-danger);
  font-size: 14px;
}

.route-editor__role--top { color: var(--role-top); }
.route-editor__role--jungle { color: var(--role-jungle); }
.route-editor__role--mid { color: var(--role-mid); }
.route-editor__role--adc { color: var(--role-adc); }
.route-editor__role--support { color: var(--role-support); }

@media (max-width: 720px) {
  .route-editor__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .route-editor__card {
    grid-template-columns: 1fr auto;
  }
}

@media (max-width: 480px) {
  .route-editor__grid {
    grid-template-columns: 1fr;
  }
}
</style>

<script setup lang="ts">
import { computed } from 'vue'

import type { RouteName, RoutePreference } from '@/services/players'

const props = defineProps<{
  modelValue: RoutePreference[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: RoutePreference[]]
}>()

const routes: RouteName[] = ['Top', 'Jungle', 'Mid', 'Adc', 'Support']

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

function updatePriority(rota: RouteName, prioridade: number) {
  emit(
    'update:modelValue',
    props.modelValue.map((preference) =>
      preference.rota === rota ? { ...preference, prioridade } : preference,
    ),
  )
}

function updateBlocked(rota: RouteName, checked: boolean) {
  emit(
    'update:modelValue',
    props.modelValue.map((preference) => ({
      ...preference,
      naoJogoNemLascando: preference.rota === rota ? checked : checked ? false : preference.naoJogoNemLascando,
    })),
  )
}

function preferenceFor(rota: RouteName) {
  return props.modelValue.find((preference) => preference.rota === rota)
}
</script>

<template>
  <fieldset class="route-editor">
    <legend>Preferencias de rotas</legend>

    <div v-for="route in routes" :key="route" class="route-editor__row">
      <span class="route-editor__role" :class="`route-editor__role--${route.toLowerCase()}`">{{ route }}</span>
      <label>
        Prioridade
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
        Nao jogo nem lascando
      </label>
    </div>

    <p v-for="error in errors" :key="error" class="route-editor__error">{{ error }}</p>
  </fieldset>
</template>

<style scoped>
.route-editor {
  display: grid;
  gap: var(--space-sm);
  min-width: 0;
  margin: 0;
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-md);
  background: var(--color-surface-1);
}

.route-editor legend {
  padding: 0 var(--space-xs);
  color: var(--color-ink-muted);
  font-weight: 700;
}

.route-editor__row {
  display: grid;
  grid-template-columns: minmax(72px, 0.8fr) minmax(112px, 1fr) minmax(160px, 1.2fr);
  gap: var(--space-sm);
  align-items: center;
}

.route-editor__role {
  font-weight: 800;
}

.route-editor label {
  display: grid;
  gap: var(--space-xxs);
  color: var(--color-ink-muted);
  font-size: 0.78rem;
}

.route-editor select {
  min-height: 38px;
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-sm);
  color: var(--color-ink);
  background: var(--color-surface-2);
}

.route-editor__blocked {
  grid-template-columns: 18px 1fr;
  align-items: center;
  color: var(--color-ink);
}

.route-editor__error {
  margin: 0;
  color: var(--color-danger);
  font-size: 0.85rem;
}

.route-editor__role--top { color: var(--role-top); }
.route-editor__role--jungle { color: var(--role-jungle); }
.route-editor__role--mid { color: var(--role-mid); }
.route-editor__role--adc { color: var(--role-adc); }
.route-editor__role--support { color: var(--role-support); }

@media (max-width: 720px) {
  .route-editor__row {
    grid-template-columns: 1fr;
  }
}
</style>

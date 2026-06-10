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

    <div class="route-editor__grid">
      <div v-for="route in routes" :key="route" class="route-editor__card">
        <span class="route-editor__role" :class="`route-editor__role--${route.toLowerCase()}`">{{ route }}</span>
        <label class="route-editor__priority">
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
          <span>Nao jogo nem lascando</span>
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
  gap: var(--space-xs);
  min-width: 0;
  margin: 0;
  padding: var(--space-sm);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-md);
  background: var(--color-surface-1);
}

.route-editor legend {
  padding: 0 var(--space-xs);
  color: var(--color-ink-muted);
  font-weight: 700;
}

.route-editor__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-xs);
}

.route-editor__card {
  display: grid;
  grid-template-columns: minmax(72px, 0.8fr) minmax(92px, 1fr);
  gap: var(--space-xs);
  align-items: center;
  min-width: 0;
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-sm);
  padding: var(--space-xs);
  background: rgb(12 19 32 / 50%);
}

.route-editor__role {
  font-weight: 800;
}

.route-editor label {
  display: grid;
  gap: 4px;
  color: var(--color-ink-muted);
  font-size: 0.72rem;
}

.route-editor select {
  min-height: 32px;
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-sm);
  color: var(--color-ink);
  background: var(--color-surface-2);
}

.route-editor__blocked {
  grid-column: 1 / -1;
  grid-template-columns: 16px 1fr;
  align-items: center;
  color: var(--color-ink);
}

.route-editor__blocked input {
  margin: 0;
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
  .route-editor__grid {
    grid-template-columns: 1fr;
  }

  .route-editor__card {
    grid-template-columns: 1fr;
  }
}
</style>

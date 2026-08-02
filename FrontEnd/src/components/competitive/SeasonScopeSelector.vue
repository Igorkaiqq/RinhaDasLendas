<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type { SeasonScope, SeasonSummary } from '@/types/season'

const props = withDefaults(
  defineProps<{
    seasons: SeasonSummary[]
    modelValue: SeasonScope
    calendarConfigured: boolean
    loading?: boolean
  }>(),
  { loading: false },
)

const emit = defineEmits<{
  'update:modelValue': [scope: SeasonScope]
}>()

const { t } = useI18n()
const radioModes = ['current', 'selected', 'all'] as const
const draftMode = ref<SeasonScope['mode']>(props.modelValue.mode)
const selectedIds = ref<string[]>(
  props.modelValue.mode === 'selected' ? [...props.modelValue.seasonIds] : [],
)
interface FocusTarget {
  focus: () => void
}

const radioElements = ref<FocusTarget[]>([])

watch(
  () => props.modelValue,
  (scope) => {
    draftMode.value = scope.mode
    if (scope.mode === 'selected') selectedIds.value = [...scope.seasonIds]
  },
  { deep: true },
)

function setRadioElement(element: unknown, index: number) {
  if (
    typeof element === 'object' &&
    element !== null &&
    'focus' in element &&
    typeof element.focus === 'function'
  ) {
    radioElements.value[index] = element as FocusTarget
  }
}

function chooseMode(mode: SeasonScope['mode'], emitChange = true) {
  draftMode.value = mode
  if (!emitChange || mode === 'selected') return
  emit('update:modelValue', { mode })
}

function commitMode(mode: SeasonScope['mode']) {
  draftMode.value = mode
  if (mode !== 'selected') {
    emit('update:modelValue', { mode })
    return
  }
  if (selectedIds.value.length === 0 && props.seasons[0]) {
    selectedIds.value = [props.seasons[0].id]
  }
  if (selectedIds.value.length > 0) applySelection()
}

async function moveRadio(index: number, offset: number) {
  const nextIndex = (index + offset + radioModes.length) % radioModes.length
  commitMode(radioModes[nextIndex]!)
  await nextTick()
  radioElements.value[nextIndex]?.focus()
}

function handleRadioKey(event: { key: string; preventDefault: () => void }, index: number) {
  if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
    event.preventDefault()
    void moveRadio(index, 1)
  } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
    event.preventDefault()
    void moveRadio(index, -1)
  } else if (event.key === ' ' || event.key === 'Enter') {
    event.preventDefault()
    chooseMode(radioModes[index]!)
  } else if (event.key === 'Home') {
    event.preventDefault()
    void moveRadio(0, 0)
  } else if (event.key === 'End') {
    event.preventDefault()
    void moveRadio(radioModes.length - 1, 0)
  }
}

function toggleSeason(seasonId: string, checked: boolean) {
  selectedIds.value = checked
    ? [...new Set([...selectedIds.value, seasonId])]
    : selectedIds.value.filter((id) => id !== seasonId)
}

function applySelection() {
  if (selectedIds.value.length === 0) return
  emit('update:modelValue', {
    mode: 'selected',
    seasonIds: selectedIds.value as [string, ...string[]],
  })
}
</script>

<template>
  <section class="scope-selector" :aria-busy="loading">
    <div
      class="scope-selector__radios"
      role="radiogroup"
      :aria-label="t('seasons.scope.label')"
    >
      <button
        v-for="(mode, index) in radioModes"
        :key="mode"
        :ref="(element) => setRadioElement(element, index)"
        type="button"
        role="radio"
        :aria-checked="draftMode === mode"
        :tabindex="draftMode === mode ? 0 : -1"
        @click="chooseMode(mode)"
        @keydown="handleRadioKey($event, index)"
      >
        {{ t(`seasons.scope.${mode}`) }}
      </button>
    </div>

    <div v-if="draftMode === 'selected'" class="scope-selector__options">
      <label v-for="season in seasons" :key="season.id" :for="`scope-${season.id}`">
        <input
          :id="`scope-${season.id}`"
          type="checkbox"
          name="seasonScope"
          autocomplete="off"
          :checked="selectedIds.includes(season.id)"
          @change="toggleSeason(season.id, ($event.target as HTMLInputElement).checked)"
        />
        <span>{{ season.nome }}</span>
      </label>
      <button
        class="scope-selector__apply"
        type="button"
        :disabled="selectedIds.length === 0"
        @click="applySelection"
      >
        {{ t('seasons.scope.apply') }}
      </button>
    </div>

    <p
      v-if="draftMode === 'current' && !calendarConfigured"
      class="scope-selector__status"
      role="status"
      aria-live="polite"
    >
      {{ t('seasons.states.calendarUnconfigured') }}
    </p>
  </section>
</template>

<style scoped>
.scope-selector {
  display: grid;
  min-width: 0;
  gap: var(--space-sm);
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
}

.scope-selector__radios {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-xs);
}

.scope-selector__radios button,
.scope-selector__apply {
  min-height: var(--control-height-md);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink-muted);
  background: var(--color-surface-2);
  overflow-wrap: anywhere;
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.scope-selector__radios button:not(:disabled):hover,
.scope-selector__apply:not(:disabled):hover {
  border-color: var(--color-primary-hover);
  background: var(--color-surface-3);
}

.scope-selector__radios button:not(:disabled):active,
.scope-selector__apply:not(:disabled):active {
  transform: translateY(1px);
}

.scope-selector__radios button[aria-checked='true'] {
  border-color: var(--color-primary);
  color: var(--color-ink);
  background: var(--color-primary-soft);
}

.scope-selector__radios button:focus-visible,
.scope-selector__apply:focus-visible,
.scope-selector__options input:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.scope-selector__options {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-sm);
  padding-top: var(--space-sm);
  border-top: 1px solid var(--color-hairline-soft);
}

.scope-selector__options label {
  display: flex;
  align-items: center;
  gap: var(--space-xs);
  min-height: 44px;
  color: var(--color-ink-muted);
  overflow-wrap: anywhere;
}

.scope-selector__apply {
  padding-inline: var(--space-md);
  color: var(--color-ink);
  background: var(--color-primary);
}

.scope-selector__apply:disabled {
  color: var(--color-ink-disabled);
  background: var(--color-surface-2);
}

.scope-selector__status {
  margin: 0;
  color: var(--color-warning);
}

@media (max-width: 767px) {
  .scope-selector__radios {
    grid-template-columns: 1fr;
  }
}

@media (prefers-reduced-motion: reduce) {
  .scope-selector button {
    transition: none;
  }

  .scope-selector button:active {
    transform: none;
  }
}
</style>

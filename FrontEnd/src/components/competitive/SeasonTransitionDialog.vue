<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import type { SeasonSummary } from '@/types/season'

import { useModalFocus } from './useModalFocus'

const props = defineProps<{
  open: boolean
  action: 'activate' | 'close'
  season: SeasonSummary
  currentSeason?: SeasonSummary | null
  calendarVersion: number
  confirmLabel?: string
  busy?: boolean
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()

const { t } = useI18n()
const panel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(null)
const confirmButton = ref<InstanceType<typeof globalThis.HTMLButtonElement> | null>(null)

useModalFocus(
  () => props.open,
  panel,
  requestCancel,
  () => confirmButton.value,
)

function requestCancel() {
  if (!props.busy) emit('cancel')
}

function confirm() {
  if (!props.busy) emit('confirm')
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="transition-dialog">
      <button
        class="transition-dialog__backdrop"
        type="button"
        :aria-label="t('common.cancel')"
        :disabled="busy"
        @click="requestCancel"
      />
      <section
        ref="panel"
        class="transition-dialog__panel"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="season-transition-title"
        aria-describedby="season-transition-description"
        :aria-busy="busy"
        tabindex="-1"
      >
        <p class="transition-dialog__eyebrow">{{ t('seasons.transition.eyebrow') }}</p>
        <h2 id="season-transition-title">
          {{ t(`seasons.transition.${action}.title`, { name: season.nome }) }}
        </h2>
        <p id="season-transition-description">
          <template v-if="action === 'activate' && currentSeason">
            {{
              t('seasons.transition.activate.replacesCurrent', {
                name: currentSeason.nome,
              })
            }}
          </template>
          <template v-else>{{ t(`seasons.transition.${action}.consequence`) }}</template>
        </p>
        <p class="transition-dialog__version">
          {{ t('seasons.transition.calendarVersion', { version: calendarVersion }) }}
        </p>
        <p v-if="busy" class="transition-dialog__progress" role="status" aria-live="polite">
          {{ t('seasons.transition.progress') }}
        </p>
        <footer>
          <button type="button" :disabled="busy" @click="requestCancel">
            {{ t('common.cancel') }}
          </button>
          <button
            id="season-transition-confirm"
            ref="confirmButton"
            class="transition-dialog__confirm"
            type="button"
            :disabled="busy"
            @click="confirm"
          >
            {{ confirmLabel ?? t(`seasons.transition.${action}.action`) }}
          </button>
        </footer>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.transition-dialog {
  position: fixed;
  inset: 0;
  z-index: 60;
  display: grid;
  place-items: center;
  padding: var(--space-md);
}

.transition-dialog__backdrop {
  position: absolute;
  inset: 0;
  width: 100%;
  border: 0;
  background: var(--color-overlay);
}

.transition-dialog__panel {
  position: relative;
  width: min(100%, 520px);
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-xl);
  color: var(--color-ink);
  background: var(--color-surface-1);
  box-shadow: var(--shadow-lg);
  overflow-wrap: anywhere;
  overscroll-behavior: contain;
}

.transition-dialog__eyebrow,
.transition-dialog__version {
  color: var(--color-warning);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.transition-dialog__panel h2 {
  margin-block: var(--space-xs) var(--space-sm);
}

.transition-dialog__panel > p:not(.transition-dialog__eyebrow):not(.transition-dialog__version) {
  color: var(--color-ink-muted);
}

.transition-dialog__panel footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-sm);
  margin-top: var(--space-lg);
}

.transition-dialog__panel button {
  min-height: 44px;
  padding-inline: var(--space-md);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink);
  background: var(--color-surface-2);
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.transition-dialog__panel button:not(:disabled):hover {
  border-color: var(--color-primary-hover);
  background: var(--color-surface-3);
}

.transition-dialog__panel button:not(:disabled):active {
  transform: translateY(1px);
}

.transition-dialog__panel .transition-dialog__confirm {
  border-color: var(--color-primary);
  background: var(--color-primary);
}

.transition-dialog__panel button:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

@media (max-width: 479px) {
  .transition-dialog__panel footer {
    display: grid;
  }
}

@media (prefers-reduced-motion: reduce) {
  .transition-dialog__panel button {
    transition: none;
  }

  .transition-dialog__panel button:active {
    transform: none;
  }
}
</style>

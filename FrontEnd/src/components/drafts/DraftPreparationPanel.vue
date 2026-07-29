<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import type { DraftMontagem, DraftMontagemModo, DraftMontagemPresenca } from '@/types/draftMontagem'

interface EligiblePresencePlayer {
  id: string
  nomeExibicao: string
}

const props = defineProps<{
  draft: DraftMontagem
  confirmedPresences: readonly DraftMontagemPresenca[]
  saving: boolean
  canConfirmPresence: boolean
  canCancelPresence: boolean
  canClosePresence: boolean
  canContinueManualPresence: boolean
  canManageManualPresence: boolean
  canChooseMode: boolean
  canSelectCaptains: boolean
  canReopenPresence: boolean
  canDefineCaptains: boolean
  canDrawOrder: boolean
  captainSelection: readonly string[]
  eligibleCaptainIds: readonly string[]
  manualPresenceSearch: string
  selectedManualPresencePlayerId: string
  availableManualPresencePlayers: readonly EligiblePresencePlayer[]
}>()

const emit = defineEmits<{
  'confirm-presence': []
  'cancel-presence': []
  'close-presence': [continueWithLess: boolean]
  'update:manualPresenceSearch': [value: string]
  'search-manual-presence': []
  'update:selectedManualPresencePlayerId': [value: string]
  'add-manual-presence': []
  'remove-manual-presence': [jogadorId: string, jogadorNome: string]
  'choose-mode': [modo: DraftMontagemModo]
  'toggle-captain': [jogadorId: string]
  'reopen-presence': []
  'define-captains': []
  'draw-order': []
}>()

const { t } = useI18n()

function emitUnlessSaving(event: 'confirm-presence' | 'cancel-presence' | 'search-manual-presence' | 'add-manual-presence' | 'define-captains' | 'draw-order') {
  if (props.saving) return
  if (event === 'confirm-presence') emit('confirm-presence')
  else if (event === 'cancel-presence') emit('cancel-presence')
  else if (event === 'search-manual-presence') emit('search-manual-presence')
  else if (event === 'add-manual-presence') emit('add-manual-presence')
  else if (event === 'define-captains') emit('define-captains')
  else emit('draw-order')
}

type ControlEvent = InstanceType<typeof globalThis.Event>

function updateManualSearch(event: ControlEvent) {
  emit('update:manualPresenceSearch', (event.target as unknown as { value: string }).value)
  emit('search-manual-presence')
}

function updateManualSelection(event: ControlEvent) {
  emit('update:selectedManualPresencePlayerId', (event.target as unknown as { value: string }).value)
}
</script>

<template>
  <section class="panel-card draft-preparation" :aria-labelledby="`draft-preparation-title-${draft.id}`">
    <header class="draft-preparation__header">
      <div>
        <p class="page-kicker">{{ t('drafts.presence.eyebrow') }}</p>
        <h2 :id="`draft-preparation-title-${draft.id}`">{{ t('drafts.presence.title') }}</h2>
      </div>
      <span class="team-status">{{ t('drafts.metrics.confirmed', confirmedPresences.length) }}</span>
    </header>

    <p v-if="draft.status === 'PresencaAberta' && confirmedPresences.length < 10" class="profile-inline-message">
      {{ t('drafts.presence.lessThanTen') }}
    </p>

    <div v-if="canConfirmPresence || canCancelPresence || canClosePresence || canContinueManualPresence" class="draft-preparation__stage-actions">
      <button
        v-if="canConfirmPresence"
        data-testid="confirm-presence"
        data-stage-primary-action
        type="button"
        :disabled="saving"
        @click="emitUnlessSaving('confirm-presence')"
      >
        {{ t('drafts.presence.confirm') }}
      </button>
      <button
        v-if="canCancelPresence"
        data-testid="cancel-presence"
        type="button"
        class="button-secondary"
        :disabled="saving"
        @click="emitUnlessSaving('cancel-presence')"
      >
        {{ t('drafts.presence.cancel') }}
      </button>
      <button
        v-if="canClosePresence"
        data-testid="close-presence"
        data-stage-primary-action
        type="button"
        class="button-secondary"
        :disabled="saving"
        @click="!saving && emit('close-presence', false)"
      >
        {{ t('drafts.presence.close') }}
      </button>
      <button
        v-if="canContinueManualPresence"
        data-testid="continue-manual-presence"
        type="button"
        class="button-secondary"
        :disabled="saving"
        @click="!saving && emit('close-presence', true)"
      >
        {{ t('drafts.presence.continueManual') }}
      </button>
    </div>

    <div
      v-if="canManageManualPresence"
      data-manual-presence
      class="draft-preparation__manual"
      role="group"
      :aria-label="t('drafts.presence.manualPlayer')"
    >
      <label>
        {{ t('drafts.presence.searchPlayer') }}
        <input name="manual-presence-search" autocomplete="off" :value="manualPresenceSearch" type="search" :disabled="saving" @input="updateManualSearch" />
      </label>
      <label>
        {{ t('drafts.presence.selectPlayer') }}
        <select name="manual-presence-player" autocomplete="off" :value="selectedManualPresencePlayerId" :disabled="saving" @change="updateManualSelection">
          <option value="">{{ t('drafts.presence.selectPlayer') }}</option>
          <option v-for="player in availableManualPresencePlayers" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
        </select>
      </label>
      <button
        data-testid="add-manual-presence"
        type="button"
        class="button-secondary"
        :disabled="saving || !selectedManualPresencePlayerId"
        @click="emitUnlessSaving('add-manual-presence')"
      >
        {{ t('drafts.presence.addManual') }}
      </button>
    </div>

    <section v-if="canChooseMode" data-mode-choice class="draft-preparation__mode" :aria-labelledby="`draft-mode-title-${draft.id}`">
      <div>
        <p class="page-kicker">{{ t('drafts.mode.eyebrow') }}</p>
        <h3 :id="`draft-mode-title-${draft.id}`">{{ t('drafts.mode.title') }}</h3>
        <p>{{ t('drafts.mode.description') }}</p>
      </div>
      <div class="draft-preparation__mode-actions">
        <Button data-testid="choose-mode-manual" type="button" variant="outline" :disabled="saving" @click="emit('choose-mode', 'Manual')">
          {{ t('drafts.mode.manual') }}
        </Button>
        <Button data-testid="choose-mode-realtime" data-stage-primary-action type="button" :disabled="saving" @click="emit('choose-mode', 'TempoReal')">
          {{ t('drafts.mode.realtime') }}
        </Button>
      </div>
    </section>

    <ul data-presence-roster class="draft-preparation__roster" :aria-label="t('drafts.presence.rosterLabel')">
      <li
        v-for="presence in confirmedPresences"
        :key="presence.id"
        data-presence-row
        class="draft-preparation__player"
        :class="{ 'draft-preparation__player--captain': captainSelection.includes(presence.jogadorId) }"
      >
        <div data-presence-identity class="draft-preparation__identity">
          <span class="draft-slot__avatar" aria-hidden="true">{{ presence.nomeExibicao.charAt(0) }}</span>
          <strong>{{ presence.nomeExibicao }}</strong>
        </div>
        <span data-presence-origin class="draft-preparation__origin">{{ t(`drafts.presenceOrigin.${presence.origemConfirmacao}`) }}</span>
        <div data-presence-actions class="draft-preparation__player-actions">
          <button
            v-if="canSelectCaptains && eligibleCaptainIds.includes(presence.jogadorId)"
            :data-testid="`toggle-captain-${presence.jogadorId}`"
            type="button"
            class="button-secondary draft-preparation__captain-toggle"
            :class="{ 'draft-preparation__captain-toggle--selected': captainSelection.includes(presence.jogadorId) }"
            :aria-label="t('drafts.presence.toggleCaptain', { name: presence.nomeExibicao })"
            :aria-pressed="captainSelection.includes(presence.jogadorId)"
            :disabled="saving"
            @click="!saving && emit('toggle-captain', presence.jogadorId)"
          >
            {{ t('drafts.roles.captainShort') }}
          </button>
          <button
            v-if="canManageManualPresence"
            data-testid="remove-manual-presence"
            type="button"
            class="button-secondary"
            :disabled="saving"
            @click="!saving && emit('remove-manual-presence', presence.jogadorId, presence.nomeExibicao)"
          >
            {{ t('drafts.presence.removeManual') }}
          </button>
        </div>
      </li>
    </ul>
    <p v-if="confirmedPresences.length === 0" data-presence-empty class="draft-preparation__empty">{{ t('drafts.presence.empty') }}</p>

    <div v-if="canSelectCaptains || canReopenPresence" class="draft-preparation__footer">
      <span v-if="canSelectCaptains" data-captains-count class="team-status">
        {{ t('drafts.presence.captainsCount', { selected: captainSelection.length, total: draft.quantidadeTimes }) }}
      </span>
      <div class="draft-preparation__stage-actions">
        <button
          v-if="canReopenPresence"
          data-testid="reopen-presence"
          type="button"
          class="button-secondary"
          :disabled="saving"
          @click="!saving && emit('reopen-presence')"
        >
          {{ t('drafts.presence.reopen') }}
        </button>
        <button
          v-if="canSelectCaptains"
          data-testid="define-captains"
          data-stage-primary-action
          type="button"
          :disabled="saving || !canDefineCaptains"
          @click="emitUnlessSaving('define-captains')"
        >
          {{ t('drafts.presence.defineCaptains') }}
        </button>
      </div>
    </div>
    <div v-else-if="canDrawOrder" class="draft-preparation__footer">
      <button data-testid="draw-order" data-stage-primary-action type="button" :disabled="saving" @click="emitUnlessSaving('draw-order')">
        {{ t('drafts.presence.drawOrder') }}
      </button>
    </div>
  </section>
</template>

<style scoped>
.draft-preparation {
  display: grid;
  gap: var(--space-md);
  min-width: 0;
}

.draft-preparation__header,
.draft-preparation__stage-actions,
.draft-preparation__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  flex-wrap: wrap;
}

.draft-preparation__header h2,
.draft-preparation__header p {
  margin: 0;
}

.draft-preparation__manual {
  display: grid;
  grid-template-columns: minmax(12rem, 1fr) minmax(12rem, 1fr) auto;
  align-items: end;
  gap: var(--space-xs);
  padding: var(--space-sm);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-lg);
  background: var(--color-surface-2);
}

.draft-preparation__mode {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: var(--space-md);
  padding: var(--space-md);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-lg);
  background: var(--color-surface-2);
}

.draft-preparation__mode :is(h3, p) {
  margin: 0;
}

.draft-preparation__mode-actions {
  display: flex;
  gap: var(--space-xs);
  flex-wrap: wrap;
}

.draft-preparation__manual label {
  display: grid;
  gap: var(--space-xxs);
  min-width: 0;
  color: var(--color-ink-muted);
  font-size: 12px;
}

.draft-preparation__roster {
  display: grid;
  gap: var(--space-xs);
  margin: 0;
  padding: 0;
  list-style: none;
}

.draft-preparation__player {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(7rem, auto) minmax(2.75rem, auto);
  align-items: center;
  gap: var(--space-sm);
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  min-height: 64px;
  padding: var(--space-xs) var(--space-sm);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-lg);
  background: var(--color-surface-2);
  transition:
    border-color var(--duration-fast) var(--ease-standard),
    background var(--duration-fast) var(--ease-standard),
    box-shadow var(--duration-fast) var(--ease-standard);
}

.draft-preparation__player--captain {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
  box-shadow: inset 3px 0 0 var(--color-primary);
}

.draft-preparation__identity {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  min-width: 0;
}

.draft-preparation__identity strong {
  overflow-wrap: anywhere;
}

.draft-preparation__origin {
  color: var(--color-ink-muted);
  font-family: var(--font-data);
  font-size: 12px;
}

.draft-preparation__player-actions {
  display: flex;
  justify-content: end;
  min-width: 2.75rem;
}

.draft-preparation__captain-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-xxs);
}

.draft-preparation__captain-toggle--selected {
  border-color: var(--color-primary);
  color: var(--color-ink);
  background: var(--color-primary);
  box-shadow: 0 0 18px var(--color-glow-primary);
}

.draft-preparation__captain-toggle--selected:hover:not(:disabled) {
  border-color: var(--color-primary-focus);
  background: var(--color-primary-focus);
}

.draft-preparation__empty {
  margin: 0;
  color: var(--color-ink-muted);
}

@media (max-width: 768px) {
  .draft-preparation__manual,
  .draft-preparation__mode,
  .draft-preparation__player {
    grid-template-columns: minmax(0, 1fr);
  }

  .draft-preparation__player-actions {
    justify-content: start;
  }
}
</style>

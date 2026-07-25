<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { DraftMontagem, DraftMontagemPresenca } from '@/types/draftMontagem'

interface EligiblePresencePlayer {
  id: string
  nomeExibicao: string
}

const props = defineProps<{
  draft: DraftMontagem
  confirmedPresences: readonly DraftMontagemPresenca[]
  currentUserHasPresence: boolean
  canManage: boolean
  saving: boolean
  captainSelection: readonly string[]
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
  'toggle-captain': [jogadorId: string]
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

    <div v-if="draft.status === 'PresencaAberta'" class="draft-preparation__stage-actions">
      <button
        v-if="!currentUserHasPresence"
        data-testid="confirm-presence"
        type="button"
        :disabled="saving"
        @click="emitUnlessSaving('confirm-presence')"
      >
        {{ t('drafts.presence.confirm') }}
      </button>
      <button
        v-else
        data-testid="cancel-presence"
        type="button"
        class="button-secondary"
        :disabled="saving"
        @click="emitUnlessSaving('cancel-presence')"
      >
        {{ t('drafts.presence.cancel') }}
      </button>
      <button
        v-if="canManage"
        data-testid="close-presence"
        type="button"
        class="button-secondary"
        :disabled="saving"
        @click="!saving && emit('close-presence', false)"
      >
        {{ t('drafts.presence.close') }}
      </button>
      <button
        v-if="canManage && confirmedPresences.length < 10"
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
      v-if="canManage && draft.status === 'PresencaAberta'"
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

    <ul data-presence-roster class="draft-preparation__roster" :aria-label="t('drafts.presence.rosterLabel')">
      <li v-for="presence in confirmedPresences" :key="presence.id" data-presence-row class="draft-preparation__player">
        <div data-presence-identity class="draft-preparation__identity">
          <span class="draft-slot__avatar" aria-hidden="true">{{ presence.nomeExibicao.charAt(0) }}</span>
          <strong>{{ presence.nomeExibicao }}</strong>
        </div>
        <span data-presence-origin class="draft-preparation__origin">{{ t(`drafts.presenceOrigin.${presence.origemConfirmacao}`) }}</span>
        <div data-presence-actions class="draft-preparation__player-actions">
          <button
            v-if="draft.status === 'PresencaEncerrada' && canManage"
            :data-testid="`toggle-captain-${presence.jogadorId}`"
            type="button"
            class="button-secondary"
            :aria-label="t('drafts.presence.toggleCaptain', { name: presence.nomeExibicao })"
            :aria-pressed="captainSelection.includes(presence.jogadorId)"
            :disabled="saving"
            @click="!saving && emit('toggle-captain', presence.jogadorId)"
          >
            {{ t('drafts.roles.captainShort') }}
          </button>
          <button
            v-if="draft.status === 'PresencaAberta' && canManage"
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

    <div v-if="canManage && draft.status === 'PresencaEncerrada'" class="draft-preparation__footer">
      <button
        data-testid="define-captains"
        type="button"
        :disabled="saving || captainSelection.length !== draft.quantidadeTimes"
        @click="emitUnlessSaving('define-captains')"
      >
        {{ t('drafts.presence.defineCaptains') }}
      </button>
    </div>
    <div v-else-if="canManage && draft.status === 'CapitaesDefinidos'" class="draft-preparation__footer">
      <button data-testid="draw-order" type="button" :disabled="saving" @click="emitUnlessSaving('draw-order')">
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
  font-family: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 12px;
}

.draft-preparation__player-actions {
  display: flex;
  justify-content: end;
  min-width: 2.75rem;
}

.draft-preparation__empty {
  margin: 0;
  color: var(--color-ink-muted);
}

@media (max-width: 768px) {
  .draft-preparation__manual,
  .draft-preparation__player {
    grid-template-columns: minmax(0, 1fr);
  }

  .draft-preparation__player-actions {
    justify-content: start;
  }
}
</style>

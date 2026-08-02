<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import CompetitionForms from './CompetitionForms.vue'
import type {
  CompetitionDetail,
  CreateCompetitionRequest,
  CreateRoundRequest,
  DraftMode,
  SeriesFormat,
} from '@/types/competition'
import type { SeasonSummary } from '@/types/season'

const props = withDefaults(
  defineProps<{
    season: SeasonSummary
    competitions: CompetitionDetail[]
    loading: boolean
    saving: boolean
    serviceMessageCode: string | null
    fieldErrors?: Record<string, string>
    seasonActions?: string[]
    mutationVersion?: number
    canManage?: boolean
  }>(),
  {
    canManage: true,
    fieldErrors: () => ({}),
    seasonActions: () => ['create-competition', 'publish-rules'],
    mutationVersion: 0,
  },
)

const emit = defineEmits<{
  createCompetition: [payload: CreateCompetitionRequest]
  updateCompetition: [competition: CompetitionDetail, payload: CreateCompetitionRequest]
  createRound: [competition: CompetitionDetail, payload: CreateRoundRequest]
  reorderRounds: [competition: CompetitionDetail, roundIds: string[]]
  publishCompetitionRules: [
    competition: CompetitionDetail,
    payload: { formato: SeriesFormat; modoDraft: DraftMode },
  ]
  publishSeasonRules: [payload: { formato: SeriesFormat; modoDraft: DraftMode }]
}>()

const { t, te, locale } = useI18n()
const forms = ref<InstanceType<typeof CompetitionForms> | null>(null)

function canUseSeasonAction(action: string) {
  return props.canManage && props.seasonActions.includes(action)
}

function canUseCompetitionAction(competition: CompetitionDetail, action: string) {
  return props.canManage && competition.acoesPermitidas?.includes(action)
}

function forwardCompetitionUpdate(
  competition: CompetitionDetail,
  payload: CreateCompetitionRequest,
) {
  emit('updateCompetition', competition, payload)
}

function forwardRoundCreate(
  competition: CompetitionDetail,
  payload: CreateRoundRequest,
) {
  emit('createRound', competition, payload)
}

function forwardCompetitionRules(
  competition: CompetitionDetail,
  payload: { formato: SeriesFormat; modoDraft: DraftMode },
) {
  emit('publishCompetitionRules', competition, payload)
}

function moveRound(competition: CompetitionDetail, index: number, offset: number) {
  const target = index + offset
  if (target < 0 || target >= competition.rodadas.length) return
  const ids = competition.rodadas.map((round) => round.id)
  ;[ids[index], ids[target]] = [ids[target]!, ids[index]!]
  emit('reorderRounds', competition, ids)
}

function serviceError(code: string | null) {
  if (!code) return null
  const key = `seasons.errors.codes.${code}`
  return te(key) ? t(key) : t('seasons.errors.competition')
}

function formatInstant(value: string) {
  return new Intl.DateTimeFormat(locale.value.startsWith('en') ? 'en-US' : 'pt-BR', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'America/Sao_Paulo',
  }).format(new Date(value))
}
</script>

<template>
  <section class="competition-panel" aria-labelledby="competition-panel-title">
    <header class="competition-panel__header">
      <div>
        <p>{{ t('seasons.competitions.eyebrow') }}</p>
        <h2 id="competition-panel-title">{{ t('seasons.competitions.title') }}</h2>
        <span>{{ t('seasons.competitions.forSeason', { name: season.nome }) }}</span>
      </div>
      <button
        v-if="canUseSeasonAction('create-competition')"
        type="button"
        :disabled="saving"
        @click="forms?.openCompetition()"
      >
        {{ t('seasons.competitions.create') }}
      </button>
      <button
        v-if="canUseSeasonAction('publish-rules')"
        type="button"
        :disabled="saving"
        @click="forms?.openRules('season')"
      >
        {{ t('seasons.rules.publishSeason') }}
      </button>
    </header>

    <p v-if="serviceError(serviceMessageCode)" class="competition-panel__alert" role="alert">
      {{ serviceError(serviceMessageCode) }}
    </p>

    <div v-if="loading" class="competition-panel__skeletons" :aria-label="t('seasons.loading')">
      <span v-for="index in 2" :key="index" />
    </div>

    <div v-else-if="competitions.length === 0" class="competition-panel__empty">
      <h3>{{ t('seasons.competitions.emptyTitle') }}</h3>
      <p>{{ t('seasons.competitions.emptyDescription') }}</p>
    </div>

    <div v-else class="competition-panel__list">
      <article
        v-for="competition in competitions"
        :key="competition.id"
        class="competition-card"
        :data-competition-id="competition.id"
      >
        <header>
          <div>
            <span class="competition-card__code">{{ competition.codigo }}</span>
            <h3>{{ competition.nome }}</h3>
          </div>
          <span v-if="competition.circuitoDiario" class="competition-card__badge">
            {{ t('seasons.competitions.dailyCircuit') }}
          </span>
        </header>

        <div class="competition-card__actions">
          <button
            v-if="canUseCompetitionAction(competition, 'edit')"
            type="button"
            :disabled="saving"
            @click="forms?.openCompetition(competition)"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="canUseCompetitionAction(competition, 'create-round')"
            type="button"
            :disabled="saving"
            @click="forms?.openRound(competition)"
          >
            {{ t('seasons.rounds.create') }}
          </button>
          <button
            v-if="canUseCompetitionAction(competition, 'publish-rules')"
            type="button"
            :disabled="saving"
            @click="forms?.openRules(competition)"
          >
            {{ t('seasons.rules.publish') }}
          </button>
        </div>

        <ol v-if="competition.rodadas.length" class="round-list">
          <li v-for="(round, index) in competition.rodadas" :key="round.id">
            <span>{{ round.ordem }}</span>
            <strong>{{ round.nome }}</strong>
            <div
              v-if="canUseCompetitionAction(competition, 'create-round')"
              :aria-label="t('seasons.rounds.reorderLabel', { name: round.nome })"
            >
              <button
                type="button"
                :disabled="saving || index === 0"
                :aria-label="t('seasons.rounds.moveUp', { name: round.nome })"
                @click="moveRound(competition, index, -1)"
              >
                ↑
              </button>
              <button
                type="button"
                :disabled="saving || index === competition.rodadas.length - 1"
                :aria-label="t('seasons.rounds.moveDown', { name: round.nome })"
                @click="moveRound(competition, index, 1)"
              >
                ↓
              </button>
            </div>
          </li>
        </ol>
        <p v-else class="competition-card__empty">{{ t('seasons.rounds.empty') }}</p>

        <div v-if="competition.regrasPublicadas.length" class="rules-history">
          <p v-for="rules in competition.regrasPublicadas" :key="rules.id">
            {{
              t('seasons.rules.version', {
                version: rules.numero,
                format: t(`seasons.rules.formats.${rules.formato}`),
                mode: t(`seasons.rules.modes.${rules.modoDraft}`),
              })
            }}
            <time :datetime="rules.publicadaEm">{{ formatInstant(rules.publicadaEm) }}</time>
          </p>
        </div>
      </article>
    </div>

    <CompetitionForms
      ref="forms"
      :season="season"
      :saving="saving"
      :field-errors="fieldErrors"
      :mutation-version="mutationVersion"
      @create-competition="emit('createCompetition', $event)"
      @update-competition="forwardCompetitionUpdate"
      @create-round="forwardRoundCreate"
      @publish-competition-rules="forwardCompetitionRules"
      @publish-season-rules="emit('publishSeasonRules', $event)"
    />
  </section>
</template>

<style scoped>
.competition-panel {
  display: grid;
  min-width: 0;
  gap: var(--space-lg);
  overscroll-behavior: contain;
}

.competition-panel__header,
.competition-card > header,
.competition-card__actions,
.round-list li,
.round-list li div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
}

.competition-panel__header p {
  margin: 0;
  color: var(--color-secondary);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.competition-panel__header h2,
.competition-card h3 {
  margin: var(--space-xxs) 0;
}

.competition-panel__header span,
.competition-card__empty,
.competition-panel__empty p {
  color: var(--color-ink-subtle);
}

.competition-panel button {
  min-height: 44px;
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink);
  background: var(--color-surface-2);
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.competition-panel button {
  padding-inline: var(--space-md);
  overflow-wrap: anywhere;
}

.competition-panel button:not(:disabled):hover {
  border-color: var(--color-primary-hover);
}

.competition-panel button:not(:disabled):hover {
  background: var(--color-surface-3);
}

.competition-panel button:not(:disabled):active {
  transform: translateY(1px);
}

.competition-panel button:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.competition-panel__header > button {
  border-color: var(--color-primary);
  background: var(--color-primary);
}

.competition-panel__list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-md);
}

.competition-card,
.competition-panel__empty {
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
  min-width: 0;
  overflow-wrap: anywhere;
}

.competition-card__code,
.competition-card__badge,
.round-list > li > span,
.rules-history {
  font-family: var(--font-data);
  font-size: 12px;
}

.competition-card__code {
  color: var(--color-secondary);
}

.competition-card__badge {
  padding: var(--space-xxs) var(--space-xs);
  border-radius: var(--radius-pill);
  color: var(--color-success);
  background: color-mix(in srgb, var(--color-success) 14%, transparent);
}

.competition-card__actions {
  justify-content: flex-start;
  flex-wrap: wrap;
  margin-block: var(--space-md);
}

.round-list {
  display: grid;
  gap: var(--space-xs);
  padding: 0;
  list-style: none;
}

.round-list li {
  padding: var(--space-xs);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-md);
}

.round-list li > span {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: var(--radius-sm);
  color: var(--color-primary-hover);
  background: var(--color-primary-soft);
}

.round-list li strong {
  margin-right: auto;
  min-width: 0;
  overflow-wrap: anywhere;
}

.round-list li button {
  min-width: 44px;
  padding: 0;
}

.rules-history {
  color: var(--color-ink-muted);
}

.rules-history time {
  display: block;
  color: var(--color-ink-subtle);
}

.competition-panel__alert {
  margin: 0;
  color: var(--color-danger);
  font-size: 14px;
}

.competition-panel__skeletons {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-md);
}

.competition-panel__skeletons span {
  min-height: 180px;
  border-radius: var(--radius-lg);
  background: var(--color-surface-2);
}

@media (max-width: 767px) {
  .competition-panel__list,
  .competition-panel__skeletons {
    grid-template-columns: 1fr;
  }

  .competition-panel__header {
    align-items: stretch;
    flex-direction: column;
  }

  .competition-panel__header > button,
  .competition-card__actions button {
    width: 100%;
  }

  .competition-card__actions {
    align-items: stretch;
    flex-direction: column;
  }

  .round-list li {
    align-items: flex-start;
    flex-wrap: wrap;
  }
}

@media (prefers-reduced-motion: reduce) {
  .competition-panel button {
    transition: none;
  }

  .competition-panel button:active {
    transform: none;
  }
}
</style>

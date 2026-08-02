<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue-sonner'

import CompetitionPanel from '@/components/competitive/CompetitionPanel.vue'
import SeasonFormDrawer from '@/components/competitive/SeasonFormDrawer.vue'
import SeasonScopeSelector from '@/components/competitive/SeasonScopeSelector.vue'
import SeasonTransitionDialog from '@/components/competitive/SeasonTransitionDialog.vue'
import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'
import * as competitionService from '@/services/competitions'
import * as seasonService from '@/services/seasons'
import type { ApiFieldError } from '@/services/apiErrors'
import type {
  CompetitionDetail,
  CompetitionPage,
  CreateCompetitionRequest,
  CreateRoundRequest,
  PublishRulesRequest,
} from '@/types/competition'
import type {
  CreateSeasonRequest,
  SeasonDetail,
  SeasonPage,
  SeasonScope,
  SeasonState,
  SeasonSummary,
} from '@/types/season'

const { t, te, locale } = useI18n()
const auth = useAuthState()
const page = ref<SeasonPage | null>(null)
const calendarEtag = ref<string | null>(null)
const seasonsLoading = ref(true)
const competitionsLoading = ref(false)
const scopedCompetitionsLoading = ref(true)
const scopedCompetitionPage = ref<CompetitionPage | null>(null)
const saving = ref(false)
const loadError = ref(false)
const alertMessage = ref<string | null>(null)
const selectedSeason = ref<SeasonDetail | null>(null)
const selectedSeasonEtag = ref<string | null>(null)
const competitions = ref<CompetitionDetail[]>([])
const competitionEtags = ref<Record<string, string>>({})
const scope = ref<SeasonScope>(readScopeFromUrl())
const stateFilter = ref<SeasonState | 'all'>('all')
const seasonPageNumber = ref(1)
const seasonPageSize = 20
const formOpen = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const formErrors = ref<Record<string, string>>({})
const competitionFieldErrors = ref<Record<string, string>>({})
const serviceMessageCode = ref<string | null>(null)
const transition = ref<'activate' | 'close' | null>(null)
const panelMutationVersion = ref(0)
let scopedRequestSequence = 0

const seasons = computed(() => page.value?.items ?? [])
const canManageSeasons = computed(() =>
  auth.hasPermission(Permissions.CanManageSeasons),
)
const canManageCompetitions = computed(() =>
  auth.hasPermission(Permissions.CanManageCompetitions),
)

onMounted(async () => {
  globalThis.addEventListener('popstate', restoreScopeFromUrl)
  await Promise.all([loadSeasons(), loadScopedCompetitions()])
})

onBeforeUnmount(() => {
  globalThis.removeEventListener('popstate', restoreScopeFromUrl)
})

function readScopeFromUrl(): SeasonScope {
  const params = new globalThis.URL(globalThis.location.href).searchParams
  const seasonIds = params.getAll('temporadaIds')
  const all = params.get('todas') === 'true'
  if (all && seasonIds.length === 0) return { mode: 'all' }
  if (!all && seasonIds.length > 0 && new Set(seasonIds).size === seasonIds.length) {
    const selected: SeasonScope = {
      mode: 'selected',
      seasonIds: seasonIds as [string, ...string[]],
    }
    try {
      seasonService.serializeSeasonScope(selected)
      return selected
    } catch {
      return { mode: 'current' }
    }
  }
  return { mode: 'current' }
}

async function changeScope(nextScope: SeasonScope) {
  scope.value = nextScope
  writeScopeToUrl(nextScope)
  await loadScopedCompetitions()
}

async function restoreScopeFromUrl() {
  scope.value = readScopeFromUrl()
  await loadScopedCompetitions()
}

function writeScopeToUrl(nextScope: SeasonScope) {
  const url = new globalThis.URL(globalThis.location.href)
  url.searchParams.delete('temporadaIds')
  url.searchParams.delete('todas')
  const serialized = seasonService.serializeSeasonScope(nextScope)
  for (const [key, value] of serialized) url.searchParams.append(key, value)
  globalThis.history.pushState({}, '', `${url.pathname}${url.search}${url.hash}`)
}

async function loadScopedCompetitions() {
  const requestSequence = ++scopedRequestSequence
  scopedCompetitionsLoading.value = true
  try {
    const response = await competitionService.listCompetitions(scope.value)
    if (requestSequence !== scopedRequestSequence) return
    scopedCompetitionPage.value = response
  } catch (error) {
    if (requestSequence !== scopedRequestSequence) return
    scopedCompetitionPage.value = null
    handleReadError(error)
  } finally {
    if (requestSequence === scopedRequestSequence) {
      scopedCompetitionsLoading.value = false
    }
  }
}

async function loadSeasons(preserveSelection = true): Promise<boolean> {
  seasonsLoading.value = true
  loadError.value = false
  try {
    const response = await seasonService.listSeasons({
      page: seasonPageNumber.value,
      pageSize: seasonPageSize,
      ...(stateFilter.value === 'all' ? {} : { estado: stateFilter.value }),
    })
    page.value = response.data
    seasonPageNumber.value = response.data.page
    calendarEtag.value = response.etag
    if (preserveSelection && selectedSeason.value) {
      const latest = response.data.items.find(({ id }) => id === selectedSeason.value?.id)
      if (latest) selectedSeason.value = { ...selectedSeason.value, ...latest }
    }
    return true
  } catch (error) {
    loadError.value = true
    handleReadError(error)
    return false
  } finally {
    seasonsLoading.value = false
  }
}

async function changeStateFilter() {
  seasonPageNumber.value = 1
  await loadSeasons()
}

async function changeSeasonPage(nextPage: number) {
  if (!page.value || nextPage < 1 || nextPage > page.value.totalPages) return
  const previousPage = seasonPageNumber.value
  seasonPageNumber.value = nextPage
  if (!(await loadSeasons())) seasonPageNumber.value = previousPage
}

async function selectSeason(season: SeasonSummary) {
  competitionsLoading.value = true
  serviceMessageCode.value = null
  try {
    const [detailResponse, competitionPage] = await Promise.all([
      seasonService.getSeason(season.id),
      competitionService.listCompetitionsForSeason(season.id),
    ])
    selectedSeason.value = detailResponse.data
    selectedSeasonEtag.value = detailResponse.etag
    await observeCompetitions(competitionPage.items)
    return true
  } catch (error) {
    handleReadError(error)
    return false
  } finally {
    competitionsLoading.value = false
  }
}

async function observeCompetitions(items: CompetitionDetail[]) {
  const nextEtags: Record<string, string> = {}
  let sessionExpired = false
  const observedItems = await Promise.all(
    items.map(async (competition) => {
      try {
        const observed = await competitionService.getCompetition(competition.id)
        if (observed.etag) nextEtags[competition.id] = observed.etag
        return observed.data
      } catch (error) {
        if (error instanceof competitionService.CompetitionServiceError) {
          if (error.status === 401) {
            sessionExpired = true
            auth.clearSession()
            alertMessage.value = t('seasons.errors.unauthorized')
            return { ...competition, acoesPermitidas: [] }
          }
          if (error.status === 403) {
            alertMessage.value = t('seasons.errors.forbidden')
            return { ...competition, acoesPermitidas: [] }
          }
          if (error.status === 404) {
            alertMessage.value = t('seasons.errors.competitionNotFound')
            return null
          }
        }
        handleReadError(error)
        return { ...competition, acoesPermitidas: [] }
      }
    }),
  )
  const availableItems = observedItems.filter(
    (competition): competition is CompetitionDetail => competition !== null,
  )
  competitions.value = sessionExpired
    ? availableItems.map((competition) => ({ ...competition, acoesPermitidas: [] }))
    : availableItems
  competitionEtags.value = nextEtags
}

async function reloadSelectedCompetitions() {
  if (!selectedSeason.value) return
  const response = await competitionService.listCompetitionsForSeason(
    selectedSeason.value.id,
  )
  await observeCompetitions(response.items)
}

function openCreate() {
  if (!canManageSeasons.value) return
  formMode.value = 'create'
  formErrors.value = {}
  serviceMessageCode.value = null
  formOpen.value = true
}

function openEdit() {
  if (
    !canManageSeasons.value ||
    !selectedSeason.value?.acoesPermitidas?.includes('edit')
  ) return
  if (!selectedSeasonEtag.value) {
    alertMessage.value = t('seasons.errors.missingVersion')
    return
  }
  formMode.value = 'edit'
  formErrors.value = {}
  serviceMessageCode.value = null
  formOpen.value = true
}

async function saveSeason(payload: CreateSeasonRequest) {
  if (!canManageSeasons.value) return
  if (
    formMode.value === 'edit' &&
    (!selectedSeason.value || !selectedSeasonEtag.value)
  ) {
    formOpen.value = false
    alertMessage.value = t('seasons.errors.missingVersion')
    return
  }
  saving.value = true
  serviceMessageCode.value = null
  formErrors.value = {}
  alertMessage.value = null
  const completedMode = formMode.value
  try {
    if (completedMode === 'edit') {
      const response = await seasonService.updateSeason(
        selectedSeason.value!.id,
        payload,
        selectedSeasonEtag.value!,
      )
      selectedSeason.value = response.data
      selectedSeasonEtag.value = response.etag
    } else {
      const response = await seasonService.createSeason(payload)
      selectedSeason.value = response.data
      selectedSeasonEtag.value = response.etag
    }
    formOpen.value = false
    toast.success(t(`seasons.messages.${completedMode === 'create' ? 'created' : 'updated'}`))
    if (!(await loadSeasons())) {
      alertMessage.value = t('seasons.errors.refreshAfterSave')
    }
  } catch (error) {
    if (isConflict(error)) {
      await refreshAfterConflict()
      alertMessage.value = t('seasons.errors.resourceConflict')
    } else if (error instanceof seasonService.SeasonServiceError) {
      await handleSeasonMutationError(
        error,
        completedMode === 'edit' ? 'edit' : undefined,
      )
    } else {
      alertMessage.value = t('seasons.errors.save')
    }
  } finally {
    saving.value = false
    if (Object.keys(formErrors.value).length > 0) {
      await nextTick()
      focusFirstSeasonFieldError()
    }
  }
}

function openTransition(action: 'activate' | 'close') {
  if (
    !canManageSeasons.value ||
    !selectedSeason.value?.acoesPermitidas?.includes(action) ||
    !calendarEtag.value
  ) return
  alertMessage.value = null
  transition.value = action
}

async function confirmTransition() {
  if (
    !canManageSeasons.value ||
    !selectedSeason.value ||
    !calendarEtag.value ||
    !transition.value
  ) return
  const action = transition.value
  saving.value = true
  try {
    const response = await (action === 'activate'
      ? seasonService.activateSeason(selectedSeason.value.id, calendarEtag.value)
      : seasonService.closeSeason(selectedSeason.value.id, calendarEtag.value))
    selectedSeason.value = response.data
    selectedSeasonEtag.value = null
    transition.value = null
    toast.success(t(`seasons.messages.${action === 'activate' ? 'activated' : 'closed'}`))
    if (!(await loadSeasons())) {
      alertMessage.value = t('seasons.errors.refreshAfterSave')
      return
    }
    const latest = seasons.value.find(({ id }) => id === response.data.id)
    if (latest && !(await selectSeason(latest))) {
      alertMessage.value = t('seasons.errors.refreshAfterSave')
      return
    }
    alertMessage.value = null
  } catch (error) {
    transition.value = null
    if (isConflict(error)) {
      await refreshAfterConflict()
      alertMessage.value = t('seasons.errors.calendarConflict')
    } else if (error instanceof seasonService.SeasonServiceError) {
      await handleSeasonMutationError(error, action)
    } else {
      alertMessage.value = t(`seasons.errors.${action}`)
    }
  } finally {
    saving.value = false
  }
}

async function refreshAfterConflict() {
  const seasonId = selectedSeason.value?.id
  await loadSeasons()
  if (seasonId) {
    const latest = seasons.value.find(({ id }) => id === seasonId)
    if (latest) await selectSeason(latest)
  }
}

async function createCompetition(payload: CreateCompetitionRequest) {
  if (!canManageCompetitions.value || !selectedSeason.value) return
  await runCompetitionMutation(
    async () => {
      await competitionService.createCompetition(selectedSeason.value!.id, payload)
    },
    undefined,
    'competition',
    undefined,
    'create-competition',
  )
}

async function updateCompetition(
  competition: CompetitionDetail,
  payload: CreateCompetitionRequest,
) {
  if (!canUseCompetitionAction(competition, 'edit')) return
  const etag = competitionEtags.value[competition.id]
  if (!etag) return void handleMissingCompetitionEtag()
  await runCompetitionMutation(
    () => competitionService.updateCompetition(competition.id, payload, etag),
    competition.id,
  )
}

async function createRound(
  competition: CompetitionDetail,
  payload: CreateRoundRequest,
) {
  if (!canUseCompetitionAction(competition, 'create-round')) return
  const etag = competitionEtags.value[competition.id]
  if (!etag) return void handleMissingCompetitionEtag()
  await runCompetitionMutation(
    () => competitionService.createRound(competition.id, payload, etag),
    competition.id,
    'round',
    (response) => preserveCompetitionEtag(competition.id, response.etag),
  )
}

async function reorderRounds(competition: CompetitionDetail, rodadaIds: string[]) {
  if (!canUseCompetitionAction(competition, 'create-round')) return
  const etag = competitionEtags.value[competition.id]
  if (!etag) return void handleMissingCompetitionEtag()
  await runCompetitionMutation(
    () => competitionService.reorderRounds(competition.id, { rodadaIds }, etag),
    competition.id,
  )
}

async function publishCompetitionRules(
  competition: CompetitionDetail,
  payload: PublishRulesRequest,
) {
  if (!canUseCompetitionAction(competition, 'publish-rules')) return
  const etag = competitionEtags.value[competition.id]
  if (!etag) return void handleMissingCompetitionEtag()
  await runCompetitionMutation(
    () => competitionService.publishCompetitionRules(competition.id, payload, etag),
    competition.id,
    'rules',
    (response) => preserveCompetitionEtag(competition.id, response.etag),
  )
}

async function publishSeasonRules(payload: PublishRulesRequest) {
  if (
    !canManageCompetitions.value ||
    !selectedSeason.value ||
    !selectedSeasonEtag.value
  ) return
  await runCompetitionMutation(
    () =>
      competitionService.publishSeasonRules(
        selectedSeason.value!.id,
        payload,
        selectedSeasonEtag.value!,
      ),
    undefined,
    'rules',
    (response) => {
      selectedSeasonEtag.value = response.etag
    },
    'publish-rules',
  )
}

function canUseCompetitionAction(competition: CompetitionDetail, action: string) {
  return canManageCompetitions.value && competition.acoesPermitidas?.includes(action)
}

function handleMissingCompetitionEtag() {
  alertMessage.value = t('seasons.errors.missingVersion')
}

function preserveCompetitionEtag(competitionId: string, etag: string | null) {
  if (!etag) return
  competitionEtags.value = { ...competitionEtags.value, [competitionId]: etag }
}

async function runCompetitionMutation<T>(
  operation: () => Promise<T>,
  competitionId?: string,
  fieldTarget: 'competition' | 'round' | 'rules' = 'competition',
  onAccepted?: (response: T) => void,
  seasonAction?: string,
) {
  saving.value = true
  serviceMessageCode.value = null
  competitionFieldErrors.value = {}
  alertMessage.value = null
  try {
    const response = await operation()
    onAccepted?.(response)
    panelMutationVersion.value += 1
    try {
      await reloadSelectedCompetitions()
    } catch (error) {
      if (
        error instanceof competitionService.CompetitionServiceError &&
        error.status === 401
      ) {
        auth.clearSession()
        clearCompetitionActions()
      }
      alertMessage.value = t('seasons.errors.refreshAfterSave')
    }
  } catch (error) {
    if (error instanceof competitionService.CompetitionServiceError) {
      await handleCompetitionMutationError(
        error,
        competitionId,
        fieldTarget,
        seasonAction,
      )
    } else {
      alertMessage.value = t('seasons.errors.competition')
    }
  } finally {
    saving.value = false
  }
}

function isConflict(error: unknown) {
  return error instanceof seasonService.SeasonServiceError && error.status === 409
}

async function handleSeasonMutationError(
  error: seasonService.SeasonServiceError,
  attemptedAction?: string,
) {
  serviceMessageCode.value = error.messageCode ?? null
  if (error.status === 400) {
    if (error.fieldErrors.length > 0) {
      formErrors.value = mapSeasonFieldErrors(error.fieldErrors)
      alertMessage.value = t('seasons.errors.validation')
    } else {
      serviceMessageCode.value = null
      alertMessage.value = localizedError(error.messageCode, 'save', error.message)
    }
    return
  }
  if (error.status === 401) {
    auth.clearSession()
    clearSeasonActions()
    formOpen.value = false
    transition.value = null
    alertMessage.value = t('seasons.errors.unauthorized')
    return
  }
  if (error.status === 403) {
    removeSeasonAction(attemptedAction)
    alertMessage.value = t('seasons.errors.forbidden')
    return
  }
  if (error.status === 404) {
    formOpen.value = false
    transition.value = null
    selectedSeason.value = null
    selectedSeasonEtag.value = null
    competitions.value = []
    competitionEtags.value = {}
    await loadSeasons(false)
    alertMessage.value = t('seasons.errors.notFound')
    return
  }
  alertMessage.value = localizedError(error.messageCode, 'save', error.message)
}

function focusFirstSeasonFieldError() {
  const fieldIds: Record<string, string> = {
    nome: 'season-name',
    ano: 'season-year',
    ordemNoAno: 'season-order',
    dataInicio: 'season-start-date',
    dataFimExclusiva: 'season-exclusive-end-date',
  }
  const firstField = Object.keys(formErrors.value)[0]
  if (firstField) globalThis.document.getElementById(fieldIds[firstField]!)?.focus()
}

async function handleCompetitionMutationError(
  error: competitionService.CompetitionServiceError,
  competitionId?: string,
  fieldTarget: 'competition' | 'round' | 'rules' = 'competition',
  seasonAction?: string,
) {
  serviceMessageCode.value = error.messageCode ?? null
  if (error.status === 400) {
    if (error.fieldErrors.length > 0) {
      competitionFieldErrors.value = mapCompetitionFieldErrors(
        error.fieldErrors,
        fieldTarget,
      )
      alertMessage.value = t('seasons.errors.validation')
    } else {
      serviceMessageCode.value = null
      alertMessage.value = localizedError(
        error.messageCode,
        'competition',
        error.message,
      )
    }
    return
  }
  if (error.status === 401) {
    auth.clearSession()
    clearCompetitionActions()
    panelMutationVersion.value += 1
    alertMessage.value = t('seasons.errors.unauthorized')
    return
  }
  if (error.status === 403) {
    if (competitionId) {
      clearCompetitionActions(competitionId)
    } else {
      removeSeasonAction(seasonAction)
    }
    panelMutationVersion.value += 1
    alertMessage.value = t('seasons.errors.forbidden')
    return
  }
  if (error.status === 404) {
    if (competitionId) {
      competitions.value = competitions.value.filter(({ id }) => id !== competitionId)
      delete competitionEtags.value[competitionId]
    }
    panelMutationVersion.value += 1
    alertMessage.value = t('seasons.errors.competitionNotFound')
    return
  }
  if (error.status === 409) {
    await reloadSelectedCompetitions()
    alertMessage.value = t('seasons.errors.resourceConflict')
    return
  }
  alertMessage.value = localizedError(
    error.messageCode,
    'competition',
    error.message,
  )
}

function handleReadError(error: unknown) {
  if (
    error instanceof seasonService.SeasonServiceError ||
    error instanceof competitionService.CompetitionServiceError
  ) {
    if (error.status === 401) {
      auth.clearSession()
      alertMessage.value = t('seasons.errors.unauthorized')
    } else if (error.status === 403) {
      alertMessage.value = t('seasons.errors.forbidden')
    } else if (error.status === 404) {
      alertMessage.value = t('seasons.errors.notFound')
    } else {
      alertMessage.value = localizedError(error.messageCode, 'load', error.message)
    }
    return
  }
  alertMessage.value = t('seasons.errors.load')
}

function mapSeasonFieldErrors(apiErrors: ApiFieldError[]) {
  const fields: Record<string, [string, string]> = {
    nome: ['nome', 'seasons.form.validation.name'],
    ano: ['ano', 'seasons.form.validation.year'],
    ordemnoano: ['ordemNoAno', 'seasons.form.validation.order'],
    datainicio: ['dataInicio', 'seasons.form.validation.startDate'],
    datafimexclusiva: ['dataFimExclusiva', 'seasons.form.validation.exclusiveEndDate'],
  }
  const codes: Record<string, [string, string]> = {
    MV106: fields.nome!,
    MV107: ['dataFimExclusiva', 'seasons.form.validation.period'],
  }
  return mapStructuredFieldErrors(apiErrors, fields, codes)
}

function mapCompetitionFieldErrors(
  apiErrors: ApiFieldError[],
  target: 'competition' | 'round' | 'rules',
) {
  if (target === 'rules') return {}
  const fields: Record<string, [string, string]> =
    target === 'round'
      ? {
          nome: ['roundNome', 'seasons.rounds.validation.name'],
          ordem: ['roundOrdem', 'seasons.rounds.validation.order'],
        }
      : {
          nome: ['nome', 'seasons.competitions.validation.name'],
          codigo: ['codigo', 'seasons.competitions.validation.code'],
        }
  return mapStructuredFieldErrors(apiErrors, fields, {})
}

function mapStructuredFieldErrors(
  apiErrors: ApiFieldError[],
  fields: Record<string, [string, string]>,
  codes: Record<string, [string, string]>,
) {
  const result: Record<string, string> = {}
  for (const error of apiErrors) {
    const fieldSegments = error.field.split(/[.[\]]/).filter(Boolean)
    const normalizedField = fieldSegments[fieldSegments.length - 1]?.toLocaleLowerCase('en-US')
    const mapping = (normalizedField && fields[normalizedField]) || codes[error.messageCode]
    if (mapping && !result[mapping[0]]) result[mapping[0]] = error.message
  }
  return result
}

function clearSeasonActions() {
  if (selectedSeason.value) selectedSeason.value.acoesPermitidas = []
}

function removeSeasonAction(action?: string) {
  if (!selectedSeason.value || !action) return
  selectedSeason.value.acoesPermitidas =
    selectedSeason.value.acoesPermitidas?.filter((item) => item !== action) ?? []
}

function clearCompetitionActions(competitionId?: string) {
  competitions.value = competitions.value.map((competition) => ({
    ...competition,
    acoesPermitidas:
      !competitionId || competition.id === competitionId
        ? []
        : competition.acoesPermitidas,
  }))
}

function localizedError(
  messageCode: string | undefined,
  fallback: string,
  localizedMessage?: string,
) {
  if (messageCode) {
    const key = `seasons.errors.codes.${messageCode}`
    if (te(key)) return t(key)
  }
  if (localizedMessage) return localizedMessage
  return t(`seasons.errors.${fallback}`)
}

function stateLabel(state: SeasonState) {
  return t(`seasons.states.${state}`)
}

function activeLocale() {
  return locale.value.startsWith('en') ? 'en-US' : 'pt-BR'
}

function scopedSeasonName(seasonId: string) {
  return scopedCompetitionPage.value?.seasonsIncluidas.find(({ id }) => id === seasonId)?.nome
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(activeLocale(), {
    dateStyle: 'medium',
    timeZone: 'UTC',
  }).format(new Date(`${value}T00:00:00Z`))
}
</script>

<template>
  <PageFrame>
    <main class="seasons-view">
      <PageHeader
        :eyebrow="t('seasons.eyebrow')"
        :title="t('seasons.title')"
        :description="t('seasons.description')"
      >
        <template #actions>
          <button
            v-if="canManageSeasons"
            class="seasons-view__primary"
            type="button"
            @click="openCreate"
          >
            {{ t('seasons.create') }}
          </button>
        </template>
      </PageHeader>

      <p v-if="alertMessage" class="seasons-view__alert" role="alert">
        {{ alertMessage }}
      </p>

      <SeasonScopeSelector
        :model-value="scope"
        :seasons="seasons"
        :calendar-configured="page?.calendarioConfigurado ?? false"
        :loading="seasonsLoading"
        @update:model-value="changeScope"
      />

      <section
        class="scope-summary"
        data-scope-summary
        aria-labelledby="scope-summary-title"
        :aria-busy="scopedCompetitionsLoading"
      >
        <h2 id="scope-summary-title">{{ t('seasons.scope.resultsTitle') }}</h2>
        <p v-if="scopedCompetitionsLoading" role="status" aria-live="polite">
          {{ t('seasons.scope.loading') }}
        </p>
        <template v-else-if="scopedCompetitionPage">
          <div>
            <h3>{{ t('seasons.scope.includedSeasons') }}</h3>
            <ul
              data-included-seasons
              :aria-label="t('seasons.scope.includedSeasons')"
            >
              <li v-for="season in scopedCompetitionPage.seasonsIncluidas" :key="season.id">
                {{ season.nome }}
              </li>
            </ul>
          </div>
          <p v-if="scopedCompetitionPage.totalItems === 0" role="status" aria-live="polite">
            {{ t('seasons.scope.empty') }}
          </p>
          <p v-else role="status" aria-live="polite">
            {{
              t(
                scopedCompetitionPage.totalItems === 1
                  ? 'seasons.scope.countOne'
                  : 'seasons.scope.countMany',
                { count: scopedCompetitionPage.totalItems },
              )
            }}
          </p>
          <ul
            v-if="scopedCompetitionPage.items.length > 0"
            class="scope-summary__competitions"
            :aria-label="t('seasons.scope.competitionList')"
          >
            <li
              v-for="competition in scopedCompetitionPage.items"
              :key="competition.id"
              :data-scope-competition-id="competition.id"
            >
              <strong>{{ competition.nome }}</strong>
              <span>{{ competition.codigo }}</span>
              <span>
                {{
                  t('seasons.scope.inSeason', {
                    name: scopedSeasonName(competition.seasonId),
                  })
                }}
              </span>
            </li>
          </ul>
        </template>
      </section>

      <section class="season-catalog" aria-labelledby="season-catalog-title">
        <header>
          <div>
            <p>{{ t('seasons.catalog.eyebrow') }}</p>
            <h2 id="season-catalog-title">{{ t('seasons.catalog.title') }}</h2>
          </div>
          <label>
            <span>{{ t('common.status') }}</span>
            <select
              v-model="stateFilter"
              name="seasonState"
              autocomplete="off"
              :disabled="seasonsLoading"
              @change="changeStateFilter"
            >
              <option value="all">{{ t('common.all') }}</option>
              <option value="Planejada">{{ stateLabel('Planejada') }}</option>
              <option value="Ativa">{{ stateLabel('Ativa') }}</option>
              <option value="Encerrada">{{ stateLabel('Encerrada') }}</option>
            </select>
          </label>
        </header>

        <div v-if="seasonsLoading" class="season-catalog__skeletons" :aria-label="t('seasons.loading')">
          <span v-for="index in 3" :key="index" />
        </div>
        <div v-else-if="loadError" class="season-catalog__empty">
          <h3>{{ t('seasons.errors.loadTitle') }}</h3>
          <p>{{ t('seasons.errors.load') }}</p>
          <button type="button" @click="loadSeasons()">{{ t('seasons.retry') }}</button>
        </div>
        <div v-else-if="seasons.length === 0" class="season-catalog__empty">
          <h3>{{ t(stateFilter !== 'all' ? 'seasons.emptyFilterTitle' : 'seasons.emptyTitle') }}</h3>
          <p>{{ t(stateFilter !== 'all' ? 'seasons.emptyFilterDescription' : 'seasons.emptyDescription') }}</p>
          <button
            v-if="seasons.length === 0 && canManageSeasons"
            type="button"
            @click="openCreate"
          >
            {{ t('seasons.create') }}
          </button>
        </div>
        <div v-else class="season-catalog__grid">
          <article
            v-for="season in seasons"
            :key="season.id"
            class="season-card"
            :class="{ 'season-card--selected': selectedSeason?.id === season.id }"
            :data-season-id="season.id"
          >
            <header>
              <span class="season-card__order">{{ season.ano }}.{{ season.ordemNoAno }}</span>
              <span class="season-card__state" :data-state="season.estado">
                {{ stateLabel(season.estado) }}
              </span>
            </header>
            <h3>{{ season.nome }}</h3>
            <p>
              <time :datetime="season.dataInicio">{{ formatDate(season.dataInicio) }}</time>
              <span aria-hidden="true">→</span>
              <time :datetime="season.dataFimExclusiva">
                {{ formatDate(season.dataFimExclusiva) }}
              </time>
            </p>
            <button
              type="button"
              :aria-label="t('seasons.select', { name: season.nome })"
              @click="selectSeason(season)"
            >
              {{ t('seasons.open') }}
            </button>
          </article>
        </div>
        <nav
          v-if="!seasonsLoading && !loadError && page && page.totalPages > 0"
          class="season-catalog__pagination"
          data-season-pagination
          :aria-label="t('seasons.pagination.label')"
        >
          <button
            type="button"
            :disabled="page.page <= 1"
            @click="changeSeasonPage(page.page - 1)"
          >
            {{ t('seasons.pagination.previous') }}
          </button>
          <p role="status" aria-live="polite">
            {{
              t('seasons.pagination.status', {
                page: page.page,
                totalPages: page.totalPages,
              })
            }}
          </p>
          <button
            type="button"
            :disabled="page.page >= page.totalPages"
            @click="changeSeasonPage(page.page + 1)"
          >
            {{ t('seasons.pagination.next') }}
          </button>
        </nav>
      </section>

      <section v-if="selectedSeason" class="season-workspace" aria-labelledby="season-workspace-title">
        <header>
          <div>
            <p>{{ t('seasons.workspace.eyebrow') }}</p>
            <h2 id="season-workspace-title">{{ selectedSeason.nome }}</h2>
          </div>
          <div class="season-workspace__actions">
            <button
              v-if="canManageSeasons && selectedSeason.acoesPermitidas?.includes('edit')"
              type="button"
              @click="openEdit"
            >
              {{ t('seasons.edit') }}
            </button>
            <button
              v-if="canManageSeasons && selectedSeason.acoesPermitidas?.includes('activate')"
              type="button"
              @click="openTransition('activate')"
            >
              {{ t('seasons.activate') }}
            </button>
            <button
              v-if="canManageSeasons && selectedSeason.acoesPermitidas?.includes('close')"
              type="button"
              @click="openTransition('close')"
            >
              {{ t('seasons.close') }}
            </button>
          </div>
        </header>

        <CompetitionPanel
          :season="selectedSeason"
          :competitions="competitions"
          :loading="competitionsLoading"
          :saving="saving"
          :service-message-code="serviceMessageCode"
          :field-errors="competitionFieldErrors"
          :can-manage="canManageCompetitions"
          :season-actions="selectedSeason.acoesPermitidas ?? []"
          :mutation-version="panelMutationVersion"
          @create-competition="createCompetition"
          @update-competition="updateCompetition"
          @create-round="createRound"
          @reorder-rounds="reorderRounds"
          @publish-competition-rules="publishCompetitionRules"
          @publish-season-rules="publishSeasonRules"
        />
      </section>
    </main>

    <SeasonFormDrawer
      :open="formOpen"
      :mode="formMode"
      :season="formMode === 'edit' ? selectedSeason : null"
      :saving="saving"
      :field-errors="formErrors"
      :service-message-code="serviceMessageCode"
      @close="formOpen = false"
      @submit="saveSeason"
    />

    <SeasonTransitionDialog
      v-if="transition && selectedSeason && page"
      :open="true"
      :action="transition"
      :season="selectedSeason"
      :current-season="page.temporadaAtual"
      :calendar-version="page.versaoCalendario"
      :confirm-label="t(`seasons.transition.${transition}.confirm`)"
      :busy="saving"
      @cancel="transition = null"
      @confirm="confirmTransition"
    />
  </PageFrame>
</template>

<style scoped>
.seasons-view {
  display: grid;
  width: 100%;
  min-width: 0;
  gap: var(--space-xl);
  overscroll-behavior: contain;
}

.scope-summary {
  min-width: 0;
  padding: var(--space-sm) var(--space-md);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-md);
  color: var(--color-ink-subtle);
  background: var(--color-surface-1);
  font-family: var(--font-data);
  font-size: 12px;
}

.scope-summary p {
  margin: 0;
}

.scope-summary h2,
.scope-summary h3 {
  margin: 0;
  color: var(--color-ink);
}

.scope-summary ul {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
  margin: var(--space-xs) 0 0;
  padding: 0;
  list-style: none;
}

.scope-summary li {
  padding: var(--space-xxs) var(--space-xs);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-md);
  background: var(--color-surface-2);
}

.scope-summary__competitions {
  flex-direction: column;
}

.scope-summary__competitions li {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: var(--space-xs);
}

.seasons-view button,
.season-catalog select {
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

.seasons-view button:not(:disabled):hover,
.season-catalog select:hover {
  border-color: var(--color-primary-hover);
  background: var(--color-surface-3);
}

.seasons-view button:not(:disabled):active {
  transform: translateY(1px);
}

.seasons-view button {
  padding-inline: var(--space-md);
}

.seasons-view button:focus-visible,
.season-catalog select:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.seasons-view .seasons-view__primary {
  border-color: var(--color-primary);
  background: var(--color-primary);
}

.seasons-view__alert {
  margin: 0;
  padding: var(--space-md);
  border: 1px solid var(--color-warning);
  border-radius: var(--radius-md);
  color: var(--color-warning);
  background: color-mix(in srgb, var(--color-warning) 10%, var(--color-surface-1));
}

.season-catalog,
.season-workspace {
  display: grid;
  gap: var(--space-lg);
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-canvas-raised);
  min-width: 0;
}

.season-catalog > header,
.season-workspace > header,
.season-card > header,
.season-workspace__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
}

.season-catalog > header p,
.season-workspace > header p {
  margin: 0;
  color: var(--color-primary-hover);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.season-catalog h2,
.season-workspace h2 {
  margin: var(--space-xxs) 0 0;
}

.season-catalog label {
  display: grid;
  gap: var(--space-xxs);
  color: var(--color-ink-subtle);
  font-size: 14px;
}

.season-catalog select {
  min-width: 180px;
  padding-inline: var(--space-sm);
}

.season-catalog__grid,
.season-catalog__skeletons {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-md);
}

.season-card,
.season-catalog__skeletons span,
.season-catalog__empty {
  min-width: 0;
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
  overflow-wrap: anywhere;
}

.season-card--selected {
  border-color: var(--color-primary);
  box-shadow: inset 3px 0 0 var(--color-primary);
}

.season-card__order,
.season-card__state {
  font-family: var(--font-data);
  font-size: 12px;
}

.season-card__order {
  color: var(--color-secondary);
}

.season-card__state {
  padding: var(--space-xxs) var(--space-xs);
  border-radius: var(--radius-pill);
  color: var(--color-ink-muted);
  background: var(--color-surface-2);
}

.season-card__state[data-state='Ativa'] {
  color: var(--color-success);
}

.season-card h3 {
  margin-block: var(--space-md) var(--space-xs);
}

.season-card p {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.season-card > button {
  width: 100%;
}

.season-catalog__skeletons span {
  min-height: 180px;
  background: var(--color-surface-2);
}

.season-catalog__empty {
  text-align: center;
}

.season-catalog__empty p {
  color: var(--color-ink-subtle);
}

.season-catalog__pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-md);
}

.season-catalog__pagination p {
  margin: 0;
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.season-workspace__actions {
  flex-wrap: wrap;
}

.season-workspace > header > div,
.season-card > header,
.season-card h3,
.season-card p {
  min-width: 0;
}

@media (max-width: 1023px) {
  .season-catalog__grid,
  .season-catalog__skeletons {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 767px) {
  .season-catalog,
  .season-workspace {
    padding: var(--space-md);
  }

  .season-catalog > header,
  .season-workspace > header {
    align-items: stretch;
    flex-direction: column;
  }

  .season-catalog__grid,
  .season-catalog__skeletons {
    grid-template-columns: 1fr;
  }

  .season-catalog select {
    width: 100%;
  }

  .season-catalog__pagination {
    align-items: stretch;
    flex-direction: column;
    text-align: center;
  }
}

@media (prefers-reduced-motion: reduce) {
  .seasons-view button,
  .season-catalog select {
    transition: none;
  }

  .seasons-view button:active {
    transform: none;
  }
}
</style>

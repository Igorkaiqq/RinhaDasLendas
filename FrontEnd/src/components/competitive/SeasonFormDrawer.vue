<script setup lang="ts">
import { nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type { CreateSeasonRequest, SeasonDetail } from '@/types/season'

import { useModalFocus } from './useModalFocus'

const props = defineProps<{
  open: boolean
  mode: 'create' | 'edit'
  season: SeasonDetail | null
  saving: boolean
  fieldErrors: Record<string, string>
  serviceMessageCode: string | null
}>()

const emit = defineEmits<{
  close: []
  submit: [payload: CreateSeasonRequest]
}>()

const { t, te } = useI18n()
const form = reactive({
  nome: '',
  ano: '',
  ordemNoAno: '',
  dataInicio: '',
  dataFimExclusiva: '',
})
const errors = reactive<Record<string, string>>({})
const panel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(null)
const nameInput = ref<InstanceType<typeof globalThis.HTMLInputElement> | null>(null)

const fields = [
  { key: 'nome', id: 'season-name', errorId: 'season-name-error' },
  { key: 'ano', id: 'season-year', errorId: 'season-year-error' },
  { key: 'ordemNoAno', id: 'season-order', errorId: 'season-order-error' },
  { key: 'dataInicio', id: 'season-start-date', errorId: 'season-start-date-error' },
  {
    key: 'dataFimExclusiva',
    id: 'season-exclusive-end-date',
    errorId: 'season-exclusive-end-date-error',
  },
] as const

watch(
  () => [props.open, props.season, props.mode] as const,
  ([open, season]) => {
    if (!open) return
    form.nome = season?.nome ?? ''
    form.ano = season ? String(season.ano) : ''
    form.ordemNoAno = season ? String(season.ordemNoAno) : ''
    form.dataInicio = season?.dataInicio ?? ''
    form.dataFimExclusiva = season?.dataFimExclusiva ?? ''
    clearErrors()
  },
  { immediate: true },
)

watch(
  () => props.fieldErrors,
  async (fieldErrors) => {
    clearErrors()
    Object.assign(errors, fieldErrors)
    if (Object.keys(fieldErrors).length > 0) {
      await nextTick()
      const firstInvalid = fields.find((field) => errors[field.key])
      if (firstInvalid) globalThis.document.getElementById(firstInvalid.id)?.focus()
    }
  },
  { deep: true },
)

useModalFocus(
  () => props.open,
  panel,
  requestClose,
  () => nameInput.value,
)

function requestClose() {
  if (!props.saving) emit('close')
}

function clearErrors() {
  for (const key of Object.keys(errors)) delete errors[key]
}

function describedBy(key: string, helpId?: string) {
  return [helpId, errors[key] ? fields.find((field) => field.key === key)?.errorId : null]
    .filter(Boolean)
    .join(' ') || undefined
}

function validate() {
  clearErrors()
  if (!form.nome.trim()) errors.nome = t('seasons.form.validation.name')
  const year = Number(form.ano)
  if (!form.ano || !Number.isInteger(year) || year < 2009 || year > 9999) {
    errors.ano = t('seasons.form.validation.year')
  }
  const order = Number(form.ordemNoAno)
  if (!form.ordemNoAno || !Number.isInteger(order) || order <= 0) {
    errors.ordemNoAno = t('seasons.form.validation.order')
  }
  if (!form.dataInicio) errors.dataInicio = t('seasons.form.validation.startDate')
  if (!form.dataFimExclusiva) {
    errors.dataFimExclusiva = t('seasons.form.validation.exclusiveEndDate')
  }
  return Object.keys(errors).length === 0
}

async function submit() {
  if (props.saving) return
  if (!validate()) {
    await nextTick()
    const firstInvalid = fields.find((field) => errors[field.key])
    if (firstInvalid) globalThis.document.getElementById(firstInvalid.id)?.focus()
    return
  }

  emit('submit', {
    nome: form.nome.trim(),
    ano: Number(form.ano),
    ordemNoAno: Number(form.ordemNoAno),
    dataInicio: form.dataInicio,
    dataFimExclusiva: form.dataFimExclusiva,
  })
}

function serviceError() {
  if (!props.serviceMessageCode) return null
  const key = `seasons.errors.codes.${props.serviceMessageCode}`
  return te(key) ? t(key) : t('seasons.errors.save')
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="season-drawer">
      <button
        class="season-drawer__backdrop"
        type="button"
        :aria-label="t('common.close')"
        :disabled="saving"
        @click="requestClose"
      />
      <section
        ref="panel"
        class="season-drawer__panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="season-drawer-title"
        tabindex="-1"
      >
        <header>
          <div>
            <p>{{ t('seasons.form.eyebrow') }}</p>
            <h2 id="season-drawer-title">
              {{ t(`seasons.form.${mode}Title`) }}
            </h2>
          </div>
          <button
            type="button"
            :aria-label="t('common.close')"
            :disabled="saving"
            @click="requestClose"
          >
            ×
          </button>
        </header>

        <form id="season-form" novalidate @submit.prevent="submit">
          <p v-if="serviceError()" class="season-drawer__summary" role="alert">
            {{ serviceError() }}
          </p>

          <div class="season-drawer__field season-drawer__field--wide">
            <label for="season-name">{{ t('seasons.form.name') }}</label>
            <input
              id="season-name"
              ref="nameInput"
              v-model="form.nome"
              name="seasonName"
              autocomplete="off"
              :disabled="saving"
              :aria-invalid="Boolean(errors.nome)"
              :aria-errormessage="errors.nome ? 'season-name-error' : undefined"
              :aria-describedby="describedBy('nome')"
            />
            <p v-if="errors.nome" id="season-name-error" role="alert">{{ errors.nome }}</p>
          </div>

          <div class="season-drawer__field">
            <label for="season-year">{{ t('seasons.form.year') }}</label>
            <input
              id="season-year"
              v-model="form.ano"
              name="seasonYear"
              autocomplete="off"
              type="number"
              inputmode="numeric"
              :disabled="saving"
              :aria-invalid="Boolean(errors.ano)"
              :aria-errormessage="errors.ano ? 'season-year-error' : undefined"
              :aria-describedby="describedBy('ano')"
            />
            <p v-if="errors.ano" id="season-year-error" role="alert">{{ errors.ano }}</p>
          </div>

          <div class="season-drawer__field">
            <label for="season-order">{{ t('seasons.form.order') }}</label>
            <input
              id="season-order"
              v-model="form.ordemNoAno"
              name="seasonOrder"
              autocomplete="off"
              type="number"
              inputmode="numeric"
              :disabled="saving"
              :aria-invalid="Boolean(errors.ordemNoAno)"
              :aria-errormessage="errors.ordemNoAno ? 'season-order-error' : undefined"
              :aria-describedby="describedBy('ordemNoAno')"
            />
            <p v-if="errors.ordemNoAno" id="season-order-error" role="alert">
              {{ errors.ordemNoAno }}
            </p>
          </div>

          <div class="season-drawer__field">
            <label for="season-start-date">{{ t('seasons.form.startDate') }}</label>
            <input
              id="season-start-date"
              v-model="form.dataInicio"
              name="seasonStartDate"
              autocomplete="off"
              type="date"
              :disabled="saving"
              :aria-invalid="Boolean(errors.dataInicio)"
              :aria-errormessage="errors.dataInicio ? 'season-start-date-error' : undefined"
              :aria-describedby="describedBy('dataInicio')"
            />
            <p v-if="errors.dataInicio" id="season-start-date-error" role="alert">
              {{ errors.dataInicio }}
            </p>
          </div>

          <div class="season-drawer__field">
            <label for="season-exclusive-end-date">{{ t('seasons.form.exclusiveEndDate') }}</label>
            <input
              id="season-exclusive-end-date"
              v-model="form.dataFimExclusiva"
              name="seasonExclusiveEndDate"
              autocomplete="off"
              type="date"
              :disabled="saving"
              :aria-invalid="Boolean(errors.dataFimExclusiva)"
              :aria-errormessage="errors.dataFimExclusiva ? 'season-exclusive-end-date-error' : undefined"
              :aria-describedby="describedBy('dataFimExclusiva', 'season-exclusive-end-help')"
            />
            <p id="season-exclusive-end-help" data-exclusive-end-help>
              {{ t('seasons.form.exclusiveEndHelp') }}
            </p>
            <p
              v-if="errors.dataFimExclusiva"
              id="season-exclusive-end-date-error"
              role="alert"
            >
              {{ errors.dataFimExclusiva }}
            </p>
          </div>

          <footer>
            <button type="button" :disabled="saving" @click="requestClose">
              {{ t('common.cancel') }}
            </button>
            <button class="season-drawer__primary" type="submit" :disabled="saving">
              {{ saving ? t('common.saving') : t('seasons.form.save') }}
            </button>
          </footer>
        </form>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.season-drawer {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  justify-content: flex-end;
}

.season-drawer__backdrop {
  position: absolute;
  inset: 0;
  width: 100%;
  border: 0;
  background: var(--color-overlay);
}

.season-drawer__panel {
  position: relative;
  width: min(100%, 680px);
  height: 100%;
  overflow: auto;
  overscroll-behavior: contain;
  padding: var(--space-lg);
  border-left: 1px solid var(--color-hairline-strong);
  background: var(--color-canvas-raised);
  box-shadow: var(--shadow-lg);
  overflow-wrap: anywhere;
}

.season-drawer__panel > header,
.season-drawer__panel footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
}

.season-drawer__panel header p {
  margin: 0 0 var(--space-xxs);
  color: var(--color-primary-hover);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.season-drawer__panel h2 {
  margin: 0;
}

.season-drawer__panel form {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-md);
  margin-top: var(--space-xl);
}

.season-drawer__field,
.season-drawer__field label {
  display: grid;
  gap: var(--space-xs);
}

.season-drawer__field--wide,
.season-drawer__summary,
.season-drawer__panel footer {
  grid-column: 1 / -1;
}

.season-drawer__field input {
  width: 100%;
  min-width: 0;
  min-height: var(--control-height-lg);
  padding-inline: var(--space-sm);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink);
  background: var(--color-surface-1);
}

.season-drawer__field input:focus-visible,
.season-drawer__panel button:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.season-drawer__field p,
.season-drawer__summary {
  margin: 0;
  color: var(--color-danger);
  font-size: 14px;
}

.season-drawer__field [data-exclusive-end-help] {
  color: var(--color-ink-subtle);
}

.season-drawer__panel footer {
  justify-content: flex-end;
  margin-top: var(--space-md);
  padding-top: var(--space-md);
  border-top: 1px solid var(--color-hairline-soft);
}

.season-drawer__panel button {
  min-height: var(--control-height-md);
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

.season-drawer__panel button:not(:disabled):hover {
  border-color: var(--color-primary-hover);
  background: var(--color-surface-3);
}

.season-drawer__panel button:not(:disabled):active {
  transform: translateY(1px);
}

.season-drawer__panel .season-drawer__primary {
  border-color: var(--color-primary);
  background: var(--color-primary);
}

@media (max-width: 479px) {
  .season-drawer__panel {
    padding: var(--space-md);
  }

  .season-drawer__panel form {
    grid-template-columns: 1fr;
  }

  .season-drawer__panel footer {
    display: grid;
  }
}

@media (prefers-reduced-motion: reduce) {
  .season-drawer__panel {
    scroll-behavior: auto;
  }

  .season-drawer__panel button {
    transition: none;
  }

  .season-drawer__panel button:active {
    transform: none;
  }
}
</style>

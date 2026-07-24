<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import type {
  IsoWeekday,
  PresenceScheduleSummary,
  SavePresenceScheduleRequest,
} from '@/types/presenceSchedule'

const weekdays: IsoWeekday[] = [
  'Segunda', 'Terca', 'Quarta', 'Quinta', 'Sexta', 'Sabado', 'Domingo',
]

interface FocusTarget {
  focus: () => void
}

const props = defineProps<{
  open: boolean
  mode: 'create' | 'edit'
  schedule: PresenceScheduleSummary | null
  saving: boolean
  serviceMessageCode: string | null
  returnFocusTo?: FocusTarget
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  submit: [payload: SavePresenceScheduleRequest]
}>()

const { t, te } = useI18n()
const nameInput = ref<InstanceType<typeof Input> | null>(null)
const observationInput = ref<InstanceType<typeof Textarea> | null>(null)
const weekdayOptions = ref<InstanceType<typeof ToggleGroup> | null>(null)
const publicationInput = ref<InstanceType<typeof Input> | null>(null)
const closingInput = ref<InstanceType<typeof Input> | null>(null)
const submitLocked = ref(false)
const form = reactive<SavePresenceScheduleRequest>({
  nome: '',
  observacao: null,
  diasSemana: [],
  horarioPublicacao: '',
  horarioEncerramento: '',
})
const errors = reactive({
  nome: '',
  observacao: '',
  diasSemana: '',
  horarioPublicacao: '',
  horarioEncerramento: '',
})

const title = computed(() => t(`settings.presenceSchedules.form.${props.mode}.title`))
const description = computed(() => t(`settings.presenceSchedules.form.${props.mode}.description`))
const submitLabel = computed(() => t(`settings.presenceSchedules.form.${props.mode}.submit`))
const serviceMessageLabel = computed(() => {
  if (!props.serviceMessageCode) return ''
  const key = `settings.presenceSchedules.messageCodes.${props.serviceMessageCode}`
  return te(key) ? t(key) : t('settings.presenceSchedules.messageCodes.requestFailed')
})

watch(
  () => props.open,
  (open, wasOpen) => {
    if (open) resetForm()
    else if (wasOpen) restoreFocus()
  },
  { immediate: true },
)

watch(
  () => props.saving,
  (saving, wasSaving) => {
    if (wasSaving && !saving) submitLocked.value = false
  },
)

function resetForm() {
  Object.assign(form, {
    nome: props.schedule?.nome ?? '',
    observacao: props.schedule?.observacao ?? null,
    diasSemana: [...(props.schedule?.diasSemana ?? [])],
    horarioPublicacao: props.schedule?.horarioPublicacao.slice(0, 5) ?? '',
    horarioEncerramento: props.schedule?.horarioEncerramento.slice(0, 5) ?? '',
  })
  clearErrors()
  submitLocked.value = false
}

function clearErrors() {
  Object.keys(errors).forEach((key) => { errors[key as keyof typeof errors] = '' })
}

function validate() {
  clearErrors()
  const nameLength = form.nome.trim().length
  const observationLength = form.observacao?.trim().length ?? 0

  if (nameLength < 3 || nameLength > 100) errors.nome = t('settings.presenceSchedules.validation.nameLength')
  if (observationLength > 500) errors.observacao = t('settings.presenceSchedules.validation.observationLength')
  if (form.diasSemana.length === 0) errors.diasSemana = t('settings.presenceSchedules.validation.weekdayRequired')
  if (!form.horarioPublicacao) errors.horarioPublicacao = t('settings.presenceSchedules.validation.publicationRequired')
  if (!form.horarioEncerramento || form.horarioEncerramento <= form.horarioPublicacao) {
    errors.horarioEncerramento = t('settings.presenceSchedules.validation.closingAfterPublication')
  }

  return !Object.values(errors).some(Boolean)
}

async function submit() {
  if (props.saving || submitLocked.value) return
  if (!validate()) {
    await focusFirstInvalid()
    return
  }
  submitLocked.value = true
  emit('submit', {
    nome: form.nome.trim(),
    observacao: form.observacao?.trim() || null,
    diasSemana: [...form.diasSemana],
    horarioPublicacao: form.horarioPublicacao.slice(0, 5),
    horarioEncerramento: form.horarioEncerramento.slice(0, 5),
  })
}

async function focusFirstInvalid() {
  await nextTick()
  const target = errors.nome
    ? nameInput.value
    : errors.observacao
      ? observationInput.value
      : errors.diasSemana
        ? weekdayOptions.value
        : errors.horarioPublicacao
          ? publicationInput.value
          : closingInput.value
  target?.$el?.focus()
}

function updateWeekdays(value: unknown) {
  form.diasSemana = Array.isArray(value) ? value as IsoWeekday[] : []
}

function setOpen(open: boolean) {
  if (props.saving) return
  emit('update:open', open)
}

function closeOnEscape() {
  setOpen(false)
}

function handleOpenAutoFocus(event: { preventDefault: () => void }) {
  event.preventDefault()
  if (globalThis.matchMedia?.('(max-width: 760px)').matches) return
  nameInput.value?.$el?.focus()
}

async function restoreFocus() {
  await nextTick()
  props.returnFocusTo?.focus()
}
</script>

<template>
  <Dialog :open="open" @update:open="setOpen">
    <DialogContent
      class="presence-schedule-dialog sm:max-w-2xl"
      @keydown.esc.stop="closeOnEscape"
      @open-auto-focus="handleOpenAutoFocus"
    >
      <DialogHeader>
        <DialogTitle>{{ title }}</DialogTitle>
        <DialogDescription>{{ description }}</DialogDescription>
      </DialogHeader>

      <form id="presence-schedule-form" @submit.prevent="submit">
        <FieldGroup class="presence-schedule-form">
          <Field :data-invalid="Boolean(errors.nome)">
            <FieldLabel for="presence-schedule-name">{{ t('settings.presenceSchedules.fields.name.label') }}</FieldLabel>
            <Input
              id="presence-schedule-name"
              ref="nameInput"
              v-model="form.nome"
              :placeholder="t('settings.presenceSchedules.fields.name.placeholder')"
              :aria-invalid="Boolean(errors.nome)"
              :aria-describedby="errors.nome ? 'presence-schedule-name-error' : undefined"
              :aria-errormessage="errors.nome ? 'presence-schedule-name-error' : undefined"
              :disabled="saving"
              maxlength="100"
              autocomplete="off"
              name="presenceScheduleName"
            />
            <FieldError v-if="errors.nome" id="presence-schedule-name-error">{{ errors.nome }}</FieldError>
          </Field>

          <Field :data-invalid="Boolean(errors.observacao)">
            <FieldLabel for="presence-schedule-observation">{{ t('settings.presenceSchedules.fields.observation.label') }}</FieldLabel>
            <Textarea
              id="presence-schedule-observation"
              ref="observationInput"
              :model-value="form.observacao ?? ''"
              :placeholder="t('settings.presenceSchedules.fields.observation.placeholder')"
              :aria-invalid="Boolean(errors.observacao)"
              :aria-describedby="errors.observacao ? 'presence-schedule-observation-description presence-schedule-observation-error' : 'presence-schedule-observation-description'"
              :aria-errormessage="errors.observacao ? 'presence-schedule-observation-error' : undefined"
              :disabled="saving"
              maxlength="500"
              autocomplete="off"
              name="presenceScheduleObservation"
              rows="3"
              @update:model-value="form.observacao = String($event)"
            />
            <FieldDescription id="presence-schedule-observation-description">{{ t('settings.presenceSchedules.fields.observation.description') }}</FieldDescription>
            <FieldError v-if="errors.observacao" id="presence-schedule-observation-error">{{ errors.observacao }}</FieldError>
          </Field>

          <FieldSet class="presence-schedule-form__weekdays" :data-invalid="Boolean(errors.diasSemana)">
            <FieldLegend variant="label">{{ t('settings.presenceSchedules.fields.weekdays.label') }}</FieldLegend>
            <FieldDescription id="presence-schedule-weekdays-description">{{ t('settings.presenceSchedules.fields.weekdays.description') }}</FieldDescription>
            <ToggleGroup
              ref="weekdayOptions"
              type="multiple"
              variant="outline"
              :model-value="form.diasSemana"
              :disabled="saving"
              :aria-label="t('settings.presenceSchedules.accessibility.weekdayOptions')"
              :aria-invalid="Boolean(errors.diasSemana)"
              :aria-describedby="errors.diasSemana ? 'presence-schedule-weekdays-description presence-schedule-weekdays-error' : 'presence-schedule-weekdays-description'"
              :aria-errormessage="errors.diasSemana ? 'presence-schedule-weekdays-error' : undefined"
              @update:model-value="updateWeekdays"
            >
              <ToggleGroupItem
                v-for="day in weekdays"
                :key="day"
                :value="day"
                :data-weekday="day"
                :aria-pressed="form.diasSemana.includes(day)"
              >
                {{ t(`settings.presenceSchedules.weekdays.${day}`) }}
              </ToggleGroupItem>
            </ToggleGroup>
            <FieldError v-if="errors.diasSemana" id="presence-schedule-weekdays-error">{{ errors.diasSemana }}</FieldError>
          </FieldSet>

          <div class="presence-schedule-form__times">
            <Field :data-invalid="Boolean(errors.horarioPublicacao)">
              <FieldLabel for="presence-schedule-publication">{{ t('settings.presenceSchedules.fields.publication.label') }}</FieldLabel>
              <Input id="presence-schedule-publication" ref="publicationInput" v-model="form.horarioPublicacao" name="presenceSchedulePublication" type="time" step="60" :disabled="saving" :aria-invalid="Boolean(errors.horarioPublicacao)" :aria-describedby="errors.horarioPublicacao ? 'presence-schedule-publication-error' : undefined" :aria-errormessage="errors.horarioPublicacao ? 'presence-schedule-publication-error' : undefined" />
              <FieldError v-if="errors.horarioPublicacao" id="presence-schedule-publication-error">{{ errors.horarioPublicacao }}</FieldError>
            </Field>
            <Field :data-invalid="Boolean(errors.horarioEncerramento)">
              <FieldLabel for="presence-schedule-closing">{{ t('settings.presenceSchedules.fields.closing.label') }}</FieldLabel>
              <Input id="presence-schedule-closing" ref="closingInput" v-model="form.horarioEncerramento" name="presenceScheduleClosing" type="time" step="60" :disabled="saving" :aria-invalid="Boolean(errors.horarioEncerramento)" :aria-describedby="errors.horarioEncerramento ? 'presence-schedule-closing-error' : undefined" :aria-errormessage="errors.horarioEncerramento ? 'presence-schedule-closing-error' : undefined" />
              <FieldError v-if="errors.horarioEncerramento" id="presence-schedule-closing-error">{{ errors.horarioEncerramento }}</FieldError>
            </Field>
          </div>

          <p class="presence-schedule-form__summary">{{ t('settings.presenceSchedules.form.summary') }}</p>
          <p v-if="serviceMessageCode" data-service-error class="form-error" role="alert">
            {{ serviceMessageLabel }}
          </p>
        </FieldGroup>
      </form>

      <DialogFooter>
        <Button type="button" variant="outline" :disabled="saving" @click="setOpen(false)">
          {{ t('settings.presenceSchedules.actions.cancel') }}
        </Button>
        <Button type="submit" form="presence-schedule-form" :disabled="saving || submitLocked">
          <Spinner v-if="saving" data-icon="inline-start" />
          {{ saving ? t('settings.presenceSchedules.actions.saving') : submitLabel }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>

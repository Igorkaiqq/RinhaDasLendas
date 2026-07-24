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

const { t } = useI18n()
const nameInput = ref<InstanceType<typeof Input> | null>(null)
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

watch(
  () => props.open,
  (open) => {
    if (open) resetForm()
    else restoreFocus()
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

function submit() {
  if (props.saving || submitLocked.value || !validate()) return
  submitLocked.value = true
  emit('submit', {
    nome: form.nome.trim(),
    observacao: form.observacao?.trim() || null,
    diasSemana: [...form.diasSemana],
    horarioPublicacao: form.horarioPublicacao.slice(0, 5),
    horarioEncerramento: form.horarioEncerramento.slice(0, 5),
  })
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
      @open-auto-focus="() => nameInput?.$el?.focus()"
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
              :disabled="saving"
              maxlength="100"
              autocomplete="off"
            />
            <FieldError v-if="errors.nome">{{ errors.nome }}</FieldError>
          </Field>

          <Field :data-invalid="Boolean(errors.observacao)">
            <FieldLabel for="presence-schedule-observation">{{ t('settings.presenceSchedules.fields.observation.label') }}</FieldLabel>
            <Textarea
              id="presence-schedule-observation"
              :model-value="form.observacao ?? ''"
              :placeholder="t('settings.presenceSchedules.fields.observation.placeholder')"
              :aria-invalid="Boolean(errors.observacao)"
              :disabled="saving"
              maxlength="500"
              rows="3"
              @update:model-value="form.observacao = String($event)"
            />
            <FieldDescription>{{ t('settings.presenceSchedules.fields.observation.description') }}</FieldDescription>
            <FieldError v-if="errors.observacao">{{ errors.observacao }}</FieldError>
          </Field>

          <FieldSet class="presence-schedule-form__weekdays" :data-invalid="Boolean(errors.diasSemana)">
            <FieldLegend variant="label">{{ t('settings.presenceSchedules.fields.weekdays.label') }}</FieldLegend>
            <FieldDescription>{{ t('settings.presenceSchedules.fields.weekdays.description') }}</FieldDescription>
            <ToggleGroup
              type="multiple"
              variant="outline"
              :model-value="form.diasSemana"
              :disabled="saving"
              :aria-label="t('settings.presenceSchedules.accessibility.weekdayOptions')"
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
            <FieldError v-if="errors.diasSemana">{{ errors.diasSemana }}</FieldError>
          </FieldSet>

          <div class="presence-schedule-form__times">
            <Field :data-invalid="Boolean(errors.horarioPublicacao)">
              <FieldLabel for="presence-schedule-publication">{{ t('settings.presenceSchedules.fields.publication.label') }}</FieldLabel>
              <Input id="presence-schedule-publication" v-model="form.horarioPublicacao" type="time" step="60" :disabled="saving" :aria-invalid="Boolean(errors.horarioPublicacao)" />
              <FieldError v-if="errors.horarioPublicacao">{{ errors.horarioPublicacao }}</FieldError>
            </Field>
            <Field :data-invalid="Boolean(errors.horarioEncerramento)">
              <FieldLabel for="presence-schedule-closing">{{ t('settings.presenceSchedules.fields.closing.label') }}</FieldLabel>
              <Input id="presence-schedule-closing" v-model="form.horarioEncerramento" type="time" step="60" :disabled="saving" :aria-invalid="Boolean(errors.horarioEncerramento)" />
              <FieldError v-if="errors.horarioEncerramento">{{ errors.horarioEncerramento }}</FieldError>
            </Field>
          </div>

          <p class="presence-schedule-form__summary">{{ t('settings.presenceSchedules.form.summary') }}</p>
          <p v-if="serviceMessageCode" class="form-error" role="alert">
            {{ t(`settings.presenceSchedules.messageCodes.${serviceMessageCode}`) }}
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

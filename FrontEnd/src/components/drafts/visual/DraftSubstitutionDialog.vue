<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import type { DraftMontagemParticipante, DraftMontagemSubstituicaoPayload, DraftMontagemTime } from '@/types/draftMontagem'

type DialogAutoFocusEvent = InstanceType<typeof globalThis.Event>

const props = withDefaults(defineProps<{
  open: boolean
  team: DraftMontagemTime
  outgoingPlayer: DraftMontagemParticipante
  reserves: DraftMontagemParticipante[]
  eligibleCaptainIds: string[]
  saving: boolean
  requiresNewCaptain?: boolean | null
}>(), {
  requiresNewCaptain: null,
})

const emit = defineEmits<{
  confirm: [payload: DraftMontagemSubstituicaoPayload]
  cancel: []
  'restore-focus': []
}>()

const { t } = useI18n()
const selectedReserveId = ref('')
const selectedCaptainId = ref('')
const reason = ref('')
const submitted = ref(false)
const commandSubmitted = ref(false)
const reserveTrigger = useTemplateRef<InstanceType<typeof SelectTrigger>>('reserveTrigger')
const cancelButton = useTemplateRef<InstanceType<typeof Button>>('cancelButton')
const captainLeaving = computed(() => props.requiresNewCaptain ?? (props.outgoingPlayer.capitao || props.team.capitaoId === props.outgoingPlayer.jogadorId))
const selectedReserve = computed(() => props.reserves.find((reserve) => reserve.jogadorId === selectedReserveId.value) ?? null)
const eligibleCaptainIds = computed(() => new Set(props.eligibleCaptainIds))
const captainCandidates = computed(() => {
  const remainingPlayers = props.team.jogadores.filter((player) => player.jogadorId !== props.outgoingPlayer.jogadorId)
  const resultingPlayers = selectedReserve.value ? [...remainingPlayers, selectedReserve.value] : remainingPlayers
  return resultingPlayers.filter((player) => eligibleCaptainIds.value.has(player.jogadorId))
})
const reasonTooLong = computed(() => reason.value.trim().length > 500)
const valid = computed(() => Boolean(selectedReserve.value)
  && (!captainLeaving.value || captainCandidates.value.some((player) => player.jogadorId === selectedCaptainId.value))
  && !reasonTooLong.value)

watch(
  () => [props.open, props.outgoingPlayer.jogadorId] as const,
  ([open]) => {
    if (!open) return
    selectedReserveId.value = ''
    selectedCaptainId.value = ''
    reason.value = ''
    submitted.value = false
    commandSubmitted.value = false
  },
  { immediate: true },
)

watch(selectedReserveId, () => {
  if (!captainCandidates.value.some((player) => player.jogadorId === selectedCaptainId.value)) {
    selectedCaptainId.value = ''
  }
})

function cancel() {
  if (props.saving) return
  emit('cancel')
}

function confirm() {
  submitted.value = true
  if (!valid.value || props.saving || !selectedReserve.value) return

  commandSubmitted.value = true
  emit('confirm', {
    timeId: props.team.id,
    jogadorSaiuId: props.outgoingPlayer.jogadorId,
    reservaEntrouId: selectedReserve.value.jogadorId,
    novoCapitaoId: captainLeaving.value ? selectedCaptainId.value : null,
    motivo: reason.value.trim() || null,
  })
}

function handleOpenAutoFocus(event: DialogAutoFocusEvent) {
  event.preventDefault()
  const mobile = globalThis.matchMedia?.('(max-width: 768px)').matches
  void nextTick(() => (mobile ? cancelButton.value?.$el : reserveTrigger.value?.$el)?.focus())
}

function handleCloseAutoFocus(event: DialogAutoFocusEvent) {
  if (!commandSubmitted.value) return
  event.preventDefault()
  commandSubmitted.value = false
  emit('restore-focus')
}
</script>

<template>
  <Dialog :open="open" @update:open="(value) => !value && cancel()">
    <DialogContent
      class="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-lg"
      :show-close-button="!saving"
      @escape-key-down="saving && $event.preventDefault()"
      @interact-outside="saving && $event.preventDefault()"
      @open-auto-focus="handleOpenAutoFocus"
      @close-auto-focus="handleCloseAutoFocus"
    >
      <form class="flex flex-col gap-5" @submit.prevent="confirm">
        <DialogHeader>
          <DialogTitle>{{ t('drafts.realtime.substitution.title') }}</DialogTitle>
          <DialogDescription>
            {{ t('drafts.realtime.substitution.description', { player: outgoingPlayer.nomeExibicao, team: team.nome }) }}
          </DialogDescription>
        </DialogHeader>

        <FieldGroup>
          <Field :data-invalid="submitted && !selectedReserve" :data-disabled="saving">
            <FieldLabel for="draft-substitution-reserve">{{ t('drafts.realtime.substitution.reserveLabel') }}</FieldLabel>
            <Select v-model="selectedReserveId" :disabled="saving">
              <SelectTrigger
                ref="reserveTrigger"
                id="draft-substitution-reserve"
                data-testid="reserve-trigger"
                :aria-invalid="submitted && !selectedReserve"
                :aria-describedby="submitted && !selectedReserve ? 'draft-substitution-reserve-error' : undefined"
              >
                <SelectValue :placeholder="t('drafts.realtime.substitution.reservePlaceholder')" />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectItem v-for="reserve in reserves" :key="reserve.jogadorId" :value="reserve.jogadorId">
                    {{ reserve.nomeExibicao }}
                  </SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
            <FieldError v-if="submitted && !selectedReserve" id="draft-substitution-reserve-error" data-testid="reserve-error">
              {{ t('drafts.realtime.substitution.reserveRequired') }}
            </FieldError>
          </Field>

          <Field v-if="captainLeaving" data-testid="new-captain-field" :data-invalid="submitted && !selectedCaptainId" :data-disabled="saving">
            <FieldLabel for="draft-substitution-captain">{{ t('drafts.realtime.substitution.captainLabel') }}</FieldLabel>
            <Select v-model="selectedCaptainId" :disabled="saving || !selectedReserve">
              <SelectTrigger
                id="draft-substitution-captain"
                data-testid="new-captain-select"
                :aria-invalid="submitted && !selectedCaptainId"
                :aria-describedby="submitted && !selectedCaptainId ? 'draft-substitution-captain-error' : undefined"
              >
                <SelectValue :placeholder="t('drafts.realtime.substitution.captainPlaceholder')" />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectItem v-for="candidate in captainCandidates" :key="candidate.jogadorId" :value="candidate.jogadorId">
                    {{ candidate.nomeExibicao }}
                  </SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
            <FieldError v-if="submitted && !selectedCaptainId" id="draft-substitution-captain-error" data-testid="captain-error">
              {{ t('drafts.realtime.substitution.captainRequired') }}
            </FieldError>
          </Field>

          <Field :data-invalid="submitted && reasonTooLong" :data-disabled="saving">
            <FieldLabel for="draft-substitution-reason">{{ t('drafts.realtime.substitution.reasonLabel') }}</FieldLabel>
            <Textarea
              id="draft-substitution-reason"
              v-model="reason"
              name="substitution-reason"
              autocomplete="off"
              maxlength="500"
              class="min-h-24 resize-y"
              :disabled="saving"
              :aria-invalid="submitted && reasonTooLong"
              :aria-describedby="submitted && reasonTooLong ? 'draft-substitution-reason-error' : undefined"
              :placeholder="t('drafts.realtime.substitution.reasonPlaceholder')"
            />
            <FieldError v-if="submitted && reasonTooLong" id="draft-substitution-reason-error">
              {{ t('drafts.realtime.substitution.reasonMaxLength') }}
            </FieldError>
          </Field>
        </FieldGroup>

        <DialogFooter>
          <Button ref="cancelButton" data-testid="substitution-cancel" type="button" variant="outline" :disabled="saving" @click="cancel">
            {{ t('common.cancel') }}
          </Button>
          <Button type="submit" :disabled="saving">
            {{ t(saving ? 'common.saving' : 'drafts.realtime.substitution.confirm') }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>

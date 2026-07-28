<script lang="ts">
import type { DraftMontagemPublicacaoDiscordStatus, DraftMontagemPublicacaoDiscordTipo } from '@/types/draftMontagem'

export type DraftReasonDialogAction =
  | { type: 'cancelDraft' }
  | { type: 'addManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'removeManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'republishDiscord'; publicationType: DraftMontagemPublicacaoDiscordTipo; publicationStatus: DraftMontagemPublicacaoDiscordStatus | string | null }
  | { type: 'archiveDraft'; draftName: string; cancelsActiveDraft: boolean }
  | { type: 'restoreDraft'; draftName: string }
  | { type: 'reopenPresence'; draftName: string }
</script>

<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'

type ReasonKeyboardEvent = InstanceType<typeof globalThis.KeyboardEvent>
type CloseAutoFocusEvent = InstanceType<typeof globalThis.Event>
type OpenAutoFocusEvent = InstanceType<typeof globalThis.Event>

const props = defineProps<{ open: boolean; action: DraftReasonDialogAction | null; saving: boolean }>()
const emit = defineEmits<{ confirm: [reason: string | null]; cancel: []; 'restore-focus': [] }>()
const { t, te } = useI18n()

const reason = ref('')
const submitted = ref(false)
const commandSubmitted = ref(false)
const reasonField = useTemplateRef<InstanceType<typeof Textarea>>('reasonField')
const backButton = useTemplateRef<InstanceType<typeof Button>>('backButton')
const confirmButton = useTemplateRef<InstanceType<typeof Button>>('confirmButton')
const discordTranslationKeys: Record<DraftMontagemPublicacaoDiscordTipo, string> = {
  Presenca: 'republishPresence',
  ChamadaPresenca: 'republishPresenceCta',
  TimesDefinidos: 'republishTeams',
  Cancelamento: 'republishCancellation',
}
const translationKey = computed(() => {
  if (!props.action) return ''
  const actionKey = props.action.type === 'republishDiscord' ? discordTranslationKeys[props.action.publicationType] : props.action.type
  return `drafts.reasonDialog.${actionKey}`
})
const discordAction = computed(() => props.action?.type === 'republishDiscord')
const constructiveAction = computed(() => discordAction.value || props.action?.type === 'addManualPresence' || props.action?.type === 'reopenPresence')
const restoreAction = computed(() => props.action?.type === 'restoreDraft')
const requiresReason = computed(() => !restoreAction.value && props.action?.type !== 'reopenPresence')
const publicationStatus = computed(() => {
  const action = props.action
  return action?.type === 'republishDiscord' ? action.publicationStatus : null
})
const publicationStatusKey = computed(() => {
  const key = publicationStatus.value ? `drafts.publication.status.${publicationStatus.value}` : ''
  return key && te(key) ? key : 'drafts.publication.status.unknown'
})
const translationParams = computed(() => {
  const action = props.action
  return action && 'draftName' in action ? { draftName: action.draftName } : {}
})
const normalizedReasonLength = computed(() => reason.value.trim().length)
const reasonTooLong = computed(() => normalizedReasonLength.value > 500)
const valid = computed(() => !requiresReason.value || (normalizedReasonLength.value > 0 && !reasonTooLong.value))

watch(
  () => [props.open, props.action] as const,
  ([open]) => {
    submitted.value = false
    if (open) commandSubmitted.value = false
    reason.value = open && props.action && props.action.type !== 'archiveDraft' && requiresReason.value
      ? t(`${translationKey.value}.defaultReason`)
      : ''
  },
  { immediate: true },
)

function cancel() {
  if (props.saving) return

  reason.value = ''
  submitted.value = false
  commandSubmitted.value = false
  emit('cancel')
}

function confirm() {
  submitted.value = true
  if (!valid.value) {
    void nextTick(() => reasonField.value?.$el.focus())
    return
  }
  if (props.saving) return

  commandSubmitted.value = true
  emit('confirm', requiresReason.value ? reason.value.trim() : null)
}

function handleReasonKeydown(event: ReasonKeyboardEvent) {
  if (event.key !== 'Enter' || event.shiftKey || event.isComposing) return

  if (valid.value && !props.saving) event.preventDefault()
  confirm()
}

function handleCloseAutoFocus(event: CloseAutoFocusEvent) {
  if (!commandSubmitted.value) return

  event.preventDefault()
  commandSubmitted.value = false
  emit('restore-focus')
}

function handleOpenAutoFocus(event: OpenAutoFocusEvent) {
  event.preventDefault()
  if (globalThis.matchMedia?.('(max-width: 768px)').matches) {
    void nextTick(() => backButton.value?.$el.focus())
    return
  }

  void nextTick(() => requiresReason.value ? reasonField.value?.$el.focus() : confirmButton.value?.$el.focus())
}

</script>

<template>
  <Dialog :open="open" @update:open="(value) => !value && cancel()">
    <DialogContent
      v-if="action"
      :show-close-button="!saving"
      class="draft-reason-dialog sm:max-w-lg"
      @escape-key-down="saving && $event.preventDefault()"
      @interact-outside="saving && $event.preventDefault()"
      @open-auto-focus="handleOpenAutoFocus"
      @close-auto-focus="handleCloseAutoFocus"
    >
      <form class="flex flex-col gap-5" @submit.prevent="confirm">
        <DialogHeader>
          <p class="page-kicker">
            {{ t(discordAction ? 'drafts.reasonDialog.discordKicker' : 'drafts.reasonDialog.administrativeKicker') }}
          </p>
          <DialogTitle>{{ t(`${translationKey}.title`) }}</DialogTitle>
          <DialogDescription>{{ t(`${translationKey}.description`, translationParams) }}</DialogDescription>
        </DialogHeader>

        <div v-if="discordAction" class="flex flex-wrap items-center justify-between gap-2 rounded-lg border bg-muted/40 p-3">
          <strong>{{ t(`${translationKey}.context`) }}</strong>
          <Badge variant="outline">
            {{ t('drafts.reasonDialog.currentStatus', { status: t(publicationStatusKey) }) }}
          </Badge>
        </div>
        <div v-else-if="action.type === 'addManualPresence' || action.type === 'removeManualPresence'" class="rounded-lg border bg-muted/40 p-3 text-sm">
          {{ t('drafts.reasonDialog.affectedPlayer', { name: action.jogadorNome }) }}
        </div>

        <Alert v-if="action.type === 'archiveDraft' && action.cancelsActiveDraft" variant="destructive" data-archive-active-warning>
          <AlertDescription>{{ t('drafts.archive.activeWarning') }}</AlertDescription>
        </Alert>

        <FieldGroup v-if="requiresReason">
          <Field :data-invalid="submitted && !valid" :data-disabled="saving">
            <FieldLabel for="draft-reason">{{ t('drafts.reasonDialog.reasonLabel') }}</FieldLabel>
            <Textarea
              ref="reasonField"
              id="draft-reason"
              v-model="reason"
              name="reason"
              autocomplete="off"
              class="min-h-24 resize-y"
              :disabled="saving"
              :aria-invalid="submitted && !valid"
              :aria-describedby="submitted && !valid ? 'draft-reason-error' : undefined"
              @keydown="handleReasonKeydown"
            />
            <FieldError v-if="submitted && !valid" id="draft-reason-error">
              {{ t(reasonTooLong ? 'drafts.reasonDialog.reasonMaxLength' : 'drafts.reasonDialog.reasonRequired') }}
            </FieldError>
          </Field>
        </FieldGroup>

        <DialogFooter>
          <Button ref="backButton" data-testid="draft-reason-cancel" type="button" variant="outline" :disabled="saving" @click="cancel">
            {{ t('drafts.reasonDialog.back') }}
          </Button>
          <Button
            ref="confirmButton"
            data-testid="draft-reason-confirm"
            type="submit"
            :variant="constructiveAction || restoreAction ? 'default' : 'destructive'"
            :disabled="saving"
          >
            <Spinner v-if="saving" data-icon="inline-start" />
            {{ t(saving ? 'common.saving' : `${translationKey}.confirm`) }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>

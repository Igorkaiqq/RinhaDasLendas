<script lang="ts">
import type { DraftMontagemPublicacaoDiscordStatus } from '@/types/draftMontagem'

export type DraftReasonDialogAction =
  | { type: 'cancelDraft' }
  | { type: 'addManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'removeManualPresence'; jogadorId: string; jogadorNome: string }
  | { type: 'republishPresence'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }
  | { type: 'republishTeams'; publicationStatus: DraftMontagemPublicacaoDiscordStatus }
</script>

<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'

type ReasonKeyboardEvent = InstanceType<typeof globalThis.KeyboardEvent>

const props = defineProps<{ open: boolean; action: DraftReasonDialogAction | null; saving: boolean }>()
const emit = defineEmits<{ confirm: [reason: string]; cancel: [] }>()
const { t } = useI18n()

const reason = ref('')
const submitted = ref(false)
const reasonField = useTemplateRef<InstanceType<typeof Textarea>>('reasonField')
const translationKey = computed(() => (props.action ? `drafts.reasonDialog.${props.action.type}` : ''))
const discordAction = computed(() => props.action?.type === 'republishPresence' || props.action?.type === 'republishTeams')
const constructiveAction = computed(() => discordAction.value || props.action?.type === 'addManualPresence')
const publicationStatus = computed(() => {
  const action = props.action
  return action?.type === 'republishPresence' || action?.type === 'republishTeams' ? action.publicationStatus : null
})
const normalizedReasonLength = computed(() => reason.value.trim().length)
const reasonTooLong = computed(() => normalizedReasonLength.value > 500)
const valid = computed(() => normalizedReasonLength.value > 0 && !reasonTooLong.value)

watch(
  () => [props.open, props.action] as const,
  ([open]) => {
    submitted.value = false
    reason.value = open && props.action ? t(`${translationKey.value}.defaultReason`) : ''
  },
  { immediate: true },
)

function cancel() {
  if (props.saving) return

  reason.value = ''
  submitted.value = false
  emit('cancel')
}

function confirm() {
  submitted.value = true
  if (!valid.value) {
    void nextTick(() => reasonField.value?.$el.focus())
    return
  }
  if (props.saving) return

  emit('confirm', reason.value.trim())
}

function handleReasonKeydown(event: ReasonKeyboardEvent) {
  if (event.key !== 'Enter' || event.shiftKey || event.isComposing) return

  if (valid.value && !props.saving) event.preventDefault()
  confirm()
}

</script>

<template>
  <Dialog :open="open" @update:open="(value) => !value && cancel()">
    <DialogContent
      v-if="action"
      :show-close-button="!saving"
      class="sm:max-w-lg"
      @escape-key-down="saving && $event.preventDefault()"
      @interact-outside="saving && $event.preventDefault()"
    >
      <form class="flex flex-col gap-5" @submit.prevent="confirm">
        <DialogHeader>
          <p class="page-kicker">
            {{ t(discordAction ? 'drafts.reasonDialog.discordKicker' : 'drafts.reasonDialog.administrativeKicker') }}
          </p>
          <DialogTitle>{{ t(`${translationKey}.title`) }}</DialogTitle>
          <DialogDescription>{{ t(`${translationKey}.description`) }}</DialogDescription>
        </DialogHeader>

        <div v-if="discordAction && publicationStatus" class="flex flex-wrap items-center justify-between gap-2 rounded-lg border bg-muted/40 p-3">
          <strong>{{ t(`${translationKey}.context`) }}</strong>
          <Badge variant="outline">
            {{ t('drafts.reasonDialog.currentStatus', { status: t(`drafts.publication.status.${publicationStatus}`) }) }}
          </Badge>
        </div>
        <div v-else-if="action.type === 'addManualPresence' || action.type === 'removeManualPresence'" class="rounded-lg border bg-muted/40 p-3 text-sm">
          {{ t('drafts.reasonDialog.affectedPlayer', { name: action.jogadorNome }) }}
        </div>

        <FieldGroup>
          <Field :data-invalid="submitted && !valid" :data-disabled="saving">
            <FieldLabel for="draft-reason">{{ t('drafts.reasonDialog.reasonLabel') }}</FieldLabel>
            <Textarea
              ref="reasonField"
              id="draft-reason"
              v-model="reason"
              autofocus
              name="reason"
              autocomplete="off"
              maxlength="500"
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
          <Button data-testid="draft-reason-cancel" type="button" variant="outline" :disabled="saving" @click="cancel">
            {{ t('drafts.reasonDialog.back') }}
          </Button>
          <Button
            data-testid="draft-reason-confirm"
            type="submit"
            :variant="constructiveAction ? 'default' : 'destructive'"
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

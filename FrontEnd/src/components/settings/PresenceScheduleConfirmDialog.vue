<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
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
import { Spinner } from '@/components/ui/spinner'

type ConfirmAction = 'pause' | 'reactivate' | 'archive'

interface FocusTarget {
  focus: () => void
}

const props = defineProps<{
  open: boolean
  action: ConfirmAction
  scheduleName: string
  submitting: boolean
  returnFocusTo?: FocusTarget
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  confirm: []
}>()

const { t } = useI18n()
const locked = ref(false)

watch(() => props.open, (open) => {
  if (open) locked.value = false
  else restoreFocus()
})
watch(() => props.submitting, (submitting, wasSubmitting) => {
  if (wasSubmitting && !submitting) locked.value = false
})

function confirm() {
  if (props.submitting || locked.value) return
  locked.value = true
  emit('confirm')
}

function setOpen(open: boolean) {
  if (props.submitting) return
  emit('update:open', open)
}

async function restoreFocus() {
  await nextTick()
  props.returnFocusTo?.focus()
}
</script>

<template>
  <Dialog :open="open" @update:open="setOpen">
    <DialogContent class="presence-schedule-confirm-dialog" @keydown.esc.stop="setOpen(false)">
      <DialogHeader>
        <DialogTitle>{{ t(`settings.presenceSchedules.confirm.${action}.title`) }}</DialogTitle>
        <DialogDescription>
          {{ t(`settings.presenceSchedules.confirm.${action}.description`, { name: scheduleName }) }}
        </DialogDescription>
      </DialogHeader>
      <DialogFooter>
        <Button type="button" variant="outline" :disabled="submitting" @click="setOpen(false)">
          {{ t('settings.presenceSchedules.actions.cancel') }}
        </Button>
        <Button
          type="button"
          data-confirm-action
          :variant="action === 'archive' ? 'destructive' : 'default'"
          :disabled="submitting || locked"
          @click="confirm"
        >
          <Spinner v-if="submitting" data-icon="inline-start" />
          {{ submitting ? t('settings.presenceSchedules.actions.processing') : t(`settings.presenceSchedules.confirm.${action}.confirm`) }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>

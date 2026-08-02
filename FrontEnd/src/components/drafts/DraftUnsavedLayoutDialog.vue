<script lang="ts">
export type DraftUnsavedLayoutIntent = 'remote-update' | 'switch-draft' | 'route-leave' | 'draft-removed' | 'archive'
</script>

<script setup lang="ts">
import { nextTick, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'

type DialogEvent = InstanceType<typeof globalThis.Event>

defineProps<{ open: boolean; intent: DraftUnsavedLayoutIntent | null }>()
const emit = defineEmits<{ 'keep-editing': []; discard: [] }>()
const { t } = useI18n()
const keepButton = useTemplateRef<InstanceType<typeof Button>>('keepButton')

function keepEditing() {
  emit('keep-editing')
}

function handleEscape(event: DialogEvent) {
  event.preventDefault()
  keepEditing()
}

function focusKeepEditing(event: DialogEvent) {
  event.preventDefault()
  void nextTick(() => keepButton.value?.$el.focus())
}
</script>

<template>
  <Dialog :open="open" @update:open="(value) => !value && keepEditing()">
    <DialogContent
      v-if="intent"
      :show-close-button="false"
      class="sm:max-w-lg"
      @escape-key-down="handleEscape"
      @interact-outside="$event.preventDefault()"
      @open-auto-focus="focusKeepEditing"
    >
      <DialogHeader>
        <DialogTitle>{{ t('drafts.unsavedLayout.title') }}</DialogTitle>
        <DialogDescription>{{ t(`drafts.unsavedLayout.descriptions.${intent}`) }}</DialogDescription>
      </DialogHeader>
      <DialogFooter>
        <Button ref="keepButton" data-testid="keep-editing" type="button" variant="outline" @click="keepEditing">
          {{ t('drafts.unsavedLayout.keepEditing') }}
        </Button>
        <Button data-testid="discard-layout" type="button" variant="destructive" @click="emit('discard')">
          {{ t('drafts.unsavedLayout.discard') }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>

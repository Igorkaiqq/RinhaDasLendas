<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type { Draft } from '@/types/draft'

const props = defineProps<{ open: boolean; draft: Draft | null }>()
const { t } = useI18n()

const emit = defineEmits<{
  cancel: []
  confirm: [motivo: string]
}>()

const motivo = ref('')

watch(
  () => props.open,
  (open) => {
    if (open) {
      motivo.value = ''
    }
  },
)
</script>

<template>
  <div v-if="open" class="modal-backdrop" role="presentation">
    <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="cancel-draft-title">
      <h2 id="cancel-draft-title">{{ t('drafts.cancelDialog.title') }}</h2>
      <p>{{ t('drafts.cancelDialog.description', { name: draft?.nome }) }}</p>
      <label class="filter-field filter-field--wide">
        {{ t('drafts.cancelDialog.reason') }}
        <span><textarea v-model="motivo" rows="3" :placeholder="t('common.optional')" /></span>
      </label>
      <footer>
        <button type="button" class="button-secondary" @click="emit('cancel')">{{ t('common.back') }}</button>
        <button type="button" class="button-danger" @click="emit('confirm', motivo)">{{ t('drafts.cancelDialog.confirm') }}</button>
      </footer>
    </section>
  </div>
</template>

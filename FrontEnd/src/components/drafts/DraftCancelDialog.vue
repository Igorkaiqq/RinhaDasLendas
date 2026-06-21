<script setup lang="ts">
import { ref, watch } from 'vue'

import type { Draft } from '@/types/draft'

const props = defineProps<{ open: boolean; draft: Draft | null }>()

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
      <h2 id="cancel-draft-title">Cancelar draft?</h2>
      <p>O draft {{ draft?.nome }} sera fechado e nao podera receber novos picks.</p>
      <label class="filter-field filter-field--wide">
        Motivo
        <span><textarea v-model="motivo" rows="3" placeholder="Opcional" /></span>
      </label>
      <footer>
        <button type="button" class="button-secondary" @click="emit('cancel')">Voltar</button>
        <button type="button" class="button-danger" @click="emit('confirm', motivo)">Cancelar draft</button>
      </footer>
    </section>
  </div>
</template>

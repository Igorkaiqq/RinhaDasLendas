<script setup lang="ts">
import type { Player } from '@/services/players'

defineProps<{
  player: Player | null
  open: boolean
}>()

defineEmits<{
  cancel: []
  confirm: []
}>()
</script>

<template>
  <div v-if="open && player" class="dialog-backdrop" role="presentation">
    <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-player-title">
      <p class="page-kicker">Acao destrutiva</p>
      <h2 id="delete-player-title">Excluir {{ player.nomeExibicao }}?</h2>
      <p>Esta acao remove o jogador dos dados temporarios. Com backend ativo, a acao usa a inativacao segura.</p>
      <div class="confirm-dialog__actions">
        <button type="button" class="button-secondary" @click="$emit('cancel')">Cancelar</button>
        <button type="button" class="button-danger" @click="$emit('confirm')">Confirmar</button>
      </div>
    </section>
  </div>
</template>

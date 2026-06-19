<script setup lang="ts">
import type { Team } from '@/types/team'

defineProps<{
  team: Team | null
  open: boolean
}>()

defineEmits<{
  cancel: []
  confirm: []
}>()
</script>

<template>
  <div v-if="open && team" class="dialog-backdrop" role="presentation">
    <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="inactivate-team-title">
      <p class="page-kicker">Confirmacao</p>
      <h2 id="inactivate-team-title">Inativar {{ team.nome }}?</h2>
      <p>O time continuara consultavel, mas deixara de aparecer como opcao ativa para novos fluxos.</p>
      <div class="confirm-dialog__actions">
        <button type="button" @click="$emit('cancel')">Cancelar</button>
        <button type="button" class="button-danger" @click="$emit('confirm')">Confirmar</button>
      </div>
    </section>
  </div>
</template>

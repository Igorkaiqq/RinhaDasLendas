<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  confirm: [{ novaSenha: string; confirmacaoSenha: string }]
  cancel: []
}>()

const novaSenha = ref('')
const confirmacaoSenha = ref('')
</script>

<template>
  <div class="dialog-backdrop">
    <form class="dialog-card" @submit.prevent="emit('confirm', { novaSenha, confirmacaoSenha })">
      <h2>Redefinir senha</h2>
      <label>Nova senha <input v-model="novaSenha" type="password" required minlength="8" /></label>
      <label>Confirmar senha <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <button class="button button--danger" type="submit">Redefinir</button>
      <button class="button" type="button" @click="emit('cancel')">Cancelar</button>
    </form>
  </div>
</template>

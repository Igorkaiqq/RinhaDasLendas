<script setup lang="ts">
import { ref } from 'vue'

import { changePassword } from '@/services/auth'

const senhaAtual = ref('')
const novaSenha = ref('')
const confirmacaoSenha = ref('')
const message = ref('')
const error = ref('')

async function submit() {
  message.value = ''
  error.value = ''
  try {
    await changePassword({ senhaAtual: senhaAtual.value, novaSenha: novaSenha.value, confirmacaoSenha: confirmacaoSenha.value })
    senhaAtual.value = ''
    novaSenha.value = ''
    confirmacaoSenha.value = ''
    message.value = 'Senha alterada com sucesso.'
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Não foi possível alterar senha'
  }
}
</script>

<template>
  <form class="panel-card change-password-card" @submit.prevent="submit">
    <header>
      <span class="eyebrow">Segurança</span>
      <h2>Alterar senha</h2>
      <p>Atualize sua senha local de acesso à plataforma.</p>
    </header>
    <label>Senha atual <input v-model="senhaAtual" type="password" required /></label>
    <label>Nova senha <input v-model="novaSenha" type="password" required minlength="8" /></label>
    <label>Confirmar senha <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
    <p v-if="message" class="status-ok">{{ message }}</p>
    <p v-if="error" class="form-error">{{ error }}</p>
    <div class="profile-actions">
      <button class="button button--primary" type="submit">Alterar senha</button>
    </div>
  </form>
</template>

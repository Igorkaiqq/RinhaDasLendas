<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { resetPassword } from '@/services/auth'

const router = useRouter()
const email = ref('')
const token = ref('')
const novaSenha = ref('')
const confirmacaoSenha = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await resetPassword({ email: email.value, token: token.value, novaSenha: novaSenha.value, confirmacaoSenha: confirmacaoSenha.value })
    await router.push(AppRoutes.Login)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Não foi possível redefinir senha'
  }
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <h1>Redefinir senha</h1>
      <label>E-mail <input v-model="email" type="email" required /></label>
      <label>Token <input v-model="token" required /></label>
      <label>Nova senha <input v-model="novaSenha" type="password" required minlength="8" /></label>
      <label>Confirmar senha <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit">Redefinir</button>
    </form>
  </main>
</template>

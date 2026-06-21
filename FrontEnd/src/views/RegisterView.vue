<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { register } from '@/services/auth'

const router = useRouter()
const nome = ref('')
const email = ref('')
const senha = ref('')
const confirmacaoSenha = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await register({ nome: nome.value, email: email.value, senha: senha.value, confirmacaoSenha: confirmacaoSenha.value })
    await router.push(AppRoutes.Login)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Não foi possível cadastrar'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">Conta de jogador</span>
      <h1>Criar conta</h1>
      <p>Todo novo cadastro começa como Jogador.</p>
      <label>Nome <input v-model="nome" required maxlength="120" /></label>
      <label>E-mail <input v-model="email" type="email" required /></label>
      <label>Senha <input v-model="senha" type="password" required minlength="8" /></label>
      <label>Confirmar senha <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit" :disabled="loading">{{ loading ? 'Criando...' : 'Criar conta' }}</button>
      <RouterLink :to="AppRoutes.Login">Já tenho conta</RouterLink>
    </form>
  </main>
</template>

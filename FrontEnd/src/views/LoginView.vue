<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { login } from '@/services/auth'

const router = useRouter()
const route = useRoute()
const email = ref('')
const senha = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await login({ email: email.value, senha: senha.value })
    await router.push((route.query.redirect as string | undefined) ?? AppRoutes.Home)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Não foi possível autenticar'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">Arena interna</span>
      <h1>Entre na Rinha</h1>
      <p>Acesse sua conta para organizar jogadores, drafts e partidas da comunidade.</p>
      <label>
        E-mail
        <input v-model="email" type="email" autocomplete="email" required />
      </label>
      <label>
        Senha
        <input v-model="senha" type="password" autocomplete="current-password" required />
      </label>
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit" :disabled="loading">{{ loading ? 'Entrando...' : 'Entrar' }}</button>
      <div class="auth-card__links">
        <RouterLink :to="AppRoutes.ForgotPassword">Esqueci minha senha</RouterLink>
        <RouterLink :to="AppRoutes.Register">Criar conta</RouterLink>
      </div>
    </form>
  </main>
</template>

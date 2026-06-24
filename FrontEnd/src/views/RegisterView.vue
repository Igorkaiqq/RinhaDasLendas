<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { getDiscordLoginUrl, register } from '@/services/auth'

const router = useRouter()
const { t } = useI18n()
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
    error.value = err instanceof Error ? err.message : t('auth.register.errors.fallback')
  } finally {
    loading.value = false
  }
}

function registerWithDiscord() {
  window.location.href = getDiscordLoginUrl()
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">{{ t('auth.register.eyebrow') }}</span>
      <h1>{{ t('auth.register.title') }}</h1>
      <p>{{ t('auth.register.description') }}</p>
      <label>{{ t('auth.fields.name') }} <input v-model="nome" required maxlength="120" /></label>
      <label>{{ t('auth.fields.email') }} <input v-model="email" type="email" required /></label>
      <label>{{ t('auth.fields.password') }} <input v-model="senha" type="password" required minlength="8" /></label>
      <label>{{ t('auth.fields.confirmPassword') }} <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit" :disabled="loading">{{ loading ? t('auth.register.submitting') : t('auth.register.submit') }}</button>
      <button class="button auth-card__discord" type="button" :disabled="loading" @click="registerWithDiscord">
        <svg class="auth-card__discord-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M20.32 4.37A19.8 19.8 0 0 0 15.36 2.8a13.7 13.7 0 0 0-.64 1.32 18.4 18.4 0 0 0-5.44 0 13.7 13.7 0 0 0-.64-1.32 19.8 19.8 0 0 0-4.96 1.57C.54 9.06-.32 13.64.1 18.15a19.9 19.9 0 0 0 6.08 3.05c.49-.66.92-1.36 1.3-2.1-.72-.27-1.4-.6-2.04-.98.17-.13.34-.26.5-.4a14.1 14.1 0 0 0 12.12 0c.16.14.33.27.5.4-.64.38-1.32.71-2.04.98.38.74.81 1.44 1.3 2.1a19.9 19.9 0 0 0 6.08-3.05c.5-5.23-.85-9.77-3.58-13.78ZM8.02 15.38c-1.18 0-2.15-1.08-2.15-2.4 0-1.32.95-2.4 2.15-2.4s2.17 1.08 2.15 2.4c0 1.32-.95 2.4-2.15 2.4Zm7.96 0c-1.18 0-2.15-1.08-2.15-2.4 0-1.32.95-2.4 2.15-2.4s2.17 1.08 2.15 2.4c0 1.32-.95 2.4-2.15 2.4Z" />
        </svg>
        {{ t('auth.register.discord') }}
      </button>
      <div class="auth-card__links">
        <RouterLink :to="AppRoutes.Login">{{ t('auth.register.haveAccount') }}</RouterLink>
      </div>
    </form>
  </main>
</template>

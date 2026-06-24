<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { getDiscordLoginUrl, login } from '@/services/auth'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const email = ref('')
const senha = ref('')
const loading = ref(false)
const error = ref('')
const discordError = computed(() => {
  if (route.query.discord !== 'error') {
    return ''
  }

  switch (route.query.code) {
    case 'MV063':
      return t('auth.login.discordErrors.notLinked')
    case 'MV070':
      return t('auth.login.discordErrors.emailRequired')
    case 'MV071':
      return t('auth.login.discordErrors.emailAlreadyRegistered')
    default:
      return t('auth.login.discordErrors.fallback')
  }
})

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await login({ email: email.value, senha: senha.value })
    await router.push((route.query.redirect as string | undefined) ?? AppRoutes.Home)
  } catch (err) {
    error.value = err instanceof Error ? err.message : t('auth.login.errors.fallback')
  } finally {
    loading.value = false
  }
}

function loginWithDiscord() {
  window.location.href = getDiscordLoginUrl()
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">{{ t('auth.login.eyebrow') }}</span>
      <h1>{{ t('auth.login.title') }}</h1>
      <p>{{ t('auth.login.description') }}</p>
      <label>
        {{ t('auth.fields.email') }}
        <input v-model="email" type="email" autocomplete="email" required />
      </label>
      <label>
        {{ t('auth.fields.password') }}
        <input v-model="senha" type="password" autocomplete="current-password" required />
      </label>
      <p v-if="error || discordError" class="form-error">{{ error || discordError }}</p>
      <button class="button button--primary" type="submit" :disabled="loading">{{ loading ? t('auth.login.submitting') : t('auth.login.submit') }}</button>
      <button class="button auth-card__discord" type="button" :disabled="loading" @click="loginWithDiscord">
        <svg class="auth-card__discord-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M20.32 4.37A19.8 19.8 0 0 0 15.36 2.8a13.7 13.7 0 0 0-.64 1.32 18.4 18.4 0 0 0-5.44 0 13.7 13.7 0 0 0-.64-1.32 19.8 19.8 0 0 0-4.96 1.57C.54 9.06-.32 13.64.1 18.15a19.9 19.9 0 0 0 6.08 3.05c.49-.66.92-1.36 1.3-2.1-.72-.27-1.4-.6-2.04-.98.17-.13.34-.26.5-.4a14.1 14.1 0 0 0 12.12 0c.16.14.33.27.5.4-.64.38-1.32.71-2.04.98.38.74.81 1.44 1.3 2.1a19.9 19.9 0 0 0 6.08-3.05c.5-5.23-.85-9.77-3.58-13.78ZM8.02 15.38c-1.18 0-2.15-1.08-2.15-2.4 0-1.32.95-2.4 2.15-2.4s2.17 1.08 2.15 2.4c0 1.32-.95 2.4-2.15 2.4Zm7.96 0c-1.18 0-2.15-1.08-2.15-2.4 0-1.32.95-2.4 2.15-2.4s2.17 1.08 2.15 2.4c0 1.32-.95 2.4-2.15 2.4Z" />
        </svg>
        {{ t('auth.login.discord') }}
      </button>
      <div class="auth-card__links">
        <RouterLink :to="AppRoutes.ForgotPassword">{{ t('auth.login.forgotPassword') }}</RouterLink>
        <RouterLink :to="AppRoutes.Register">{{ t('auth.login.createAccount') }}</RouterLink>
      </div>
    </form>
  </main>
</template>

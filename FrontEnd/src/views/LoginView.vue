<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { login } from '@/services/auth'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
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
    error.value = err instanceof Error ? err.message : t('auth.login.errors.fallback')
  } finally {
    loading.value = false
  }
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
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit" :disabled="loading">{{ loading ? t('auth.login.submitting') : t('auth.login.submit') }}</button>
      <div class="auth-card__links">
        <RouterLink :to="AppRoutes.ForgotPassword">{{ t('auth.login.forgotPassword') }}</RouterLink>
        <RouterLink :to="AppRoutes.Register">{{ t('auth.login.createAccount') }}</RouterLink>
      </div>
    </form>
  </main>
</template>

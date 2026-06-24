<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { resetPassword } from '@/services/auth'

const router = useRouter()
const { t } = useI18n()
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
    error.value = err instanceof Error ? err.message : t('auth.resetPassword.errors.fallback')
  }
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">{{ t('auth.resetPassword.eyebrow') }}</span>
      <h1>{{ t('auth.resetPassword.title') }}</h1>
      <p>{{ t('auth.resetPassword.description') }}</p>
      <label>{{ t('auth.fields.email') }} <input v-model="email" type="email" required /></label>
      <label>{{ t('auth.fields.token') }} <input v-model="token" required /></label>
      <label>{{ t('auth.fields.newPassword') }} <input v-model="novaSenha" type="password" required minlength="8" /></label>
      <label>{{ t('auth.fields.confirmPassword') }} <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <p v-if="error" class="form-error">{{ error }}</p>
      <button class="button button--primary" type="submit">{{ t('auth.resetPassword.submit') }}</button>
    </form>
  </main>
</template>

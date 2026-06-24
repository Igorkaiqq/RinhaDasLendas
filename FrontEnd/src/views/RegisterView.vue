<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRouter } from 'vue-router'

import { AppRoutes } from '@/constants/appRoutes'
import { register } from '@/services/auth'

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
      <div class="auth-card__links">
        <RouterLink :to="AppRoutes.Login">{{ t('auth.register.haveAccount') }}</RouterLink>
      </div>
    </form>
  </main>
</template>

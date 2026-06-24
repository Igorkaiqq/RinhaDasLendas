<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { forgotPassword } from '@/services/auth'

const email = ref('')
const message = ref('')
const { t } = useI18n()

async function submit() {
  await forgotPassword({ email: email.value })
  message.value = t('auth.forgotPassword.success')
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <span class="auth-card__eyebrow">{{ t('auth.forgotPassword.eyebrow') }}</span>
      <h1>{{ t('auth.forgotPassword.title') }}</h1>
      <p>{{ t('auth.forgotPassword.description') }}</p>
      <label>{{ t('auth.fields.email') }} <input v-model="email" type="email" required /></label>
      <button class="button button--primary" type="submit">{{ t('auth.forgotPassword.submit') }}</button>
      <p v-if="message" class="status-ok auth-card__message">{{ message }}</p>
    </form>
  </main>
</template>

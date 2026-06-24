<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { changePassword } from '@/services/auth'

const senhaAtual = ref('')
const novaSenha = ref('')
const confirmacaoSenha = ref('')
const message = ref('')
const error = ref('')
const { t } = useI18n()

async function submit() {
  message.value = ''
  error.value = ''
  try {
    await changePassword({ senhaAtual: senhaAtual.value, novaSenha: novaSenha.value, confirmacaoSenha: confirmacaoSenha.value })
    senhaAtual.value = ''
    novaSenha.value = ''
    confirmacaoSenha.value = ''
    message.value = t('usersAdmin.changePassword.success')
  } catch (err) {
    error.value = err instanceof Error ? err.message : t('usersAdmin.changePassword.error')
  }
}
</script>

<template>
  <form class="panel-card change-password-card" @submit.prevent="submit">
    <header>
      <span class="eyebrow">{{ t('usersAdmin.changePassword.eyebrow') }}</span>
      <h2>{{ t('usersAdmin.changePassword.title') }}</h2>
      <p>{{ t('usersAdmin.changePassword.description') }}</p>
    </header>
    <label>{{ t('usersAdmin.changePassword.currentPassword') }} <input v-model="senhaAtual" type="password" required /></label>
    <label>{{ t('auth.fields.newPassword') }} <input v-model="novaSenha" type="password" required minlength="8" /></label>
    <label>{{ t('auth.fields.confirmPassword') }} <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
    <p v-if="message" class="status-ok">{{ message }}</p>
    <p v-if="error" class="form-error">{{ error }}</p>
    <div class="profile-actions">
      <button class="button button--primary" type="submit">{{ t('usersAdmin.changePassword.submit') }}</button>
    </div>
  </form>
</template>

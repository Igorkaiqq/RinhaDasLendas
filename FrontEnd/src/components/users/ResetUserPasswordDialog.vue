<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

const emit = defineEmits<{
  confirm: [{ novaSenha: string; confirmacaoSenha: string }]
  cancel: []
}>()

const novaSenha = ref('')
const confirmacaoSenha = ref('')
const { t } = useI18n()
</script>

<template>
  <div class="dialog-backdrop">
    <form class="dialog-card" @submit.prevent="emit('confirm', { novaSenha, confirmacaoSenha })">
      <h2>{{ t('usersAdmin.resetPassword.title') }}</h2>
      <label>{{ t('auth.fields.newPassword') }} <input v-model="novaSenha" type="password" required minlength="8" /></label>
      <label>{{ t('auth.fields.confirmPassword') }} <input v-model="confirmacaoSenha" type="password" required minlength="8" /></label>
      <button class="button button--danger" type="submit">{{ t('usersAdmin.resetPassword.submit') }}</button>
      <button class="button" type="button" @click="emit('cancel')">{{ t('common.cancel') }}</button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{
  active: boolean
}>()

defineEmits<{
  confirm: []
  cancel: []
}>()

const { t } = useI18n()
</script>

<template>
  <div class="dialog-backdrop">
    <section class="dialog-card">
      <h2>{{ active ? t('usersAdmin.statusDialog.deactivateTitle') : t('usersAdmin.statusDialog.activateTitle') }}</h2>
      <p>{{ t('usersAdmin.statusDialog.description') }}</p>
      <button class="button button--danger" type="button" @click="$emit('confirm')">{{ t('common.confirm') }}</button>
      <button class="button" type="button" @click="$emit('cancel')">{{ t('common.cancel') }}</button>
    </section>
  </div>
</template>

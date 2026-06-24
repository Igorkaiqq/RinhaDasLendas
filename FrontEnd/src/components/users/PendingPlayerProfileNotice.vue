<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <section class="panel-card pending-player-notice">
    <div>
      <span class="eyebrow">{{ t('usersAdmin.pendingProfile.eyebrow') }}</span>
      <h2>{{ t('usersAdmin.pendingProfile.title') }}</h2>
      <p>{{ t('usersAdmin.pendingProfile.description') }}</p>
    </div>
    <RouterLink class="button button--primary" to="/perfil">{{ t('usersAdmin.pendingProfile.action') }}</RouterLink>
  </section>
</template>

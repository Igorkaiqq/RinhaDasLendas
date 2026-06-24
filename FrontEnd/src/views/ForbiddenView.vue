<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <section class="page-stack">
    <div class="page-header-card">
      <span>403</span>
      <h1>{{ t('forbidden.title') }}</h1>
      <p>{{ t('forbidden.description') }}</p>
    </div>
  </section>
</template>

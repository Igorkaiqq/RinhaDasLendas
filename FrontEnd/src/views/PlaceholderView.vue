<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

const route = useRoute()
const { t } = useI18n()
const title = computed(() => t(String(route.meta.titleKey ?? 'placeholder.defaultTitle')))
</script>

<template>
  <section class="placeholder-page">
    <div class="page-header-card placeholder-card">
      <span>{{ t('placeholder.badge') }}</span>
      <h1>{{ title }}</h1>
      <p>{{ t('placeholder.description') }}</p>
    </div>
  </section>
</template>

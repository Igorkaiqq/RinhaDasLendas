<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'

const { t } = useI18n()
</script>

<template>
  <PageFrame>
    <PageHeader
      :eyebrow="t('updates.eyebrow')"
      :title="t('updates.title')"
      :description="t('updates.description')"
    />
  </PageFrame>
</template>

<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useRoute } from 'vue-router'

import AppShell from '@/components/layout/AppShell.vue'

const route = useRoute()
</script>

<template>
  <RouterView v-if="route.meta.publicOnly" />
  <AppShell v-else>
    <RouterView />
  </AppShell>
</template>

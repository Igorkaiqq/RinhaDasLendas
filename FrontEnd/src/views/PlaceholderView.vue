<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const title = computed(() => String(route.meta.title ?? 'Tela em construcao'))
</script>

<template>
  <section class="placeholder-page">
    <div class="page-header-card placeholder-card">
      <span>Em breve</span>
      <h1>{{ title }}</h1>
      <p>Esta área já usa o layout principal e está pronta para receber a próxima etapa da rinha.</p>
    </div>
  </section>
</template>

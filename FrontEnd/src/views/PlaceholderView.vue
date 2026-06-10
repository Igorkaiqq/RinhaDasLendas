<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const title = computed(() => String(route.meta.title ?? 'Tela em construcao'))
</script>

<template>
  <section class="placeholder-page">
    <p class="page-kicker">Em breve</p>
    <h1>{{ title }}</h1>
    <p>Esta area ja usa o layout principal e esta pronta para receber a proxima etapa da rinha.</p>
  </section>
</template>

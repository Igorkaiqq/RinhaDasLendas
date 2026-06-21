<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { loadDiscordStatus } from '@/services/auth'
import type { DiscordLinkStatus } from '@/types/auth'

const status = ref<DiscordLinkStatus>({ vinculado: false })

onMounted(async () => {
  status.value = await loadDiscordStatus()
})
</script>

<template>
  <section class="panel-card">
    <h2>Discord</h2>
    <p v-if="status.vinculado">Vinculado como {{ status.username }}</p>
    <p v-else>Discord ainda não vinculado. Login e vínculo via Discord serão habilitados em uma etapa futura.</p>
    <button class="button" type="button" disabled>Vincular Discord em breve</button>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'

import type { ProfileMenuItem } from '@/types/layout'

defineProps<{
  items: ProfileMenuItem[]
}>()

const open = ref(false)
</script>

<template>
  <div class="profile-menu">
    <button class="profile-menu__trigger" type="button" aria-haspopup="menu" :aria-expanded="open" @click="open = !open">
      Perfil
    </button>
    <div v-if="open" class="profile-menu__panel" role="menu">
      <button v-for="item in items" :key="item.id" type="button" role="menuitem" @click="open = false">
        {{ item.label }}
      </button>
    </div>
  </div>
</template>

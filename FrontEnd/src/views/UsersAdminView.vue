<script setup lang="ts">
import { onMounted, ref } from 'vue'

import UserDetailsModal from '@/components/users/UserDetailsModal.vue'
import UserFilters from '@/components/users/UserFilters.vue'
import UserList from '@/components/users/UserList.vue'
import { listUsers, type UserFilters as Filters } from '@/services/users'
import type { UserSummary } from '@/types/users'

const users = ref<UserSummary[]>([])
const selectedId = ref<string | null>(null)
const loading = ref(false)

async function load(filters: Filters = {}) {
  loading.value = true
  try {
    const response = await listUsers({ ...filters, page: 1, pageSize: 50 })
    users.value = response.items
  } finally {
    loading.value = false
  }
}

onMounted(() => load())
</script>

<template>
  <section class="page-stack">
    <header class="page-header-card">
      <span>Administração da arena</span>
      <h1>Usuários</h1>
      <p>Gerencie contas, permissões, status e vínculo com jogador sem sair do fluxo operacional.</p>
    </header>
    <UserFilters @filter="load" />
    <p v-if="loading" class="panel-card user-loading-card">Carregando usuários...</p>
    <UserList :users="users" @select="selectedId = $event" />
    <UserDetailsModal v-if="selectedId" :id="selectedId" @close="selectedId = null" @updated="load" />
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'

import type { AuthRole } from '@/constants/authRoles'

const emit = defineEmits<{
  filter: [{ search?: string; role?: AuthRole | ''; status?: 'Ativo' | 'Desativado' | '' }]
}>()

const search = ref('')
const role = ref<AuthRole | ''>('')
const status = ref<'Ativo' | 'Desativado' | ''>('')
</script>

<template>
  <form class="panel-card user-filters" @submit.prevent="emit('filter', { search, role, status })">
    <label>Busca <input v-model="search" placeholder="Nome ou e-mail" /></label>
    <label>
      Role
      <select v-model="role">
        <option value="">Todas</option>
        <option>SuperAdmin</option>
        <option>Admin</option>
        <option>Moderador</option>
        <option>Capitão</option>
        <option>Jogador</option>
      </select>
    </label>
    <label>
      Status
      <select v-model="status">
        <option value="">Todos</option>
        <option>Ativo</option>
        <option>Desativado</option>
      </select>
    </label>
    <button class="button button--primary" type="submit">Filtrar</button>
  </form>
</template>

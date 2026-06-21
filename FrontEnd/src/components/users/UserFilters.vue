<script setup lang="ts">
import { ref } from 'vue'

import type { AuthRole } from '@/constants/authRoles'

const emit = defineEmits<{
  filter: [{ search?: string; role?: AuthRole | ''; status?: 'Ativo' | 'Desativado' | '' }]
}>()

const search = ref('')
const role = ref<AuthRole | ''>('')
const status = ref<'Ativo' | 'Desativado' | ''>('')
const roleOptions: Array<AuthRole | ''> = ['', 'SuperAdmin', 'Admin', 'Moderador', 'Capitão', 'Jogador']
const statusOptions: Array<'Ativo' | 'Desativado' | ''> = ['', 'Ativo', 'Desativado']
</script>

<template>
  <form class="panel-card user-filters" @submit.prevent="emit('filter', { search, role, status })">
    <label class="user-filter-search">
      Busca
      <span>
        <span aria-hidden="true">S</span>
        <input v-model="search" placeholder="Nome ou e-mail" />
      </span>
    </label>
    <fieldset class="user-filter-group">
      <legend>Role</legend>
      <button v-for="option in roleOptions" :key="option || 'Todas'" type="button" :class="{ 'is-selected': role === option }" @click="role = option">
        {{ option || 'Todas' }}
      </button>
    </fieldset>
    <fieldset class="user-filter-group user-filter-group--compact">
      <legend>Status</legend>
      <button v-for="option in statusOptions" :key="option || 'Todos'" type="button" :class="{ 'is-selected': status === option }" @click="status = option">
        {{ option || 'Todos' }}
      </button>
    </fieldset>
    <button class="button button--primary user-filter-submit" type="submit">Filtrar</button>
  </form>
</template>

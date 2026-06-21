<script setup lang="ts">
import type { UserSummary } from '@/types/users'

defineProps<{
  users: UserSummary[]
}>()

defineEmits<{
  select: [string]
}>()
</script>

<template>
  <div class="panel-card user-list">
    <p v-if="users.length === 0" class="user-empty-state">Nenhum usuário encontrado.</p>
    <button v-for="user in users" :key="user.id" class="user-row" type="button" @click="$emit('select', user.id)">
      <span class="user-row__identity">
        <span class="user-avatar user-avatar--sm" aria-hidden="true">{{ user.nome.charAt(0).toUpperCase() }}</span>
        <span>
          <strong>{{ user.nome }}</strong>
          <small>{{ user.email }}</small>
        </span>
      </span>
      <span class="user-role-stack">
        <span v-for="role in user.roles" :key="role" class="role-chip">{{ role }}</span>
      </span>
      <span class="status-pill" :class="user.ativo ? 'status-pill--active' : 'status-pill--inactive'">{{ user.ativo ? 'Ativo' : 'Desativado' }}</span>
      <span class="user-row__chevron" aria-hidden="true">›</span>
    </button>
  </div>
</template>

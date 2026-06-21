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
    <p v-if="users.length === 0">Nenhum usuário encontrado.</p>
    <button v-for="user in users" :key="user.id" class="user-row" type="button" @click="$emit('select', user.id)">
      <span>
        <strong>{{ user.nome }}</strong>
        <small>{{ user.email }}</small>
      </span>
      <span>{{ user.roles.join(', ') }}</span>
      <span :class="user.ativo ? 'status-ok' : 'status-danger'">{{ user.ativo ? 'Ativo' : 'Desativado' }}</span>
    </button>
  </div>
</template>

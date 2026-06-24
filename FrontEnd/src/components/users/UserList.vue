<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { UserSummary } from '@/types/users'

const { t } = useI18n()

defineProps<{
  users: UserSummary[]
}>()

defineEmits<{
  select: [string]
}>()
</script>

<template>
  <div class="panel-card user-list">
    <p v-if="users.length === 0" class="user-empty-state">{{ t('usersAdmin.list.empty') }}</p>
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
      <span class="status-pill" :class="user.jogadorId ? 'status-pill--active' : 'status-pill--inactive'">
        {{ user.jogadorId ? t('usersAdmin.list.playerComplete') : t('usersAdmin.list.playerPending') }}
      </span>
      <span class="status-pill" :class="user.ativo ? 'status-pill--active' : 'status-pill--inactive'">
        {{ user.ativo ? t('usersAdmin.status.active') : t('usersAdmin.status.disabled') }}
      </span>
      <span class="user-row__chevron" aria-hidden="true">›</span>
    </button>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import ResetUserPasswordDialog from '@/components/users/ResetUserPasswordDialog.vue'
import UserRolesEditor from '@/components/users/UserRolesEditor.vue'
import UserStatusConfirmDialog from '@/components/users/UserStatusConfirmDialog.vue'
import { activateUser, deactivateUser, getUser, resetUserPassword, updateUser } from '@/services/users'
import type { UserDetails } from '@/types/users'

const props = defineProps<{
  id: string
}>()

const emit = defineEmits<{
  close: []
  updated: []
}>()

const user = ref<UserDetails | null>(null)
const editingName = ref('')
const confirmStatus = ref(false)
const resettingPassword = ref(false)
const { t } = useI18n()

async function load() {
  user.value = await getUser(props.id)
  editingName.value = user.value.nome
}

async function saveBasics() {
  if (!user.value) return
  user.value = await updateUser(user.value.id, { nome: editingName.value })
  emit('updated')
}

async function toggleStatus() {
  if (!user.value) return
  user.value = user.value.ativo ? await deactivateUser(user.value.id) : await activateUser(user.value.id)
  confirmStatus.value = false
  emit('updated')
}

async function resetPassword(payload: { novaSenha: string; confirmacaoSenha: string }) {
  if (!user.value) return
  await resetUserPassword(user.value.id, payload)
  resettingPassword.value = false
}

onMounted(load)
</script>

<template>
  <div class="dialog-backdrop user-modal-backdrop" role="presentation" @click.self="$emit('close')">
    <section class="dialog-card user-details-modal" role="dialog" aria-modal="true" aria-labelledby="user-details-title">
      <header class="user-details-modal__header">
        <div>
          <span class="eyebrow">{{ t('usersAdmin.details.eyebrow') }}</span>
          <h2 id="user-details-title">{{ t('usersAdmin.details.title') }}</h2>
        </div>
        <button class="icon-button" type="button" :aria-label="t('common.close')" @click="$emit('close')">×</button>
      </header>

      <p v-if="!user" class="user-loading-card">{{ t('usersAdmin.details.loading') }}</p>

      <template v-else>
        <section class="user-profile-summary">
          <span class="user-avatar" aria-hidden="true">{{ user.nome.charAt(0).toUpperCase() }}</span>
          <div>
            <h3>{{ user.nome }}</h3>
            <p>{{ user.email }}</p>
          </div>
          <span class="status-pill" :class="user.ativo ? 'status-pill--active' : 'status-pill--inactive'">
            {{ user.ativo ? t('usersAdmin.status.active') : t('usersAdmin.status.disabled') }}
          </span>
        </section>

        <form class="user-edit-form" @submit.prevent="saveBasics">
          <label>
            {{ t('usersAdmin.details.displayName') }}
            <input v-model="editingName" :placeholder="t('usersAdmin.details.namePlaceholder')" />
          </label>
          <button class="button button--primary" type="submit">{{ t('usersAdmin.details.saveData') }}</button>
        </form>

        <UserRolesEditor :user-id="user.id" :roles="user.roles" @saved="load(); emit('updated')" />

        <section class="user-meta-grid" :aria-label="t('usersAdmin.details.infoLabel')">
          <div>
            <span>{{ t('playerForm.discord') }}</span>
            <strong>{{ user.discord?.vinculado ? user.discord.username : t('usersAdmin.details.notLinked') }}</strong>
          </div>
          <div>
            <span>{{ t('usersAdmin.details.playerProfile') }}</span>
            <strong>{{ user.jogadorId ? t('usersAdmin.details.complete') : t('usersAdmin.details.pending') }}</strong>
          </div>
          <div>
            <span>{{ t('usersAdmin.details.createdAt') }}</span>
            <strong>{{ new Date(user.dataCadastro).toLocaleString() }}</strong>
          </div>
          <div>
            <span>{{ t('usersAdmin.details.lastLogin') }}</span>
            <strong>{{ user.ultimoLoginEm ? new Date(user.ultimoLoginEm).toLocaleString() : t('usersAdmin.details.never') }}</strong>
          </div>
        </section>

        <footer class="user-details-modal__actions">
          <button class="button button--danger" type="button" @click="confirmStatus = true">
            {{ user.ativo ? t('usersAdmin.details.deactivateUser') : t('usersAdmin.details.activateUser') }}
          </button>
          <button class="button" type="button" @click="resettingPassword = true">{{ t('usersAdmin.details.resetPassword') }}</button>
        </footer>

        <UserStatusConfirmDialog v-if="confirmStatus" :active="user.ativo" @confirm="toggleStatus" @cancel="confirmStatus = false" />
        <ResetUserPasswordDialog v-if="resettingPassword" @confirm="resetPassword" @cancel="resettingPassword = false" />
      </template>
    </section>
  </div>
</template>

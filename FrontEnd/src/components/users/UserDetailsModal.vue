<script setup lang="ts">
import { onMounted, ref } from 'vue'

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
          <span class="eyebrow">Administração</span>
          <h2 id="user-details-title">Editar usuário</h2>
        </div>
        <button class="icon-button" type="button" aria-label="Fechar" @click="$emit('close')">x</button>
      </header>

      <p v-if="!user" class="user-loading-card">Carregando usuário...</p>

      <template v-else>
        <section class="user-profile-summary">
          <span class="user-avatar" aria-hidden="true">{{ user.nome.charAt(0).toUpperCase() }}</span>
          <div>
            <h3>{{ user.nome }}</h3>
            <p>{{ user.email }}</p>
          </div>
          <span class="status-pill" :class="user.ativo ? 'status-pill--active' : 'status-pill--inactive'">
            {{ user.ativo ? 'Ativo' : 'Desativado' }}
          </span>
        </section>

        <form class="user-edit-form" @submit.prevent="saveBasics">
          <label>
            Nome de exibição
            <input v-model="editingName" placeholder="Nome do usuário" />
          </label>
          <button class="button button--primary" type="submit">Salvar dados</button>
        </form>

        <UserRolesEditor :user-id="user.id" :roles="user.roles" @saved="load(); emit('updated')" />

        <section class="user-meta-grid" aria-label="Informações do usuário">
          <div>
            <span>Discord</span>
            <strong>{{ user.discord?.vinculado ? user.discord.username : 'Não vinculado' }}</strong>
          </div>
          <div>
            <span>Perfil de jogador</span>
            <strong>{{ user.jogadorId ? 'Completo' : 'Pendente' }}</strong>
          </div>
          <div>
            <span>Criado em</span>
            <strong>{{ new Date(user.dataCadastro).toLocaleString() }}</strong>
          </div>
          <div>
            <span>Último login</span>
            <strong>{{ user.ultimoLoginEm ? new Date(user.ultimoLoginEm).toLocaleString() : 'Nunca' }}</strong>
          </div>
        </section>

        <footer class="user-details-modal__actions">
          <button class="button button--danger" type="button" @click="confirmStatus = true">
            {{ user.ativo ? 'Desativar usuário' : 'Ativar usuário' }}
          </button>
          <button class="button" type="button" @click="resettingPassword = true">Redefinir senha</button>
        </footer>

        <UserStatusConfirmDialog v-if="confirmStatus" :active="user.ativo" @confirm="toggleStatus" @cancel="confirmStatus = false" />
        <ResetUserPasswordDialog v-if="resettingPassword" @confirm="resetPassword" @cancel="resettingPassword = false" />
      </template>
    </section>
  </div>
</template>

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
  <aside class="drawer-card">
    <button class="button" type="button" @click="$emit('close')">Fechar</button>
    <p v-if="!user">Carregando...</p>
    <template v-else>
      <h2>{{ user.nome }}</h2>
      <p>{{ user.email }}</p>
      <label>Nome <input v-model="editingName" /></label>
      <button class="button" type="button" @click="saveBasics">Salvar dados</button>
      <UserRolesEditor :user-id="user.id" :roles="user.roles" @saved="load(); emit('updated')" />
      <p>Status: {{ user.ativo ? 'Ativo' : 'Desativado' }}</p>
      <p>Discord: {{ user.discord?.vinculado ? user.discord.username : 'Não vinculado' }}</p>
      <p>Criado em: {{ new Date(user.dataCadastro).toLocaleString() }}</p>
      <p>Último login: {{ user.ultimoLoginEm ? new Date(user.ultimoLoginEm).toLocaleString() : 'Nunca' }}</p>
      <button class="button button--danger" type="button" @click="confirmStatus = true">{{ user.ativo ? 'Desativar' : 'Ativar' }}</button>
      <button class="button" type="button" @click="resettingPassword = true">Redefinir senha</button>
      <UserStatusConfirmDialog v-if="confirmStatus" :active="user.ativo" @confirm="toggleStatus" @cancel="confirmStatus = false" />
      <ResetUserPasswordDialog v-if="resettingPassword" @confirm="resetPassword" @cancel="resettingPassword = false" />
    </template>
  </aside>
</template>

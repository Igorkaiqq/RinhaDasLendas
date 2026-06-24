<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import ChangePasswordForm from '@/components/users/ChangePasswordForm.vue'
import CompletePlayerProfileCard from '@/components/users/CompletePlayerProfileCard.vue'
import DiscordLinkSection from '@/components/users/DiscordLinkSection.vue'
import { loadCurrentUser, updateOwnProfile } from '@/services/auth'
import { getAccessToken, setSession, useAuthState } from '@/services/authState'
import { completeMeuJogadorProfile, getMeuJogadorProfile, updateMeuJogadorProfile } from '@/services/meuJogador'
import type { MeuJogadorProfile, MeuJogadorProfilePayload } from '@/types/meuJogador'

const auth = useAuthState()
const nome = ref(auth.user.value?.nome ?? '')
const message = ref('')
const jogadorMessage = ref('')
const jogadorErrors = ref<string[]>([])
const jogador = ref<MeuJogadorProfile | null>(null)
const loadingJogador = ref(false)
const savingJogador = ref(false)
const user = computed(() => auth.user.value)

onMounted(loadJogador)

async function save() {
  const updated = await updateOwnProfile({ nome: nome.value })
  nome.value = updated.nome
  message.value = 'Perfil atualizado.'
}

async function loadJogador() {
  loadingJogador.value = true
  try {
    jogador.value = await getMeuJogadorProfile()
  } finally {
    loadingJogador.value = false
  }
}

async function saveJogador(payload: MeuJogadorProfilePayload) {
  savingJogador.value = true
  jogadorErrors.value = []
  jogadorMessage.value = ''
  try {
    jogador.value = jogador.value ? await updateMeuJogadorProfile(payload) : await completeMeuJogadorProfile(payload)
    const updatedUser = await loadCurrentUser()
    const token = getAccessToken()
    if (token) {
      setSession(token, updatedUser)
    }
    jogadorMessage.value = 'Perfil de jogador salvo.'
  } catch (error: unknown) {
    const data = typeof error === 'object' && error !== null && 'response' in error
      ? (error as { response?: { data?: { message?: string; errors?: string[] } } }).response?.data
      : undefined
    jogadorErrors.value = data?.errors?.length ? data.errors : [data?.message ?? 'Não foi possível salvar o perfil de jogador.']
  } finally {
    savingJogador.value = false
  }
}
</script>

<template>
  <section class="page-stack">
    <header class="page-header-card">
      <span>Perfil do competidor</span>
      <h1>Minha conta</h1>
      <p>Gerencie seus dados de acesso, senha e vínculo com jogador para entrar em drafts e listas da comunidade.</p>
    </header>
    <form class="panel-card profile-account-card" @submit.prevent="save">
      <header>
        <span class="eyebrow">Conta</span>
        <h2>Dados de acesso</h2>
        <p>Esses dados identificam sua conta na plataforma.</p>
      </header>
      <label>Nome <input v-model="nome" required maxlength="120" /></label>
      <label>E-mail <input :value="user?.email" disabled /></label>
      <p class="profile-role-line">Roles: {{ user?.roles.join(', ') }}</p>
      <p v-if="message" class="status-ok profile-inline-message">{{ message }}</p>
      <div class="profile-actions">
        <button class="button button--primary" type="submit">Salvar perfil</button>
      </div>
    </form>
    <p v-if="loadingJogador" class="panel-card profile-loading-card">Carregando perfil de jogador...</p>
    <CompletePlayerProfileCard
      v-else
      :jogador="jogador"
      :saving="savingJogador"
      :errors="jogadorErrors"
      @submit="saveJogador"
    />
    <p v-if="jogadorMessage" class="panel-card status-ok profile-inline-message">{{ jogadorMessage }}</p>
    <ChangePasswordForm />
    <DiscordLinkSection />
  </section>
</template>

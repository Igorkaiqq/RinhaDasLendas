<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import ChangePasswordForm from '@/components/users/ChangePasswordForm.vue'
import CompletePlayerProfileCard from '@/components/users/CompletePlayerProfileCard.vue'
import DiscordLinkSection from '@/components/users/DiscordLinkSection.vue'
import { loadCurrentUser, updateOwnProfile } from '@/services/auth'
import { getAccessToken, setSession, useAuthState } from '@/services/authState'
import { completeMeuJogadorProfile, getMeuJogadorProfile, updateMeuJogadorProfile } from '@/services/meuJogador'
import type { MeuJogadorProfile, MeuJogadorProfilePayload } from '@/types/meuJogador'

const auth = useAuthState()
const { t } = useI18n()
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
  message.value = t('profile.messages.updated')
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
    jogadorMessage.value = t('profile.player.messages.saved')
  } catch (error: unknown) {
    const data = typeof error === 'object' && error !== null && 'response' in error
      ? (error as { response?: { data?: { message?: string; errors?: string[] } } }).response?.data
      : undefined
    jogadorErrors.value = data?.errors?.length ? data.errors : [data?.message ?? t('profile.player.errors.saveFallback')]
  } finally {
    savingJogador.value = false
  }
}
</script>

<template>
  <section class="page-stack">
    <header class="page-header-card">
      <span>{{ t('profile.account.eyebrow') }}</span>
      <h1>{{ t('profile.account.title') }}</h1>
      <p>{{ t('profile.account.description') }}</p>
    </header>
    <form class="panel-card profile-account-card" @submit.prevent="save">
      <header>
        <span class="eyebrow">{{ t('profile.account.sectionEyebrow') }}</span>
        <h2>{{ t('profile.account.accessData') }}</h2>
        <p>{{ t('profile.account.accessDescription') }}</p>
      </header>
      <label>{{ t('auth.fields.name') }} <input v-model="nome" required maxlength="120" /></label>
      <label>{{ t('auth.fields.email') }} <input :value="user?.email" disabled /></label>
      <p class="profile-role-line">{{ t('profile.account.roles') }}: {{ user?.roles.join(', ') }}</p>
      <p v-if="message" class="status-ok profile-inline-message">{{ message }}</p>
      <div class="profile-actions">
        <button class="button button--primary" type="submit">{{ t('profile.account.save') }}</button>
      </div>
    </form>
    <p v-if="loadingJogador" class="panel-card profile-loading-card">{{ t('profile.player.loading') }}</p>
    <CompletePlayerProfileCard
      v-else
      :jogador="jogador"
      :initial-discord="user?.discord?.username"
      :saving="savingJogador"
      :errors="jogadorErrors"
      @submit="saveJogador"
    />
    <p v-if="jogadorMessage" class="panel-card status-ok profile-inline-message">{{ jogadorMessage }}</p>
    <ChangePasswordForm />
    <DiscordLinkSection />
  </section>
</template>

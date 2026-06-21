<script setup lang="ts">
import { computed, ref } from 'vue'

import ChangePasswordForm from '@/components/users/ChangePasswordForm.vue'
import DiscordLinkSection from '@/components/users/DiscordLinkSection.vue'
import { updateOwnProfile } from '@/services/auth'
import { useAuthState } from '@/services/authState'

const auth = useAuthState()
const nome = ref(auth.user.value?.nome ?? '')
const message = ref('')
const user = computed(() => auth.user.value)

async function save() {
  const updated = await updateOwnProfile({ nome: nome.value })
  nome.value = updated.nome
  message.value = 'Perfil atualizado.'
}
</script>

<template>
  <section class="page-stack">
    <header class="page-header-card">
      <span>Perfil</span>
      <h1>Minha conta</h1>
      <p>Gerencie seus dados de acesso e vínculo com jogador.</p>
    </header>
    <form class="panel-card" @submit.prevent="save">
      <label>Nome <input v-model="nome" required maxlength="120" /></label>
      <label>E-mail <input :value="user?.email" disabled /></label>
      <p>Roles: {{ user?.roles.join(', ') }}</p>
      <p v-if="message">{{ message }}</p>
      <button class="button button--primary" type="submit">Salvar perfil</button>
    </form>
    <ChangePasswordForm />
    <DiscordLinkSection />
  </section>
</template>

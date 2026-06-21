<script setup lang="ts">
import { ref } from 'vue'

import { forgotPassword } from '@/services/auth'

const email = ref('')
const message = ref('')

async function submit() {
  await forgotPassword({ email: email.value })
  message.value = 'Se o e-mail estiver cadastrado, enviaremos instruções de recuperação.'
}
</script>

<template>
  <main class="auth-page">
    <form class="auth-card" @submit.prevent="submit">
      <h1>Recuperar senha</h1>
      <label>E-mail <input v-model="email" type="email" required /></label>
      <button class="button button--primary" type="submit">Enviar instruções</button>
      <p v-if="message">{{ message }}</p>
    </form>
  </main>
</template>

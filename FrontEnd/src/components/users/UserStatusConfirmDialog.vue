<script setup lang="ts">
defineProps<{
  active: boolean
}>()

defineEmits<{
  confirm: []
  cancel: []
}>()
</script>

<template>
  <div class="dialog-backdrop">
    <section class="dialog-card">
      <h2>{{ active ? 'Desativar usuário?' : 'Ativar usuário?' }}</h2>
      <p>Esta ação altera o acesso do usuário ao sistema.</p>
      <button class="button button--danger" type="button" @click="$emit('confirm')">Confirmar</button>
      <button class="button" type="button" @click="$emit('cancel')">Cancelar</button>
    </section>
  </div>
</template>

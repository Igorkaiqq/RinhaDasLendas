<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'

import type { AuthRole } from '@/constants/authRoles'
import { listAssignableRoles, updateUserRoles } from '@/services/users'
import type { RoleOption } from '@/types/users'

const props = defineProps<{
  userId: string
  roles: AuthRole[]
}>()

const emit = defineEmits<{
  saved: []
}>()

const options = ref<RoleOption[]>([])
const selected = ref<AuthRole[]>([...props.roles])

watch(
  () => props.roles,
  (roles) => {
    selected.value = [...roles]
  },
)

onMounted(async () => {
  options.value = await listAssignableRoles()
})

async function save() {
  await updateUserRoles(props.userId, { roles: selected.value })
  emit('saved')
}
</script>

<template>
  <section class="roles-editor">
    <h3>Roles</h3>
    <label v-for="option in options" :key="option.nome">
      <input v-model="selected" type="checkbox" :value="option.nome" />
      {{ option.nome }}
    </label>
    <button class="button" type="button" @click="save">Salvar roles</button>
  </section>
</template>

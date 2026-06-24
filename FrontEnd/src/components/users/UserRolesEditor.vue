<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

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

const { t } = useI18n()

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
    <header>
      <div>
        <span class="eyebrow">{{ t('usersAdmin.roles.eyebrow') }}</span>
        <h3>{{ t('usersAdmin.roles.title') }}</h3>
      </div>
      <button class="button" type="button" @click="save">{{ t('usersAdmin.roles.save') }}</button>
    </header>
    <div class="role-selector" role="group" :aria-label="t('usersAdmin.roles.assignableLabel')">
      <label v-for="option in options" :key="option.nome" class="role-option" :class="{ 'is-selected': selected.includes(option.nome) }">
        <input v-model="selected" type="checkbox" :value="option.nome" />
        <span>{{ option.nome }}</span>
      </label>
    </div>
  </section>
</template>

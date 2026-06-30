<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import type { AuthRole } from '@/constants/authRoles'
import {
  USER_ROLE_FILTER_OPTIONS,
  USER_STATUS_FILTER_OPTIONS,
  type UserStatusFilterValue,
} from '@/constants/userFilters'

const emit = defineEmits<{
  filter: [{ search?: string; role?: AuthRole | ''; status?: UserStatusFilterValue | '' }]
}>()

const { t } = useI18n()

const search = ref('')
const role = ref<AuthRole | ''>('')
const status = ref<UserStatusFilterValue | ''>('')
const roleOptions = USER_ROLE_FILTER_OPTIONS
const statusOptions = USER_STATUS_FILTER_OPTIONS
</script>

<template>
  <form class="panel-card user-filters" @submit.prevent="emit('filter', { search, role, status })">
    <label class="user-filter-search">
      {{ t('usersAdmin.filters.search') }}
      <span>
        <span aria-hidden="true">⌕</span>
        <input v-model="search" :placeholder="t('usersAdmin.filters.searchPlaceholder')" />
      </span>
    </label>
    <fieldset class="user-filter-group">
      <legend>{{ t('usersAdmin.filters.role') }}</legend>
      <button v-for="option in roleOptions" :key="option || 'allRoles'" type="button" :class="{ 'is-selected': role === option }" @click="role = option">
        {{ option || t('usersAdmin.filters.allRoles') }}
      </button>
    </fieldset>
    <fieldset class="user-filter-group user-filter-group--compact">
      <legend>{{ t('common.status') }}</legend>
      <button v-for="option in statusOptions" :key="option || 'allStatus'" type="button" :class="{ 'is-selected': status === option }" @click="status = option">
        {{ option ? t(`usersAdmin.statusOptions.${option}`) : t('common.all') }}
      </button>
    </fieldset>
    <button class="button button--primary user-filter-submit" type="submit">{{ t('usersAdmin.filters.submit') }}</button>
  </form>
</template>

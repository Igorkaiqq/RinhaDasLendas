<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { SUPPORTED_LOCALES, type LocaleCode } from '@/types/i18n'
import type { ProfileMenuItem } from '@/types/layout'

const props = defineProps<{
  items: ProfileMenuItem[]
}>()

const emit = defineEmits<{
  action: [string]
}>()

const open = ref(false)
const { locale, t } = useI18n()

const localeOptions = computed(() =>
  SUPPORTED_LOCALES.map((option) => ({
    code: option,
    label: option === 'pt' ? t('profile.ptBR') : t('profile.enUS'),
  })),
)

function selectLocale(value: LocaleCode) {
  locale.value = value
}
</script>

<template>
  <div class="profile-menu">
    <button class="profile-menu__trigger" type="button" aria-haspopup="menu" :aria-expanded="open" @click="open = !open">
      {{ t('profile.trigger') }}
    </button>
    <div v-if="open" class="profile-menu__panel" role="menu">
      <button
        v-for="item in props.items"
        :key="item.id"
        type="button"
        role="menuitem"
        @click="open = false; emit('action', item.action)"
      >
        {{ item.label }}
      </button>
      <div class="profile-menu__locale" role="group" :aria-label="t('profile.language')">
        <button
          v-for="option in localeOptions"
          :key="option.code"
          type="button"
          :class="{ 'is-active': locale === option.code }"
          @click="selectLocale(option.code)"
        >
          {{ option.label }}
        </button>
      </div>
    </div>
  </div>
</template>

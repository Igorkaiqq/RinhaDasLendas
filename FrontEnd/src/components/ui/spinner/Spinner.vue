<script setup lang="ts">
import type { HTMLAttributes } from 'vue'

import { Loader2Icon } from '@lucide/vue'
import { cn } from '@/lib/utils'

defineOptions({ name: 'UiSpinner' })

const props = defineProps<{
  class?: HTMLAttributes['class']
}>()
</script>

<template>
  <Loader2Icon data-slot="spinner" aria-hidden="true" :class="cn('size-4 animate-spin motion-reduce:animate-none', props.class)" />
</template>

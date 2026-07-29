<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Field, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import type { Player } from '@/services/players'
import type { DraftMontagemPayload } from '@/types/draftMontagem'

const props = defineProps<{ open: boolean; players: Player[]; saving: boolean; errors: string[] }>()
const { t } = useI18n()

const emit = defineEmits<{ close: []; submit: [payload: DraftMontagemPayload] }>()

const search = ref('')
const form = reactive({
  nome: '',
  observacoes: '',
  tamanhoEquipe: 5,
  horarioEncerramentoPresenca: '',
  jogadoresIds: [] as string[],
})

const filteredPlayers = computed(() => {
  const term = search.value.trim().toLowerCase()
  return props.players.filter((player) => !term || player.nomeExibicao.toLowerCase().includes(term) || player.discord?.toLowerCase().includes(term) || player.riotId?.toLowerCase().includes(term))
})

const quantidadeTimes = computed(() => Math.floor(form.jogadoresIds.length / Math.max(form.tamanhoEquipe, 1)))
const quantidadeReservas = computed(() => form.jogadoresIds.length % Math.max(form.tamanhoEquipe, 1))
const canSubmit = computed(() => Boolean(form.nome.trim()))

function togglePlayer(playerId: string) {
  if (form.jogadoresIds.includes(playerId)) {
    form.jogadoresIds = form.jogadoresIds.filter((id) => id !== playerId)
    return
  }
  form.jogadoresIds = [...form.jogadoresIds, playerId]
}

function submit() {
  if (!canSubmit.value) {
    return
  }
  emit('submit', {
    nome: form.nome,
    observacoes: form.observacoes || null,
    tamanhoEquipe: form.tamanhoEquipe,
    horarioEncerramentoPresenca: form.horarioEncerramentoPresenca || null,
    sortearCapitaes: false,
    capitaesIds: [],
    jogadoresIds: form.jogadoresIds,
  })
}

function setOpen(open: boolean) {
  if (!open && !props.saving) emit('close')
}
</script>

<template>
  <Dialog :open="open" @update:open="setOpen">
    <DialogContent class="draft-create-modal sm:max-w-[1040px]">
      <DialogHeader class="player-modal__header">
        <div>
          <span class="eyebrow">{{ t('drafts.visualSetup.eyebrow') }}</span>
          <DialogTitle>{{ t('drafts.visualSetup.title') }}</DialogTitle>
          <DialogDescription>{{ t('drafts.visualSetup.description') }}</DialogDescription>
        </div>
      </DialogHeader>

      <form id="draft-create-form" class="player-form draft-create-form" @submit.prevent="submit">
        <div v-if="errors.length" class="form-errors" role="alert">
          <p v-for="error in errors" :key="error">{{ error }}</p>
        </div>
        <Field class="player-form__field">
          <FieldLabel for="draft-name">{{ t('drafts.createModal.name') }}</FieldLabel>
          <Input id="draft-name" v-model="form.nome" name="draftName" autocomplete="off" required :placeholder="t('drafts.visualSetup.namePlaceholder')" />
        </Field>
        <Field class="player-form__field">
          <FieldLabel for="draft-team-size">{{ t('drafts.visualSetup.teamSize') }}</FieldLabel>
          <Input id="draft-team-size" :model-value="form.tamanhoEquipe" name="draftTeamSize" type="number" min="1" max="5" @update:model-value="form.tamanhoEquipe = Number($event)" />
        </Field>
        <Field class="player-form__field">
          <FieldLabel for="draft-presence-close">{{ t('drafts.presence.closeAt') }}</FieldLabel>
          <Input id="draft-presence-close" v-model="form.horarioEncerramentoPresenca" name="draftPresenceClose" type="datetime-local" />
        </Field>
        <Field class="player-form__field player-form__field--wide">
          <FieldLabel for="draft-notes">{{ t('drafts.createModal.notes') }}</FieldLabel>
          <Textarea id="draft-notes" v-model="form.observacoes" name="draftNotes" rows="2" />
        </Field>

        <section class="draft-player-picker player-form__field--wide">
          <div class="draft-player-picker__header">
            <div>
              <span class="eyebrow">{{ t('drafts.visualSetup.players') }}</span>
              <h3>{{ t('drafts.createModal.selectedCount', { count: form.jogadoresIds.length }) }}</h3>
            </div>
            <span>{{ t('drafts.visualSetup.manualSummary', { teams: quantidadeTimes, reserves: quantidadeReservas }) }}</span>
          </div>
          <label class="draft-search-field">
            <span aria-hidden="true">⌕</span>
            <Input v-model="search" name="draftPlayerSearch" type="search" :aria-label="t('drafts.visualSetup.searchPlayer')" :placeholder="t('drafts.visualSetup.searchPlayer')" />
          </label>
          <div class="draft-player-picker__grid">
            <Button v-for="player in filteredPlayers" :key="player.id" type="button" variant="outline" class="draft-player-option" :class="{ 'is-selected': form.jogadoresIds.includes(player.id) }" :aria-pressed="form.jogadoresIds.includes(player.id)" @click="togglePlayer(player.id)">
              <span class="draft-slot__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
              <span>
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ player.elo ? `${player.elo} ${player.divisao ?? ''}` : t('common.eloNotInformed') }}</small>
              </span>
            </Button>
          </div>
        </section>

        <DialogFooter class="player-modal__actions">
          <Button type="button" variant="outline" :disabled="saving" @click="setOpen(false)">{{ t('common.cancel') }}</Button>
          <Button type="submit" :disabled="saving || !canSubmit">{{ saving ? t('drafts.createModal.creating') : t('drafts.createModal.submit') }}</Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>

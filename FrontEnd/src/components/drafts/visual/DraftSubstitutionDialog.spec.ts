// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { Select } from '@/components/ui/select'
import { i18n, setLocale } from '@/i18n'
import type { DraftMontagemParticipante, DraftMontagemTime } from '@/types/draftMontagem'

import DraftSubstitutionDialog from './DraftSubstitutionDialog.vue'

function player(id: string, name: string, capitao = false, estado: DraftMontagemParticipante['estado'] = 'Time'): DraftMontagemParticipante {
  return {
    jogadorId: id,
    nomeExibicao: name,
    status: 'Ativo',
    preferencias: [],
    estado,
    capitao,
    ordem: 1,
    dataCadastro: '2026-07-29T12:00:00Z',
    dataAtualizacao: '2026-07-29T12:00:00Z',
  }
}

const captain = player('captain-1', 'Capitã Ahri', true)
const teammate = player('player-2', 'Jogadora Lux')
const team: DraftMontagemTime = {
  id: 'team-1',
  nome: 'Time Violeta',
  ordem: 1,
  cor: 'blue',
  capitaoId: captain.jogadorId,
  jogadores: [captain, teammate],
}
const reserves = [
  player('reserve-1', 'Reserva Jinx', false, 'Reserva'),
  player('reserve-2', 'Reserva Leona', false, 'Reserva'),
]

async function mountDialog(outgoingPlayer = captain, mobile = false) {
  vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: mobile }))
  const wrapper = mount(DraftSubstitutionDialog, {
    attachTo: document.body,
    props: {
      open: true,
      team,
      outgoingPlayer,
      reserves,
      eligibleCaptainIds: ['player-2', 'reserve-1'],
      saving: false,
    },
    global: {
      plugins: [i18n],
      stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } },
    },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('DraftSubstitutionDialog', () => {
  afterEach(() => {
    setLocale('pt')
    vi.unstubAllGlobals()
  })

  it('requires an explicit reserve and never promotes an eligible reserve automatically', async () => {
    const wrapper = await mountDialog()
    const selects = wrapper.findAllComponents(Select)

    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('[data-testid="reserve-error"]').text()).toContain('reserva')

    selects[0]!.vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()

    expect(wrapper.get('[data-testid="new-captain-select"]').text()).not.toContain('Reserva Jinx')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('[data-testid="captain-error"]').text()).toContain('capitão')

    selects[1]!.vm.$emit('update:modelValue', 'reserve-1')
    await wrapper.get('textarea').setValue('  ajuste tático  ')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toEqual([[
      {
        timeId: 'team-1',
        jogadorSaiuId: 'captain-1',
        reservaEntrouId: 'reserve-1',
        novoCapitaoId: 'reserve-1',
        motivo: 'ajuste tático',
      },
    ]])
    wrapper.unmount()
  })

  it('does not request or emit a new captain when a regular player leaves', async () => {
    const wrapper = await mountDialog(teammate)
    const reserveSelect = wrapper.getComponent(Select)

    expect(wrapper.find('[data-testid="new-captain-select"]').exists()).toBe(false)
    reserveSelect.vm.$emit('update:modelValue', 'reserve-2')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')?.[0]?.[0]).toEqual({
      timeId: 'team-1',
      jogadorSaiuId: 'player-2',
      reservaEntrouId: 'reserve-2',
      novoCapitaoId: null,
      motivo: null,
    })
    wrapper.unmount()
  })

  it('uses localized accessible fields and limits captain options to the resulting team', async () => {
    setLocale('en')
    const wrapper = await mountDialog()
    const selects = wrapper.findAllComponents(Select)

    expect(wrapper.get('[role="dialog"]').attributes('aria-describedby')).toBeTruthy()
    expect(wrapper.text()).toContain('Substitute player')
    expect(wrapper.text()).toContain('Reserve')
    selects[0]!.vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()

    const captainField = wrapper.get('[data-testid="new-captain-field"]')
    expect(captainField.text()).toContain('Jogadora Lux')
    expect(captainField.text()).toContain('Reserva Jinx')
    expect(captainField.text()).not.toContain('Reserva Leona')
    expect(wrapper.get('textarea').attributes('maxlength')).toBe('500')
    wrapper.unmount()
  })

  it('clears a captain that loses eligibility and exposes the reconciled invalid field', async () => {
    const wrapper = await mountDialog()
    const selects = wrapper.findAllComponents(Select)
    selects[0]!.vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()
    selects[1]!.vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()

    await wrapper.setProps({ eligibleCaptainIds: ['player-2'] })
    await nextTick()
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('[data-testid="new-captain-select"]').attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('[data-testid="new-captain-select"]').text()).not.toContain('Reserva Jinx')
    expect(wrapper.get('[data-testid="captain-error"]').text()).toContain('capitão')
    wrapper.unmount()
  })

  it('focuses the reserve control on desktop and the cancel action on mobile', async () => {
    const desktop = await mountDialog()
    await new Promise((resolve) => setTimeout(resolve))
    expect(document.activeElement).toBe(desktop.get('[data-testid="reserve-trigger"]').element)
    desktop.unmount()

    const mobile = await mountDialog(captain, true)
    expect(document.activeElement).toBe(mobile.get('[data-testid="substitution-cancel"]').element)
    mobile.unmount()
  })

  it('cancels with Escape, blocks dismissal while saving, and requests focus restoration after confirmation', async () => {
    const wrapper = await mountDialog(teammate)
    const reserveSelect = wrapper.getComponent(Select)

    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('cancel')).toEqual([[]])

    reserveSelect.vm.$emit('update:modelValue', 'reserve-2')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toHaveLength(1)
    wrapper.getComponent({ name: 'DialogContent' }).vm.$emit('closeAutoFocus', new Event('closeAutoFocus', { cancelable: true }))
    expect(wrapper.emitted('restore-focus')).toEqual([[]])

    await wrapper.setProps({ saving: true })
    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('cancel')).toHaveLength(1)
    wrapper.unmount()
  })
})

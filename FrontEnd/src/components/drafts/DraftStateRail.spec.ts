// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'

import DraftStateRail from './DraftStateRail.vue'
import DraftStateRailSource from './DraftStateRail.vue?raw'

describe('DraftStateRail', () => {
  it.each([
    ['PresencaAberta', 'drafts.rail.presenceOpen', 0, 'active'],
    ['PresencaEncerrada', 'drafts.rail.presenceClosed', 1, 'active'],
    ['CapitaesDefinidos', 'drafts.rail.captains', 2, 'active'],
    ['OrdemDefinida', 'drafts.rail.order', 3, 'active'],
    ['Aberta', 'drafts.rail.picking', 4, 'active'],
    ['Finalizada', 'drafts.rail.finished', 5, 'terminal'],
  ])('maps %s to its canonical current step', (status, label, completedSteps, state) => {
    const wrapper = mount(DraftStateRail, {
      props: { status, publicationStatus: 'Pendente' },
      global: { mocks: { $t: (key: string) => key } },
    })

    const current = wrapper.get(`[data-state="${state}"]`)
    expect(current.text()).toContain(label)
    expect(current.attributes('aria-current')).toBe('step')
    expect(wrapper.findAll('[data-state="done"]')).toHaveLength(completedSteps)
  })

  it('renders cancellation as terminal without an active operational step', () => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'Cancelada', publicationStatus: 'Publicada' },
      global: { mocks: { $t: (key: string) => key } },
    })

    expect(wrapper.find('[data-state="active"]').exists()).toBe(false)
    expect(wrapper.find('[aria-current="step"]').exists()).toBe(false)
    expect(wrapper.get('[data-state="terminal"]').text()).toContain('drafts.rail.cancelled')
  })

  it('renders an unknown status as neutral without inferring progress', () => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'StatusLegado', publicationStatus: null },
      global: { mocks: { $t: (key: string) => key } },
    })

    expect(wrapper.find('[data-state="active"]').exists()).toBe(false)
    expect(wrapper.find('[data-state="done"]').exists()).toBe(false)
    expect(wrapper.get('[data-state="unknown"]').text()).toContain('drafts.rail.unknown')
  })

  it.each([
    ['Falha', 'attention'],
    ['Pendente', 'attention'],
    ['RequerReconciliacao', 'attention'],
    ['EmAndamento', 'attention'],
    ['Publicada', 'done'],
    ['Ignorada', 'pending'],
    [null, 'pending'],
  ])('keeps Discord %s parallel and never current', (publicationStatus, state) => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'Aberta', publicationStatus },
      global: { mocks: { $t: (key: string) => key } },
    })

    const discord = wrapper.get('[data-step-id="discord"]')
    expect(discord.attributes('data-state')).toBe(state)
    expect(discord.attributes('aria-current')).toBeUndefined()
  })

  it('derives the operational sequence from the canonical status options', () => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'PresencaAberta', publicationStatus: null },
      global: { mocks: { $t: (key: string) => key } },
    })
    const expectedIds = DRAFT_MONTAGEM_STATUS_OPTIONS.filter((status) => status !== 'Cancelada')

    expect(wrapper.findAll('.draft-rail__step').slice(0, -1).map((step) => step.attributes('data-step-id'))).toEqual(expectedIds)
    expect(DraftStateRailSource).toContain('DRAFT_MONTAGEM_STATUS_OPTIONS')
    expect(DraftStateRailSource).not.toMatch(/const order\s*=\s*\[/)
  })
})

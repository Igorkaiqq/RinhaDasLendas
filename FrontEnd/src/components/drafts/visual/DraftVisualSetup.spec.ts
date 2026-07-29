// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { i18n } from '@/i18n'
import type { Player } from '@/services/players'

import DraftVisualSetup from './DraftVisualSetup.vue'

const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')
const player: Player = {
  id: 'player-1',
  nomeExibicao: 'Ahri',
  status: 'Ativo',
  dataCadastro: '2026-07-25T12:00:00Z',
  dataAtualizacao: '2026-07-25T12:00:00Z',
  preferencias: [],
}

function mountSetup() {
  return mount(DraftVisualSetup, {
    attachTo: document.body,
    props: { open: true, players: [player], saving: false, errors: [] },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
}

describe('DraftVisualSetup', () => {
  it('removes captain configuration from direct creation', () => {
    const wrapper = mountSetup()

    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('capitães')
  })

  it('submits neutral compatibility captain fields with the selected players', async () => {
    const wrapper = mountSetup()

    await wrapper.get('input[required]').setValue('Manual direto')
    await wrapper.get('.draft-player-option').trigger('click')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]?.[0]).toMatchObject({
      nome: 'Manual direto',
      sortearCapitaes: false,
      capitaesIds: [],
      jogadoresIds: ['player-1'],
    })
  })

  it('keeps scrolling on the setup form and lets nested player grids grow', () => {
    const wrapper = mountSetup()

    expect(wrapper.get('.draft-create-modal').get('.draft-create-form')).toBeTruthy()
    expect(wrapper.get('.draft-player-picker__grid').findAll('.draft-player-option')).toHaveLength(1)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s*{[^}]*overflow-y:\s*auto/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s+\.draft-player-picker__grid\s*{[^}]*max-height:\s*none[^}]*overflow:\s*visible/s)
  })

  it('composes the installed shadcn-vue dialog and form controls', () => {
    const wrapper = mountSetup()

    expect(wrapper.findComponent(Dialog).exists()).toBe(true)
    expect(wrapper.findComponent(DialogContent).exists()).toBe(true)
    expect(wrapper.findComponent(DialogTitle).exists()).toBe(true)
    expect(wrapper.findComponent(DialogDescription).exists()).toBe(true)
    expect(wrapper.findComponent(DialogFooter).exists()).toBe(true)
    expect(wrapper.findAllComponents(Input).length).toBeGreaterThanOrEqual(4)
    expect(wrapper.findComponent(Textarea).exists()).toBe(true)
    expect(wrapper.findAllComponents(Button).length).toBeGreaterThanOrEqual(3)
    wrapper.unmount()
  })

  it('delegates close behavior to Reka and exposes player selection state', async () => {
    const wrapper = mountSetup()
    const playerOption = wrapper.get('.draft-player-option')

    expect(playerOption.attributes('aria-pressed')).toBe('false')
    await playerOption.trigger('click')
    expect(playerOption.attributes('aria-pressed')).toBe('true')

    wrapper.findComponent(Dialog).vm.$emit('update:open', false)
    expect(wrapper.emitted('close')).toEqual([[]])
    wrapper.unmount()
  })
})

// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

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
    props: { open: true, players: [player], captains: [player], saving: false, errors: [] },
    global: { plugins: [i18n] },
  })
}

describe('DraftVisualSetup', () => {
  it('uses the checkbox label as the 44px target while preserving the native checkbox size', () => {
    const wrapper = mountSetup()
    const label = wrapper.get('.draft-create-form .checkbox-line')
    const checkbox = label.get('input[type="checkbox"]')

    expect(label.text()).toContain('Sortear capitães automaticamente')
    expect(label.element.contains(checkbox.element)).toBe(true)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s+\.checkbox-line\s*{[^}]*min-height:\s*44px/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s+\.checkbox-line\s+input\[type='checkbox'\]\s*{[^}]*width:\s*16px[^}]*height:\s*16px[^}]*min-height:\s*16px/s)
  })

  it('keeps scrolling on the setup form and lets nested player grids grow', () => {
    const wrapper = mountSetup()

    expect(wrapper.get('.draft-create-modal').get('.draft-create-form')).toBeTruthy()
    expect(wrapper.get('.draft-player-picker__grid').findAll('.draft-player-option')).toHaveLength(1)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s*{[^}]*overflow-y:\s*auto/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-create-form\s+\.draft-player-picker__grid\s*{[^}]*max-height:\s*none[^}]*overflow:\s*visible/s)
  })
})

// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { nextTick } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import { Dialog, DialogContent } from '@/components/ui/dialog'

import DraftReasonDialog, { type DraftReasonDialogAction } from './DraftReasonDialog.vue'

const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

const mountDialog = async (action: DraftReasonDialogAction, saving = false) => {
  const wrapper = mount(DraftReasonDialog, {
    attachTo: document.body,
    props: { open: true, action, saving },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('DraftReasonDialog', () => {
  afterEach(() => {
    setLocale('pt')
    vi.unstubAllGlobals()
  })

  it.each([
    ['cancelDraft', { type: 'cancelDraft' }, 'Cancelar draft'],
    ['addManualPresence', { type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' }, 'Adicionar presença'],
    ['removeManualPresence', { type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' }, 'Remover presença'],
    ['republishPresence', { type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'Falha' }, 'Republicar lista de presença'],
    ['republishPresenceCta', { type: 'republishDiscord', publicationType: 'ChamadaPresenca', publicationStatus: 'Falha' }, 'Republicar chamada de presença'],
    ['republishTeams', { type: 'republishDiscord', publicationType: 'TimesDefinidos', publicationStatus: 'Pendente' }, 'Republicar times'],
    ['archiveDraft', { type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: true }, 'Arquivar draft'],
    ['restoreDraft', { type: 'restoreDraft', draftName: 'Rinha' }, 'Restaurar draft'],
  ] as const)('renders the %s context', async (_, action, title) => {
    const wrapper = await mountDialog(action)

    expect(wrapper.text()).toContain(title)
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
    wrapper.unmount()
  })

  it.each([
    ['pt', 'Status atual: estado de publicação desconhecido'],
    ['en', 'Current status: unknown publication status'],
  ] as const)('uses the localized neutral publication fallback in %s', async (locale, expected) => {
    setLocale(locale)
    const wrapper = await mountDialog({ type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'EstadoLegado' } as DraftReasonDialogAction)

    expect(wrapper.get('[data-slot="badge"]').text().toLocaleLowerCase()).toBe(expected.toLocaleLowerCase())
    expect(wrapper.text()).not.toContain('drafts.publication.status.EstadoLegado')
    wrapper.unmount()
  })

  it.each([
    ['pt', 'Status atual: estado de publicação desconhecido'],
    ['en', 'Current status: unknown publication status'],
  ] as const)('renders neutral publication context for a missing status in %s', async (locale, expected) => {
    setLocale(locale)
    const wrapper = await mountDialog({ type: 'republishDiscord', publicationType: 'TimesDefinidos', publicationStatus: null })

    expect(wrapper.get('[data-slot="badge"]').text().toLocaleLowerCase()).toBe(expected.toLocaleLowerCase())
    expect(wrapper.text()).toContain(locale === 'pt' ? 'Times definidos' : 'Defined teams')
    wrapper.unmount()
  })

  it('renders action details and the localized default reason', async () => {
    const wrapper = await mountDialog({ type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' })

    expect(wrapper.text()).toContain('Jogador afetado: Ahri')
    expect(wrapper.get('textarea').element.value).toBe('Presença removida manualmente')
    wrapper.unmount()
  })

  it('renders the target player and localized default reason for manual addition', async () => {
    const wrapper = await mountDialog({ type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' })

    expect(wrapper.text()).toContain('Jogador afetado: Lux')
    expect(wrapper.get('textarea').element.value).toBe('Presença adicionada manualmente')
    wrapper.unmount()
  })

  it.each([
    [
      { type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'Falha' },
      'Republicar lista de presença',
      'Lista de presença',
      'Status atual: falhou',
    ],
    [{ type: 'republishDiscord', publicationType: 'TimesDefinidos', publicationStatus: 'Pendente' }, 'Republicar times', 'Times definidos', 'Status atual: pendente'],
    [{ type: 'republishDiscord', publicationType: 'ChamadaPresenca', publicationStatus: 'Falha' }, 'Republicar chamada de presença', 'Chamada de presença', 'Status atual: falhou'],
  ] as const)('renders localized publication type and status for %s', async (action, title, context, status) => {
    const wrapper = await mountDialog(action)

    expect(wrapper.text()).toContain(title)
    expect(wrapper.text()).toContain(context)
    expect(wrapper.get('[data-slot="badge"]').text()).toBe(status)
    wrapper.unmount()
  })

  it('focuses the reason field when opened', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await new Promise((resolve) => setTimeout(resolve))

    expect(document.activeElement).toBe(wrapper.get('textarea').element)
    wrapper.unmount()
  })

  it('focuses the back control inside the dialog without opening the mobile keyboard', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: true }))
    const opener = document.createElement('button')
    document.body.append(opener)
    opener.focus()
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    const dialog = wrapper.get('[role="dialog"]')

    expect(wrapper.get('textarea').attributes('autofocus')).toBeUndefined()
    expect(document.activeElement).toBe(wrapper.get('[data-testid="draft-reason-cancel"]').element)
    expect(dialog.element.contains(document.activeElement)).toBe(true)

    wrapper.unmount()
    opener.remove()
  })

  it('re-evaluates desktop and mobile focus behavior every time it opens', async () => {
    let mobile = false
    const matchMedia = vi.fn().mockImplementation(() => ({ matches: mobile }))
    vi.stubGlobal('matchMedia', matchMedia)
    const opener = document.createElement('button')
    document.body.append(opener)
    const wrapper = mount(DraftReasonDialog, {
      attachTo: document.body,
      props: { open: false, action: { type: 'cancelDraft' }, saving: false },
      global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
    })

    opener.focus()
    await wrapper.setProps({ open: true })
    await new Promise((resolve) => setTimeout(resolve))
    expect(document.activeElement).toBe(wrapper.get('textarea').element)

    await wrapper.setProps({ open: false })
    mobile = true
    opener.focus()
    await wrapper.setProps({ open: true })
    const openEvent = new Event('openAutoFocus', { cancelable: true })
    wrapper.findComponent(DialogContent).vm.$emit('openAutoFocus', openEvent)
    await nextTick()
    expect(openEvent.defaultPrevented).toBe(true)
    expect(document.activeElement).toBe(wrapper.get('[data-testid="draft-reason-cancel"]').element)
    expect(wrapper.get('[role="dialog"]').element.contains(document.activeElement)).toBe(true)
    expect(matchMedia).toHaveBeenCalledTimes(2)

    wrapper.unmount()
    opener.remove()
  })

  it('normalizes and emits a valid reason', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await wrapper.get('textarea').setValue('  motivo válido  ')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toEqual([['motivo válido']])
    wrapper.unmount()
  })

  it('starts archive with an empty reason and warns when an active draft will be cancelled', async () => {
    const wrapper = await mountDialog({ type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: true })

    expect(wrapper.get('textarea').element.value).toBe('')
    expect(wrapper.get('[data-archive-active-warning]').text()).toContain('também será cancelado')
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').attributes('data-variant')).toBe('destructive')
    wrapper.unmount()
  })

  it('does not show the active warning when archiving a terminal draft', async () => {
    const wrapper = await mountDialog({ type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: false })

    expect(wrapper.find('[data-archive-active-warning]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('accepts exactly 500 trimmed characters for archive', async () => {
    const wrapper = await mountDialog({ type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: false })
    const rawReason = `  ${'a'.repeat(500)}  `

    expect(wrapper.get('textarea').attributes('maxlength')).toBeUndefined()
    await wrapper.get('textarea').setValue(rawReason)
    expect(wrapper.get('textarea').element.value).toBe(rawReason)
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toEqual([[`${'a'.repeat(500)}`]])
    wrapper.unmount()
  })

  it('restores without rendering a reason and emits null', async () => {
    const wrapper = await mountDialog({ type: 'restoreDraft', draftName: 'Rinha' })

    expect(wrapper.find('textarea').exists()).toBe(false)
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toEqual([[null]])
    wrapper.unmount()
  })

  it('confirms with Enter and prevents a newline', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    await wrapper.get('textarea').setValue('motivo pelo teclado')
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true })

    wrapper.get('textarea').element.dispatchEvent(event)

    expect(event.defaultPrevented).toBe(true)
    expect(wrapper.emitted('confirm')).toEqual([['motivo pelo teclado']])
    wrapper.unmount()
  })

  it('keeps Shift+Enter for a line break', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    await wrapper.get('textarea').setValue('primeira linha\nsegunda linha')
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true, bubbles: true, cancelable: true })

    wrapper.get('textarea').element.dispatchEvent(event)

    expect(event.defaultPrevented).toBe(false)
    expect(wrapper.get('textarea').element.value).toBe('primeira linha\nsegunda linha')
    expect(wrapper.emitted('confirm')).toBeUndefined()
    wrapper.unmount()
  })

  it('does not confirm Enter while an IME composition is active', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    await wrapper.get('textarea').setValue('motivo em composição')
    const event = new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true, cancelable: true })

    wrapper.get('textarea').element.dispatchEvent(event)

    expect(event.defaultPrevented).toBe(false)
    expect(wrapper.emitted('confirm')).toBeUndefined()
    wrapper.unmount()
  })

  it('does not submit a blank reason and exposes the localized error', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await wrapper.get('textarea').setValue('   ')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('textarea').attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('textarea').attributes('aria-describedby')).toBe('draft-reason-error')
    expect(wrapper.get('[role="alert"]').attributes('id')).toBe('draft-reason-error')
    expect(wrapper.get('[role="alert"]').text()).toBe('Informe um motivo para continuar.')
    const cancelButton = wrapper.get('[data-testid="draft-reason-cancel"]').element as InstanceType<typeof globalThis.HTMLButtonElement>
    cancelButton.focus()
    await wrapper.get('form').trigger('submit')
    await nextTick()
    expect(document.activeElement).toBe(wrapper.get('#draft-reason').element)
    wrapper.unmount()
  })

  it('does not submit an archive reason with 501 meaningful characters', async () => {
    const wrapper = await mountDialog({ type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: false })

    await wrapper.get('textarea').setValue(`  ${'a'.repeat(501)}  `)
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('textarea').element.value).toBe(`  ${'a'.repeat(501)}  `)
    expect(wrapper.get('[role="alert"]').text()).toBe('O motivo deve ter no máximo 500 caracteres.')
    wrapper.unmount()
  })

  it('groups the reason field with FieldGroup', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    expect(wrapper.get('[data-slot="field-group"] [data-slot="field"]')).toBeTruthy()
    wrapper.unmount()
  })

  it('scopes 44px button targets to every portaled reason-dialog action', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    const dialog = wrapper.get('[role="dialog"]')
    const buttons = dialog.findAll('button')

    expect(dialog.classes()).toContain('draft-reason-dialog')
    expect(buttons).toHaveLength(3)
    expect(dialog.get('button[data-slot="dialog-close"]')).toBeTruthy()
    expect(MainCss).toMatch(/\.draft-reason-dialog\s+button\s*{[^}]*min-width:\s*44px[^}]*min-height:\s*44px/s)
    expect(MainCss).toMatch(/\.draft-reason-dialog\s+textarea\s*{[^}]*min-height:\s*44px/s)
    expect(MainCss).toMatch(/\.draft-reason-dialog\s*{[^}]*max-height:\s*calc\(100dvh\s*-\s*[^)]+\)[^}]*overflow-y:\s*auto[^}]*overscroll-behavior:\s*contain/s)
    wrapper.unmount()
  })

  it('emits cancel from the back button', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await wrapper.get('[data-testid="draft-reason-cancel"]').trigger('click')

    expect(wrapper.emitted('cancel')).toEqual([[]])
    wrapper.unmount()
  })

  it('requests stage focus instead of a removed opener after a completed command', async () => {
    const wrapper = await mountDialog({ type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' })
    const content = wrapper.findComponent(DialogContent)
    await wrapper.get('form').trigger('submit')
    await wrapper.setProps({ open: false })
    const closeEvent = new Event('closeAutoFocus', { cancelable: true })

    content.vm.$emit('closeAutoFocus', closeEvent)
    await nextTick()

    expect(closeEvent.defaultPrevented).toBe(true)
    expect(wrapper.emitted('restore-focus')).toEqual([[]])
    wrapper.unmount()
  })

  it('preserves default opener restoration when the dialog is cancelled', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    const content = wrapper.findComponent(DialogContent)
    await wrapper.get('[data-testid="draft-reason-cancel"]').trigger('click')
    await wrapper.setProps({ open: false })
    const closeEvent = new Event('closeAutoFocus', { cancelable: true })

    content.vm.$emit('closeAutoFocus', closeEvent)
    await nextTick()

    expect(closeEvent.defaultPrevented).toBe(false)
    expect(wrapper.emitted('restore-focus')).toBeUndefined()
    wrapper.unmount()
  })

  it.each([
    [{ type: 'cancelDraft' }, 'destructive'],
    [{ type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' }, 'default'],
    [{ type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' }, 'destructive'],
    [{ type: 'archiveDraft', draftName: 'Rinha', cancelsActiveDraft: true }, 'destructive'],
    [{ type: 'restoreDraft', draftName: 'Rinha' }, 'default'],
      [{ type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'Falha' }, 'default'],
      [{ type: 'republishDiscord', publicationType: 'ChamadaPresenca', publicationStatus: 'Falha' }, 'default'],
      [{ type: 'republishDiscord', publicationType: 'TimesDefinidos', publicationStatus: 'Pendente' }, 'default'],
  ] as const)('uses the %s confirmation variant', async (action, variant) => {
    const wrapper = await mountDialog(action)

    expect(wrapper.get('[data-testid="draft-reason-confirm"]').attributes('data-variant')).toBe(variant)
    wrapper.unmount()
  })

  it('emits cancel when Escape closes the dialog', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })
    const dialog = wrapper.findComponent(Dialog)
    const content = wrapper.findComponent(DialogContent)
    const escapeEvent = new Event('escapeKeyDown', { cancelable: true })

    content.vm.$emit('escapeKeyDown', escapeEvent)
    dialog.vm.$emit('update:open', false)
    await nextTick()

    expect(escapeEvent.defaultPrevented).toBe(false)
    expect(wrapper.emitted('cancel')).toEqual([[]])
    wrapper.unmount()
  })

  it('blocks actions while saving', async () => {
    const wrapper = await mountDialog({ type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'Pendente' }, true)

    expect(wrapper.get('[data-slot="field"]').attributes('data-disabled')).toBe('true')
    expect(wrapper.get('textarea').attributes('disabled')).toBeDefined()
    expect(wrapper.get('textarea').attributes('name')).toBe('reason')
    expect(wrapper.get('textarea').attributes('autocomplete')).toBe('off')
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-testid="draft-reason-cancel"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-testid="draft-reason-confirm"] [data-slot="spinner"]')).toBeTruthy()
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').text()).toBe('Salvando…')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toBeUndefined()
    wrapper.unmount()
  })

  it('keeps the controlled dialog open when closing is attempted while saving', async () => {
    const wrapper = await mountDialog({ type: 'republishDiscord', publicationType: 'Presenca', publicationStatus: 'Pendente' }, true)
    const dialog = wrapper.findComponent(Dialog)
    const content = wrapper.findComponent(DialogContent)
    const escapeEvent = new Event('escapeKeyDown', { cancelable: true })
    const outsideEvent = new Event('interactOutside', { cancelable: true })

    dialog.vm.$emit('update:open', false)
    content.vm.$emit('escapeKeyDown', escapeEvent)
    content.vm.$emit('interactOutside', outsideEvent)
    await nextTick()

    expect(escapeEvent.defaultPrevented).toBe(true)
    expect(outsideEvent.defaultPrevented).toBe(true)
    expect(wrapper.emitted('cancel')).toBeUndefined()
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
    wrapper.unmount()
  })
})

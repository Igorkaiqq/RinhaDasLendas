// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import { Dialog, DialogContent } from '@/components/ui/dialog'

import DraftReasonDialog, { type DraftReasonDialogAction } from './DraftReasonDialog.vue'

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
  it.each([
    ['cancelDraft', { type: 'cancelDraft' }, 'Cancelar draft'],
    ['addManualPresence', { type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' }, 'Adicionar presença'],
    ['removeManualPresence', { type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' }, 'Remover presença'],
    ['republishPresence', { type: 'republishPresence', publicationStatus: 'Falha' }, 'Republicar lista de presença'],
    ['republishTeams', { type: 'republishTeams', publicationStatus: 'Pendente' }, 'Republicar times'],
  ] as const)('renders the %s context', async (_, action, title) => {
    const wrapper = await mountDialog(action)

    expect(wrapper.text()).toContain(title)
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
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
      { type: 'republishPresence', publicationStatus: 'Falha' },
      'Republicar lista de presença',
      'Lista de presença',
      'Status atual: falhou',
    ],
    [{ type: 'republishTeams', publicationStatus: 'Pendente' }, 'Republicar times', 'Times definidos', 'Status atual: pendente'],
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

  it('normalizes and emits a valid reason', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await wrapper.get('textarea').setValue('  motivo válido  ')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toEqual([['motivo válido']])
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

  it('does not submit a reason longer than 500 characters', async () => {
    const wrapper = await mountDialog({ type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' })

    await wrapper.get('textarea').setValue('a'.repeat(501))
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('confirm')).toBeUndefined()
    expect(wrapper.get('textarea').attributes('maxlength')).toBe('500')
    expect(wrapper.get('[role="alert"]').text()).toBe('O motivo deve ter no máximo 500 caracteres.')
    wrapper.unmount()
  })

  it('groups the reason field with FieldGroup', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    expect(wrapper.get('[data-slot="field-group"] [data-slot="field"]')).toBeTruthy()
    wrapper.unmount()
  })

  it('emits cancel from the back button', async () => {
    const wrapper = await mountDialog({ type: 'cancelDraft' })

    await wrapper.get('[data-testid="draft-reason-cancel"]').trigger('click')

    expect(wrapper.emitted('cancel')).toEqual([[]])
    wrapper.unmount()
  })

  it.each([
    [{ type: 'cancelDraft' }, 'destructive'],
    [{ type: 'addManualPresence', jogadorId: 'j2', jogadorNome: 'Lux' }, 'default'],
    [{ type: 'removeManualPresence', jogadorId: 'j1', jogadorNome: 'Ahri' }, 'destructive'],
    [{ type: 'republishPresence', publicationStatus: 'Falha' }, 'default'],
    [{ type: 'republishTeams', publicationStatus: 'Pendente' }, 'default'],
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
    const wrapper = await mountDialog({ type: 'republishPresence', publicationStatus: 'Pendente' }, true)

    expect(wrapper.get('[data-slot="field"]').attributes('data-disabled')).toBe('true')
    expect(wrapper.get('textarea').attributes('disabled')).toBeDefined()
    expect(wrapper.get('textarea').attributes('name')).toBe('reason')
    expect(wrapper.get('textarea').attributes('autocomplete')).toBe('off')
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-testid="draft-reason-cancel"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-testid="draft-reason-confirm"] [data-slot="spinner"]')).toBeTruthy()
    expect(wrapper.get('[data-testid="draft-reason-confirm"]').text()).toBe('Salvando...')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('confirm')).toBeUndefined()
    wrapper.unmount()
  })

  it('keeps the controlled dialog open when closing is attempted while saving', async () => {
    const wrapper = await mountDialog({ type: 'republishPresence', publicationStatus: 'Pendente' }, true)
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

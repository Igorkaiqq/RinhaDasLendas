import { nextTick, onBeforeUnmount, watch } from 'vue'
import type { Ref } from 'vue'

type FocusableElement = InstanceType<typeof globalThis.HTMLElement>
type ModalKeyboardEvent = InstanceType<typeof globalThis.KeyboardEvent>

const focusableSelector = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',')

export function useModalFocus(
  open: () => boolean,
  container: Ref<FocusableElement | null>,
  close: () => void,
  initialFocus: () => FocusableElement | null,
) {
  let previousFocus: FocusableElement | null = null
  let appWasInert = false

  watch(
    open,
    async (isOpen, wasOpen) => {
      if (isOpen) {
        activate()
        await nextTick()
        const mobile = globalThis.matchMedia?.('(max-width: 767px)').matches ?? false
        ;(mobile ? container.value : initialFocus() ?? container.value)?.focus()
      } else if (wasOpen) {
        await deactivate(true)
      }
    },
    { immediate: true },
  )

  onBeforeUnmount(() => {
    void deactivate(true)
  })

  function activate() {
    previousFocus =
      globalThis.document.activeElement instanceof globalThis.HTMLElement
        ? globalThis.document.activeElement
        : null
    const app = globalThis.document.getElementById('app')
    if (app && !app.contains(container.value)) {
      appWasInert = app.hasAttribute('inert')
      app.setAttribute('inert', '')
    }
    globalThis.document.addEventListener('keydown', handleKeydown)
  }

  async function deactivate(restoreFocus: boolean) {
    globalThis.document.removeEventListener('keydown', handleKeydown)
    const app = globalThis.document.getElementById('app')
    if (app && !appWasInert) app.removeAttribute('inert')
    if (restoreFocus) {
      await nextTick()
      previousFocus?.focus()
    }
    previousFocus = null
  }

  function handleKeydown(event: ModalKeyboardEvent) {
    if (!open()) return
    if (event.key === 'Escape') {
      event.preventDefault()
      close()
      return
    }
    if (event.key !== 'Tab') return

    const elements = Array.from(
      container.value?.querySelectorAll<FocusableElement>(focusableSelector) ?? [],
    )
    const first = elements[0]
    const last = elements[elements.length - 1]
    if (!first || !last) {
      event.preventDefault()
      container.value?.focus()
      return
    }
    if (event.shiftKey && globalThis.document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && globalThis.document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }
}

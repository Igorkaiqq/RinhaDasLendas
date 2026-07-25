import { describe, expect, it } from 'vitest'

import en from './locales/en.json'
import pt from './locales/pt.json'
import { i18n, setLocale } from './index'

const settingsComponents = import.meta.glob('../components/settings/*.vue', {
  eager: true,
  import: 'default',
  query: '?raw',
}) as Record<string, string>

const draftComponents = import.meta.glob(
  ['../views/DraftsView.vue', '../components/drafts/**/*.vue'],
  {
    eager: true,
    import: 'default',
    query: '?raw',
  },
) as Record<string, string>

function leafPaths(source: object, prefix = ''): string[] {
  return Object.entries(source).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return value && typeof value === 'object' ? leafPaths(value, path) : [path]
  })
}

function findTagEnd(source: string, start: number): number {
  let quote: '"' | "'" | null = null

  for (let index = start; index < source.length; index += 1) {
    const character = source[index]
    if (quote) {
      if (character === quote && source[index - 1] !== '\\') {
        quote = null
      }
      continue
    }
    if (character === '"' || character === "'") {
      quote = character
    } else if (character === '>') {
      return index
    }
  }

  return -1
}

function outerSfcTemplate(source: string): string {
  const templateTag = /<\/?template\b/gi
  let depth = 0
  let contentStart = -1
  let match: RegExpExecArray | null

  while ((match = templateTag.exec(source))) {
    const tagEnd = findTagEnd(source, templateTag.lastIndex)
    if (tagEnd === -1) {
      return ''
    }

    const closing = source[match.index + 1] === '/'
    if (!closing) {
      depth += 1
      if (depth === 1) {
        contentStart = tagEnd + 1
      }
    } else {
      depth -= 1
      if (depth === 0 && contentStart !== -1) {
        return source.slice(contentStart, match.index)
      }
    }
    templateTag.lastIndex = tagEnd + 1
  }

  return ''
}

function hasVisibleCharacters(value: string): boolean {
  return /[\p{L}\p{N}]/u.test(value)
}

function boundStringLiteral(value: string): string | null {
  const expression = value.trim()
  const quote = expression[0]
  if (!quote || !['"', "'", '`'].includes(quote) || expression.at(-1) !== quote) {
    return null
  }
  return expression.slice(1, -1)
}

function stringLiteralAt(source: string, start: number): string | null {
  const quote = source[start]
  if (!quote || !['"', "'", '`'].includes(quote)) {
    return null
  }

  let value = ''
  for (let index = start + 1; index < source.length; index += 1) {
    const character = source[index]
    if (character === '\\' && index + 1 < source.length) {
      value += source[index + 1]
      index += 1
    } else if (character === quote) {
      return value
    } else {
      value += character
    }
  }

  return null
}

function visibleTemplateText(template: string): string[] {
  const source = template.replace(/{{[\s\S]*?}}/g, '')
  const textNodes: string[] = []
  let cursor = 0

  while (cursor < source.length) {
    const tagStart = source.indexOf('<', cursor)
    const textEnd = tagStart === -1 ? source.length : tagStart
    const text = source.slice(cursor, textEnd).trim()
    if (hasVisibleCharacters(text)) {
      textNodes.push(text)
    }
    if (tagStart === -1) {
      break
    }

    const tagEnd = findTagEnd(source, tagStart + 1)
    if (tagEnd === -1) {
      break
    }
    cursor = tagEnd + 1
  }

  return textNodes
}

function draftHardcodedTextViolations(source: string): string[] {
  const template = outerSfcTemplate(source)
  const violations: string[] = []

  for (const text of visibleTemplateText(template)) {
    violations.push(`visible text: ${text}`)
  }

  const attributePattern = /(?:^|\s)(:|v-bind:)?(aria-label|title|placeholder|alt)\s*=\s*(["'])([\s\S]*?)\3/g
  for (const match of template.matchAll(attributePattern)) {
    const literal = match[1] ? boundStringLiteral(match[4]) : match[4]
    if (literal !== null && hasVisibleCharacters(literal)) {
      violations.push(`${match[2]}: ${literal}`)
    }
  }

  const notificationPatterns = [
    /\b(?:toast(?:\.\w+)?|notify|notification(?:\.\w+)?)\s*\(\s*/g,
    /\bnotification(?:\.\w+)*\s*=\s*/g,
  ]
  for (const pattern of notificationPatterns) {
    for (const match of source.matchAll(pattern)) {
      const literal = stringLiteralAt(source, (match.index ?? 0) + match[0].length)
      if (literal !== null && hasVisibleCharacters(literal)) {
        violations.push(`notification: ${literal}`)
      }
    }
  }

  return violations
}

describe('i18n', () => {
  it('loads Portuguese translations by default', () => {
    setLocale('pt')

    expect(i18n.global.t('navigation.players')).toBe('Jogadores')
  })

  it('loads English translations when locale changes', () => {
    setLocale('en')

    expect(i18n.global.t('navigation.players')).toBe('Players')
  })

  it('keeps Portuguese and English translation leaf paths synchronized', () => {
    expect(leafPaths(pt).sort()).toEqual(leafPaths(en).sort())
    expect(
      i18n.global.t(
        'updates.releases.2026_07_2.details.weekly-presence-scheduling.title',
      ),
    ).not.toContain('updates.')
  })

  it('provides equivalent and product-safe presence scheduling release content', () => {
    const detailIds = [
      'weekly-presence-scheduling',
      'publication-closing-times',
      'moderator-management',
      'window-recovery',
      'duplicate-draft-protection',
    ] as const

    for (const locale of [pt, en]) {
      const release = locale.updates.releases['2026_07_2']
      expect(release.title).toBeTruthy()
      expect(release.summary).toBeTruthy()
      expect(Object.keys(release.details)).toEqual(detailIds)
      for (const detail of Object.values(release.details)) {
        expect(detail.title).toBeTruthy()
        expect(detail.description).toBeTruthy()
        expect(detail.description).not.toMatch(/claims?|locks?|tokens?|endpoints?|\/api\//i)
      }
    }
  })

  it('provides the complete localized presence schedule interface', () => {
    const requiredPaths = [
      'settings.presenceSchedules.title',
      'settings.presenceSchedules.actions.viewHistory',
      'settings.presenceSchedules.fields.name.placeholder',
      'settings.presenceSchedules.weekdays.Sabado',
      'settings.presenceSchedules.statuses.occurrence.Bloqueada',
      'settings.presenceSchedules.confirm.archive.description',
      'settings.presenceSchedules.history.livePage',
      'settings.presenceSchedules.toasts.created',
      'settings.presenceSchedules.validation.closingAfterPublication',
      'settings.presenceSchedules.accessibility.scheduleList',
      'settings.presenceSchedules.messageCodes.requestFailed',
      'settings.presenceSchedules.refreshError',
      'settings.presenceSchedules.actions.refreshing',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
    }
    expect(pt.settings.presenceSchedules.weekdays.Sabado).toBe('Sábado')
    expect(pt.settings.presenceSchedules.fields.observation.label).toBe('Observação')
    expect(pt.settings.presenceSchedules.loading).toMatch(/…$/)
    expect(en.settings.presenceSchedules.loading).toMatch(/…$/)
    expect(pt.settings.presenceSchedules.history.loading).toMatch(/…$/)
    expect(en.settings.presenceSchedules.history.loading).toMatch(/…$/)
    expect(pt.settings.presenceSchedules.fields.name.placeholder).toMatch(/^Ex\.: .+…$/)
    expect(en.settings.presenceSchedules.fields.name.placeholder).toMatch(/^E\.g\. .+…$/)
    expect(pt.settings.presenceSchedules.fields.observation.placeholder).toMatch(/…$/)
    expect(en.settings.presenceSchedules.fields.observation.placeholder).toMatch(/…$/)
  })

  it('maps MV100 to the closed occurrence window meaning in both locales', () => {
    expect(pt.settings.presenceSchedules.messageCodes.MV100)
      .toBe('A janela desta ocorrência já foi encerrada.')
    expect(en.settings.presenceSchedules.messageCodes.MV100)
      .toBe('This occurrence window has already closed.')
  })

  it('keeps presence schedule visible text and accessible names in i18n', () => {
    for (const [path, source] of Object.entries(settingsComponents)) {
      const template = source.match(/<template>([\s\S]*?)<\/template>/)?.[1] ?? ''
      expect(template, path).not.toMatch(/>\s*[A-Za-zÀ-ÿ][^<{]*</)
      expect(source, path).not.toMatch(/(?<![:@])\b(?:aria-label|placeholder|title)="[^"{]*[A-Za-zÀ-ÿ][^"]*"/)
      expect(source, path).not.toMatch(/toast\.(?:success|error)\(\s*['"`]/)
    }
  })

  it('keeps draft visible text and accessible names in i18n', () => {
    for (const [path, source] of Object.entries(draftComponents)) {
      expect(draftHardcodedTextViolations(source), path).toEqual([])
    }
  })

  it.each([
    {
      name: 'visible text after a nested template',
      source: '<template><template v-if="ok"><span>{{ value }}</span></template><span>Later</span></template>',
    },
    { name: 'one-character visible text', source: '<template><span>C</span></template>' },
    { name: 'double-quoted static aria-label', source: '<template><button aria-label="Close" /></template>' },
    { name: 'single-quoted static title', source: "<template><button title='Close' /></template>" },
    { name: 'double-quoted static alt', source: '<template><img alt="Avatar" /></template>' },
    { name: 'single-quoted static placeholder', source: "<template><input placeholder='Search' /></template>" },
    {
      name: 'double-quoted bound aria-label literal',
      source: `<template><button :aria-label="'Close'" /></template>`,
    },
    {
      name: 'single-quoted bound title literal',
      source: `<template><button :title='"Close"' /></template>`,
    },
    {
      name: 'double-quoted v-bind placeholder literal',
      source: `<template><input v-bind:placeholder="'Search'" /></template>`,
    },
    {
      name: 'single-quoted bound alt literal',
      source: `<template><img :alt='"Avatar"' /></template>`,
    },
    {
      name: 'toast literal',
      source: `<script setup>toast.warning('Check this')</script><template><div /></template>`,
    },
    {
      name: 'notification literal',
      source: `<script setup>notification.value = "Saved"</script><template><div /></template>`,
    },
  ])('detects $name', ({ source }) => {
    expect(draftHardcodedTextViolations(source)).not.toEqual([])
  })

  it('does not treat greater-than operators inside attributes as visible text', () => {
    const source = `<template><Dialog @update:open="(value) => close(value)">{{ t('common.close') }}</Dialog></template>`

    expect(draftHardcodedTextViolations(source)).toEqual([])
  })

  it('provides shared draft state, progress, action, and accessibility translations', () => {
    const requiredPaths = [
      'drafts.status.unknown',
      'drafts.rail.cancelled',
      'drafts.rail.unknown',
      'drafts.progress.completed',
      'drafts.progress.current',
      'drafts.progress.pending',
      'drafts.progress.terminal',
      'drafts.progress.unknown',
      'drafts.actions.clearFilters',
      'drafts.actions.retry',
      'drafts.accessibility.selectedDraft',
      'drafts.accessibility.currentStep',
      'drafts.roles.captainShort',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
    }
  })

  it('describes security hardening without exposing sensitive implementation details', () => {
    const descriptions = [
      pt.updates.releases['2026_07_1'].details['security-stability-hardening']
        .description,
      en.updates.releases['2026_07_1'].details['security-stability-hardening']
        .description,
    ]

    for (const description of descriptions) {
      expect(description).not.toMatch(/tokens?|endpoints?|https?:\/\/|\/api\//i)
    }
  })
})

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
  if (!quote || !['"', "'", '`'].includes(quote) || expression[expression.length - 1] !== quote) {
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

function stringLiterals(source: string): Array<{ quote: string; value: string }> {
  const literals: Array<{ quote: string; value: string }> = []

  for (let index = 0; index < source.length; index += 1) {
    const value = stringLiteralAt(source, index)
    if (value === null) {
      continue
    }

    const quote = source[index]!
    literals.push({ quote, value })
    index += 1
    while (index < source.length) {
      if (source[index] === '\\') {
        index += 2
      } else if (source[index] === quote) {
        break
      } else {
        index += 1
      }
    }
  }

  return literals
}

function withoutI18nLookups(expression: string): string {
  const source = [...expression]
  const lookup = /(?:^|[^\w$])\$?t\s*\(/g
  let match: RegExpExecArray | null

  while ((match = lookup.exec(expression))) {
    const open = match.index + match[0].lastIndexOf('(')
    let depth = 1
    let quote: string | null = null
    let end = open + 1

    for (; end < expression.length && depth > 0; end += 1) {
      const character = expression[end]!
      if (quote) {
        if (character === '\\') {
          end += 1
        } else if (character === quote) {
          quote = null
        }
      } else if (['"', "'", '`'].includes(character)) {
        quote = character
      } else if (character === '(') {
        depth += 1
      } else if (character === ')') {
        depth -= 1
      }
    }

    if (depth === 0) {
      source.fill(' ', match.index, end)
      lookup.lastIndex = end
    }
  }

  return source.join('')
}

function expressionLiterals(expression: string): string[] {
  return stringLiterals(withoutI18nLookups(expression))
    .map(({ quote, value }) => quote === '`' ? value.replace(/\$\{[\s\S]*?}/g, '') : value)
    .filter(hasVisibleCharacters)
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

  for (const match of template.matchAll(/{{([\s\S]*?)}}/g)) {
    for (const literal of expressionLiterals(match[1]!)) {
      violations.push(`interpolation: ${literal}`)
    }
  }

  const attributePattern = /(?:^|\s)(:|v-bind:)?(aria-label|title|placeholder|alt)\s*=\s*(["'])([\s\S]*?)\3/g
  for (const match of template.matchAll(attributePattern)) {
    const literal = match[1] ? boundStringLiteral(match[4]!) : match[4]!
    if (literal !== null && hasVisibleCharacters(literal)) {
      violations.push(`${match[2]!}: ${literal}`)
    }
  }

  const vTextPattern = /(?:^|\s)v-text\s*=\s*(["'])([\s\S]*?)\1/g
  for (const match of template.matchAll(vTextPattern)) {
    for (const literal of expressionLiterals(match[2]!)) {
      violations.push(`v-text: ${literal}`)
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
        'updates.releases.2026_07_3.details.selected-weekday-feedback.title',
      ),
    ).not.toContain('updates.')
  })

  it('provides exact benefit-oriented draft redesign content in both locales', () => {
    expect(pt.updates.releases['2026_07_4']).toEqual({
      title: 'Draft mais claro do início ao fim',
      summary:
        'Organize presença, etapas e escolhas em uma área mais clara, acessível e pronta para qualquer tela.',
      details: {
        'operational-hierarchy': {
          title: 'Hierarquia para conduzir a partida',
          description:
            'O draft destaca contexto, progresso e próxima ação, ajudando a organizar cada decisão sem perder o momento atual.',
        },
        'presence-roster': {
          title: 'Lista de presença mais organizada',
          description:
            'Confirmados, reservas e inclusão manual ficam reunidos para facilitar ajustes antes da formação dos times.',
        },
        'stage-accessibility-clarity': {
          title: 'Etapas mais fáceis de acompanhar',
          description:
            'Estados, ações e avisos ganharam sinais mais claros para navegação por teclado e leitura assistida.',
        },
        'responsive-mobile-operation': {
          title: 'Operação confortável em qualquer tela',
          description:
            'O fluxo se adapta do desktop ao celular, mantendo jogadores, preferências e ações essenciais ao alcance.',
        },
      },
    })
    expect(en.updates.releases['2026_07_4']).toEqual({
      title: 'A clearer draft from start to finish',
      summary:
        'Organize presence, stages, and picks in a clearer, accessible workspace ready for any screen.',
      details: {
        'operational-hierarchy': {
          title: 'A clear hierarchy for running the match',
          description:
            'The draft highlights context, progress, and the next action so every decision stays connected to the current stage.',
        },
        'presence-roster': {
          title: 'A more organized presence roster',
          description:
            'Confirmed players, reserves, and manual additions stay together, making adjustments easier before teams are formed.',
        },
        'stage-accessibility-clarity': {
          title: 'Stages that are easier to follow',
          description:
            'Statuses, actions, and notices now use clearer signals for keyboard navigation and assisted reading.',
        },
        'responsive-mobile-operation': {
          title: 'Comfortable operation on any screen',
          description:
            'The flow adapts from desktop to mobile while keeping players, preferences, and essential actions within reach.',
        },
      },
    })

    for (const release of [
      pt.updates.releases['2026_07_4'],
      en.updates.releases['2026_07_4'],
    ]) {
      expect(JSON.stringify(release)).not.toMatch(
        /(?:api|css|component|endpoint|payload|signalr|vue|breakpoint)/i,
      )
    }
  })

  it('freezes the complete localized presence scheduling release', () => {
    expect(pt.updates.releases['2026_07_2']).toEqual({
      title: 'Listas de presença agendadas',
      summary:
        'Moderadores agora podem organizar listas semanais com horários definidos, acompanhamento do histórico e recuperação segura durante a janela configurada.',
      details: {
        'weekly-presence-scheduling': {
          title: 'Agendamento semanal de presença',
          description:
            'Crie agendas para os dias da semana escolhidos e deixe cada nova lista entrar automaticamente no fluxo atual do Discord.',
        },
        'publication-closing-times': {
          title: 'Horários claros para publicar e encerrar',
          description:
            'Cada agenda informa quando a lista será publicada e até que horário os jogadores poderão confirmar presença.',
        },
        'moderator-management': {
          title: 'Gestão disponível para Moderador+',
          description:
            'Usuários autorizados podem criar, editar, pausar, reativar e acompanhar agendas pela área de configurações.',
        },
        'window-recovery': {
          title: 'Recuperação dentro da janela',
          description:
            'Se a plataforma ficar temporariamente indisponível, a lista ainda pode ser criada quando o serviço voltar antes do encerramento.',
        },
        'duplicate-draft-protection': {
          title: 'Proteção contra listas duplicadas',
          description:
            'Cada agenda e data gera no máximo um draft, mesmo após reinícios ou processamentos simultâneos.',
        },
      },
    })
    expect(en.updates.releases['2026_07_2']).toEqual({
      title: 'Scheduled presence lists',
      summary:
        'Moderators can now organize weekly lists with defined times, history tracking, and safe recovery during the configured window.',
      details: {
        'weekly-presence-scheduling': {
          title: 'Weekly presence scheduling',
          description:
            'Create schedules for selected weekdays and let each new list enter the current Discord flow automatically.',
        },
        'publication-closing-times': {
          title: 'Clear publication and closing times',
          description:
            'Each schedule states when the list will be published and until what time players can confirm presence.',
        },
        'moderator-management': {
          title: 'Management available to Moderator+',
          description:
            'Authorized users can create, edit, pause, reactivate, and monitor schedules from the settings area.',
        },
        'window-recovery': {
          title: 'Recovery within the window',
          description:
            'If the platform is temporarily unavailable, the list can still be created when service returns before closing.',
        },
        'duplicate-draft-protection': {
          title: 'Duplicate list protection',
          description:
            'Each schedule and date creates at most one draft, even after restarts or simultaneous processing.',
        },
      },
    })
  })

  it('provides exact benefit-oriented selected weekday fix content in both locales', () => {
    expect(pt.updates.releases['2026_07_3']).toEqual({
      title: 'Dias selecionados mais claros',
      summary:
        'Os dias escolhidos nos agendamentos de presença agora ficam destacados, facilitando a revisão antes de salvar.',
      details: {
        'selected-weekday-feedback': {
          title: 'Confirmação visual dos dias selecionados',
          description:
            'Ao configurar um agendamento, você identifica imediatamente quais dias da semana estão selecionados e evita dúvidas antes de salvar.',
        },
      },
    })
    expect(en.updates.releases['2026_07_3']).toEqual({
      title: 'Clearer selected weekdays',
      summary:
        'Selected days in presence schedules are now highlighted, making them easier to review before saving.',
      details: {
        'selected-weekday-feedback': {
          title: 'Visual confirmation for selected weekdays',
          description:
            'When configuring a schedule, you can immediately identify which weekdays are selected and avoid uncertainty before saving.',
        },
      },
    })

    for (const release of [
      pt.updates.releases['2026_07_3'],
      en.updates.releases['2026_07_3'],
    ]) {
      expect(JSON.stringify(release)).not.toMatch(/redesenho|redesign/i)
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
    {
      name: 'string literal interpolation',
      source: `<template><span>{{ 'Saved' }}</span></template>`,
    },
    {
      name: 'conditional string literal interpolation',
      source: `<template><span>{{ condition ? 'Saved' : 'Failed' }}</span></template>`,
    },
    {
      name: 'v-text string literal',
      source: `<template><span v-text="'Saved'" /></template>`,
    },
  ])('detects $name', ({ source }) => {
    expect(draftHardcodedTextViolations(source)).not.toEqual([])
  })

  it('does not treat greater-than operators inside attributes as visible text', () => {
    const source = `<template><Dialog @update:open="(value) => close(value)">{{ t('common.close') }}</Dialog></template>`

    expect(draftHardcodedTextViolations(source)).toEqual([])
  })

  it('allows translated interpolation expressions', () => {
    const source = `<template><h1>{{ t('drafts.title') }}</h1></template>`

    expect(draftHardcodedTextViolations(source)).toEqual([])
  })

  it('allows dynamic template literals without visible copy', () => {
    const source = "<template><span>{{ player.elo ? `${player.elo} ${player.divisao ?? ''}` : t('common.eloNotInformed') }}</span></template>"

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
      'drafts.visualBoard.moveDestination',
      'drafts.visualBoard.moveToFree',
      'drafts.visualBoard.moveAnnouncement',
      'drafts.visualBoard.detailsFor',
      'drafts.realtime.liveStatus',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
    }
  })

  it('provides equivalent archive, restore, permission, conflict, and cancellation copy', () => {
    const requiredPaths = [
      'drafts.archive.filter',
      'drafts.archive.badge',
      'drafts.archive.activeWarning',
      'drafts.archive.historyTitle',
      'drafts.archive.errors.unauthorized',
      'drafts.archive.errors.forbidden',
      'drafts.archive.errors.conflict',
      'drafts.publication.cancellation',
      'drafts.publication.republishCancellation',
      'drafts.reasonDialog.archiveDraft.title',
      'drafts.reasonDialog.restoreDraft.title',
      'updates.releases.2026_07_5.title',
      'updates.releases.2026_07_5.summary',
      'updates.releases.2026_07_5.details.archive-and-restore.title',
      'updates.releases.2026_07_5.details.archive-and-restore.description',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
      expect(i18n.global.t(path)).not.toContain(path)
    }
  })

  it('uses the ellipsis character for reviewed loading and saving copy', () => {
    const reviewed = [
      pt.common.saving,
      en.common.saving,
      pt.auth.login.submitting,
      en.auth.login.submitting,
      pt.auth.register.submitting,
      en.auth.register.submitting,
      pt.drafts.createModal.creating,
      en.drafts.createModal.creating,
      pt.topbar.searchPlaceholder,
      en.topbar.searchPlaceholder,
      pt.players.searchPlaceholder,
      en.players.searchPlaceholder,
    ]

    expect(reviewed.every((value) => value.endsWith('…') && !value.endsWith('...'))).toBe(true)
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

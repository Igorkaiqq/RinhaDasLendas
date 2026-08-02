// @vitest-environment happy-dom
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { computed, nextTick, ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemParticipante, DraftMontagemRealtimeSnapshot, DraftMontagemRealtimeState, DraftMontagemResumo, DraftMontagemStatus, DraftMontagemSubstituicaoPayload } from '@/types/draftMontagem'

import DraftsView from './DraftsView.vue'
import DraftsViewSource from './DraftsView.vue?raw'
import DraftVisualBoardSource from '@/components/drafts/visual/DraftVisualBoard.vue?raw'
import DraftPreparationPanelSource from '@/components/drafts/DraftPreparationPanel.vue?raw'
import DraftDiscordPublicationPanelSource from '@/components/drafts/DraftDiscordPublicationPanel.vue?raw'
import DraftNavigatorSource from '@/components/drafts/DraftNavigator.vue?raw'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')
const IndexHtml = readFileSync(resolve(process.cwd(), 'index.html'), 'utf8')

const serviceMocks = vi.hoisted(() => ({
  cancelDraftMontagem: vi.fn(),
  archiveDraftMontagem: vi.fn(),
  restoreDraftMontagem: vi.fn(),
  addManualDraftMontagemPresence: vi.fn(),
  getDraftMontagemById: vi.fn(),
  getDraftMontagemAdminById: vi.fn(),
  getDraftMontagemArchivingById: vi.fn(),
  getDraftMontagemRealtimeState: vi.fn(),
  listDraftMontagens: vi.fn(),
  listEligibleManualPresencePlayers: vi.fn(),
  removeManualDraftMontagemPresence: vi.fn(),
  reopenDraftMontagemPresence: vi.fn(),
  republishDraftMontagemDiscordPublication: vi.fn(),
  republishArchivedDraftCancellation: vi.fn(),
  cancelDraftMontagemPresence: vi.fn(),
  closeDraftMontagemPresence: vi.fn(),
  chooseDraftMontagemMode: vi.fn(),
  confirmDraftMontagemPresence: vi.fn(),
  defineDraftMontagemCaptains: vi.fn(),
  defineDraftMontagemPickOrder: vi.fn(),
  drawDraftMontagemCaptains: vi.fn(),
  finalizeDraftMontagem: vi.fn(),
  registerDraftMontagemPick: vi.fn(),
  saveDraftMontagemLayout: vi.fn(),
  startDraftMontagemRealtime: vi.fn(),
  substituteDraftMontagemReserve: vi.fn(),
}))
const playerMocks = vi.hoisted(() => ({
  listPlayers: vi.fn(),
  listEligibleCaptains: vi.fn(),
}))
const authMock = vi.hoisted(() => ({
  canManageDrafts: true,
  canArchiveDrafts: true,
  jogadorId: null as string | null,
  roles: ['Admin'] as string[],
  rolesRef: null as unknown as { value: string[] },
}))
const routeMock = vi.hoisted(() => ({ query: {} as Record<string, string> }))
const routeGuardMock = vi.hoisted(() => ({ guard: null as null | (() => boolean | Promise<boolean>) }))
const realtimeMock = vi.hoisted(() => ({
  handlers: new Map<string, (state: DraftMontagemRealtimeSnapshot) => void | Promise<void>>(),
  archivedHandlers: new Map<string, (draftMontagemId: string) => void | Promise<void>>(),
  reconnectHandlers: new Map<string, () => void | Promise<void>>(),
  degradedHandlers: new Map<string, (status: 'reconnecting' | 'fallback' | 'disconnected') => void>(),
  disconnected: [] as string[],
  order: [] as string[],
  startGate: null as Promise<void> | null,
  joinGate: null as Promise<void> | null,
}))
let configuredCanonicalAdmin: DraftMontagemAdmin | null = null
let mountedCanonicalAdmin: DraftMontagemAdmin | null = null
const setAdminResolvedValue = serviceMocks.getDraftMontagemAdminById.mockResolvedValue.bind(serviceMocks.getDraftMontagemAdminById)
serviceMocks.getDraftMontagemAdminById.mockResolvedValue = ((value: DraftMontagemAdmin) => {
  configuredCanonicalAdmin = value
  return setAdminResolvedValue(value)
}) as typeof serviceMocks.getDraftMontagemAdminById.mockResolvedValue

vi.mock('vue-router', () => ({
  useRoute: () => routeMock,
  onBeforeRouteLeave: (guard: () => boolean | Promise<boolean>) => { routeGuardMock.guard = guard },
}))

vi.mock('@/services/authState', () => ({
  useAuthState: () => {
    const roles = ref(authMock.roles)
    authMock.rolesRef = roles
    return {
    user: computed(() => ({ id: 'organizador-1', jogadorId: authMock.jogadorId, roles: roles.value })),
    hasPermission: (permission: string) => permission === 'CanArchiveDrafts' ? authMock.canArchiveDrafts : authMock.canManageDrafts,
    hasRole: (role: string) => roles.value.includes(role),
  }
  },
}))

vi.mock('@/services/players', () => ({
  ...playerMocks,
}))

vi.mock('@/services/draftMontagens', () => ({
  ...serviceMocks,
  DraftMontagemServiceError: class DraftMontagemServiceError extends Error {
    constructor(public errors: string[] = [], public status?: number) {
      super(errors[0])
    }
  },
  createDraftMontagem: vi.fn(),
  drawDraftMontagemCaptains: serviceMocks.drawDraftMontagemCaptains,
  substituteDraftMontagemReserve: serviceMocks.substituteDraftMontagemReserve,
}))

vi.mock('@/services/draftMontagemRealtime', () => ({
  DraftMontagemRealtimeConnection: class DraftMontagemRealtimeConnection {
    constructor(private readonly id: string) {}
    connect = vi.fn().mockImplementation(async (onStateUpdated, onReady, onArchived, onDegraded) => {
      realtimeMock.handlers.set(this.id, onStateUpdated)
      realtimeMock.reconnectHandlers.set(this.id, onReady)
      realtimeMock.archivedHandlers.set(this.id, onArchived)
      realtimeMock.degradedHandlers.set(this.id, onDegraded)
      realtimeMock.order.push('start')
      await realtimeMock.startGate
      realtimeMock.order.push('JoinDraftMontagem')
      await realtimeMock.joinGate
      await onReady?.()
    })
    disconnect = vi.fn().mockImplementation(async () => {
      realtimeMock.disconnected.push(this.id)
    })
  },
}))

const montagem: DraftMontagem = {
  id: 'montagem-1',
  nome: 'Rinha de domingo',
  status: 'PresencaAberta',
  modo: 'Manual',
  cicloVersao: 'Legado',
  tamanhoEquipe: 5,
  quantidadeTimes: 2,
  quantidadeReservas: 2,
  criterioCapitaes: 'Manual',
  duracaoTurnoSegundos: 60,
  presencaContinuadaManualmente: false,
  presencas: [
    {
      id: 'presenca-1',
      usuarioId: 'usuario-1',
      jogadorId: 'jogador-1',
      nomeExibicao: 'Ahri',
      origemConfirmacao: 'Manual',
      status: 'Confirmada',
      confirmadoEm: '2026-07-19T12:00:00Z',
      ordemConfirmacao: 1,
    },
  ],
  times: [],
  livres: [],
  reservas: [],
  escolhas: [],
  substituicoes: [],
  publicacoesDiscord: [
    { tipo: 'Presenca', status: 'Falha' },
    { tipo: 'ChamadaPresenca', status: 'Falha' },
    { tipo: 'TimesDefinidos', status: 'Publicada' },
  ],
  arquivado: false,
  versaoEstado: 7,
  dataCadastro: '2026-07-19T12:00:00Z',
  dataAtualizacao: '2026-07-19T12:00:00Z',
}

i18n.global.mergeLocaleMessage('pt', {
  drafts: {
    presence: {
      captainsCount: '{selected} / {total} capitães',
      reopen: 'Reabrir presença',
      reopened: 'Presença reaberta.',
    },
    reasonDialog: {
      reopenPresence: {
        title: 'Reabrir presença',
        description: 'Reabrir a presença de {draftName}?',
        confirm: 'Reabrir presença',
      },
    },
  },
})

const resumo: DraftMontagemResumo = {
  id: montagem.id,
  nome: montagem.nome,
  status: montagem.status,
  modo: montagem.modo,
  cicloVersao: montagem.cicloVersao,
  tamanhoEquipe: montagem.tamanhoEquipe,
  quantidadeTimes: montagem.quantidadeTimes,
  quantidadeReservas: montagem.quantidadeReservas,
  presencaContinuadaManualmente: montagem.presencaContinuadaManualmente,
  dataRinha: '2026-07-27T03:00:00Z',
  dataCadastro: montagem.dataCadastro,
  dataAtualizacao: montagem.dataAtualizacao,
  arquivado: false,
  versaoEstado: 7,
}

const montagemB: DraftMontagem = {
  ...montagem,
  id: 'montagem-2',
  nome: 'Rinha de segunda',
  status: 'Aberta',
  presencas: [],
  publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }],
}

const resumoB: DraftMontagemResumo = {
  ...resumo,
  id: montagemB.id,
  nome: montagemB.nome,
  status: montagemB.status,
}

let nextRealtimeVersion = montagem.versaoEstado

const realtimeCaptain: DraftMontagem['times'][number]['jogadores'][number] = {
  jogadorId: 'capitao-atual',
  nomeExibicao: 'Capitão atual',
  status: 'Ativo',
  preferencias: [],
  estado: 'Time',
  capitao: true,
  ordem: 1,
  dataCadastro: montagem.dataCadastro,
  dataAtualizacao: montagem.dataAtualizacao,
}

const realtimeTeam: DraftMontagem['times'][number] = {
  id: 'time-1',
  nome: 'Time atual',
  ordem: 1,
  cor: 'blue',
  capitaoId: realtimeCaptain.jogadorId,
  jogadores: [realtimeCaptain],
}

function adminProjection(status: DraftMontagemStatus = montagem.status, auditReason = 'auditoria inicial'): DraftMontagemAdmin {
  return {
    ...montagem,
    status,
    discordGuildId: 'guild-admin',
    discordPresenceMessageId: 'message-admin',
    presencas: montagem.presencas,
    substituicoes: [],
    publicacoesDiscord: [
      {
        id: 'publicacao-cta-admin',
        tipo: 'ChamadaPresenca',
        status: 'Falha',
        guildId: 'guild-admin',
        channelId: 'channel-admin',
        ultimaTentativaEm: '2026-07-19T12:00:00Z',
      },
      {
        id: 'publicacao-admin',
        tipo: 'Presenca',
        status: status === 'Cancelada' ? 'Pendente' : 'Falha',
        guildId: 'guild-admin',
        channelId: 'channel-admin',
        messageId: 'message-admin',
        publicadaEm: '2026-07-19T12:00:00Z',
        ultimaTentativaEm: '2026-07-19T12:00:00Z',
      },
      {
        id: 'publicacao-times-admin',
        tipo: 'TimesDefinidos',
        status: 'Publicada',
        guildId: 'guild-admin',
        channelId: 'channel-admin',
        messageId: 'message-times-admin',
        publicadaEm: '2026-07-19T12:00:00Z',
        ultimaTentativaEm: '2026-07-19T12:00:00Z',
      },
    ],
    acoesAdministrativas: [
      {
        id: `acao-${auditReason}`,
        tipo: 'RepublicacaoDiscord:Presenca',
        responsavelTipo: 'User',
        responsavelUsuarioId: 'organizador-1',
        motivo: auditReason,
        registradoEm: '2026-07-19T12:00:00Z',
      },
    ],
    capitaesElegiveisIds: ['jogador-1', 'jogador-2'],
    capitaesElegiveisSubstituicaoIds: ['jogador-1', 'jogador-2'],
  }
}

function adminProjectionB(auditReason = 'auditoria B'): DraftMontagemAdmin {
  return {
    ...adminProjection('Aberta', auditReason),
    ...montagemB,
    presencas: [],
    substituicoes: [],
    publicacoesDiscord: [
      {
        id: 'publicacao-admin-b',
        tipo: 'Presenca',
        status: 'Pendente',
        ultimaTentativaEm: '2026-07-20T12:00:00Z',
      },
    ],
    acoesAdministrativas: [
      {
        id: 'acao-b',
        tipo: 'RepublicacaoDiscord:Presenca',
        responsavelTipo: 'User',
        responsavelUsuarioId: 'organizador-1',
        motivo: auditReason,
        registradoEm: '2026-07-20T12:00:00Z',
      },
    ],
  }
}

function editableAdminProjection(version = montagem.versaoEstado): DraftMontagemAdmin {
  return {
    ...adminProjection('Aberta'),
    versaoEstado: version,
    quantidadeTimes: 1,
    tamanhoEquipe: 2,
    times: [{ ...realtimeTeam, nome: 'Original team' }],
    livres: [],
    reservas: [],
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (error: unknown) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, resolve, reject }
}

function sharedFromAdmin(detail: DraftMontagemAdmin): DraftMontagem {
  const shared = {
    ...detail,
    presencas: detail.presencas.map((presence) => omitTestFields(presence, ['discordUserId'])),
    substituicoes: detail.substituicoes.map((substitution) => omitTestFields(substitution, ['motivo', 'responsavelUsuarioId'])),
    publicacoesDiscord: detail.publicacoesDiscord.map((publication) => omitTestFields(publication, [
      'id', 'guildId', 'channelId', 'messageId', 'ultimoErroCodigo', 'claimId', 'claimExpiraEm', 'publicadaEm', 'ultimaTentativaEm',
    ])),
  } as Record<string, unknown>
  for (const key of ['discordGuildId', 'discordPresenceMessageId', 'acoesAdministrativas', 'capitaesElegiveisIds', 'capitaesElegiveisSubstituicaoIds', 'motivoCancelamento']) {
    delete shared[key]
  }
  return shared as unknown as DraftMontagem
}

function omitTestFields(value: object, keys: string[]) {
  const shared = { ...value } as Record<string, unknown>
  for (const key of keys) delete shared[key]
  return shared
}

async function emitRealtime(
  id: string,
  projection: DraftMontagem,
  canCurrentUserPick = false,
  personalizedState: DraftMontagemRealtimeState | null = { montagem: projection, canCurrentUserPick, serverNow: projection.dataAtualizacao },
) {
  nextRealtimeVersion = Math.max(nextRealtimeVersion + 1, projection.versaoEstado)
  const versionedProjection = { ...projection, versaoEstado: nextRealtimeVersion }
  if (personalizedState) serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ ...personalizedState, montagem: versionedProjection })
  await realtimeMock.handlers.get(id)?.({ montagem: versionedProjection, serverNow: projection.dataAtualizacao })
}

async function mountView(options: { realBoard?: boolean } = {}) {
  mountedCanonicalAdmin = configuredCanonicalAdmin
  const wrapper = mount(DraftsView, {
    attachTo: document.body,
    global: {
      plugins: [i18n],
      stubs: {
        teleport: { template: '<div data-teleport-stub><slot /></div>' },
        PageFrame: { template: '<div><slot /></div>' },
        PageHeader: { template: '<header><slot name="actions" /></header>' },
        PendingPlayerProfileNotice: true,
        DraftStateRail: true,
        DraftVisualBoard: options.realBoard ? false : true,
        DraftVisualSetup: true,
      },
    },
  })
  await flushPromises()
  return wrapper
}

function findButton(wrapper: VueWrapper, text: string) {
  const button = wrapper.findAll('button').find((candidate) => candidate.text().includes(text))
  expect(button, `button containing "${text}"`).toBeDefined()
  return button!
}

function expectStageFocus(wrapper: VueWrapper) {
  const activeElement = document.activeElement
  const workspace = wrapper.get('[data-draft-workspace]').element
  const header = wrapper.get('[data-testid="draft-workspace-header"]').element

  expect(activeElement).not.toBe(document.body)
  expect(workspace.contains(activeElement)).toBe(true)
  expect(activeElement === header || activeElement?.matches('[data-stage-primary-action]')).toBe(true)
}

async function openReasonDialog(wrapper: VueWrapper, buttonText: string) {
  await findButton(wrapper, buttonText).trigger('click')
  await flushPromises()
  expect(wrapper.get('[role="dialog"]')).toBeTruthy()
}

async function confirmReasonAction(wrapper: VueWrapper, buttonText: string, reason: string) {
  await openReasonDialog(wrapper, buttonText)
  await wrapper.get('textarea').setValue(reason)
  await wrapper.get('form').trigger('submit')
  await flushPromises()
}

describe('DraftsView reason actions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    authMock.canManageDrafts = true
    authMock.canArchiveDrafts = true
    authMock.jogadorId = null
    authMock.roles = ['Admin']
    if (authMock.rolesRef) authMock.rolesRef.value = ['Admin']
    routeMock.query = {}
    routeGuardMock.guard = null
    realtimeMock.handlers.clear()
    realtimeMock.archivedHandlers.clear()
    realtimeMock.reconnectHandlers.clear()
    realtimeMock.degradedHandlers.clear()
    realtimeMock.disconnected = []
    realtimeMock.order = []
    realtimeMock.startGate = null
    realtimeMock.joinGate = null
    nextRealtimeVersion = montagem.versaoEstado
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection())
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({
      draft: montagem,
      arquivadoEm: null,
      arquivadoPorUsuarioId: null,
      motivoArquivamento: null,
      acoes: [],
    })
    serviceMocks.getDraftMontagemById.mockResolvedValue(montagem)
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => {
      const canonical = mountedCanonicalAdmin
      return {
        montagem: canonical && canonical.id === id
          ? sharedFromAdmin(canonical)
          : id === montagemB.id ? montagemB : montagem,
        canCurrentUserPick: false,
      }
    })
    serviceMocks.listEligibleManualPresencePlayers.mockResolvedValue([{ id: 'jogador-2', nomeExibicao: 'Lux' }])
    playerMocks.listPlayers.mockResolvedValue([])
    playerMocks.listEligibleCaptains.mockResolvedValue([{ id: 'jogador-1', nomeExibicao: 'Ahri' }])
    serviceMocks.addManualDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.cancelDraftMontagem.mockResolvedValue(montagem)
    serviceMocks.archiveDraftMontagem.mockResolvedValue({ id: montagem.id, status: 'Cancelada', arquivado: true, versaoEstado: 8 })
    serviceMocks.restoreDraftMontagem.mockResolvedValue({ id: montagem.id, status: 'Cancelada', arquivado: false, versaoEstado: 9 })
    serviceMocks.republishArchivedDraftCancellation.mockResolvedValue({ id: montagem.id, status: 'Cancelada', arquivado: true, versaoEstado: 9 })
    serviceMocks.removeManualDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.reopenDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.republishDraftMontagemDiscordPublication.mockResolvedValue(montagem)
    serviceMocks.cancelDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.closeDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.chooseDraftMontagemMode.mockResolvedValue(montagem)
    serviceMocks.confirmDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.defineDraftMontagemCaptains.mockResolvedValue(montagem)
    serviceMocks.defineDraftMontagemPickOrder.mockResolvedValue(montagem)
    serviceMocks.drawDraftMontagemCaptains.mockResolvedValue(montagem)
    serviceMocks.finalizeDraftMontagem.mockResolvedValue(montagem)
    serviceMocks.registerDraftMontagemPick.mockResolvedValue({ montagem, canCurrentUserPick: false })
    serviceMocks.saveDraftMontagemLayout.mockResolvedValue(montagem)
    serviceMocks.startDraftMontagemRealtime.mockResolvedValue({ montagem, canCurrentUserPick: false })
    serviceMocks.substituteDraftMontagemReserve.mockResolvedValue({ montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
  })

  it('loads only the administrative endpoint when the user can manage drafts', async () => {
    const wrapper = await mountView()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledWith('montagem-1', expect.any(AbortSignal))
    expect(serviceMocks.getDraftMontagemById).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('opens and recovers only after start, Join and a successful canonical personalized GET', async () => {
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async () => {
      realtimeMock.order.push('canonical GET')
      return { montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao }
    })

    const wrapper = await mountView()

    expect(realtimeMock.order).toEqual(['start', 'JoinDraftMontagem', 'canonical GET'])
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('connected')

    realtimeMock.order = []
    realtimeMock.order.push('start', 'JoinDraftMontagem')
    await realtimeMock.reconnectHandlers.get(montagem.id)?.()
    expect(realtimeMock.order).toEqual(['start', 'JoinDraftMontagem', 'canonical GET'])
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('connected')
    wrapper.unmount()
  })

  it('waits for deferred start, Join and canonical GET but not administrative enrichment before connected', async () => {
    const start = deferred<void>()
    const join = deferred<void>()
    const canonical = deferred<DraftMontagemRealtimeState>()
    const admin = deferred<DraftMontagemAdmin>()
    realtimeMock.startGate = start.promise
    realtimeMock.joinGate = join.promise
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => {
      realtimeMock.order.push('canonical GET')
      return canonical.promise
    })
    serviceMocks.getDraftMontagemAdminById.mockReturnValueOnce(admin.promise)

    const mounting = mountView()
    await vi.waitFor(() => expect(realtimeMock.order).toEqual(['start']))
    start.resolve()
    await vi.waitFor(() => expect(realtimeMock.order).toEqual(['start', 'JoinDraftMontagem']))
    expect(serviceMocks.getDraftMontagemRealtimeState).not.toHaveBeenCalled()
    join.resolve()
    await vi.waitFor(() => expect(realtimeMock.order).toEqual(['start', 'JoinDraftMontagem', 'canonical GET']))
    const wrapper = await mounting
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).not.toBe('connected')

    canonical.resolve({ montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    await flushPromises()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalled()
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('connected')
    admin.resolve(adminProjection())
    wrapper.unmount()
  })

  it('keeps failed canonical GET degraded and runs fixed fallback with timeout and no overlap', async () => {
    vi.useFakeTimers()
    let rejectInitial!: (error: Error) => void
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => new Promise((_, reject) => { rejectInitial = reject }))
    const mounting = mountView()
    await vi.waitFor(() => expect(rejectInitial).toBeTypeOf('function'))
    rejectInitial(new Error('GET failed'))
    const wrapper = await mounting
    await flushPromises()

    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('fallback')
    expect(serviceMocks.listEligibleManualPresencePlayers).not.toHaveBeenCalled()
    expect(playerMocks.listEligibleCaptains).not.toHaveBeenCalled()
    serviceMocks.getDraftMontagemRealtimeState.mockClear()
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation((_id, signal: AbortSignal) => new Promise((_, reject) => {
      signal.addEventListener('abort', () => reject(new Error('aborted')), { once: true })
    }))

    await vi.advanceTimersByTimeAsync(3000)
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(1)
    const signal = serviceMocks.getDraftMontagemRealtimeState.mock.calls[0]?.[1] as AbortSignal
    expect(signal.aborted).toBe(false)
    await vi.advanceTimersByTimeAsync(2000)
    expect(signal.aborted).toBe(true)
    await vi.advanceTimersByTimeAsync(1000)
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)

    wrapper.unmount()
    vi.useRealTimers()
  })

  it('does not start auxiliary enrichment from a broadcast when canonical health is still degraded', async () => {
    const initial = deferred<DraftMontagemRealtimeState>()
    const broadcastRefresh = deferred<DraftMontagemRealtimeState>()
    serviceMocks.getDraftMontagemRealtimeState
      .mockReturnValueOnce(initial.promise)
      .mockReturnValueOnce(broadcastRefresh.promise)
    const mounting = mountView()
    await vi.waitFor(() => expect(realtimeMock.handlers.has(montagem.id)).toBe(true))

    const broadcast = realtimeMock.handlers.get(montagem.id)?.({
      montagem: { ...montagem, versaoEstado: montagem.versaoEstado + 1 },
      serverNow: montagem.dataAtualizacao,
    })
    initial.reject(new Error('initial canonical failed'))
    broadcastRefresh.reject(new Error('broadcast canonical failed'))
    const wrapper = await mounting
    await broadcast
    await flushPromises()

    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('fallback')
    expect(serviceMocks.listEligibleManualPresencePlayers).not.toHaveBeenCalled()
    expect(playerMocks.listEligibleCaptains).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('does not let pending administrative enrichment hold fallback canonical in-flight', async () => {
    vi.useFakeTimers()
    const wrapper = await mountView()
    const admin = deferred<DraftMontagemAdmin>()
    serviceMocks.getDraftMontagemAdminById.mockReturnValue(admin.promise)
    serviceMocks.getDraftMontagemRealtimeState.mockClear()
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    realtimeMock.degradedHandlers.get(montagem.id)?.('fallback')

    await vi.advanceTimersByTimeAsync(3000)
    await flushPromises()
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(3000)

    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    admin.resolve(adminProjection())
    wrapper.unmount()
    vi.useRealTimers()
  })

  it('does not poll canonical state while realtime health is connected', async () => {
    vi.useFakeTimers()
    const wrapper = await mountView()
    serviceMocks.getDraftMontagemRealtimeState.mockClear()

    await vi.advanceTimersByTimeAsync(9000)

    expect(serviceMocks.getDraftMontagemRealtimeState).not.toHaveBeenCalled()
    wrapper.unmount()
    vi.useRealTimers()
  })

  it('keeps reconnecting visible until the first fallback cadence tick', async () => {
    vi.useFakeTimers()
    const wrapper = await mountView()

    realtimeMock.degradedHandlers.get(montagem.id)?.('reconnecting')
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('reconnecting')

    await vi.advanceTimersByTimeAsync(3000)
    expect((wrapper.vm as unknown as { connectionStatus: string }).connectionStatus).toBe('fallback')
    wrapper.unmount()
    vi.useRealTimers()
  })

  it('does not let a stale fallback completion clear the active generation overlap guard', async () => {
    vi.useFakeTimers()
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao }))
    const wrapper = await mountView()
    let resolveOld!: (state: DraftMontagemRealtimeState) => void
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => new Promise((resolve) => { resolveOld = resolve }))
    realtimeMock.degradedHandlers.get(montagem.id)?.('fallback')
    await vi.advanceTimersByTimeAsync(3000)
    await vi.waitFor(() => expect(resolveOld).toBeTypeOf('function'))

    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem: montagemB, canCurrentUserPick: false, serverNow: montagemB.dataAtualizacao })
    await wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await flushPromises()
    let resolveCurrent!: (state: DraftMontagemRealtimeState) => void
    serviceMocks.getDraftMontagemRealtimeState.mockClear()
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => new Promise((resolve) => { resolveCurrent = resolve }))
    realtimeMock.degradedHandlers.get(montagemB.id)?.('fallback')
    await vi.advanceTimersByTimeAsync(3000)
    await vi.waitFor(() => expect(resolveCurrent).toBeTypeOf('function'))

    resolveOld({ montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    await flushPromises()
    expect((wrapper.vm as unknown as { fallbackRequestInFlight: boolean }).fallbackRequestInFlight).toBe(true)
    await vi.advanceTimersByTimeAsync(3000)

    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    wrapper.unmount()
    vi.useRealTimers()
  })

  it('accepts shared state only when version is strictly greater while allowing newer same-version personalized metadata', async () => {
    authMock.canManageDrafts = false
    const localNow = Date.parse('2026-07-30T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem, canCurrentUserPick: false, serverNow: '2026-07-30T12:00:00Z' })
    const wrapper = await mountView()
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({
      montagem: { ...montagem, status: 'Finalizada' },
      canCurrentUserPick: true,
      serverNow: '2026-07-30T12:05:00Z',
    })

    await realtimeMock.handlers.get(montagem.id)?.({ montagem: { ...montagem, status: 'Cancelada' }, serverNow: '2026-07-30T12:01:00Z' })
    await flushPromises()

    const vm = wrapper.vm as unknown as { selectedMontagem: DraftMontagem; canCurrentUserPick: boolean; serverClockOffsetMs: number }
    expect(vm.selectedMontagem.status).toBe(montagem.status)
    expect(vm.canCurrentUserPick).toBe(true)
    expect(vm.serverClockOffsetMs).toBe(5 * 60 * 1000)
    dateNow.mockRestore()
    wrapper.unmount()
  })

  it('keeps equal-version admin out of shared acceptance and applies only explicit identity metadata', async () => {
    const wrapper = await mountView()
    const vm = wrapper.vm as unknown as {
      activeDraftGeneration: number
      selectedMontagem: DraftMontagemAdmin
      beginDraftUpdate: (id: string, generation: number, lane: 'passive', personalized: boolean) => unknown
      applySharedProjection: (context: unknown, draft: DraftMontagem, administrative?: boolean) => boolean
      applyAdministrativeMetadata?: (context: unknown, detail: DraftMontagemAdmin) => boolean
    }
    const conflictingAdmin = {
      ...adminProjection('Finalizada', 'metadado administrativo novo'),
      nome: 'nome publico indevido',
      versaoEstado: montagem.versaoEstado,
    }
    const context = vm.beginDraftUpdate(montagem.id, vm.activeDraftGeneration, 'passive', true)

    vm.applySharedProjection(context, conflictingAdmin, true)

    expect(vm.selectedMontagem.status).toBe(montagem.status)
    expect(vm.selectedMontagem.nome).toBe(montagem.nome)
    expect(vm.selectedMontagem.acoesAdministrativas[0]?.motivo).toBe('auditoria inicial')
    expect(vm.applyAdministrativeMetadata).toBeTypeOf('function')
    if (vm.applyAdministrativeMetadata) vm.applyAdministrativeMetadata(context, conflictingAdmin)
    expect(vm.selectedMontagem.status).toBe(montagem.status)
    expect(vm.selectedMontagem.nome).toBe(montagem.nome)
    expect(vm.selectedMontagem.acoesAdministrativas[0]?.motivo).toBe('metadado administrativo novo')
    wrapper.unmount()
  })

  it('rejects stale admin identity metadata after a newer personalized sequence', async () => {
    const wrapper = await mountView()
    const vm = wrapper.vm as unknown as {
      activeDraftGeneration: number
      selectedMontagem: DraftMontagemAdmin
      beginDraftUpdate: (id: string, generation: number, lane: 'passive' | 'mutation', personalized: boolean) => unknown
      applyPersonalizedRealtimeState: (context: unknown, state: DraftMontagemRealtimeState) => boolean
      applyAdministrativeMetadata?: (context: unknown, detail: DraftMontagemAdmin) => boolean
    }
    const adminContext = vm.beginDraftUpdate(montagem.id, vm.activeDraftGeneration, 'passive', true)
    const newerContext = vm.beginDraftUpdate(montagem.id, vm.activeDraftGeneration, 'mutation', true)
    vm.applyPersonalizedRealtimeState(newerContext, { montagem, canCurrentUserPick: true, serverNow: montagem.dataAtualizacao })

    expect(vm.applyAdministrativeMetadata).toBeTypeOf('function')
    if (vm.applyAdministrativeMetadata) {
      vm.applyAdministrativeMetadata(adminContext, adminProjection(montagem.status, 'admin antigo'))
    }
    expect(vm.selectedMontagem.acoesAdministrativas[0]?.motivo).toBe('auditoria inicial')
    wrapper.unmount()
  })

  it('aborts canonical, admin and conflict requests when opening another generation', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection())
    const wrapper = await mountView()
    const canonical = deferred<DraftMontagemRealtimeState>()
    const admin = deferred<DraftMontagemAdmin>()
    const conflict = deferred<DraftMontagemRealtimeState>()
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => canonical.promise)
    serviceMocks.getDraftMontagemAdminById.mockImplementationOnce(() => admin.promise)
    const vm = wrapper.vm as unknown as {
      activeDraftGeneration: number
      refreshMontagemDetail: (id: string, generation: number) => Promise<boolean>
      selectedMontagem: DraftMontagem
    }
    void vm.refreshMontagemDetail(montagem.id, vm.activeDraftGeneration)
    void realtimeMock.handlers.get(montagem.id)?.({ montagem: { ...montagem, versaoEstado: 8 }, serverNow: montagem.dataAtualizacao })
    serviceMocks.saveDraftMontagemLayout.mockRejectedValueOnce(new ServiceError([], 409))
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => conflict.promise)
    vm.selectedMontagem = { ...montagem, status: 'Aberta', versaoEstado: 8 }
    await nextTick()
    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('save', { times: [], livres: [], reservas: [], versaoEstado: 8 })
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(3))
    const canonicalSignal = serviceMocks.getDraftMontagemRealtimeState.mock.calls[1]?.[1] as AbortSignal | undefined
    const conflictSignal = serviceMocks.getDraftMontagemRealtimeState.mock.calls[2]?.[1] as AbortSignal | undefined
    const adminSignal = serviceMocks.getDraftMontagemAdminById.mock.calls[1]?.[1] as AbortSignal | undefined

    await wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await flushPromises()

    expect(canonicalSignal?.aborted).toBe(true)
    expect(adminSignal?.aborted).toBe(true)
    expect(conflictSignal?.aborted).toBe(true)
    wrapper.unmount()
  })

  it('orders personalized metadata globally across passive and mutation lanes', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const realtimeDraft = { ...montagem, status: 'Aberta' as const, modo: 'TempoReal' as const }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    const wrapper = await mountView()
    let resolvePassive!: (state: DraftMontagemRealtimeState) => void
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => new Promise((resolve) => { resolvePassive = resolve }))

    const passive = realtimeMock.handlers.get(montagem.id)?.({ montagem: { ...realtimeDraft, versaoEstado: 8 }, serverNow: montagem.dataAtualizacao })
    await vi.waitFor(() => expect(resolvePassive).toBeTypeOf('function'))
    const vm = wrapper.vm as unknown as {
      beginSelectedDraftUpdate: (personalized: boolean) => unknown
      applyMutationRealtimeState: (context: unknown, state: DraftMontagemRealtimeState) => Promise<boolean>
    }
    const mutationContext = vm.beginSelectedDraftUpdate(true)
    await vm.applyMutationRealtimeState(mutationContext, { montagem: { ...realtimeDraft, versaoEstado: 9 }, canCurrentUserPick: true, serverNow: '2026-07-30T12:09:00Z' })
    await flushPromises()
    resolvePassive({ montagem: { ...realtimeDraft, versaoEstado: 8 }, canCurrentUserPick: false, serverNow: '2026-07-30T12:08:00Z' })
    await passive
    await flushPromises()

    expect((wrapper.vm as unknown as { canCurrentUserPick: boolean }).canCurrentUserPick).toBe(true)
    expect((wrapper.vm as unknown as { lastPersonalizedSequence: number; personalizedSequence: number }).lastPersonalizedSequence)
      .toBe((wrapper.vm as unknown as { personalizedSequence: number }).personalizedSequence)
    wrapper.unmount()
  })

  it('routes canonical administrative detail through the passive lane', async () => {
    const wrapper = await mountView()
    const passiveBefore = (wrapper.vm as unknown as { passiveRequestId: number }).passiveRequestId
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce({ ...montagem, status: 'Cancelada', versaoEstado: 8 })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValueOnce({ ...adminProjection('Cancelada'), versaoEstado: 8 })

    await confirmReasonAction(wrapper, 'Cancelar', 'mudança administrativa')

    const vm = wrapper.vm as unknown as { passiveRequestId: number; mutationRequestId: number }
    expect(vm.passiveRequestId).toBeGreaterThan(passiveBefore)
    expect(vm.mutationRequestId).toBeGreaterThan(0)
    wrapper.unmount()
  })

  it('starts optional presence and captain enrichment only after canonical health without blocking connected state or actions', async () => {
    const canonical = deferred<DraftMontagemRealtimeState>()
    const presence = deferred<Array<{ id: string; nomeExibicao: string }>>()
    const captains = deferred<Array<{ id: string; nomeExibicao: string }>>()
    serviceMocks.getDraftMontagemRealtimeState.mockReturnValueOnce(canonical.promise)
    serviceMocks.listEligibleManualPresencePlayers.mockReturnValueOnce(presence.promise)
    playerMocks.listEligibleCaptains.mockReturnValueOnce(captains.promise)

    const mounting = mountView()
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(1))
    expect(serviceMocks.listEligibleManualPresencePlayers).not.toHaveBeenCalled()
    expect(playerMocks.listEligibleCaptains).not.toHaveBeenCalled()

    canonical.resolve({ montagem, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    const wrapper = await mounting
    await vi.waitFor(() => expect(playerMocks.listEligibleCaptains).toHaveBeenCalledTimes(1))

    expect((wrapper.vm as unknown as { connectionStatus: string; saving: boolean }).connectionStatus).toBe('connected')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect(findButton(wrapper, 'Cancelar').attributes('disabled')).toBeUndefined()
    presence.resolve([{ id: 'jogador-2', nomeExibicao: 'Lux' }])
    captains.resolve([{ id: 'jogador-1', nomeExibicao: 'Ahri' }])
    wrapper.unmount()
  })

  it('orders auxiliary requests without advancing canonical lane IDs or the personalized sequence', async () => {
    const wrapper = await mountView()
    const before = wrapper.vm as unknown as {
      auxiliaryRequestId: number
      passiveRequestId: number
      mutationRequestId: number
      personalizedSequence: number
      loadEligibleManualPresencePlayers: () => Promise<void>
      loadEligibleCaptains: () => Promise<void>
    }
    const canonicalIds = {
      passive: before.passiveRequestId,
      mutation: before.mutationRequestId,
      personalized: before.personalizedSequence,
    }
    const auxiliaryBefore = before.auxiliaryRequestId

    await before.loadEligibleManualPresencePlayers()
    await before.loadEligibleCaptains()

    expect(before.auxiliaryRequestId).toBeGreaterThan(auxiliaryBefore)
    expect(before.passiveRequestId).toBe(canonicalIds.passive)
    expect(before.mutationRequestId).toBe(canonicalIds.mutation)
    expect(before.personalizedSequence).toBe(canonicalIds.personalized)
    wrapper.unmount()
  })

  it('aborts captain enrichment on generation change and leaves its stale completion inert', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem }))
    const staleCaptains = deferred<Array<{ id: string; nomeExibicao: string }>>()
    playerMocks.listEligibleCaptains
      .mockReturnValueOnce(staleCaptains.promise)
      .mockResolvedValueOnce([{ id: 'capitao-b', nomeExibicao: 'Capitão B' }])
    const wrapper = await mountView()
    await vi.waitFor(() => expect(playerMocks.listEligibleCaptains).toHaveBeenCalledTimes(1))
    const staleSignal = playerMocks.listEligibleCaptains.mock.calls[0]?.[0] as AbortSignal | undefined

    await findButton(wrapper, 'Rinha de segunda').trigger('click')
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id))
    staleCaptains.resolve([{ id: 'capitao-antigo', nomeExibicao: 'Capitão antigo' }])
    await flushPromises()

    expect(staleSignal?.aborted).toBe(true)
    expect((wrapper.vm as unknown as { eligibleCaptainIds: string[] }).eligibleCaptainIds).toEqual(['capitao-b'])
    wrapper.unmount()
  })

  it.each([
    ['pt', 'Não foi possível carregar os jogadores elegíveis.', 'Tentar novamente'],
    ['en', 'Eligible players could not be loaded.', 'Try again'],
  ] as const)('contains auxiliary presence failure in its dependent control with exact localized retry in %s', async (locale, failure, retry) => {
    setLocale(locale)
    serviceMocks.listEligibleManualPresencePlayers.mockRejectedValueOnce(new Error('presence unavailable'))
    const wrapper = await mountView()

    const feedback = wrapper.get('[data-auxiliary-presence-error]')
    expect(feedback.text()).toContain(failure)
    expect(feedback.get('button').text()).toBe(retry)
    expect(wrapper.get('select[name="manual-presence-player"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-draft-workspace]').exists()).toBe(true)
    expect((wrapper.vm as unknown as { connectionStatus: string; errors: string[] }).connectionStatus).toBe('connected')
    expect((wrapper.vm as unknown as { errors: string[] }).errors).toEqual([])

    serviceMocks.listEligibleManualPresencePlayers.mockResolvedValueOnce([{ id: 'retry-player', nomeExibicao: 'Retry Player' }])
    await feedback.get('button').trigger('click')
    await flushPromises()
    expect(serviceMocks.listEligibleManualPresencePlayers).toHaveBeenLastCalledWith(montagem.id, '', 1, 20, expect.any(AbortSignal))
    expect(wrapper.find('[data-auxiliary-presence-error]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Retry Player')
    wrapper.unmount()
  })

  it.each([
    ['pt', 'Não foi possível carregar os capitães elegíveis.', 'Tentar novamente'],
    ['en', 'Eligible captains could not be loaded.', 'Try again'],
  ] as const)('contains auxiliary captain failure in captain controls with exact localized retry in %s', async (locale, failure, retry) => {
    setLocale(locale)
    const closed = {
      ...adminProjection('PresencaEncerrada'),
      modo: 'TempoReal',
      cicloVersao: 'ModoPosPresenca',
    } as DraftMontagemAdmin
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: sharedFromAdmin(closed), canCurrentUserPick: false })
    playerMocks.listEligibleCaptains.mockRejectedValueOnce(new Error('captains unavailable'))
    const wrapper = await mountView()

    const feedback = wrapper.get('[data-auxiliary-captain-error]')
    expect(feedback.text()).toContain(failure)
    expect(feedback.get('button').text()).toBe(retry)
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canDefineCaptains')).toBe(false)
    expect((wrapper.vm as unknown as { connectionStatus: string; errors: string[] }).connectionStatus).toBe('connected')
    expect((wrapper.vm as unknown as { errors: string[] }).errors).toEqual([])

    playerMocks.listEligibleCaptains.mockResolvedValueOnce([{ id: 'jogador-1', nomeExibicao: 'Ahri' }])
    await feedback.get('button').trigger('click')
    await flushPromises()
    expect(playerMocks.listEligibleCaptains).toHaveBeenLastCalledWith(expect.any(AbortSignal))
    expect(wrapper.find('[data-auxiliary-captain-error]').exists()).toBe(false)
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('eligibleCaptainIds')).toEqual(['jogador-1'])
    wrapper.unmount()
  })

  it('reconciles a layout 409 with canonical GET before unlocking mutation', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    serviceMocks.saveDraftMontagemLayout.mockRejectedValueOnce(new ServiceError([], 409))
    let resolveReconciliation!: (state: DraftMontagemRealtimeState) => void
    const wrapper = await mountView()
    serviceMocks.getDraftMontagemRealtimeState.mockImplementationOnce(() => new Promise((resolve) => { resolveReconciliation = resolve }))
    const payload = { times: [], livres: [], reservas: [], versaoEstado: montagem.versaoEstado }

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('save', payload)
    await vi.waitFor(() => expect(resolveReconciliation).toBeTypeOf('function'))
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(true)
    resolveReconciliation({ montagem: { ...montagem, versaoEstado: 8 }, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    await flushPromises()

    expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledWith(montagem.id, payload)
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    wrapper.unmount()
  })

  it('accepts conflict canonical state while a broadcast passive GET is pending', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    serviceMocks.saveDraftMontagemLayout.mockRejectedValueOnce(new ServiceError([], 409))
    const wrapper = await mountView()
    const conflict = deferred<DraftMontagemRealtimeState>()
    const passive = deferred<DraftMontagemRealtimeState>()
    serviceMocks.getDraftMontagemRealtimeState
      .mockReturnValueOnce(conflict.promise)
      .mockReturnValueOnce(passive.promise)
    const payload = { times: [], livres: [], reservas: [], versaoEstado: montagem.versaoEstado }

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('save', payload)
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2))
    const broadcast = realtimeMock.handlers.get(montagem.id)?.({
      montagem: { ...montagem, nome: 'broadcast', status: 'Aberta', versaoEstado: 8 },
      serverNow: montagem.dataAtualizacao,
    })
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(3))

    conflict.resolve({
      montagem: { ...montagem, nome: 'conflict canonical', status: 'Aberta', versaoEstado: 9 },
      canCurrentUserPick: false,
      serverNow: montagem.dataAtualizacao,
    })
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.nome).toBe('conflict canonical')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledTimes(1)
    passive.resolve({
      montagem: { ...montagem, nome: 'passive stale', status: 'Aberta', versaoEstado: 8 },
      canCurrentUserPick: true,
      serverNow: montagem.dataAtualizacao,
    })
    await broadcast
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.nome).toBe('conflict canonical')
    wrapper.unmount()
  })

  it('keeps conflict barrier locked until its stale response follows a newer passive canonical acceptance', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    serviceMocks.saveDraftMontagemLayout.mockRejectedValueOnce(new ServiceError([], 409))
    const wrapper = await mountView()
    const conflict = deferred<DraftMontagemRealtimeState>()
    const passive = deferred<DraftMontagemRealtimeState>()
    serviceMocks.getDraftMontagemRealtimeState
      .mockReturnValueOnce(conflict.promise)
      .mockReturnValueOnce(passive.promise)

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('save', { times: [], livres: [], reservas: [], versaoEstado: montagem.versaoEstado })
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2))
    const broadcast = realtimeMock.handlers.get(montagem.id)?.({
      montagem: { ...montagem, nome: 'broadcast', status: 'Aberta', versaoEstado: 8 },
      serverNow: montagem.dataAtualizacao,
    })
    await vi.waitFor(() => expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(3))
    passive.resolve({
      montagem: { ...montagem, nome: 'passive canonical', status: 'Aberta', versaoEstado: 10 },
      canCurrentUserPick: true,
      serverNow: montagem.dataAtualizacao,
    })
    await broadcast

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.nome).toBe('passive canonical')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(true)
    conflict.resolve({
      montagem: { ...montagem, nome: 'conflict stale', status: 'Aberta', versaoEstado: 9 },
      canCurrentUserPick: false,
      serverNow: montagem.dataAtualizacao,
    })
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.nome).toBe('passive canonical')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('loads only the public endpoint for a regular player', async () => {
    authMock.canManageDrafts = false
    const wrapper = await mountView()

    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledWith('montagem-1', expect.any(AbortSignal))
    expect(serviceMocks.getDraftMontagemById).not.toHaveBeenCalled()
    expect(serviceMocks.getDraftMontagemAdminById).not.toHaveBeenCalled()
    expect(wrapper.text()).not.toContain('Republicar presença')
    wrapper.unmount()
  })

  it('falls back to the public endpoint and hides admin actions after an administrative 403', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new ServiceError([], 403))
    const wrapper = await mountView()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledWith('montagem-1', expect.any(AbortSignal))
    expect(serviceMocks.getDraftMontagemById).not.toHaveBeenCalled()
    expect(wrapper.text()).not.toContain('Republicar presença')

    await emitRealtime('montagem-1', { ...montagem, status: 'Aberta' })
    await emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await flushPromises()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('reloads and preserves the administrative projection after a public realtime event', async () => {
    const wrapper = await mountView()
    const refreshedAdmin = { ...adminProjection('Aberta', 'auditoria atualizada pelo realtime'), versaoEstado: 8 }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValueOnce(refreshedAdmin)

    await emitRealtime('montagem-1', { ...montagem, status: 'Aberta', publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }] })
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.acoesAdministrativas).toEqual(refreshedAdmin.acoesAdministrativas)
    expect(selected.discordGuildId).toBe('guild-admin')
    expect(selected.status).toBe('Aberta')
    wrapper.unmount()
  })

  it('reloads updated administrative audit after a mutation returns a public projection', async () => {
    const wrapper = await mountView()
    const updatedAdmin = adminProjection('PresencaAberta', 'republicacao registrada')
    serviceMocks.republishDraftMontagemDiscordPublication.mockResolvedValueOnce({
      ...montagem,
      publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }],
    })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValueOnce(updatedAdmin)

    await confirmReasonAction(wrapper, 'Republicar presença', 'republicacao registrada')

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.acoesAdministrativas).toEqual(updatedAdmin.acoesAdministrativas)
    expect(selected.publicacoesDiscord.find((publication) => publication.tipo === 'Presenca')?.id).toBe('publicacao-admin')
    wrapper.unmount()
  })

  it('ignores stale administrative refresh responses from older realtime events', async () => {
    const wrapper = await mountView()
    let resolveFirst!: (value: DraftMontagemAdmin) => void
    let resolveSecond!: (value: DraftMontagemAdmin) => void
    serviceMocks.getDraftMontagemAdminById
      .mockImplementationOnce(() => new Promise((resolve) => { resolveFirst = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveSecond = resolve }))

    const firstRefresh = emitRealtime('montagem-1', { ...montagem, status: 'Aberta' })
    await vi.waitFor(() => expect(resolveFirst).toBeTypeOf('function'))
    const secondRefresh = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await vi.waitFor(() => expect(resolveSecond).toBeTypeOf('function'))
    resolveSecond({ ...adminProjection('Finalizada', 'evento mais novo'), versaoEstado: 9 })
    await secondRefresh
    resolveFirst({ ...adminProjection('Aberta', 'evento antigo'), versaoEstado: 8 })
    await firstRefresh
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('evento mais novo')
    wrapper.unmount()
  })

  it('never reapplies draft A after opening draft B starts', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem }))
    const wrapper = await mountView()
    let resolveOldA!: (value: DraftMontagemAdmin) => void
    let resolveB!: (value: DraftMontagemAdmin) => void
    serviceMocks.getDraftMontagemAdminById
      .mockImplementationOnce(() => new Promise((resolve) => { resolveOldA = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveB = resolve }))
      .mockResolvedValue({ ...adminProjectionB('B realtime'), versaoEstado: 20 })

    const oldARefresh = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await vi.waitFor(() => expect(resolveOldA).toBeTypeOf('function'))
    const openB = wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await vi.waitFor(() => expect(realtimeMock.disconnected).toContain('montagem-1'))
    const lateAEvent = emitRealtime('montagem-1', { ...montagem, status: 'Cancelada' }, false, null)
    resolveOldA(adminProjection('Finalizada', 'resposta antiga A'))
    await oldARefresh
    await lateAEvent

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).not.toBe('montagem-1')

    await vi.waitFor(() => expect(resolveB).toBeTypeOf('function'))
    resolveB({ ...adminProjectionB('B assumiu'), versaoEstado: 7 })
    await openB
    await flushPromises()
    await vi.waitFor(() => expect(
      (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem.acoesAdministrativas[0]?.motivo,
    ).toBe('B assumiu'))

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.id).toBe('montagem-2')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('B assumiu')
    expect(selected.acoesAdministrativas.some((action) => action.motivo?.includes('A'))).toBe(false)
    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(3)
    wrapper.unmount()
  })

  it.each([
    ['network', new Error('network unavailable')],
    ['500', { status: 500 }],
  ])('keeps successful public mutation state when admin refresh fails with %s', async (_, refreshError) => {
    const wrapper = await mountView()
    const publicMutation = {
      ...montagem,
      status: 'Cancelada' as const,
      versaoEstado: 8,
      publicacoesDiscord: [{ tipo: 'Presenca' as const, status: 'Pendente' as const }],
    }
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce(publicMutation)
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(refreshError)

    await confirmReasonAction(wrapper, 'Cancelar', 'mutacao concluida')

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.status).toBe('Cancelada')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('auditoria inicial')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.findAll('[role="status"]')).toHaveLength(1)
    expect(wrapper.get('[role="status"]').text()).toContain('cancelado')
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('uses the successful public mutation as a permanent fallback after admin refresh returns 403', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    const wrapper = await mountView()
    const publicMutation = { ...montagem, status: 'Cancelada' as const, versaoEstado: 8 }
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce(publicMutation)
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new ServiceError([], 403))

    await confirmReasonAction(wrapper, 'Cancelar', 'mutacao concluida')
    await emitRealtime('montagem-1', { ...publicMutation, status: 'Finalizada', versaoEstado: 9 })
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect('acoesAdministrativas' in selected).toBe(false)
    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(2)
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)
    expect(wrapper.findAll('[role="status"]')).toHaveLength(1)
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('finishes mutation UX once while a newer realtime refresh wins over its pending admin refresh', async () => {
    const wrapper = await mountView()
    let resolveMutationRefresh!: (value: DraftMontagemAdmin) => void
    let resolveRealtimeRefresh!: (value: DraftMontagemAdmin) => void
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce({ ...montagem, status: 'Cancelada', versaoEstado: 8 })
    serviceMocks.getDraftMontagemAdminById
      .mockImplementationOnce(() => new Promise((resolve) => { resolveMutationRefresh = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveRealtimeRefresh = resolve }))

    const mutation = confirmReasonAction(wrapper, 'Cancelar', 'mutacao antiga')
    await vi.waitFor(() => expect(resolveMutationRefresh).toBeTypeOf('function'))

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.findAll('[role="status"]')).toHaveLength(1)
    expect(wrapper.get('[role="status"]').text()).toContain('cancelado')
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)

    const realtime = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada', versaoEstado: 9 })
    await vi.waitFor(() => expect(resolveRealtimeRefresh).toBeTypeOf('function'))
    resolveRealtimeRefresh({ ...adminProjection('Finalizada', 'realtime novo'), versaoEstado: 9 })
    await realtime
    resolveMutationRefresh({ ...adminProjection('Cancelada', 'refresh antigo da mutacao'), versaoEstado: 8 })
    await mutation

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('realtime novo')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.findAll('[role="status"]')).toHaveLength(1)
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('does not apply or surface a late mutation response after another draft becomes active', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem }))
    let resolveMutation!: (value: DraftMontagem) => void
    serviceMocks.cancelDraftMontagem.mockImplementationOnce(() => new Promise((resolve) => { resolveMutation = resolve }))
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Cancelar')
    await wrapper.get('textarea').setValue('resposta tardia A')
    await wrapper.get('form').trigger('submit')
    await vi.waitFor(() => expect(resolveMutation).toBeTypeOf('function'))
    await wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe('montagem-2'))

    resolveMutation({ ...montagem, status: 'Cancelada' })
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.id).toBe('montagem-2')
    expect(selected.status).toBe('Aberta')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.find('[role="status"]').exists()).toBe(false)
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  afterEach(() => {
    document.body.innerHTML = ''
    setLocale('pt')
  })

  it('republishes presence with its current status and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar presença')
    expect(wrapper.text()).toContain('Status atual: falhou')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect((wrapper.vm as unknown as { pendingReasonAction: { type: string } }).pendingReasonAction).toMatchObject({
      type: 'republishDiscord',
      publicationType: 'Presenca',
      publicationStatus: 'Falha',
    })
    await wrapper.get('textarea').setValue('canal corrigido')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect((wrapper.vm as unknown as { pendingReasonAction: unknown }).pendingReasonAction).toBeNull()
    expect(serviceMocks.republishDraftMontagemDiscordPublication).toHaveBeenCalledWith('montagem-1', 'Presenca', 'canal corrigido')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('republishes teams with its current status and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar times')
    expect(wrapper.text()).toContain('Status atual: publicada')
    await wrapper.get('textarea').setValue('mensagem dos times removida')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.republishDraftMontagemDiscordPublication).toHaveBeenCalledWith('montagem-1', 'TimesDefinidos', 'mensagem dos times removida')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('republishes only the recoverable presence CTA with its current status and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar chamada')
    expect(wrapper.text()).toContain('Status atual: falhou')
    await wrapper.get('textarea').setValue('menção corrigida')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.republishDraftMontagemDiscordPublication).toHaveBeenCalledWith('montagem-1', 'ChamadaPresenca', 'menção corrigida')
    wrapper.unmount()
  })

  it('hides the CTA republish action while its publication is not recoverable', async () => {
    const projection = adminProjection()
    projection.publicacoesDiscord = projection.publicacoesDiscord.map((publication) =>
      publication.tipo === 'ChamadaPresenca' ? { ...publication, status: 'Publicada' } : publication,
    )
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(projection)

    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Chamada no Discord: publicada')
    expect(wrapper.text()).not.toContain('Republicar chamada')
    wrapper.unmount()
  })

  it.each([
    {
      locale: 'pt',
      publications: [],
      actionTestId: 'republish-presence',
      expectedContext: 'Lista de presença',
      expectedStatus: 'Status atual: Estado de publicação desconhecido',
      expectedPublicationType: 'Presenca',
    },
    {
      locale: 'en',
      publications: [{ tipo: 'ChamadaPresenca', status: 'Falha' }],
      actionTestId: 'republish-final-teams',
      expectedContext: 'Defined teams',
      expectedStatus: 'Current status: Unknown publication status',
      expectedPublicationType: 'TimesDefinidos',
    },
  ] as const)('keeps missing status null from a $locale $publications projection through the reason dialog', async ({ locale, publications, actionTestId, expectedContext, expectedStatus, expectedPublicationType }) => {
    setLocale(locale)
    const projection = adminProjection()
    projection.publicacoesDiscord = publications as unknown as DraftMontagemAdmin['publicacoesDiscord']
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(projection)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftDiscordPublicationPanel' })

    expect(panel.props('publications')).toEqual([
      { tipo: 'Presenca', status: null },
      { tipo: 'ChamadaPresenca', status: publications[0]?.tipo === 'ChamadaPresenca' ? 'Falha' : null },
      { tipo: 'TimesDefinidos', status: null },
    ])
    await panel.get(`[data-testid="${actionTestId}"]`).trigger('click')
    await flushPromises()

    expect((wrapper.vm as unknown as { pendingReasonAction: { type: string; publicationStatus: unknown } }).pendingReasonAction).toEqual({
      type: 'republishDiscord',
      publicationType: expectedPublicationType,
      publicationStatus: null,
    })
    expect(wrapper.get('[role="dialog"]').text()).toContain(expectedContext)
    expect(wrapper.get('[role="dialog"] [data-slot="badge"]').text()).toBe(expectedStatus)
    wrapper.unmount()
  })

  it.each([
    ['pt', 'Publicação no Discord: Estado de publicação desconhecido'],
    ['en', 'Discord publication: Unknown publication status'],
  ] as const)('keeps one neutral noncanonical publication row without actions in %s', async (locale, expectedText) => {
    setLocale(locale)
    const projection = adminProjection()
    const legacyPublication = {
      id: 'publicacao-legada-1',
      tipo: 'IntegracaoLegada',
      status: 'Falha',
      ultimaTentativaEm: '2026-07-21T12:00:00Z',
    }
    projection.publicacoesDiscord = [
      projection.publicacoesDiscord.find((publication) => publication.tipo === 'Presenca')!,
      { ...projection.publicacoesDiscord.find((publication) => publication.tipo === 'Presenca')!, id: 'publicacao-presenca-duplicada', status: 'Publicada' },
      legacyPublication,
      { ...legacyPublication, id: 'publicacao-legada-duplicada', status: 'Publicada' },
    ] as unknown as DraftMontagemAdmin['publicacoesDiscord']
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(projection)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftDiscordPublicationPanel' })

    expect(panel.props('publications')).toEqual([
      { tipo: 'Presenca', status: 'Falha' },
      { tipo: 'ChamadaPresenca', status: null },
      { tipo: 'TimesDefinidos', status: null },
      legacyPublication,
    ])
    const legacyRow = panel.get('[data-publication-type="IntegracaoLegada"]')
    expect(legacyRow.get('[data-publication-status]').attributes('data-publication-status')).toBe('unknown')
    expect(legacyRow.text()).toBe(expectedText)
    expect(legacyRow.find('button').exists()).toBe(false)
    wrapper.unmount()
  })

  it('keeps results from the newest manual presence search when it resolves first', async () => {
    const wrapper = await mountView()
    serviceMocks.listEligibleManualPresencePlayers.mockClear()
    let resolveOld!: (value: Array<{ id: string; nomeExibicao: string }>) => void
    let resolveNew!: (value: Array<{ id: string; nomeExibicao: string }>) => void
    serviceMocks.listEligibleManualPresencePlayers
      .mockImplementationOnce(() => new Promise((resolve) => { resolveOld = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveNew = resolve }))
    const search = wrapper.findAll('input[type="search"]')[1]!

    await search.setValue('a')
    await search.setValue('ah')
    resolveNew([{ id: 'new', nomeExibicao: 'Ahri atual' }])
    await flushPromises()
    resolveOld([{ id: 'old', nomeExibicao: 'Ashe antiga' }])
    await flushPromises()

    expect(wrapper.text()).toContain('Ahri atual')
    expect(wrapper.text()).not.toContain('Ashe antiga')
    const oldSignal = serviceMocks.listEligibleManualPresencePlayers.mock.calls[0]?.[4] as AbortSignal | undefined
    expect(oldSignal?.aborted).toBe(true)
    wrapper.unmount()
  })

  it('aborts and ignores a manual search from the previous draft generation', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem }))
    const wrapper = await mountView()
    serviceMocks.listEligibleManualPresencePlayers.mockClear()
    let resolveOld!: (value: Array<{ id: string; nomeExibicao: string }>) => void
    serviceMocks.listEligibleManualPresencePlayers.mockImplementation((id) => id === montagem.id
      ? new Promise((resolve) => { resolveOld = resolve })
      : Promise.resolve([{ id: 'b-player', nomeExibicao: 'Jogador B' }]))
    const search = wrapper.findAll('input[type="search"]')[1]!

    await search.setValue('jogador')
    const lastSearchCall = serviceMocks.listEligibleManualPresencePlayers.mock.calls[serviceMocks.listEligibleManualPresencePlayers.mock.calls.length - 1]
    const oldSignal = lastSearchCall?.[4] as AbortSignal | undefined
    await wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe('montagem-2'))
    resolveOld([{ id: 'a-player', nomeExibicao: 'Jogador A antigo' }])
    await flushPromises()

    expect(oldSignal?.aborted).toBe(true)
    const manualPlayers = (wrapper.vm as unknown as { manualPresencePlayers: Array<{ nomeExibicao: string }> }).manualPresencePlayers
    expect(manualPlayers.map((player) => player.nomeExibicao)).toEqual(['Jogador B'])
    wrapper.unmount()
  })

  it('cancels the draft with the exact reason', async () => {
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Cancelar', 'evento cancelado pelo organizador')

    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledWith('montagem-1', 'evento cancelado pelo organizador')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it.each(['Finalizada', 'Cancelada'] as const)('clears a stale cancel action when realtime changes the draft to %s before confirmation', async (status) => {
    const wrapper = await mountView()
    await openReasonDialog(wrapper, 'Cancelar')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValueOnce(adminProjection(status))

    await emitRealtime('montagem-1', { ...montagem, status })
    await flushPromises()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
    const requestVersionBefore = (wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion

    await wrapper.get('textarea').setValue('cancelamento obsoleto')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.cancelDraftMontagem).not.toHaveBeenCalled()
    expect((wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion).toBe(requestVersionBefore)
    expect((wrapper.vm as unknown as { pendingReasonAction: unknown }).pendingReasonAction).toBeNull()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('removes the selected manual presence with player context and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Remover')
    expect(wrapper.text()).toContain('Jogador afetado: Ahri')
    await wrapper.get('textarea').setValue('jogador avisou ausência')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.removeManualDraftMontagemPresence).toHaveBeenCalledWith('montagem-1', 'jogador-1', 'jogador avisou ausência')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('adds the selected manual presence with player context and exact reason', async () => {
    const wrapper = await mountView()
    const select = wrapper.findAll('select').find((candidate) => candidate.text().includes('Lux'))!
    await select.setValue('jogador-2')

    await openReasonDialog(wrapper, 'Adicionar presença')
    expect(wrapper.text()).toContain('Jogador afetado: Lux')
    await wrapper.get('textarea').setValue('convidado pelo organizador')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.addManualDraftMontagemPresence).toHaveBeenCalledWith('montagem-1', 'jogador-2', 'convidado pelo organizador')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('closes an explicitly cancelled action without calling a service', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Cancelar')
    await wrapper.get('[data-testid="draft-reason-cancel"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(serviceMocks.cancelDraftMontagem).not.toHaveBeenCalled()
    expect(serviceMocks.removeManualDraftMontagemPresence).not.toHaveBeenCalled()
    expect(serviceMocks.republishDraftMontagemDiscordPublication).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('keeps the dialog open when the service fails', async () => {
    serviceMocks.cancelDraftMontagem.mockRejectedValueOnce(new Error('failure'))
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Cancelar', 'motivo preservado')

    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
    expect(wrapper.get('textarea').element.value).toBe('motivo preservado')
    wrapper.unmount()
  })

  it('ignores a second confirmation while the first request is pending', async () => {
    let resolveCancellation!: (value: DraftMontagem) => void
    serviceMocks.cancelDraftMontagem.mockReturnValueOnce(
      new Promise<DraftMontagem>((resolve) => {
        resolveCancellation = resolve
      }),
    )
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Cancelar')
    await wrapper.get('textarea').setValue('evento cancelado')
    const form = wrapper.get('form')
    await Promise.all([form.trigger('submit'), form.trigger('submit')])

    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)

    resolveCancellation(montagem)
    await flushPromises()
    wrapper.unmount()
  })

  it('does not use native prompts', () => {
    expect(DraftsViewSource).not.toContain('window.prompt')
  })

  it.each([
    'PresencaAberta',
    'PresencaEncerrada',
    'CapitaesDefinidos',
    'OrdemDefinida',
    'Aberta',
    'Finalizada',
    'Cancelada',
  ] satisfies DraftMontagemStatus[])('keeps one workspace hierarchy and at most one primary action for %s', async (status) => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection(status))

    const wrapper = await mountView()
    const workspace = wrapper.get('[data-testid="draft-workspace-header"]')

    expect(workspace.get('h2').text()).toBe(montagem.nome)
    expect(workspace.get('[data-workspace-status]').text()).toBe(i18n.global.t(`drafts.status.${status}`))
    expect(workspace.get('[data-action-group="primary"]').findAll('button').length).toBeLessThanOrEqual(1)
    if (status === 'Finalizada' || status === 'Cancelada') {
      expect(workspace.get('[data-action-group="primary"]').find('button').exists()).toBe(false)
      expect(workspace.get('[data-action-group="danger"]').findAll('button')).toHaveLength(1)
      expect(workspace.get('[data-action-group="danger"] button').text()).toBe('Arquivar')
    } else {
      expect(workspace.get('[data-action-group="danger"] button').attributes('data-variant')).toBe('destructive')
    }
    wrapper.unmount()
  })

  it('uses the application landmark and workspace identity only once', () => {
    expect(DraftsViewSource).not.toMatch(/<main\b/)
    expect(DraftVisualBoardSource).not.toContain('{{ localMontagem.nome }}')
  })

  it('keeps navigator before the labelled workspace in the responsive draft shell', async () => {
    const wrapper = await mountView()
    const shell = wrapper.get('[data-draft-shell]')
    const navigator = shell.get('[data-testid="draft-navigator"]')
    const workspace = shell.get('[data-draft-workspace]')

    expect(shell.attributes('aria-label')).toBe('Draft de Jogadores')
    expect(navigator.element.compareDocumentPosition(workspace.element) & 4).toBe(4)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-layout\s*{[^}]*grid-template-columns:\s*260px minmax\(0, 1fr\)/s)
    expect(MainCss).not.toMatch(/\.drafts-page\s*{[^}]*overflow-x:\s*clip/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-main\s*>\s*\*[\s\S]*?min-width:\s*0/s)
    wrapper.unmount()
  })

  it('declares the dark browser chrome color', () => {
    expect(IndexHtml).toContain('<meta name="theme-color" content="#09090B" />')
  })

  it('keeps cancellation only in the parent header while preserving the board emit contract', () => {
    expect(DraftVisualBoardSource).toContain('cancel: []')
    expect(DraftVisualBoardSource).not.toContain(`@click="emit('cancel')"`)
  })

  it('integrates presentation-only preparation and Discord panels', () => {
    expect(DraftsViewSource).toContain('<DraftPreparationPanel')
    expect(DraftsViewSource).toContain('<DraftDiscordPublicationPanel')
    expect(DraftPreparationPanelSource).not.toMatch(/@\/services\//)
    expect(DraftDiscordPublicationPanelSource).not.toMatch(/@\/services\//)
    expect(DraftPreparationPanelSource).not.toMatch(/\bcanManage:\s*boolean/)
    expect(DraftDiscordPublicationPanelSource).not.toContain('canManage')
  })

  it('passes the selected summary dataRinha and parent-computed action capabilities', async () => {
    const wrapper = await mountView()
    const header = wrapper.getComponent({ name: 'DraftWorkspaceHeader' })
    const preparation = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    const publications = wrapper.getComponent({ name: 'DraftDiscordPublicationPanel' })

    expect(header.props('dataRinha')).toBe('2026-07-27T03:00:00Z')
    expect(preparation.props()).toMatchObject({
      canConfirmPresence: true,
      canCancelPresence: false,
      canClosePresence: true,
      canContinueManualPresence: true,
      canManageManualPresence: true,
      canSelectCaptains: false,
      canDefineCaptains: false,
      canDrawOrder: false,
    })
    expect(publications.props('republishableTypes')).toEqual(['Presenca', 'ChamadaPresenca', 'TimesDefinidos'])
    wrapper.unmount()
  })

  it('integrates the presentation-only navigator with filtered data and permissions', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })

    expect(navigator.props()).toMatchObject({
      drafts: [resumo, resumoB],
      selectedDraftId: montagem.id,
      searchTerm: '',
      selectedStatus: '',
      statusOptions: [
        'PresencaAberta',
        'PresencaEncerrada',
        'CapitaesDefinidos',
        'OrdemDefinida',
        'Aberta',
        'Finalizada',
        'Cancelada',
      ],
      loading: false,
      loadFailed: false,
      hasKnownDrafts: true,
      canCreate: true,
    })
    expect(DraftNavigatorSource).not.toMatch(/@\/services\//)
    expect(DraftNavigatorSource).not.toContain('useAuthState')
    wrapper.unmount()
  })

  it('preserves search and status filter behavior through navigator v-model events', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })

    navigator.vm.$emit('update:searchTerm', 'segunda')
    await nextTick()
    expect(navigator.props('drafts')).toEqual([resumoB])

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumoB])
    navigator.vm.$emit('update:selectedStatus', 'Aberta')
    await flushPromises()

    expect(serviceMocks.listDraftMontagens).toHaveBeenLastCalledWith({ status: 'Aberta', includeArchived: false })
    expect((wrapper.vm as unknown as { searchTerm: string; selectedStatus: string }).searchTerm).toBe('segunda')
    expect((wrapper.vm as unknown as { selectedStatus: string }).selectedStatus).toBe('Aberta')
    expect(navigator.props('drafts')).toEqual([resumoB])
    wrapper.unmount()
  })

  it('preserves the selected draft date when a server-side filter excludes its summary', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumoB])
    navigator.vm.$emit('update:selectedStatus', 'Aberta')
    await flushPromises()

    expect(wrapper.getComponent({ name: 'DraftWorkspaceHeader' }).props('dataRinha')).toBe(resumo.dataRinha)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    wrapper.unmount()
  })

  it('uses the selected detail fallback date when a deep-linked draft has no summary', async () => {
    routeMock.query = { draftId: montagem.id }
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection(),
      horarioEncerramentoPresenca: '2026-07-28T03:00:00Z',
    })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({
      montagem: { ...montagem, horarioEncerramentoPresenca: '2026-07-28T03:00:00Z' },
      canCurrentUserPick: false,
    })

    const wrapper = await mountView()
    const header = wrapper.getComponent({ name: 'DraftWorkspaceHeader' })

    expect(header.props('dataRinha')).toBeNull()
    expect(header.props('draft').id).toBe(montagem.id)
    expect(header.text()).toContain('28/07/2026')
    wrapper.unmount()
  })

  it('resets both navigator filters and reloads the unfiltered list', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:searchTerm', 'segunda')
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumoB])
    navigator.vm.$emit('update:selectedStatus', 'Aberta')
    await flushPromises()

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    navigator.vm.$emit('reset')
    await flushPromises()

    expect((wrapper.vm as unknown as { searchTerm: string; selectedStatus: string })).toMatchObject({
      searchTerm: '',
      selectedStatus: '',
    })
    expect(serviceMocks.listDraftMontagens).toHaveBeenLastCalledWith({ status: '', includeArchived: false })
    expect(navigator.props('drafts')).toEqual([resumo, resumoB])
    wrapper.unmount()
  })

  it('preserves exact selection and creation intents from the navigator', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemRealtimeState.mockImplementation(async (id) => ({ montagem: id === montagemB.id ? montagemB : montagem }))
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })

    navigator.vm.$emit('select', montagemB.id)
    await flushPromises()
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)

    navigator.vm.$emit('create')
    await nextTick()
    expect((wrapper.vm as unknown as { visualSetupOpen: boolean }).visualSetupOpen).toBe(true)
    wrapper.unmount()
  })

  it('keeps canonical selection while administrative enrichment loads and fails', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    let rejectDetail!: (reason: Error) => void
    serviceMocks.getDraftMontagemAdminById.mockImplementationOnce(() => new Promise((_, reject) => { rejectDetail = reject }))

    navigator.vm.$emit('select', montagemB.id)
    await vi.waitFor(() => expect(rejectDetail).toBeTypeOf('function'))

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).toBe(montagemB.id)
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect(navigator.get(`[data-draft-id="${montagemB.id}"]`).attributes('aria-current')).toBe('true')

    rejectDetail(new Error('detail unavailable'))
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).toBe(montagemB.id)
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect(navigator.get(`[data-draft-id="${montagemB.id}"]`).attributes('aria-current')).toBe('true')
    wrapper.unmount()
  })

  it('does not change canonical selection after failed admin enrichment across list completions', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new Error('detail unavailable'))

    navigator.vm.$emit('select', montagemB.id)
    await flushPromises()
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).toBe(montagemB.id)
    const detailCallCountAfterFailure = serviceMocks.getDraftMontagemAdminById.mock.calls.length

    let resolveStaleList!: (value: DraftMontagemResumo[]) => void
    serviceMocks.listDraftMontagens.mockImplementationOnce(() => new Promise((resolve) => { resolveStaleList = resolve }))
    navigator.vm.$emit('retry')
    await vi.waitFor(() => expect(resolveStaleList).toBeTypeOf('function'))

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    navigator.vm.$emit('retry')
    await flushPromises()
    resolveStaleList([resumo])
    await flushPromises()

    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect(navigator.props('drafts')).toEqual([resumo, resumoB])
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).toBe(montagemB.id)
    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(detailCallCountAfterFailure)
    wrapper.unmount()
  })

  it('tracks list failure independently and retries without clearing the selected workspace', async () => {
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    serviceMocks.listDraftMontagens.mockRejectedValueOnce(new Error('list unavailable'))

    navigator.vm.$emit('retry')
    await flushPromises()

    expect(navigator.props('loadFailed')).toBe(true)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    expect((wrapper.vm as unknown as { errors: string[] }).errors).toEqual([])

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    navigator.vm.$emit('retry')
    await flushPromises()

    expect(navigator.props('loadFailed')).toBe(false)
    expect(navigator.props('drafts')).toEqual([resumo, resumoB])
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    wrapper.unmount()
  })

  it('keeps known draft items rendered during refresh and after list failure', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    let rejectRefresh!: (reason: Error) => void
    serviceMocks.listDraftMontagens.mockImplementationOnce(() => new Promise((_, reject) => { rejectRefresh = reject }))

    navigator.vm.$emit('retry')
    await vi.waitFor(() => expect(rejectRefresh).toBeTypeOf('function'))

    expect(navigator.props('loading')).toBe(true)
    expect(navigator.findAll('[data-draft-id]')).toHaveLength(2)
    expect(navigator.find('[data-slot="skeleton"]').exists()).toBe(false)
    expect(navigator.get('[data-navigator-feedback="loading"]')).toBeTruthy()

    rejectRefresh(new Error('refresh unavailable'))
    await flushPromises()

    expect(navigator.props('loadFailed')).toBe(true)
    expect(navigator.findAll('[data-draft-id]')).toHaveLength(2)
    expect(navigator.get('[data-navigator-feedback="error"]')).toBeTruthy()
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    wrapper.unmount()
  })

  it('shows filtered no-results only after the parent list transition settles successfully', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    let rejectFiltered!: (reason: Error) => void
    serviceMocks.listDraftMontagens.mockImplementationOnce(() => new Promise((_, reject) => { rejectFiltered = reject }))

    navigator.vm.$emit('update:selectedStatus', 'Cancelada')
    await vi.waitFor(() => expect(rejectFiltered).toBeTypeOf('function'))

    expect(navigator.props('drafts')).toEqual([])
    expect(navigator.get('[data-navigator-feedback="loading"]')).toBeTruthy()
    expect(navigator.find('[data-navigator-no-results]').exists()).toBe(false)

    rejectFiltered(new Error('filtered unavailable'))
    await flushPromises()
    expect(navigator.get('[data-navigator-feedback="error"]')).toBeTruthy()
    expect(navigator.find('[data-navigator-no-results]').exists()).toBe(false)

    serviceMocks.listDraftMontagens.mockResolvedValueOnce([])
    navigator.vm.$emit('retry')
    await flushPromises()

    expect(navigator.props('drafts')).toEqual([])
    expect(navigator.props('hasKnownDrafts')).toBe(true)
    expect(navigator.get('[data-navigator-no-results]').text()).toContain('Nenhum draft corresponde aos filtros')
    expect(navigator.find('[data-navigator-create]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('keeps the newest list result when an older retry fails later', async () => {
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    let rejectOlder!: (reason: Error) => void
    let resolveNewest!: (value: DraftMontagemResumo[]) => void
    serviceMocks.listDraftMontagens
      .mockImplementationOnce(() => new Promise((_, reject) => { rejectOlder = reject }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveNewest = resolve }))

    navigator.vm.$emit('retry')
    navigator.vm.$emit('retry')
    await vi.waitFor(() => expect(resolveNewest).toBeTypeOf('function'))
    resolveNewest([resumo, resumoB])
    await flushPromises()
    rejectOlder(new Error('late list failure'))
    await flushPromises()

    expect(navigator.props('loadFailed')).toBe(false)
    expect(navigator.props('drafts')).toEqual([resumo, resumoB])
    expect(navigator.props('loading')).toBe(false)
    wrapper.unmount()
  })

  it('does not conflate action errors with list-load failure', async () => {
    serviceMocks.confirmDraftMontagemPresence.mockRejectedValueOnce(new Error('action unavailable'))
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftPreparationPanel' }).vm.$emit('confirm-presence')
    await flushPromises()

    expect(wrapper.getComponent({ name: 'DraftNavigator' }).props('loadFailed')).toBe(false)
    expect((wrapper.vm as unknown as { errors: string[] }).errors).toEqual(['Não foi possível concluir a ação.'])
    wrapper.unmount()
  })

  it('preserves confirmation and both close-presence payloads through the preparation panel', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('confirm-presence')
    await flushPromises()
    expect(serviceMocks.confirmDraftMontagemPresence).toHaveBeenCalledWith('montagem-1')

    panel.vm.$emit('close-presence', false)
    await flushPromises()
    panel.vm.$emit('close-presence', true)
    await flushPromises()
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenNthCalledWith(1, 'montagem-1', false, 5)
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenNthCalledWith(2, 'montagem-1', true, 5)
    wrapper.unmount()
  })

  it('cancels presence only when the current player still has a confirmed presence', async () => {
    authMock.jogadorId = 'jogador-1'
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('cancel-presence')
    await flushPromises()
    expect(serviceMocks.cancelDraftMontagemPresence).toHaveBeenCalledWith('montagem-1')

    ;(wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.presencas = []
    panel.vm.$emit('cancel-presence')
    await flushPromises()
    expect(serviceMocks.cancelDraftMontagemPresence).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('preserves exact player identity and publication type when opening reason actions', async () => {
    const wrapper = await mountView()
    const preparation = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    const publications = wrapper.getComponent({ name: 'DraftDiscordPublicationPanel' })

    preparation.vm.$emit('remove-manual-presence', 'jogador-1', 'Ahri')
    await flushPromises()
    expect(wrapper.get('[role="dialog"]').text()).toContain('Jogador afetado: Ahri')
    await wrapper.get('[data-testid="draft-reason-cancel"]').trigger('click')

    publications.vm.$emit('republish', { publicationType: 'TimesDefinidos', publicationStatus: 'Publicada' })
    await flushPromises()
    expect(wrapper.get('[role="dialog"]').text()).toContain('Republicar times')
    wrapper.unmount()
  })

  it('does not expose management actions after permission denial', async () => {
    authMock.canManageDrafts = false
    const wrapper = await mountView()

    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props()).toMatchObject({
      canClosePresence: false,
      canManageManualPresence: false,
      canSelectCaptains: false,
    })
    expect(wrapper.findComponent({ name: 'DraftDiscordPublicationPanel' }).exists()).toBe(false)
    wrapper.unmount()
  })

  it('blocks rapid duplicate confirmation requests', async () => {
    let resolveConfirmation!: (value: DraftMontagem) => void
    serviceMocks.confirmDraftMontagemPresence.mockReturnValueOnce(new Promise((resolve) => { resolveConfirmation = resolve }))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('confirm-presence')
    panel.vm.$emit('confirm-presence')
    await nextTick()

    expect(serviceMocks.confirmDraftMontagemPresence).toHaveBeenCalledTimes(1)
    resolveConfirmation(montagem)
    await flushPromises()
    wrapper.unmount()
  })

  it('revalidates substitution membership and blocks rapid duplicate requests', async () => {
    const outgoing = { ...realtimeCaptain, jogadorId: 'outgoing-1', nomeExibicao: 'Jogador titular', capitao: false, ordem: 2 }
    const reserve = { ...realtimeCaptain, jogadorId: 'reserve-1', nomeExibicao: 'Jogador reserva', estado: 'Reserva' as const, capitao: false }
    const activeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      times: [{ ...realtimeTeam, jogadores: [realtimeCaptain, outgoing] }],
      reservas: [reserve],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...adminProjection('Aberta'), ...activeDraft })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    let resolveSubstitution!: (value: DraftMontagemRealtimeState) => void
    serviceMocks.substituteDraftMontagemReserve.mockReturnValueOnce(new Promise((resolve) => { resolveSubstitution = resolve }))
    const wrapper = await mountView()
    const payload = { timeId: 'time-1', jogadorSaiuId: 'outgoing-1', reservaEntrouId: 'reserve-1', novoCapitaoId: null, motivo: null }
    const vm = wrapper.vm as unknown as { substituteReserve: (value: typeof payload, complete?: (success: boolean) => void) => Promise<void> }
    const complete = vi.fn()

    void vm.substituteReserve(payload, complete)
    void vm.substituteReserve(payload)
    await nextTick()

    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledTimes(1)
    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledWith('montagem-1', payload)
    expect(complete).not.toHaveBeenCalled()
    const advancedDraft = { ...activeDraft, versaoEstado: activeDraft.versaoEstado + 1 }
    resolveSubstitution({ montagem: advancedDraft, canCurrentUserPick: false, serverNow: advancedDraft.dataAtualizacao })
    await flushPromises()
    expect(complete).toHaveBeenCalledWith(true)
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    await vm.substituteReserve(payload)
    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('reports substitution failure for retry after the service rejects', async () => {
    const outgoing = { ...realtimeCaptain, jogadorId: 'outgoing-1', capitao: false, ordem: 2 }
    const reserve = { ...realtimeCaptain, jogadorId: 'reserve-1', estado: 'Reserva' as const, capitao: false }
    const activeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      times: [{ ...realtimeTeam, jogadores: [realtimeCaptain, outgoing] }],
      reservas: [reserve],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...adminProjection('Aberta'), ...activeDraft })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    serviceMocks.substituteDraftMontagemReserve.mockRejectedValueOnce(new Error('network'))
    const wrapper = await mountView()
    const complete = vi.fn()

    await (wrapper.vm as unknown as {
      substituteReserve: (value: DraftMontagemSubstituicaoPayload, complete: (success: boolean) => void) => Promise<void>
    }).substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: 'outgoing-1',
      reservaEntrouId: 'reserve-1',
      novoCapitaoId: null,
      motivo: null,
    }, complete)

    expect(complete).toHaveBeenCalledOnce()
    expect(complete).toHaveBeenCalledWith(false)
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    wrapper.unmount()
  })

  it('uses auxiliary substitution eligibility on the board', async () => {
    const outgoingCaptain = { ...realtimeCaptain, jogadorId: 'outgoing-captain', nomeExibicao: 'Capitão atual' }
    const teammate = { ...realtimeCaptain, jogadorId: 'teammate-1', nomeExibicao: 'Novo capitão', capitao: false, ordem: 2 }
    const reserve = { ...realtimeCaptain, jogadorId: 'reserve-1', nomeExibicao: 'Reserva elegível', estado: 'Reserva' as const, capitao: false }
    const activeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      cicloVersao: 'ModoPosPresenca',
      times: [{ ...realtimeTeam, capitaoId: outgoingCaptain.jogadorId, jogadores: [outgoingCaptain, teammate] }],
      reservas: [reserve],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection('Aberta'),
      ...activeDraft,
      capitaesElegiveisIds: ['teammate-1'],
      capitaesElegiveisSubstituicaoIds: ['teammate-1', 'reserve-1'],
    })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    serviceMocks.substituteDraftMontagemReserve.mockResolvedValue({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    playerMocks.listEligibleCaptains.mockResolvedValue([
      { id: 'teammate-1', nomeExibicao: 'Novo capitão' },
      { id: 'reserve-1', nomeExibicao: 'Reserva elegível' },
    ])
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    const vm = wrapper.vm as unknown as { substituteReserve: (payload: DraftMontagemSubstituicaoPayload) => Promise<void> }

    expect(board.props('eligibleCaptainIds')).toEqual(['teammate-1', 'reserve-1'])
    await vm.substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: 'outgoing-captain',
      reservaEntrouId: 'reserve-1',
      novoCapitaoId: null,
      motivo: null,
    })
    expect(serviceMocks.substituteDraftMontagemReserve).not.toHaveBeenCalled()

    await vm.substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: 'outgoing-captain',
      reservaEntrouId: 'reserve-1',
      novoCapitaoId: 'teammate-1',
      motivo: null,
    })
    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledWith('montagem-1', expect.objectContaining({ novoCapitaoId: 'teammate-1' }))
    wrapper.unmount()
  })

  it.each(['CapitaesDefinidos', 'OrdemDefinida'] as const)('exposes and submits captain recovery while realtime is %s', async (status) => {
    const outgoingCaptain = { ...realtimeCaptain, jogadorId: 'outgoing-captain' }
    const reserve = { ...realtimeCaptain, jogadorId: 'reserve-1', estado: 'Reserva' as const, capitao: false }
    const preStartDraft: DraftMontagem = {
      ...montagem,
      status,
      modo: 'TempoReal',
      cicloVersao: 'ModoPosPresenca',
      times: [{ ...realtimeTeam, capitaoId: outgoingCaptain.jogadorId, jogadores: [outgoingCaptain] }],
      reservas: [reserve],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection(status),
      ...preStartDraft,
      capitaesElegiveisIds: [],
      capitaesElegiveisSubstituicaoIds: ['reserve-1'],
    })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: preStartDraft, canCurrentUserPick: false, serverNow: preStartDraft.dataAtualizacao })
    serviceMocks.substituteDraftMontagemReserve.mockResolvedValue({ montagem: { ...preStartDraft, versaoEstado: preStartDraft.versaoEstado + 1 }, canCurrentUserPick: false, serverNow: preStartDraft.dataAtualizacao })
    playerMocks.listEligibleCaptains.mockResolvedValue([{ id: 'reserve-1', nomeExibicao: 'Reserva elegível' }])
    const wrapper = await mountView()

    expect(wrapper.findComponent({ name: 'DraftVisualBoard' }).exists()).toBe(true)
    await (wrapper.vm as unknown as { substituteReserve: (payload: DraftMontagemSubstituicaoPayload) => Promise<void> }).substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: 'outgoing-captain',
      reservaEntrouId: 'reserve-1',
      novoCapitaoId: 'reserve-1',
      motivo: null,
    })

    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledWith('montagem-1', expect.objectContaining({ novoCapitaoId: 'reserve-1' }))
    wrapper.unmount()
  })

  it.each<[string, {
    status?: DraftMontagemStatus
    times?: DraftMontagem['times']
    outgoingId?: string
    reserveState?: DraftMontagemParticipante['estado']
  }]>([
    ['terminal status', { status: 'Finalizada' }],
    ['missing team', { times: [] }],
    ['outgoing player outside team', { outgoingId: 'missing-player' }],
    ['ineligible reserve', { reserveState: 'Livre' }],
  ])('rejects a substitution with %s', async (_, scenario) => {
    const outgoing = { ...realtimeCaptain, jogadorId: 'outgoing-1', capitao: false, ordem: 2 }
    const reserve = { ...realtimeCaptain, jogadorId: 'reserve-1', estado: scenario.reserveState ?? 'Reserva', capitao: false }
    const activeDraft: DraftMontagem = {
      ...montagem,
      status: scenario.status ?? 'Aberta',
      modo: 'TempoReal',
      times: scenario.times ?? [{ ...realtimeTeam, jogadores: [realtimeCaptain, outgoing] }],
      reservas: [reserve],
    } as DraftMontagem
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...adminProjection(activeDraft.status), ...activeDraft })
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    const wrapper = await mountView()

    const complete = vi.fn()
    await (wrapper.vm as unknown as { substituteReserve: (value: object, complete: (success: boolean) => void) => Promise<void> }).substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: scenario.outgoingId ?? 'outgoing-1',
      reservaEntrouId: 'reserve-1',
      motivo: null,
    }, complete)

    expect(serviceMocks.substituteDraftMontagemReserve).not.toHaveBeenCalled()
    expect(complete).toHaveBeenCalledWith(false)
    wrapper.unmount()
  })

  it('blocks rapid duplicate close requests and ignores close after management permission is lost', async () => {
    let resolveClose!: (value: DraftMontagem) => void
    serviceMocks.closeDraftMontagemPresence.mockReturnValueOnce(new Promise((resolve) => { resolveClose = resolve }))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('close-presence', false)
    panel.vm.$emit('close-presence', false)
    await nextTick()
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenCalledTimes(1)
    resolveClose(montagem)
    await flushPromises()

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    panel.vm.$emit('close-presence', false)
    await nextTick()
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('blocks duplicate removal confirmation and ignores it after management permission is lost', async () => {
    let resolveRemoval!: (value: DraftMontagem) => void
    serviceMocks.removeManualDraftMontagemPresence.mockReturnValueOnce(new Promise((resolve) => { resolveRemoval = resolve }))
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Remover')
    await wrapper.get('textarea').setValue('remoção concorrente')
    const form = wrapper.get('form')
    await Promise.all([form.trigger('submit'), form.trigger('submit')])
    expect(serviceMocks.removeManualDraftMontagemPresence).toHaveBeenCalledTimes(1)
    resolveRemoval(montagem)
    await flushPromises()

    await openReasonDialog(wrapper, 'Remover')
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    await wrapper.get('form').trigger('submit')
    expect(serviceMocks.removeManualDraftMontagemPresence).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('blocks duplicate republication confirmation and localizes unknown status after permission loss', async () => {
    const projection = adminProjection()
    projection.publicacoesDiscord = projection.publicacoesDiscord.map((publication) => publication.tipo === 'Presenca'
      ? { ...publication, status: 'EstadoLegado' as never }
      : publication)
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(projection)
    let resolveRepublish!: (value: DraftMontagem) => void
    serviceMocks.republishDraftMontagemDiscordPublication.mockReturnValueOnce(new Promise((resolve) => { resolveRepublish = resolve }))
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar presença')
    expect(wrapper.get('[role="dialog"] [data-slot="badge"]').text()).toContain('Estado de publicação desconhecido')
    await wrapper.get('textarea').setValue('republicação concorrente')
    const form = wrapper.get('form')
    await Promise.all([form.trigger('submit'), form.trigger('submit')])
    expect(serviceMocks.republishDraftMontagemDiscordPublication).toHaveBeenCalledTimes(1)
    resolveRepublish(montagem)
    await flushPromises()

    await openReasonDialog(wrapper, 'Republicar presença')
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    await wrapper.get('form').trigger('submit')
    expect(serviceMocks.republishDraftMontagemDiscordPublication).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('keeps personal confirmation available after management permission is lost', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true

    panel.vm.$emit('confirm-presence')
    await flushPromises()

    expect(serviceMocks.confirmDraftMontagemPresence).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('accepts captain toggles only for authorized confirmed players while presence is closed', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('PresencaEncerrada'))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('toggle-captain', 'jogador-inexistente')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])

    panel.vm.$emit('toggle-captain', 'jogador-1')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-1'])

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    panel.vm.$emit('toggle-captain', 'jogador-1')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])
    wrapper.unmount()
  })

  it.each([
    [19, 3, '0 / 3 capitães'],
    [20, 4, '0 / 4 capitães'],
  ] as const)('exposes reopen and captain count for a closed draft with %i participants', async (participantCount, teamCount, expectedCount) => {
    const presencas = Array.from({ length: participantCount }, (_, index) => ({
      ...montagem.presencas[0]!,
      id: `presenca-${index}`,
      usuarioId: `usuario-${index}`,
      jogadorId: `jogador-${index}`,
      nomeExibicao: `Jogador ${index}`,
      ordemConfirmacao: index + 1,
    }))
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection('PresencaEncerrada'),
      quantidadeTimes: teamCount,
      presencas,
    })
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(panel.props('canReopenPresence')).toBe(true)
    expect(panel.get('[data-captains-count]').text()).toBe(expectedCount)
    expect(panel.findAll('[data-stage-primary-action]')).toHaveLength(1)
    expect(panel.get('[data-stage-primary-action]').attributes('data-testid')).toBe('define-captains')
    wrapper.unmount()
  })

  it('revalidates reopening at request and confirmation, applies the mutation, and restores focus', async () => {
    const closed = adminProjection('PresencaEncerrada')
    const reopened = { ...closed, status: 'PresencaAberta' as const, quantidadeTimes: 0, quantidadeReservas: 0, versaoEstado: 8 }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    serviceMocks.reopenDraftMontagemPresence.mockResolvedValueOnce(reopened)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('reopen-presence')
    await flushPromises()
    expect((wrapper.vm as unknown as { pendingReasonAction: unknown }).pendingReasonAction).toEqual({
      type: 'reopenPresence',
      draftName: montagem.nome,
    })
    expect(wrapper.find('textarea').exists()).toBe(false)
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(reopened)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.reopenDraftMontagemPresence).toHaveBeenCalledWith('montagem-1')
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.status).toBe('PresencaAberta')
    expect(wrapper.get('[role="status"]').text()).toContain('Presença reaberta.')
    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('clears selected captains only after reopening is successfully projected', async () => {
    const closed = adminProjection('PresencaEncerrada')
    const reopened = { ...closed, status: 'PresencaAberta' as const, quantidadeTimes: 0, quantidadeReservas: 0 }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    serviceMocks.reopenDraftMontagemPresence.mockResolvedValueOnce(reopened)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    panel.vm.$emit('toggle-captain', 'jogador-1')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-1'])

    panel.vm.$emit('reopen-presence')
    await flushPromises()
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(reopened)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.reopenDraftMontagemPresence).toHaveBeenCalledTimes(1)
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])
    wrapper.unmount()
  })

  it('rejects reopening when the draft leaves closed presence before confirmation', async () => {
    const closed = adminProjection('PresencaEncerrada')
    const captainsDefined = { ...closed, status: 'CapitaesDefinidos' as const }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('reopen-presence')
    await flushPromises()
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()
    serviceMocks.getDraftMontagemAdminById.mockResolvedValueOnce(captainsDefined)
    await emitRealtime('montagem-1', captainsDefined)
    await flushPromises()
    expect(wrapper.get('[role="dialog"]')).toBeTruthy()

    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.reopenDraftMontagemPresence).not.toHaveBeenCalled()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('rejects stale or unauthorized reopen intents and submits only once while saving', async () => {
    const closed = adminProjection('PresencaEncerrada')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    let resolveReopen!: (value: DraftMontagem) => void
    serviceMocks.reopenDraftMontagemPresence.mockReturnValueOnce(new Promise((resolve) => { resolveReopen = resolve }))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('reopen-presence')
    await flushPromises()
    const form = wrapper.get('form')
    await Promise.all([form.trigger('submit'), form.trigger('submit')])
    expect(serviceMocks.reopenDraftMontagemPresence).toHaveBeenCalledTimes(1)
    resolveReopen({ ...closed, status: 'PresencaAberta' })
    await flushPromises()

    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    await emitRealtime('montagem-1', closed)
    panel.vm.$emit('reopen-presence')
    await flushPromises()
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    await wrapper.get('form').trigger('submit')
    expect(serviceMocks.reopenDraftMontagemPresence).toHaveBeenCalledTimes(1)

    ;(wrapper.vm as unknown as { pendingReasonAction: unknown }).pendingReasonAction = null
    panel.vm.$emit('reopen-presence')
    await nextTick()
    expect((wrapper.vm as unknown as { pendingReasonAction: unknown }).pendingReasonAction).toBeNull()
    wrapper.unmount()
  })

  it('ignores captain toggles outside the closed-presence state', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('toggle-captain', 'jogador-1')

    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])
    wrapper.unmount()
  })

  it('lets Admin choose a v2 mode once and never offers mode choice to legacy drafts', async () => {
    const waiting = { ...adminProjection('PresencaEncerrada'), modo: null, cicloVersao: 'ModoPosPresenca' as const }
    const manual = { ...waiting, status: 'Aberta' as const, modo: 'Manual' as const, versaoEstado: 8 }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(waiting)
    serviceMocks.chooseDraftMontagemMode.mockResolvedValueOnce(manual)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(panel.props('canChooseMode')).toBe(true)
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(manual)
    panel.vm.$emit('choose-mode', 'Manual')
    panel.vm.$emit('choose-mode', 'TempoReal')
    await flushPromises()

    expect(serviceMocks.chooseDraftMontagemMode).toHaveBeenCalledTimes(1)
    expect(serviceMocks.chooseDraftMontagemMode).toHaveBeenCalledWith('montagem-1', 'Manual')
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.modo).toBe('Manual')

    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...waiting, cicloVersao: 'Legado' })
    await emitRealtime('montagem-1', { ...waiting, cicloVersao: 'Legado', versaoEstado: 9 })
    await flushPromises()
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canChooseMode')).toBe(false)
    wrapper.unmount()
  })

  it('requires an explicit Admin+ role for cycle actions even with CanManageDrafts', async () => {
    authMock.roles = ['Moderador']
    if (authMock.rolesRef) authMock.rolesRef.value = ['Moderador']
    const waiting = { ...montagem, status: 'PresencaEncerrada' as const, modo: null, cicloVersao: 'ModoPosPresenca' as const }
    serviceMocks.getDraftMontagemById.mockResolvedValue(waiting)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: waiting, canCurrentUserPick: false })
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(serviceMocks.getDraftMontagemAdminById).not.toHaveBeenCalled()
    expect(panel.props('canChooseMode')).toBe(false)
    expect(panel.props('canReopenPresence')).toBe(false)
    expect(wrapper.findAll('button').some((button) => button.text().includes('Criar Draft'))).toBe(false)
    panel.vm.$emit('choose-mode', 'Manual')
    await flushPromises()
    expect(serviceMocks.chooseDraftMontagemMode).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('denies every legacy cycle operation to Moderador even with CanManageDrafts', async () => {
    authMock.roles = ['Moderador']
    if (authMock.rolesRef) authMock.rolesRef.value = ['Moderador']
    const secondPresence = { ...montagem.presencas[0]!, id: 'presenca-2', jogadorId: 'jogador-2', ordemConfirmacao: 2 }
    const closed = { ...montagem, status: 'PresencaEncerrada' as const, presencas: [...montagem.presencas, secondPresence] }
    serviceMocks.getDraftMontagemById.mockResolvedValue(closed)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: closed, canCurrentUserPick: false })
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(panel.props('canSelectCaptains')).toBe(false)
    expect(panel.props('canReopenPresence')).toBe(false)
    panel.vm.$emit('reopen-presence')
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('define-captains')
    panel.vm.$emit('draw-order')
    expect(serviceMocks.reopenDraftMontagemPresence).not.toHaveBeenCalled()

    const open = { ...closed, status: 'Aberta' as const, modo: 'Manual' as const }
    serviceMocks.getDraftMontagemById.mockResolvedValue(open)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem: open, canCurrentUserPick: false })
    await emitRealtime('montagem-1', open)
    await flushPromises()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    expect(board.props('canManage')).toBe(false)
    board.vm.$emit('save', { times: [], livres: [], reservas: [] })
    board.vm.$emit('start-realtime')
    board.vm.$emit('substitute-reserve', { timeId: 'time-1', jogadorSaiuId: 'jogador-1', reservaEntrouId: 'reserva-1' })
    board.vm.$emit('draw-captains')
    board.vm.$emit('finalize')
    await flushPromises()

    expect(serviceMocks.defineDraftMontagemCaptains).not.toHaveBeenCalled()
    expect(serviceMocks.defineDraftMontagemPickOrder).not.toHaveBeenCalled()
    expect(serviceMocks.saveDraftMontagemLayout).not.toHaveBeenCalled()
    expect(serviceMocks.startDraftMontagemRealtime).not.toHaveBeenCalled()
    expect(serviceMocks.substituteDraftMontagemReserve).not.toHaveBeenCalled()
    expect(serviceMocks.drawDraftMontagemCaptains).not.toHaveBeenCalled()
    expect(serviceMocks.finalizeDraftMontagem).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('uses only auxiliary eligible starters for realtime captain selection', async () => {
    const secondPresence = {
      ...montagem.presencas[0]!,
      id: 'presenca-2',
      jogadorId: 'jogador-2',
      nomeExibicao: 'Lux',
      ordemConfirmacao: 2,
    }
    const projection = {
      ...adminProjection('PresencaEncerrada'),
      versaoEstado: 8,
      modo: 'TempoReal',
      cicloVersao: 'ModoPosPresenca',
      presencas: [...montagem.presencas, secondPresence],
      capitaesElegiveisIds: ['jogador-2'],
    } as DraftMontagemAdmin
    playerMocks.listEligibleCaptains.mockResolvedValue([{ id: 'jogador-2', nomeExibicao: 'Lux' }])
    const wrapper = await mountView()
    const vm = wrapper.vm as unknown as {
      activeDraftGeneration: number
      beginDraftUpdate: (id: string, generation: number, lane: 'passive', personalized?: boolean) => unknown
      applySharedProjection: (context: unknown, draft: DraftMontagem) => boolean
      applyAdministrativeMetadata: (context: unknown, detail: DraftMontagemAdmin) => boolean
      captainSelection: string[]
      highestSharedVersion: number
    }
    const acceptedProjection = { ...projection, versaoEstado: vm.highestSharedVersion + 1 }
    const sharedContext = vm.beginDraftUpdate(montagem.id, vm.activeDraftGeneration, 'passive')
    vm.applySharedProjection(sharedContext, sharedFromAdmin(acceptedProjection))
    const adminContext = vm.beginDraftUpdate(montagem.id, vm.activeDraftGeneration, 'passive', true)
    vm.applyAdministrativeMetadata(adminContext, acceptedProjection)
    await nextTick()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(panel.props('eligibleCaptainIds')).toEqual(['jogador-2'])
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    await nextTick()
    expect(vm.captainSelection).toEqual(['jogador-2'])
    wrapper.unmount()
  })

  it('reconciles selected captains after eligibility changes and clears them after Admin+ role loss', async () => {
    const presencas = ['jogador-1', 'jogador-2', 'jogador-3'].map((jogadorId, index) => ({
      ...montagem.presencas[0]!,
      id: `presenca-${index + 1}`,
      jogadorId,
      ordemConfirmacao: index + 1,
    }))
    const initial = {
      ...adminProjection('PresencaEncerrada'),
      modo: 'TempoReal' as const,
      cicloVersao: 'ModoPosPresenca' as const,
      presencas,
      capitaesElegiveisIds: ['jogador-1', 'jogador-2'],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(initial)
    playerMocks.listEligibleCaptains.mockResolvedValue([
      { id: 'jogador-1', nomeExibicao: 'Jogador 1' },
      { id: 'jogador-2', nomeExibicao: 'Jogador 2' },
    ])
    const wrapper = await mountView()
    let panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-1', 'jogador-2'])

    playerMocks.listEligibleCaptains.mockResolvedValue([
      { id: 'jogador-2', nomeExibicao: 'Jogador 2' },
      { id: 'jogador-3', nomeExibicao: 'Jogador 3' },
    ])
    await (wrapper.vm as unknown as { loadEligibleCaptains: () => Promise<void> }).loadEligibleCaptains()
    await flushPromises()
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-2'])

    panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    panel.vm.$emit('toggle-captain', 'jogador-3')
    await nextTick()
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-2', 'jogador-3'])
    expect(panel.props('canDefineCaptains')).toBe(true)

    authMock.rolesRef.value = ['Moderador']
    await nextTick()
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canSelectCaptains')).toBe(false)
    wrapper.unmount()
  })

  it('defines captains once with exact identities and rejects permission loss', async () => {
    const secondPresence = {
      ...montagem.presencas[0]!,
      id: 'presenca-2',
      jogadorId: 'jogador-2',
      nomeExibicao: 'Lux',
      ordemConfirmacao: 2,
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection('PresencaEncerrada'),
      presencas: [...montagem.presencas, secondPresence],
    })
    let resolveCaptains!: (value: DraftMontagem) => void
    serviceMocks.defineDraftMontagemCaptains.mockReturnValueOnce(new Promise((resolve) => { resolveCaptains = resolve }))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    panel.vm.$emit('define-captains')
    panel.vm.$emit('define-captains')
    await nextTick()

    expect(serviceMocks.defineDraftMontagemCaptains).toHaveBeenCalledTimes(1)
    expect(serviceMocks.defineDraftMontagemCaptains).toHaveBeenCalledWith('montagem-1', ['jogador-1', 'jogador-2'])
    resolveCaptains(montagem)
    await flushPromises()

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    panel.vm.$emit('define-captains')
    await nextTick()
    expect(serviceMocks.defineDraftMontagemCaptains).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('defines pick order once only in the authorized captains-defined state', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('CapitaesDefinidos'))
    let resolveOrder!: (value: DraftMontagem) => void
    serviceMocks.defineDraftMontagemPickOrder.mockReturnValueOnce(new Promise((resolve) => { resolveOrder = resolve }))
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('draw-order')
    panel.vm.$emit('draw-order')
    await nextTick()

    expect(serviceMocks.defineDraftMontagemPickOrder).toHaveBeenCalledTimes(1)
    expect(serviceMocks.defineDraftMontagemPickOrder).toHaveBeenCalledWith('montagem-1', 'Sorteado')
    resolveOrder(montagem)
    await flushPromises()

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    panel.vm.$emit('draw-order')
    await nextTick()
    expect(serviceMocks.defineDraftMontagemPickOrder).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('starts a v2 realtime draft only from the explicitly ordered state', async () => {
    const ordered = { ...adminProjection('OrdemDefinida'), cicloVersao: 'ModoPosPresenca' as const, modo: 'TempoReal' as const }
    const started = { ...ordered, status: 'Aberta' as const }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(ordered)
    serviceMocks.startDraftMontagemRealtime.mockResolvedValueOnce({ montagem: started, canCurrentUserPick: false, serverNow: started.dataAtualizacao })
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('start-realtime')
    await flushPromises()

    expect(serviceMocks.startDraftMontagemRealtime).toHaveBeenCalledWith('montagem-1')
    wrapper.unmount()
  })

  it('ignores captain and order intents outside their matching states', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('define-captains')
    panel.vm.$emit('draw-order')
    await flushPromises()

    expect(serviceMocks.defineDraftMontagemCaptains).not.toHaveBeenCalled()
    expect(serviceMocks.defineDraftMontagemPickOrder).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('forwards the layout payload without reordering it', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    const payload = {
      versaoEstado: montagem.versaoEstado,
      times: [
        { timeId: 'time-2', nome: 'Segundo', capitaoId: 'capitao-2', jogadores: [{ jogadorId: 'jogador-2', ordem: 1, rotaContextual: 'Top' as const }] },
        { timeId: 'time-1', nome: 'Primeiro', capitaoId: 'capitao-1', jogadores: [{ jogadorId: 'jogador-1', ordem: 1, rotaContextual: 'Mid' as const }] },
      ],
      livres: [],
      reservas: [],
    }

    board.vm.$emit('save', payload)
    await flushPromises()

    expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledWith('montagem-1', payload)
    wrapper.unmount()
  })

  it('passes Admin+ cycle capability to the board and no captain configuration to direct setup', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...adminProjection('Aberta'), cicloVersao: 'ModoPosPresenca', modo: 'Manual' })
    const wrapper = await mountView()

    expect(wrapper.getComponent({ name: 'DraftVisualBoard' }).props('canManage')).toBe(true)
    expect(wrapper.getComponent({ name: 'DraftVisualSetup' }).props('captains')).toBeUndefined()
    wrapper.unmount()
  })

  it('draws captains once when duplicate intents arrive in the same tick', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    let resolveDraw!: (value: DraftMontagem) => void
    serviceMocks.drawDraftMontagemCaptains.mockReturnValueOnce(new Promise((resolve) => { resolveDraw = resolve }))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    board.vm.$emit('draw-captains')
    board.vm.$emit('draw-captains')
    await nextTick()

    expect(serviceMocks.drawDraftMontagemCaptains).toHaveBeenCalledTimes(1)
    expect(serviceMocks.drawDraftMontagemCaptains).toHaveBeenCalledWith('montagem-1')
    resolveDraw(montagem)
    await flushPromises()
    wrapper.unmount()
  })

  it('rejects a captain draw after draft-management permission is lost', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const requestVersionBefore = (wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('draw-captains')
    await flushPromises()

    expect(serviceMocks.drawDraftMontagemCaptains).not.toHaveBeenCalled()
    expect((wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion).toBe(requestVersionBefore)
    wrapper.unmount()
  })

  it.each(['Finalizada', 'Cancelada'] as const)('rejects a stale captain draw when the draft is %s', async (status) => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection(status))
    const wrapper = await mountView()
    const requestVersionBefore = (wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('draw-captains')
    await flushPromises()

    expect(serviceMocks.drawDraftMontagemCaptains).not.toHaveBeenCalled()
    expect((wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion).toBe(requestVersionBefore)
    wrapper.unmount()
  })

  it('rejects a captain draw outside manual mode', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({ ...adminProjection('Aberta'), modo: 'TempoReal' })
    const wrapper = await mountView()
    const requestVersionBefore = (wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('draw-captains')
    await flushPromises()

    expect(serviceMocks.drawDraftMontagemCaptains).not.toHaveBeenCalled()
    expect((wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion).toBe(requestVersionBefore)
    wrapper.unmount()
  })

  it('does not invalidate a legitimate realtime refresh when a captain draw is rejected', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    let resolveRefresh!: (value: DraftMontagemAdmin) => void
    serviceMocks.getDraftMontagemAdminById.mockImplementationOnce(() => new Promise((resolve) => { resolveRefresh = resolve }))

    const refresh = emitRealtime('montagem-1', { ...montagem, status: 'Aberta' })
    await vi.waitFor(() => expect(resolveRefresh).toBeTypeOf('function'))
    const requestVersionBefore = (wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('draw-captains')
    await nextTick()

    expect(serviceMocks.drawDraftMontagemCaptains).not.toHaveBeenCalled()
    expect((wrapper.vm as unknown as { detailRequestVersion: number }).detailRequestVersion).toBe(requestVersionBefore)

    resolveRefresh({ ...adminProjection('Finalizada', 'refresh legítimo'), versaoEstado: 9 })
    await refresh
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('refresh legítimo')
    wrapper.unmount()
  })

  it('allows only the current realtime captain to pick and blocks rapid duplicates', async () => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const available = {
      jogadorId: 'jogador-livre',
      nomeExibicao: 'Jogador livre',
      status: 'Ativo',
      preferencias: [],
      estado: 'Livre' as const,
      capitao: false,
      ordem: 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: '2026-07-25T12:20:00Z',
      times: [realtimeTeam],
      livres: [available],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: realtimeDraft, canCurrentUserPick: true, serverNow: '2026-07-25T12:00:00Z' })
    let resolvePick!: (value: DraftMontagemRealtimeState) => void
    serviceMocks.registerDraftMontagemPick.mockReturnValueOnce(new Promise((resolve) => { resolvePick = resolve }))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    expect(board.props('currentPlayerId')).toBe('capitao-atual')
    expect(board.props('canCurrentUserPick')).toBe(true)
    board.vm.$emit('pick', 'jogador-livre')
    board.vm.$emit('pick', 'jogador-livre')
    await nextTick()

    expect(serviceMocks.registerDraftMontagemPick).toHaveBeenCalledTimes(1)
    expect(serviceMocks.registerDraftMontagemPick).toHaveBeenCalledWith('montagem-1', 'jogador-livre')
    resolvePick({ montagem: realtimeDraft, canCurrentUserPick: false, serverNow: '2026-07-25T12:05:00Z' })
    await flushPromises()
    dateNow.mockRestore()
    expect(board.props('canCurrentUserPick')).toBe(false)
    expect(board.props('serverClockOffsetMs')).toBe(5 * 60 * 1000)
    wrapper.unmount()
  })

  it('uses personalized GET permission instead of the SignalR broadcast permission', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState
      .mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: true, serverNow: montagem.dataAtualizacao })
      .mockResolvedValueOnce({ montagem: { ...realtimeDraft, versaoEstado: 8 }, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    expect(board.props('canCurrentUserPick')).toBe(true)
    await emitRealtime('montagem-1', realtimeDraft, true, null)
    await flushPromises()

    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    expect(board.props('canCurrentUserPick')).toBe(false)
    wrapper.unmount()
  })

  it('ignores broadcast permission and fetches a personalized realtime state for the active draft', async () => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const personalizedDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: '2026-07-25T12:20:00Z',
      times: [realtimeTeam],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(personalizedDraft)
    serviceMocks.getDraftMontagemRealtimeState
      .mockResolvedValueOnce({ montagem: personalizedDraft, canCurrentUserPick: false, serverNow: '2026-07-25T12:00:00Z' })
      .mockResolvedValueOnce({ montagem: { ...personalizedDraft, versaoEstado: 8 }, canCurrentUserPick: true, serverNow: '2026-07-25T12:10:00Z' })
    const wrapper = await mountView()

    await emitRealtime('montagem-1', { ...personalizedDraft, status: 'Finalizada' }, false, null)
    await flushPromises()
    dateNow.mockRestore()

    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.status).toBe('Finalizada')
    expect(board.props('canCurrentUserPick')).toBe(true)
    expect(board.props('serverClockOffsetMs')).toBe(10 * 60 * 1000)
    wrapper.unmount()
  })

  it('ignores an older personalized GET response that resolves after a newer broadcast refresh', async () => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    authMock.canManageDrafts = false
    const wrapper = await mountView()
    let resolveOlder!: (state: DraftMontagemRealtimeState) => void
    let resolveNewer!: (state: DraftMontagemRealtimeState) => void
    serviceMocks.getDraftMontagemRealtimeState
      .mockImplementationOnce(() => new Promise((resolve) => { resolveOlder = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveNewer = resolve }))

    const olderRefresh = emitRealtime('montagem-1', { ...montagem, status: 'Aberta' }, true, null)
    await vi.waitFor(() => expect(resolveOlder).toBeTypeOf('function'))
    const newerRefresh = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' }, false, null)
    await vi.waitFor(() => expect(resolveNewer).toBeTypeOf('function'))
    resolveNewer({ montagem: { ...montagem, status: 'Finalizada', versaoEstado: 9 }, canCurrentUserPick: false, serverNow: '2026-07-25T12:05:00Z' })
    await newerRefresh
    resolveOlder({ montagem: { ...montagem, status: 'Aberta', versaoEstado: 8 }, canCurrentUserPick: true, serverNow: '2026-07-25T12:01:00Z' })
    await olderRefresh
    await flushPromises()
    dateNow.mockRestore()

    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.status).toBe('Finalizada')
    expect(board.props('canCurrentUserPick')).toBe(false)
    expect(board.props('serverClockOffsetMs')).toBe(5 * 60 * 1000)
    wrapper.unmount()
  })

  it('refreshes personalized permission and server clock offset after reconnect', async () => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    authMock.canManageDrafts = false
    const realtimeDraft: DraftMontagem = { ...montagem, status: 'Aberta', modo: 'TempoReal' }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState
      .mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: true, serverNow: '2026-07-25T12:00:00Z' })
      .mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: false, serverNow: '2026-07-25T12:03:00Z' })
    const wrapper = await mountView()

    await realtimeMock.reconnectHandlers.get('montagem-1')?.()
    await flushPromises()
    dateNow.mockRestore()

    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    expect(board.props('canCurrentUserPick')).toBe(false)
    expect(board.props('serverClockOffsetMs')).toBe(3 * 60 * 1000)
    wrapper.unmount()
  })

  it.each([
    ['current team is missing', { times: [] as DraftMontagem['times'], expiresAt: '2026-07-25T12:20:00Z', playerState: 'Livre' as const }],
    ['captain mismatches the current team', { times: [{ ...realtimeTeam, capitaoId: 'outro-capitao' }], expiresAt: '2026-07-25T12:20:00Z', playerState: 'Livre' as const }],
    ['turn is expired against the server clock', { times: [realtimeTeam], expiresAt: '2026-07-25T12:05:00Z', playerState: 'Livre' as const }],
    ['requested player is not eligible and free', { times: [realtimeTeam], expiresAt: '2026-07-25T12:20:00Z', playerState: 'Time' as const }],
  ])('rejects a parent pick when %s', async (_, scenario) => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const available = {
      jogadorId: 'jogador-livre',
      nomeExibicao: 'Jogador livre',
      status: 'Ativo',
      preferencias: [],
      estado: scenario.playerState,
      capitao: false,
      ordem: 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: scenario.expiresAt,
      times: scenario.times,
      livres: [available],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({
      montagem: realtimeDraft,
      canCurrentUserPick: true,
      serverNow: '2026-07-25T12:10:00Z',
    })
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('pick', 'jogador-livre')
    await flushPromises()
    dateNow.mockRestore()

    expect(serviceMocks.registerDraftMontagemPick).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('rejects pick intents when the latest realtime state denies permission', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const available = {
      jogadorId: 'jogador-livre',
      nomeExibicao: 'Jogador livre',
      status: 'Ativo',
      preferencias: [],
      estado: 'Livre' as const,
      capitao: false,
      ordem: 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: new Date(Date.now() + 60_000).toISOString(),
      times: [realtimeTeam],
      livres: [available],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: realtimeDraft, canCurrentUserPick: false })
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    expect(board.props('canCurrentUserPick')).toBe(false)
    board.vm.$emit('pick', 'jogador-livre')
    await flushPromises()

    expect(serviceMocks.registerDraftMontagemPick).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('rejects a pick intent when realtime identity does not own the current turn', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'outro-jogador'
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      livres: [],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: realtimeDraft, canCurrentUserPick: true })
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('pick', 'jogador-invalido')
    await flushPromises()

    expect(serviceMocks.registerDraftMontagemPick).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('preserves the current realtime projection and surfaces an invalid pick error', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const available = {
      jogadorId: 'jogador-livre',
      nomeExibicao: 'Jogador livre',
      status: 'Ativo',
      preferencias: [],
      estado: 'Livre' as const,
      capitao: false,
      ordem: 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: new Date(Date.now() + 60_000).toISOString(),
      times: [realtimeTeam],
      livres: [available],
    }
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: realtimeDraft, canCurrentUserPick: true })
    serviceMocks.registerDraftMontagemPick.mockRejectedValueOnce(new ServiceError(['Escolha inválida']))
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('pick', 'jogador-livre')
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem).toMatchObject({
      id: 'montagem-1',
      turnoAtualCapitaoId: 'capitao-atual',
      livres: [expect.objectContaining({ jogadorId: 'jogador-livre' })],
    })
    expect(wrapper.get('[role="alert"]').text()).toContain('Escolha inválida')
    wrapper.unmount()
  })

  it('finalizes once only while authorized in an open manual draft', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    let resolveFinalize!: (value: DraftMontagem) => void
    serviceMocks.finalizeDraftMontagem.mockReturnValueOnce(new Promise((resolve) => { resolveFinalize = resolve }))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    board.vm.$emit('finalize')
    board.vm.$emit('finalize')
    await nextTick()

    expect(serviceMocks.finalizeDraftMontagem).toHaveBeenCalledTimes(1)
    expect(serviceMocks.finalizeDraftMontagem).toHaveBeenCalledWith('montagem-1')
    resolveFinalize({ ...montagem, status: 'Finalizada' })
    await flushPromises()

    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    board.vm.$emit('finalize')
    await nextTick()
    expect(serviceMocks.finalizeDraftMontagem).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('restores stage focus after confirming and closing presence', async () => {
    const ownPresence = {
      ...montagem.presencas[0]!,
      id: 'presenca-organizador',
      usuarioId: 'organizador-1',
      jogadorId: 'jogador-organizador',
      nomeExibicao: 'Organizador',
    }
    const confirmed = { ...adminProjection(), versaoEstado: 8, presencas: [...montagem.presencas, ownPresence] }
    serviceMocks.confirmDraftMontagemPresence.mockResolvedValueOnce(confirmed)
    serviceMocks.getDraftMontagemAdminById
      .mockResolvedValueOnce(adminProjection())
      .mockResolvedValueOnce(adminProjection())
      .mockResolvedValue(confirmed)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem, canCurrentUserPick: false })
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('confirm-presence')
    await flushPromises()
    expectStageFocus(wrapper)

    const closed = { ...confirmed, status: 'PresencaEncerrada' as const, versaoEstado: 9 }
    serviceMocks.closeDraftMontagemPresence.mockResolvedValueOnce(closed)
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    panel.vm.$emit('close-presence', false)
    await flushPromises()
    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('restores stage focus after defining captains and pick order', async () => {
    const secondPresence = {
      ...montagem.presencas[0]!,
      id: 'presenca-2',
      usuarioId: 'usuario-2',
      jogadorId: 'jogador-2',
      nomeExibicao: 'Lux',
      ordemConfirmacao: 2,
    }
    const closed = { ...adminProjection('PresencaEncerrada'), presencas: [...montagem.presencas, secondPresence] }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(closed)
    const captainsDefined = { ...closed, status: 'CapitaesDefinidos' as const }
    serviceMocks.defineDraftMontagemCaptains.mockResolvedValueOnce(captainsDefined)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    panel.vm.$emit('define-captains')
    await flushPromises()
    expectStageFocus(wrapper)

    const orderDefined = { ...captainsDefined, status: 'OrdemDefinida' as const }
    serviceMocks.defineDraftMontagemPickOrder.mockResolvedValueOnce(orderDefined)
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(orderDefined)
    wrapper.getComponent({ name: 'DraftPreparationPanel' }).vm.$emit('draw-order')
    await flushPromises()
    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('restores stage focus after a successful realtime pick', async () => {
    authMock.canManageDrafts = false
    authMock.jogadorId = 'capitao-atual'
    const available = {
      jogadorId: 'jogador-livre',
      nomeExibicao: 'Jogador livre',
      status: 'Ativo',
      preferencias: [],
      estado: 'Livre' as const,
      capitao: false,
      ordem: 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }
    const realtimeDraft: DraftMontagem = {
      ...montagem,
      status: 'Aberta',
      modo: 'TempoReal',
      turnoAtualTimeId: 'time-1',
      turnoAtualCapitaoId: 'capitao-atual',
      turnoExpiraEm: new Date(Date.now() + 60_000).toISOString(),
      times: [realtimeTeam],
      livres: [available],
    }
    serviceMocks.getDraftMontagemById.mockResolvedValue(realtimeDraft)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: realtimeDraft, canCurrentUserPick: true, serverNow: new Date().toISOString() })
    serviceMocks.registerDraftMontagemPick.mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: false, serverNow: new Date().toISOString() })
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('pick', 'jogador-livre')
    await flushPromises()

    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('restores stage focus after finalizing the draft', async () => {
    const open = adminProjection('Aberta')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(open)
    serviceMocks.finalizeDraftMontagem.mockResolvedValueOnce({ ...open, status: 'Finalizada' })
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('finalize')
    await flushPromises()

    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('rejects a synthetic finalize event until the projected v2 manual layout is complete', async () => {
    const incomplete = {
      ...adminProjection('Aberta'),
      cicloVersao: 'ModoPosPresenca' as const,
      modo: 'Manual' as const,
      times: [{ id: 'time-1', nome: 'Time 1', ordem: 1, cor: 'blue', capitaoId: null, jogadores: [] }],
      livres: [],
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(incomplete)
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('finalize')
    await flushPromises()
    expect(serviceMocks.finalizeDraftMontagem).not.toHaveBeenCalled()

    const teamPlayers = Array.from({ length: incomplete.tamanhoEquipe }, (_, index) => ({
      jogadorId: `jogador-${index}`,
      nomeExibicao: `Jogador ${index}`,
      status: 'Ativo' as const,
      preferencias: [],
      estado: 'Time' as const,
      capitao: false,
      ordem: index + 1,
      dataCadastro: montagem.dataCadastro,
      dataAtualizacao: montagem.dataAtualizacao,
    }))
    const complete = {
      ...incomplete,
      times: Array.from({ length: incomplete.quantidadeTimes }, (_, index) => ({
        id: `time-${index + 1}`,
        nome: `Time ${index + 1}`,
        ordem: index + 1,
        cor: 'blue',
        capitaoId: null,
        jogadores: teamPlayers.map((player, playerIndex) => ({ ...player, jogadorId: `${index}-${playerIndex}` })),
      })),
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(complete)
    await emitRealtime('montagem-1', complete)
    await flushPromises()
    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('finalize')
    await flushPromises()

    expect(serviceMocks.finalizeDraftMontagem).toHaveBeenCalledWith('montagem-1')
    wrapper.unmount()
  })

  it('restores stage focus after adding a manual presence through the reason dialog', async () => {
    const wrapper = await mountView()
    await wrapper.get('select[name="manual-presence-player"]').setValue('jogador-2')

    await confirmReasonAction(wrapper, 'Adicionar presença', 'inclusão administrativa')

    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it.each([
    ['Remover', 'remoção administrativa'],
    ['Republicar presença', 'republicação administrativa'],
    ['Cancelar', 'cancelamento administrativo'],
  ])('restores stage focus after completing the reason action %s', async (buttonText, reason) => {
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, buttonText, reason)

    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('does not steal focus when a passive realtime update arrives', async () => {
    const wrapper = await mountView()
    const createButton = findButton(wrapper, 'Criar Draft').element as HTMLButtonElement
    createButton.focus()

    await emitRealtime('montagem-1', { ...montagem, dataAtualizacao: '2026-07-26T12:00:00Z' })
    await flushPromises()

    expect(document.activeElement).toBe(createButton)
    wrapper.unmount()
  })

  it('ignores board mutation intents before the draft is open', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('OrdemDefinida'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })

    board.vm.$emit('save', { times: [], livres: [], reservas: [] })
    board.vm.$emit('start-realtime')
    board.vm.$emit('pick', 'jogador-1')
    board.vm.$emit('finalize')
    await flushPromises()

    expect(serviceMocks.saveDraftMontagemLayout).not.toHaveBeenCalled()
    expect(serviceMocks.startDraftMontagemRealtime).not.toHaveBeenCalled()
    expect(serviceMocks.registerDraftMontagemPick).not.toHaveBeenCalled()
    expect(serviceMocks.finalizeDraftMontagem).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('keeps archive capability independent from Moderator draft management', async () => {
    authMock.canArchiveDrafts = false
    const wrapper = await mountView()

    expect(wrapper.find('[data-include-archived]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="archive-draft"]').exists()).toBe(false)
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canManageManualPresence')).toBe(true)
    wrapper.unmount()
  })

  it('requests the normal list with archived drafts explicitly disabled', async () => {
    const wrapper = await mountView()

    expect(serviceMocks.listDraftMontagens).toHaveBeenCalledWith({ status: '', includeArchived: false })
    wrapper.unmount()
  })

  it('archives with the observed version and selects the next visible draft', async () => {
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumo, resumoB])
      .mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemArchivingById.mockImplementation(async (id) => ({
      draft: id === montagemB.id ? montagemB : montagem,
      arquivadoEm: null,
      arquivadoPorUsuarioId: null,
      motivoArquivamento: null,
      acoes: [],
    }))
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Arquivar', '  fim da operação  ')

    expect(serviceMocks.archiveDraftMontagem).toHaveBeenCalledWith(montagem.id, 'fim da operação', 7)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect(realtimeMock.disconnected).toContain(montagem.id)
    wrapper.unmount()
  })

  it('reconciles archived A after selecting B while the archive request is pending without reopening A', async () => {
    let resolveArchive!: (value: { id: string; status: DraftMontagemStatus; arquivado: boolean; versaoEstado: number }) => void
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB]).mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.archiveDraftMontagem.mockReturnValueOnce(new Promise((resolve) => { resolveArchive = resolve }))
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Arquivar')
    await wrapper.get('textarea').setValue('encerrado')
    void wrapper.get('form').trigger('submit')
    await vi.waitFor(() => expect(serviceMocks.archiveDraftMontagem).toHaveBeenCalledTimes(1))
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('select', montagemB.id)
    await flushPromises()
    const archivedDetailCallsBeforeResolution = serviceMocks.getDraftMontagemAdminById.mock.calls.filter(([id]) => id === montagem.id).length
    await openReasonDialog(wrapper, 'Cancelar')

    resolveArchive({ id: montagem.id, status: 'Cancelada', arquivado: true, versaoEstado: 8 })
    await flushPromises()

    expect(serviceMocks.listDraftMontagens).toHaveBeenCalledTimes(2)
    expect((wrapper.vm as unknown as { visualMontagens: DraftMontagemResumo[] }).visualMontagens.map(({ id }) => id)).toEqual([montagemB.id])
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect(serviceMocks.getDraftMontagemAdminById.mock.calls.filter(([id]) => id === montagem.id)).toHaveLength(archivedDetailCallsBeforeResolution)
    expect(wrapper.get('[role="dialog"]').text()).toContain('Cancelar draft')
    wrapper.unmount()
  })

  it('keeps B selected when an archived realtime event for pending A arrives first', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB]).mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('select', montagemB.id)
    await flushPromises()

    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect(realtimeMock.disconnected.filter((id) => id === montagemB.id)).toHaveLength(0)
    wrapper.unmount()
  })

  it('selects the previous visible draft when archiving the last item', async () => {
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumo, resumoB])
      .mockResolvedValueOnce([resumo])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    serviceMocks.getDraftMontagemArchivingById.mockImplementation(async (id) => ({
      draft: id === montagemB.id ? montagemB : montagem,
      arquivadoEm: null,
      arquivadoPorUsuarioId: null,
      motivoArquivamento: null,
      acoes: [],
    }))
    const wrapper = await mountView()
    await wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('select', montagemB.id)
    await flushPromises()

    await confirmReasonAction(wrapper, 'Arquivar', 'encerrado')

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    wrapper.unmount()
  })

  it('shows an empty workspace after archiving the only visible draft', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo]).mockResolvedValueOnce([])
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Arquivar', 'encerrado')

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem).toBeNull()
    expect(wrapper.get('[data-draft-workspace]').text()).toContain('Nenhum draft selecionado')
    expect(wrapper.get('[data-empty-workspace]').attributes('tabindex')).toBe('-1')
    expect(document.activeElement).toBe(wrapper.get('[data-empty-workspace]').element)
    wrapper.unmount()
  })

  it('focuses the replacement workspace after archive reconciliation removes its opener', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB]).mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Arquivar', 'encerrado')

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('clears a denied archived detail and selects the first visible normal draft', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([archivedSummary, resumoB]).mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockRejectedValueOnce(new ServiceError([], 403))
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())

    const wrapper = await mountView()

    expect((wrapper.vm as unknown as { archiveAccessDenied: boolean }).archiveAccessDenied).toBe(true)
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { activeDraftId: string }).activeDraftId).toBe(montagemB.id)
    expect(wrapper.find('[data-draft-id="montagem-1"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('disconnects an archived realtime selection and reconciles when archive permission is lost', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
      .mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:includeArchived', true)
    await flushPromises()
    navigator.vm.$emit('select', montagem.id)
    await flushPromises()

    ;(wrapper.vm as unknown as { archiveAccessDenied: boolean }).archiveAccessDenied = true
    await flushPromises()

    expect(realtimeMock.disconnected).not.toContain(montagem.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { includeArchived: boolean }).includeArchived).toBe(false)
    wrapper.unmount()
  })

  it('loads an archived detail, disables operational actions, and restores without a reason', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens.mockResolvedValue([archivedSummary])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({
      draft: archived,
      arquivadoEm: '2026-07-26T12:00:00Z',
      arquivadoPorUsuarioId: 'admin-current',
      motivoArquivamento: 'Evento concluído',
      acoes: [{
        id: 'acao-1',
        tipo: 'Arquivamento',
        responsavelTipo: 'User',
        responsavelUsuarioId: 'admin-action',
        motivo: 'Solicitação administrativa',
        registradoEm: '2026-07-26T12:00:00Z',
      }],
    })
    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()

    expect(wrapper.findComponent({ name: 'DraftPreparationPanel' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'DraftVisualBoard' }).exists()).toBe(false)
    expect(wrapper.text()).toContain('Evento concluído')
    expect(wrapper.text()).toContain('admin-current')
    expect(wrapper.text()).toContain('admin-action')
    expect(wrapper.text()).toContain('Solicitação administrativa')
    expect(wrapper.get('[data-archived-workspace]').classes()).toContain('draft-archive-audit')
    const archiveValues = wrapper.findAll('[data-archive-value]')
    expect(archiveValues).toHaveLength(4)
    expect(archiveValues.every((value) => value.classes().includes('draft-archive-audit__value'))).toBe(true)
    expect(DraftsViewSource).toMatch(/\.draft-archive-audit__value\s*{[^}]*overflow-wrap:\s*anywhere/s)
    await openReasonDialog(wrapper, 'Restaurar')
    expect(wrapper.find('textarea').exists()).toBe(false)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(serviceMocks.restoreDraftMontagem).toHaveBeenCalledWith(montagem.id, 8)
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagem.id)
    wrapper.unmount()
  })

  it('renders a localized system actor without a blank or fake user id', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens.mockResolvedValue([archivedSummary])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({
      draft: archived,
      arquivadoEm: null,
      arquivadoPorUsuarioId: null,
      motivoArquivamento: null,
      acoes: [{
        id: 'acao-system',
        tipo: 'CancelamentoPorArquivamento',
        responsavelTipo: 'System',
        responsavelUsuarioId: null,
        motivo: null,
        registradoEm: '2026-07-26T12:00:00Z',
      }],
    })

    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()

    const text = wrapper.get('[data-archived-workspace]').text()
    expect(text).toContain('Sistema')
    expect(text).not.toContain('Responsável:')
    expect(text).not.toContain('00000000-0000-0000-0000-000000000000')
    wrapper.unmount()
  })

  it('reconciles restored A after selecting B while the restore request is pending without reopening A', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const restoredSummary = { ...archivedSummary, arquivado: false, versaoEstado: 9 }
    let resolveRestore!: (value: { id: string; status: DraftMontagemStatus; arquivado: boolean; versaoEstado: number }) => void
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
      .mockResolvedValueOnce([restoredSummary, resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())
    serviceMocks.restoreDraftMontagem.mockReturnValueOnce(new Promise((resolve) => { resolveRestore = resolve }))
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:includeArchived', true)
    await flushPromises()
    navigator.vm.$emit('select', montagem.id)
    await flushPromises()
    await openReasonDialog(wrapper, 'Restaurar')
    void wrapper.get('form').trigger('submit')
    await vi.waitFor(() => expect(serviceMocks.restoreDraftMontagem).toHaveBeenCalledTimes(1))
    navigator.vm.$emit('select', montagemB.id)
    await flushPromises()
    const restoredDetailCallsBeforeResolution = serviceMocks.getDraftMontagemArchivingById.mock.calls.length

    resolveRestore({ id: montagem.id, status: 'Cancelada', arquivado: false, versaoEstado: 9 })
    await flushPromises()

    expect(serviceMocks.listDraftMontagens).toHaveBeenCalledTimes(3)
    expect((wrapper.vm as unknown as { visualMontagens: DraftMontagemResumo[] }).visualMontagens.map(({ id }) => id)).toEqual([montagem.id, montagemB.id])
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect(serviceMocks.getDraftMontagemArchivingById).toHaveBeenCalledTimes(restoredDetailCallsBeforeResolution)
    wrapper.unmount()
  })

  it('reconciles a deferred restored canceled draft when the refreshed list omits it', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    let resolveRestore!: (value: { id: string; status: DraftMontagemStatus; arquivado: boolean; versaoEstado: number }) => void
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
      .mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())
    serviceMocks.restoreDraftMontagem.mockReturnValueOnce(new Promise((resolve) => { resolveRestore = resolve }))
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:includeArchived', true)
    await flushPromises()
    navigator.vm.$emit('select', montagem.id)
    await flushPromises()
    await openReasonDialog(wrapper, 'Restaurar')
    void wrapper.get('form').trigger('submit')
    await vi.waitFor(() => expect(serviceMocks.restoreDraftMontagem).toHaveBeenCalledTimes(1))

    resolveRestore({ id: montagem.id, status: 'Cancelada', arquivado: false, versaoEstado: 9 })
    await flushPromises()

    expect((wrapper.vm as unknown as { visualMontagens: DraftMontagemResumo[] }).visualMontagens).toEqual([resumoB])
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    expect(wrapper.find('[data-archived-workspace]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('reconciles a restored draft into the normal list when include archived is disabled while restore is pending', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const restoredSummary = { ...archivedSummary, arquivado: false, versaoEstado: 9 }
    let resolveRestore!: (value: { id: string; status: DraftMontagemStatus; arquivado: boolean; versaoEstado: number }) => void
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([restoredSummary, resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())
    serviceMocks.restoreDraftMontagem.mockReturnValueOnce(new Promise((resolve) => { resolveRestore = resolve }))
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:includeArchived', true)
    await flushPromises()
    navigator.vm.$emit('select', montagem.id)
    await flushPromises()
    await openReasonDialog(wrapper, 'Restaurar')
    void wrapper.get('form').trigger('submit')
    await vi.waitFor(() => expect(serviceMocks.restoreDraftMontagem).toHaveBeenCalledTimes(1))

    navigator.vm.$emit('update:includeArchived', false)
    await flushPromises()
    resolveRestore({ id: montagem.id, status: 'Cancelada', arquivado: false, versaoEstado: 9 })
    await flushPromises()

    expect((wrapper.vm as unknown as { includeArchived: boolean }).includeArchived).toBe(false)
    expect(serviceMocks.listDraftMontagens).toHaveBeenCalledTimes(4)
    expect((wrapper.vm as unknown as { visualMontagens: DraftMontagemResumo[] }).visualMontagens.map(({ id }) => id)).toEqual([montagem.id, montagemB.id])
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    wrapper.unmount()
  })

  it('republishes an archived cancellation without opening or sending a reason', async () => {
    const archived = {
      ...montagem,
      status: 'Cancelada' as const,
      arquivado: true,
      versaoEstado: 8,
      publicacoesDiscord: [{ tipo: 'Cancelamento' as const, status: 'Falha' as const }],
    }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens.mockResolvedValue([archivedSummary])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    const wrapper = await mountView()
    const trigger = wrapper.get('[data-testid="republish-cancellation"]')

    ;(trigger.element as HTMLButtonElement).focus()
    expect(document.activeElement).toBe(trigger.element)
    await trigger.trigger('click')
    await flushPromises()

    expect(serviceMocks.republishArchivedDraftCancellation).toHaveBeenCalledWith(montagem.id)
    expect(serviceMocks.republishDraftMontagemDiscordPublication).not.toHaveBeenCalled()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expectStageFocus(wrapper)
    wrapper.unmount()
  })

  it('removes an archived selection when the administrative filter is disabled', async () => {
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
      .mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({
      draft: archived,
      arquivadoEm: '2026-07-26T12:00:00Z',
      arquivadoPorUsuarioId: 'admin-1',
      motivoArquivamento: 'Evento concluído',
      acoes: [],
    })
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjectionB())
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    navigator.vm.$emit('update:includeArchived', true)
    await flushPromises()
    navigator.vm.$emit('select', montagem.id)
    await flushPromises()

    navigator.vm.$emit('update:includeArchived', false)
    await flushPromises()

    expect((wrapper.vm as unknown as { includeArchived: boolean }).includeArchived).toBe(false)
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagemB.id)
    wrapper.unmount()
  })

  it.each([401, 403, 409])('handles archive HTTP %s without applying a stale result', async (status) => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.archiveDraftMontagem.mockRejectedValueOnce(new ServiceError([], status))
    const wrapper = await mountView()

    await confirmReasonAction(wrapper, 'Arquivar', 'encerrado')

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.id).toBe(montagem.id)
    if (status === 403) {
      expect((wrapper.vm as unknown as { archiveAccessDenied: boolean }).archiveAccessDenied).toBe(true)
      expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canManageManualPresence')).toBe(true)
    }
    if (status === 409) expect(serviceMocks.listDraftMontagens).toHaveBeenCalledTimes(2)
    expect(wrapper.get('[role="alert"]')).toBeTruthy()
    wrapper.unmount()
  })

  it('disables only archive capability when includeArchived is rejected with 403', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo]).mockRejectedValueOnce(new ServiceError([], 403)).mockResolvedValueOnce([resumo])
    const wrapper = await mountView()

    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()

    expect((wrapper.vm as unknown as { includeArchived: boolean }).includeArchived).toBe(false)
    expect((wrapper.vm as unknown as { archiveAccessDenied: boolean }).archiveAccessDenied).toBe(true)
    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canManageManualPresence')).toBe(true)
    expect(wrapper.find('[data-include-archived]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('removes an archived ID-only realtime event and reconciles selection', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB]).mockResolvedValueOnce([resumoB])
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection())
    const wrapper = await mountView()

    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    await flushPromises()

    expect((wrapper.vm as unknown as { visualMontagens: DraftMontagemResumo[] }).visualMontagens.map(({ id }) => id)).toEqual([montagemB.id])
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagemB.id)
    wrapper.unmount()
  })

  it('queues the greatest remote canonical version while preserving a dirty board', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)
    await emitRealtime(montagem.id, { ...montagem, status: 'Aberta', nome: 'Remote 8', versaoEstado: 8 })
    await emitRealtime(montagem.id, { ...montagem, status: 'Aberta', nome: 'Remote 10', versaoEstado: 10 })
    await flushPromises()

    const vm = wrapper.vm as unknown as { boardDirty: boolean; boardBaseVersion: number; pendingCanonicalSnapshot: DraftMontagem }
    expect(vm.boardDirty).toBe(true)
    expect(vm.boardBaseVersion).toBe(7)
    expect(vm.pendingCanonicalSnapshot).toMatchObject({ nome: 'Remote 10', versaoEstado: expect.any(Number) })
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('remote-update')

    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await flushPromises()
    expect(wrapper.getComponent({ name: 'DraftVisualBoard' }).props('canonicalResetToken')).toBe(1)
    wrapper.unmount()
  })

  it('preserves dirty state and requires reconciliation after a layout 409', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    const open = adminProjection('Aberta')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(open)
    serviceMocks.saveDraftMontagemLayout.mockRejectedValueOnce(new ServiceError([], 409))
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem: { ...open, versaoEstado: 8 }, canCurrentUserPick: false })
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)
    board.vm.$emit('save', { versaoEstado: 7, times: [], livres: [], reservas: [] })
    await flushPromises()

    const vm = wrapper.vm as unknown as { boardDirty: boolean; requiresLayoutReconciliation: boolean; acceptedSaveVersion: number | null }
    expect(vm.boardDirty).toBe(true)
    expect(vm.requiresLayoutReconciliation).toBe(true)
    expect(vm.acceptedSaveVersion).toBeNull()
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('remote-update')
    wrapper.unmount()
  })

  it('blocks saving after keeping a newer remote version and reopens the same decision dialog', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)
    await emitRealtime(montagem.id, { ...montagem, status: 'Aberta', versaoEstado: 8 })
    await flushPromises()

    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await nextTick()
    expect(wrapper.get('[data-layout-reconciliation]').text()).toContain('Há uma versão mais recente aguardando sua decisão')

    board.vm.$emit('save', { versaoEstado: 7, times: [], livres: [], reservas: [] })
    await flushPromises()
    expect(serviceMocks.saveDraftMontagemLayout).not.toHaveBeenCalled()

    await wrapper.get('[data-testid="review-layout-update"]').trigger('click')
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('remote-update')
    wrapper.unmount()
  })

  it('accepts only a matching successful layout save version', async () => {
    const open = adminProjection('Aberta')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(open)
    serviceMocks.saveDraftMontagemLayout.mockResolvedValueOnce({ ...open, versaoEstado: 8 })
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)
    board.vm.$emit('save', { versaoEstado: 7, times: [], livres: [], reservas: [] })
    await flushPromises()

    expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledWith(open.id, expect.objectContaining({ versaoEstado: 7 }))
    expect(wrapper.getComponent({ name: 'DraftVisualBoard' }).props('acceptedSaveVersion')).toBe(8)
    wrapper.unmount()
  })

  it('guards draft switching and ignores a second archive intent', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? adminProjectionB() : adminProjection('Aberta'))
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)

    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('select', montagemB.id)
    await wrapper.get('[data-testid="archive-draft"]').trigger('click')
    await flushPromises()
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagem.id)
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('switch-draft')

    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await flushPromises()
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagemB.id)
    expect(wrapper.findComponent({ name: 'DraftReasonDialog' }).props('open')).toBe(false)
    wrapper.unmount()
  })

  it('guards a filter that would remove the current draft and restores trigger focus on keep', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)
    const search = wrapper.get('input[name="draft-search"]')
    ;(search.element as HTMLInputElement).focus()

    await search.setValue('segunda')
    await flushPromises()
    expect((wrapper.vm as unknown as { searchTerm: string }).searchTerm).toBe('')
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('draft-removed')

    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await nextTick()
    expect(document.activeElement).toBe(search.element)
    wrapper.unmount()
  })

  it('guards route leave and resolves it only after the explicit decision', async () => {
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('dirty-change', true, 7)

    const leaving = routeGuardMock.guard!()
    await nextTick()
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('route-leave')
    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await expect(leaving).resolves.toBe(false)

    const discarding = routeGuardMock.guard!()
    await nextTick()
    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await expect(discarding).resolves.toBe(true)
    wrapper.unmount()
  })

  it('guards remote removal and archive before either intent can discard the board', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    board.vm.$emit('dirty-change', true, 7)

    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    await flushPromises()
    expect((wrapper.vm as unknown as { selectedDraftId: string }).selectedDraftId).toBe(montagem.id)
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('draft-removed')
    await wrapper.get('[data-testid="keep-editing"]').trigger('click')

    await wrapper.get('[data-testid="archive-draft"]').trigger('click')
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('archive')
    expect(wrapper.findComponent({ name: 'DraftReasonDialog' }).props('open')).toBe(false)
    wrapper.unmount()
  })

  it('uses native beforeunload only while dirty and removes its listener on unmount', async () => {
    const add = vi.spyOn(window, 'addEventListener')
    const remove = vi.spyOn(window, 'removeEventListener')
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    const listener = add.mock.calls.find(([type]) => type === 'beforeunload')?.[1] as EventListener
    expect(listener).toBeTypeOf('function')

    const clean = new Event('beforeunload', { cancelable: true })
    listener(clean)
    expect(clean.defaultPrevented).toBe(false)

    wrapper.getComponent({ name: 'DraftVisualBoard' }).vm.$emit('dirty-change', true, 7)
    const dirty = new Event('beforeunload', { cancelable: true })
    listener(dirty)
    expect(dirty.defaultPrevented).toBe(true)

    wrapper.unmount()
    expect(remove).toHaveBeenCalledWith('beforeunload', listener)
    add.mockRestore()
    remove.mockRestore()
  })

  it('keeps a real board second edit dirty when a deferred save resolves', async () => {
    const original = editableAdminProjection()
    const accepted = {
      ...original,
      versaoEstado: 8,
      times: original.times.map((team) => ({ ...team, nome: 'First saved edit' })),
    }
    const save = deferred<DraftMontagem>()
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(original)
    serviceMocks.saveDraftMontagemLayout.mockReturnValueOnce(save.promise)
    const wrapper = await mountView({ realBoard: true })
    const teamName = wrapper.get('[data-team-id="time-1"] input')
    await teamName.setValue('First saved edit')
    await findButton(wrapper, 'Salvar layout').trigger('click')
    await vi.waitFor(() => expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledTimes(1))

    await teamName.setValue('Second pending edit')
    save.resolve(accepted)
    await flushPromises()

    expect((wrapper.get('[data-team-id="time-1"] input').element as HTMLInputElement).value).toBe('Second pending edit')
    expect((wrapper.vm as unknown as { boardDirty: boolean; boardBaseVersion: number }).boardDirty).toBe(true)
    expect((wrapper.vm as unknown as { boardBaseVersion: number }).boardBaseVersion).toBe(8)
    expect(findButton(wrapper, 'Salvar layout').attributes('disabled')).toBeUndefined()
    wrapper.unmount()
  })

  it('does not open a stale remote dialog when SignalR applies the accepted save before HTTP resolves', async () => {
    const original = editableAdminProjection()
    const accepted = {
      ...original,
      versaoEstado: 8,
      times: original.times.map((team) => ({ ...team, nome: 'Saved through SignalR' })),
    }
    const save = deferred<DraftMontagem>()
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(original)
    serviceMocks.saveDraftMontagemLayout.mockReturnValueOnce(save.promise)
    const wrapper = await mountView({ realBoard: true })
    await wrapper.get('[data-team-id="time-1"] input').setValue('Saved through SignalR')
    await findButton(wrapper, 'Salvar layout').trigger('click')
    await vi.waitFor(() => expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledTimes(1))
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem: accepted, canCurrentUserPick: false, serverNow: accepted.dataAtualizacao })

    await realtimeMock.handlers.get(montagem.id)?.({ montagem: accepted, serverNow: accepted.dataAtualizacao })
    await flushPromises()
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)

    save.resolve(accepted)
    await flushPromises()

    expect((wrapper.vm as unknown as { boardDirty: boolean }).boardDirty).toBe(false)
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)
    expect(findButton(wrapper, 'Salvar layout').attributes('disabled')).toBeDefined()
    wrapper.unmount()
  })

  it('keeps the dirty clone when an included draft is archived remotely', async () => {
    const openSummary = { ...resumo, status: 'Aberta' as const, modo: 'Manual' as const }
    const archived = { ...montagem, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    const archivedSummary = { ...resumo, status: 'Cancelada' as const, arquivado: true, versaoEstado: 8 }
    serviceMocks.listDraftMontagens.mockReset()
    serviceMocks.getDraftMontagemAdminById.mockReset()
    serviceMocks.getDraftMontagemArchivingById.mockReset()
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([openSummary, resumoB])
      .mockResolvedValueOnce([openSummary, resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()
    ;(wrapper.vm as unknown as { handleBoardDirtyChange: (dirty: boolean, baseVersion: number) => void }).handleBoardDirtyChange(true, 7)

    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    await flushPromises()
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('remote-update')
    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await flushPromises()

    expect((wrapper.vm as unknown as { boardDirty: boolean }).boardDirty).toBe(true)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.arquivado).toBe(false)
    expect(serviceMocks.getDraftMontagemArchivingById).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('discards the maximum canonical clone before reopening an included remotely archived draft', async () => {
    const openSummary = { ...resumo, status: 'Aberta' as const, modo: 'Manual' as const }
    const remote = { ...montagem, status: 'Aberta' as const, versaoEstado: 8, nome: 'Maximum canonical' }
    const archived = { ...remote, status: 'Cancelada' as const, arquivado: true, versaoEstado: 9 }
    const archivedSummary = { ...resumo, nome: remote.nome, status: 'Cancelada' as const, arquivado: true, versaoEstado: 9 }
    serviceMocks.listDraftMontagens.mockReset()
    serviceMocks.getDraftMontagemAdminById.mockReset()
    serviceMocks.getDraftMontagemArchivingById.mockReset()
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([openSummary, resumoB])
      .mockResolvedValueOnce([openSummary, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    const wrapper = await mountView()
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()
    serviceMocks.listDraftMontagens.mockImplementationOnce(async () => {
      const vm = wrapper.vm as unknown as { selectedMontagem: DraftMontagem; canonicalResetToken: number }
      expect(vm.selectedMontagem).toMatchObject({ nome: 'Maximum canonical', versaoEstado: 8 })
      expect(vm.canonicalResetToken).toBe(1)
      return [archivedSummary, resumoB]
    })
    const vm = wrapper.vm as unknown as {
      handleBoardDirtyChange: (dirty: boolean, baseVersion: number) => void
      queueCanonicalSnapshot: (draft: DraftMontagem) => void
    }
    vm.handleBoardDirtyChange(true, 7)
    vm.queueCanonicalSnapshot(remote)
    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    await flushPromises()

    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await flushPromises()
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem).toMatchObject({ nome: 'Maximum canonical', arquivado: true, versaoEstado: 9 }))
    expect(wrapper.find('[data-archived-workspace]').exists()).toBe(true)
    wrapper.unmount()
  })

  it('keeps draft B save ownership when draft A resolves after a discard and switch', async () => {
    const summaryA = { ...resumo, status: 'Aberta' as const, modo: 'Manual' as const }
    const summaryB = { ...resumoB, status: 'Aberta' as const, modo: 'Manual' as const, versaoEstado: 20 }
    const draftA = editableAdminProjection(7)
    const draftB = {
      ...editableAdminProjection(20),
      id: montagemB.id,
      nome: montagemB.nome,
      times: editableAdminProjection(20).times.map((team) => ({ ...team, id: 'time-b', nome: 'Original B' })),
    }
    const acceptedA = { ...draftA, versaoEstado: 8, times: draftA.times.map((team) => ({ ...team, nome: 'Saved A' })) }
    const acceptedB = { ...draftB, versaoEstado: 21, times: draftB.times.map((team) => ({ ...team, nome: 'Saved B' })) }
    const saveA = deferred<DraftMontagem>()
    const saveB = deferred<DraftMontagem>()
    serviceMocks.listDraftMontagens.mockReset()
    serviceMocks.getDraftMontagemAdminById.mockReset()
    serviceMocks.saveDraftMontagemLayout.mockReset()
    serviceMocks.listDraftMontagens.mockResolvedValue([summaryA, summaryB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(draftA)
    serviceMocks.getDraftMontagemAdminById.mockImplementation(async (id) => id === montagemB.id ? draftB : draftA)
    serviceMocks.saveDraftMontagemLayout.mockImplementation(async (id) => id === montagemB.id ? saveB.promise : saveA.promise)
    const wrapper = await mountView({ realBoard: true })

    await wrapper.get('[data-team-id="time-1"] input').setValue('Saved A')
    await findButton(wrapper, 'Salvar layout').trigger('click')
    await vi.waitFor(() => expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledWith(montagem.id, expect.anything()))
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('select', montagemB.id)
    await nextTick()
    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).toBe(montagemB.id))

    await wrapper.get('[data-team-id="time-b"] input').setValue('Saved B')
    await findButton(wrapper, 'Salvar layout').trigger('click')
    await vi.waitFor(() => expect(serviceMocks.saveDraftMontagemLayout).toHaveBeenCalledWith(montagemB.id, expect.anything()))
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce({ montagem: acceptedB, canCurrentUserPick: false, serverNow: acceptedB.dataAtualizacao })
    await realtimeMock.handlers.get(montagemB.id)?.({ montagem: acceptedB, serverNow: acceptedB.dataAtualizacao })
    await flushPromises()

    saveA.resolve(acceptedA)
    await flushPromises()
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)
    expect((wrapper.vm as unknown as { outstandingLayoutSave: { draftId: string; generation: number; baseVersion: number } | null }).outstandingLayoutSave).toMatchObject({ draftId: montagemB.id, baseVersion: 20 })

    saveB.resolve(acceptedB)
    await flushPromises()
    expect((wrapper.vm as unknown as { boardDirty: boolean }).boardDirty).toBe(false)
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('preserves a deferred archive through two keeps and applies it only after review and discard', async () => {
    const openSummary = { ...resumo, status: 'Aberta' as const, modo: 'Manual' as const }
    const remote = { ...montagem, status: 'Aberta' as const, nome: 'Remote pending', versaoEstado: 8 }
    const archived = { ...remote, status: 'Cancelada' as const, arquivado: true, versaoEstado: 9 }
    const archivedSummary = { ...openSummary, nome: remote.nome, status: 'Cancelada' as const, arquivado: true, versaoEstado: 9 }
    serviceMocks.listDraftMontagens.mockReset()
    serviceMocks.getDraftMontagemAdminById.mockReset()
    serviceMocks.getDraftMontagemArchivingById.mockReset()
    serviceMocks.listDraftMontagens
      .mockResolvedValueOnce([openSummary, resumoB])
      .mockResolvedValueOnce([openSummary, resumoB])
      .mockResolvedValueOnce([archivedSummary, resumoB])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection('Aberta'))
    serviceMocks.getDraftMontagemArchivingById.mockResolvedValue({ draft: archived, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] })
    const wrapper = await mountView()
    wrapper.getComponent({ name: 'DraftNavigator' }).vm.$emit('update:includeArchived', true)
    await flushPromises()
    const vm = wrapper.vm as unknown as {
      handleBoardDirtyChange: (dirty: boolean, baseVersion: number) => void
      queueCanonicalSnapshot: (draft: DraftMontagem) => void
      pendingLayoutIntentAction: (() => void | Promise<void>) | null
    }
    vm.handleBoardDirtyChange(true, 7)
    vm.queueCanonicalSnapshot(remote)

    await realtimeMock.archivedHandlers.get(montagem.id)?.(montagem.id)
    expect(vm.pendingLayoutIntentAction).toBeNull()
    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)
    expect((wrapper.vm as unknown as { deferredArchivedDraftId: string | null }).deferredArchivedDraftId).toBe(montagem.id)
    expect(serviceMocks.getDraftMontagemArchivingById).not.toHaveBeenCalled()

    await wrapper.get('[data-testid="review-layout-update"]').trigger('click')
    expect(wrapper.getComponent({ name: 'DraftUnsavedLayoutDialog' }).props('intent')).toBe('remote-update')
    expect(vm.pendingLayoutIntentAction).toBeTypeOf('function')
    await wrapper.get('[data-testid="keep-editing"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="keep-editing"]').exists()).toBe(false)
    expect((wrapper.vm as unknown as { deferredArchivedDraftId: string | null }).deferredArchivedDraftId).toBe(montagem.id)

    await wrapper.get('[data-testid="review-layout-update"]').trigger('click')
    await wrapper.get('[data-testid="discard-layout"]').trigger('click')
    await vi.waitFor(() => expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem).toMatchObject({ arquivado: true, versaoEstado: 9 }))
    expect((wrapper.vm as unknown as { deferredArchivedDraftId: string | null }).deferredArchivedDraftId).toBeNull()
    wrapper.unmount()
  })
})

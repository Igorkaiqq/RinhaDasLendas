// @vitest-environment happy-dom
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { computed, nextTick, ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemParticipante, DraftMontagemRealtimeState, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

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
const authMock = vi.hoisted(() => ({
  canManageDrafts: true,
  canArchiveDrafts: true,
  jogadorId: null as string | null,
  roles: ['Admin'] as string[],
  rolesRef: null as unknown as { value: string[] },
}))
const routeMock = vi.hoisted(() => ({ query: {} as Record<string, string> }))
const realtimeMock = vi.hoisted(() => ({
  handlers: new Map<string, (state: DraftMontagemRealtimeState) => void | Promise<void>>(),
  archivedHandlers: new Map<string, (draftMontagemId: string) => void | Promise<void>>(),
  reconnectHandlers: new Map<string, () => void | Promise<void>>(),
  disconnected: [] as string[],
}))

vi.mock('vue-router', () => ({ useRoute: () => routeMock }))

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
  listPlayers: vi.fn().mockResolvedValue([]),
  listEligibleCaptains: vi.fn().mockResolvedValue([]),
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
    connect = vi.fn().mockImplementation(async (onStateUpdated, onReconnected, onArchived) => {
      realtimeMock.handlers.set(this.id, onStateUpdated)
      realtimeMock.reconnectHandlers.set(this.id, onReconnected)
      realtimeMock.archivedHandlers.set(this.id, onArchived)
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
        responsavelUsuarioId: 'organizador-1',
        motivo: auditReason,
        registradoEm: '2026-07-19T12:00:00Z',
      },
    ],
    capitaesElegiveisIds: ['jogador-1', 'jogador-2'],
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
        responsavelUsuarioId: 'organizador-1',
        motivo: auditReason,
        registradoEm: '2026-07-20T12:00:00Z',
      },
    ],
  }
}

async function emitRealtime(
  id: string,
  projection: DraftMontagem,
  canCurrentUserPick = false,
  personalizedState: DraftMontagemRealtimeState | null = { montagem: projection, canCurrentUserPick, serverNow: projection.dataAtualizacao },
) {
  if (personalizedState) serviceMocks.getDraftMontagemRealtimeState.mockResolvedValueOnce(personalizedState)
  await realtimeMock.handlers.get(id)?.({ montagem: projection, canCurrentUserPick, serverNow: projection.dataAtualizacao })
}

async function mountView() {
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
        DraftVisualBoard: true,
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
    realtimeMock.handlers.clear()
    realtimeMock.archivedHandlers.clear()
    realtimeMock.reconnectHandlers.clear()
    realtimeMock.disconnected = []
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
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem, canCurrentUserPick: false })
    serviceMocks.listEligibleManualPresencePlayers.mockResolvedValue([{ id: 'jogador-2', nomeExibicao: 'Lux' }])
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

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledWith('montagem-1')
    expect(serviceMocks.getDraftMontagemById).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('loads only the public endpoint for a regular player', async () => {
    authMock.canManageDrafts = false
    const wrapper = await mountView()

    expect(serviceMocks.getDraftMontagemById).toHaveBeenCalledWith('montagem-1')
    expect(serviceMocks.getDraftMontagemAdminById).not.toHaveBeenCalled()
    expect(wrapper.text()).not.toContain('Republicar presença')
    wrapper.unmount()
  })

  it('falls back to the public endpoint and hides admin actions after an administrative 403', async () => {
    const ServiceError = (await import('@/services/draftMontagens')).DraftMontagemServiceError
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new ServiceError([], 403))
    const wrapper = await mountView()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledWith('montagem-1')
    expect(serviceMocks.getDraftMontagemById).toHaveBeenCalledWith('montagem-1')
    expect(wrapper.text()).not.toContain('Republicar presença')

    await emitRealtime('montagem-1', { ...montagem, status: 'Aberta' })
    await emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await flushPromises()

    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('reloads and preserves the administrative projection after a public realtime event', async () => {
    const wrapper = await mountView()
    const refreshedAdmin = adminProjection('Aberta', 'auditoria atualizada pelo realtime')
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
    resolveSecond(adminProjection('Finalizada', 'evento mais novo'))
    await secondRefresh
    resolveFirst(adminProjection('Aberta', 'evento antigo'))
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
      .mockResolvedValue(adminProjectionB('B realtime'))

    const oldARefresh = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await vi.waitFor(() => expect(resolveOldA).toBeTypeOf('function'))
    const openB = wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await vi.waitFor(() => expect(realtimeMock.disconnected).toContain('montagem-1'))
    const lateAEvent = emitRealtime('montagem-1', { ...montagem, status: 'Cancelada' }, false, null)
    resolveOldA(adminProjection('Finalizada', 'resposta antiga A'))
    await oldARefresh
    await lateAEvent

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).not.toBe('montagem-1')

    resolveB(adminProjectionB('B assumiu'))
    await openB
    await flushPromises()
    await vi.waitFor(() => expect(
      (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem.acoesAdministrativas[0]?.motivo,
    ).toBe('B realtime'))

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.id).toBe('montagem-2')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('B realtime')
    expect(selected.acoesAdministrativas.some((action) => action.motivo?.includes('A'))).toBe(false)
    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(5)
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
    const publicMutation = { ...montagem, status: 'Cancelada' as const }
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce(publicMutation)
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new ServiceError([], 403))

    await confirmReasonAction(wrapper, 'Cancelar', 'mutacao concluida')
    await emitRealtime('montagem-1', { ...publicMutation, status: 'Finalizada' })
    await flushPromises()

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect('acoesAdministrativas' in selected).toBe(false)
    expect(serviceMocks.getDraftMontagemAdminById).toHaveBeenCalledTimes(3)
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
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce({ ...montagem, status: 'Cancelada' })
    serviceMocks.getDraftMontagemAdminById
      .mockImplementationOnce(() => new Promise((resolve) => { resolveMutationRefresh = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveRealtimeRefresh = resolve }))

    const mutation = confirmReasonAction(wrapper, 'Cancelar', 'mutacao antiga')
    await vi.waitFor(() => expect(resolveMutationRefresh).toBeTypeOf('function'))

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.findAll('[role="status"]')).toHaveLength(1)
    expect(wrapper.get('[role="status"]').text()).toContain('cancelado')
    expect(serviceMocks.cancelDraftMontagem).toHaveBeenCalledTimes(1)

    const realtime = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    await vi.waitFor(() => expect(resolveRealtimeRefresh).toBeTypeOf('function'))
    resolveRealtimeRefresh(adminProjection('Finalizada', 'realtime novo'))
    await realtime
    resolveMutationRefresh(adminProjection('Cancelada', 'refresh antigo da mutacao'))
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

  it('keeps navigator selection while the new draft detail loads and after it fails', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    let rejectDetail!: (reason: Error) => void
    serviceMocks.getDraftMontagemAdminById.mockImplementationOnce(() => new Promise((_, reject) => { rejectDetail = reject }))

    navigator.vm.$emit('select', montagemB.id)
    await vi.waitFor(() => expect(rejectDetail).toBeTypeOf('function'))

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem).toBeNull()
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect(navigator.get(`[data-draft-id="${montagemB.id}"]`).attributes('aria-current')).toBe('true')

    rejectDetail(new Error('detail unavailable'))
    await flushPromises()

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem).toBeNull()
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect(navigator.get(`[data-draft-id="${montagemB.id}"]`).attributes('aria-current')).toBe('true')
    wrapper.unmount()
  })

  it('does not auto-open another draft after failed detail selection across successful and stale list completions', async () => {
    serviceMocks.listDraftMontagens.mockResolvedValueOnce([resumo, resumoB])
    const wrapper = await mountView()
    const navigator = wrapper.getComponent({ name: 'DraftNavigator' })
    serviceMocks.getDraftMontagemAdminById.mockRejectedValueOnce(new Error('detail unavailable'))

    navigator.vm.$emit('select', montagemB.id)
    await flushPromises()
    expect(navigator.props('selectedDraftId')).toBe(montagemB.id)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem).toBeNull()
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
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem).toBeNull()
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
    const payload = { timeId: 'time-1', jogadorSaiuId: 'outgoing-1', reservaEntrouId: 'reserve-1', motivo: null }
    const vm = wrapper.vm as unknown as { substituteReserve: (value: typeof payload) => Promise<void> }

    void vm.substituteReserve(payload)
    void vm.substituteReserve(payload)
    await nextTick()

    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledTimes(1)
    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledWith('montagem-1', payload)
    resolveSubstitution({ montagem: activeDraft, canCurrentUserPick: false, serverNow: activeDraft.dataAtualizacao })
    await flushPromises()
    ;(wrapper.vm as unknown as { adminAccessDenied: boolean }).adminAccessDenied = true
    await vm.substituteReserve(payload)
    expect(serviceMocks.substituteDraftMontagemReserve).toHaveBeenCalledTimes(1)
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

    await (wrapper.vm as unknown as { substituteReserve: (value: object) => Promise<void> }).substituteReserve({
      timeId: 'time-1',
      jogadorSaiuId: scenario.outgoingId ?? 'outgoing-1',
      reservaEntrouId: 'reserve-1',
      motivo: null,
    })

    expect(serviceMocks.substituteDraftMontagemReserve).not.toHaveBeenCalled()
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
    const reopened = { ...closed, status: 'PresencaAberta' as const, quantidadeTimes: 0, quantidadeReservas: 0 }
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
    const manual = { ...waiting, status: 'Aberta' as const, modo: 'Manual' as const }
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
    await emitRealtime('montagem-1', { ...waiting, cicloVersao: 'Legado' })
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
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('define-captains')
    panel.vm.$emit('draw-order')

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

  it('uses only backend-projected eligible starters for realtime captain selection', async () => {
    const secondPresence = {
      ...montagem.presencas[0]!,
      id: 'presenca-2',
      jogadorId: 'jogador-2',
      nomeExibicao: 'Lux',
      ordemConfirmacao: 2,
    }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue({
      ...adminProjection('PresencaEncerrada'),
      modo: 'TempoReal',
      cicloVersao: 'ModoPosPresenca',
      presencas: [...montagem.presencas, secondPresence],
      capitaesElegiveisIds: ['jogador-2'],
    })
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    expect(panel.props('eligibleCaptainIds')).toEqual(['jogador-2'])
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    await nextTick()
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-2'])
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
    const wrapper = await mountView()
    let panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })
    panel.vm.$emit('toggle-captain', 'jogador-1')
    panel.vm.$emit('toggle-captain', 'jogador-2')
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-1', 'jogador-2'])

    const changed = { ...initial, capitaesElegiveisIds: ['jogador-2', 'jogador-3'] }
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(changed)
    await emitRealtime('montagem-1', changed)
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

    resolveRefresh(adminProjection('Finalizada', 'refresh legítimo'))
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
      .mockResolvedValueOnce({ montagem: realtimeDraft, canCurrentUserPick: false, serverNow: montagem.dataAtualizacao })
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
      .mockResolvedValueOnce({ montagem: personalizedDraft, canCurrentUserPick: true, serverNow: '2026-07-25T12:10:00Z' })
    const wrapper = await mountView()

    await emitRealtime('montagem-1', { ...personalizedDraft, status: 'Finalizada' }, false, null)
    await flushPromises()
    dateNow.mockRestore()

    const board = wrapper.getComponent({ name: 'DraftVisualBoard' })
    expect(serviceMocks.getDraftMontagemRealtimeState).toHaveBeenCalledTimes(2)
    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem }).selectedMontagem.status).toBe('Aberta')
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
    resolveNewer({ montagem: { ...montagem, status: 'Finalizada' }, canCurrentUserPick: false, serverNow: '2026-07-25T12:05:00Z' })
    await newerRefresh
    resolveOlder({ montagem: { ...montagem, status: 'Aberta' }, canCurrentUserPick: true, serverNow: '2026-07-25T12:01:00Z' })
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
    const confirmed = { ...adminProjection(), presencas: [...montagem.presencas, ownPresence] }
    serviceMocks.confirmDraftMontagemPresence.mockResolvedValueOnce(confirmed)
    serviceMocks.getDraftMontagemAdminById
      .mockResolvedValueOnce(adminProjection())
      .mockResolvedValueOnce(adminProjection())
      .mockResolvedValue(confirmed)
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('confirm-presence')
    await flushPromises()
    expectStageFocus(wrapper)

    const closed = { ...confirmed, status: 'PresencaEncerrada' as const }
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

    expect(realtimeMock.disconnected).toContain(montagem.id)
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
})

// @vitest-environment happy-dom
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { nextTick, ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n } from '@/i18n'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

import DraftsView from './DraftsView.vue'
import DraftsViewSource from './DraftsView.vue?raw'
import DraftVisualBoardSource from '@/components/drafts/visual/DraftVisualBoard.vue?raw'
import DraftPreparationPanelSource from '@/components/drafts/DraftPreparationPanel.vue?raw'
import DraftDiscordPublicationPanelSource from '@/components/drafts/DraftDiscordPublicationPanel.vue?raw'

const serviceMocks = vi.hoisted(() => ({
  cancelDraftMontagem: vi.fn(),
  addManualDraftMontagemPresence: vi.fn(),
  getDraftMontagemById: vi.fn(),
  getDraftMontagemAdminById: vi.fn(),
  getDraftMontagemRealtimeState: vi.fn(),
  listDraftMontagens: vi.fn(),
  listEligibleManualPresencePlayers: vi.fn(),
  removeManualDraftMontagemPresence: vi.fn(),
  republishDraftMontagemDiscordPublication: vi.fn(),
  cancelDraftMontagemPresence: vi.fn(),
  closeDraftMontagemPresence: vi.fn(),
  confirmDraftMontagemPresence: vi.fn(),
}))
const authMock = vi.hoisted(() => ({ canManageDrafts: true }))
const realtimeMock = vi.hoisted(() => ({
  handlers: new Map<string, (state: { montagem: DraftMontagem }) => void | Promise<void>>(),
  reconnectHandlers: new Map<string, () => void | Promise<void>>(),
  disconnected: [] as string[],
}))

vi.mock('vue-router', () => ({ useRoute: () => ({ query: {} }) }))

vi.mock('@/services/authState', () => ({
  useAuthState: () => ({
    user: ref({ id: 'organizador-1', jogadorId: null }),
    hasPermission: () => authMock.canManageDrafts,
  }),
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
  defineDraftMontagemCaptains: vi.fn(),
  defineDraftMontagemPickOrder: vi.fn(),
  drawDraftMontagemCaptains: vi.fn(),
  finalizeDraftMontagem: vi.fn(),
  registerDraftMontagemPick: vi.fn(),
  saveDraftMontagemLayout: vi.fn(),
  startDraftMontagemRealtime: vi.fn(),
  substituteDraftMontagemReserve: vi.fn(),
}))

vi.mock('@/services/draftMontagemRealtime', () => ({
  DraftMontagemRealtimeConnection: class DraftMontagemRealtimeConnection {
    constructor(private readonly id: string) {}
    connect = vi.fn().mockImplementation(async (onStateUpdated, onReconnected) => {
      realtimeMock.handlers.set(this.id, onStateUpdated)
      realtimeMock.reconnectHandlers.set(this.id, onReconnected)
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
  dataCadastro: '2026-07-19T12:00:00Z',
  dataAtualizacao: '2026-07-19T12:00:00Z',
}

const resumo: DraftMontagemResumo = {
  id: montagem.id,
  nome: montagem.nome,
  status: montagem.status,
  modo: montagem.modo,
  tamanhoEquipe: montagem.tamanhoEquipe,
  quantidadeTimes: montagem.quantidadeTimes,
  quantidadeReservas: montagem.quantidadeReservas,
  presencaContinuadaManualmente: montagem.presencaContinuadaManualmente,
  dataCadastro: montagem.dataCadastro,
  dataAtualizacao: montagem.dataAtualizacao,
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

async function emitRealtime(id: string, projection: DraftMontagem) {
  await realtimeMock.handlers.get(id)?.({ montagem: projection })
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
    realtimeMock.handlers.clear()
    realtimeMock.reconnectHandlers.clear()
    realtimeMock.disconnected = []
    serviceMocks.listDraftMontagens.mockResolvedValue([resumo])
    serviceMocks.getDraftMontagemAdminById.mockResolvedValue(adminProjection())
    serviceMocks.getDraftMontagemById.mockResolvedValue(montagem)
    serviceMocks.getDraftMontagemRealtimeState.mockResolvedValue({ montagem })
    serviceMocks.listEligibleManualPresencePlayers.mockResolvedValue([{ id: 'jogador-2', nomeExibicao: 'Lux' }])
    serviceMocks.addManualDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.cancelDraftMontagem.mockResolvedValue(montagem)
    serviceMocks.removeManualDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.republishDraftMontagemDiscordPublication.mockResolvedValue(montagem)
    serviceMocks.cancelDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.closeDraftMontagemPresence.mockResolvedValue(montagem)
    serviceMocks.confirmDraftMontagemPresence.mockResolvedValue(montagem)
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
    const secondRefresh = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
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
    const openB = wrapper.findAll('button').find((button) => button.text().includes('Rinha de segunda'))!.trigger('click')
    await vi.waitFor(() => expect(realtimeMock.disconnected).toContain('montagem-1'))
    const lateAEvent = emitRealtime('montagem-1', { ...montagem, status: 'Cancelada' })
    resolveOldA(adminProjection('Finalizada', 'resposta antiga A'))
    await oldARefresh
    await lateAEvent

    expect((wrapper.vm as unknown as { selectedMontagem: DraftMontagem | null }).selectedMontagem?.id).not.toBe('montagem-1')

    resolveB(adminProjectionB('B assumiu'))
    await openB
    await flushPromises()

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
  })

  it('republishes presence with its current status and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar presença')
    expect(wrapper.text()).toContain('Status atual: falhou')
    expect((wrapper.vm as unknown as { saving: boolean }).saving).toBe(false)
    expect((wrapper.vm as unknown as { pendingReasonAction: { type: string } }).pendingReasonAction).toMatchObject({ type: 'republishPresence' })
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

    expect(workspace.get('h1').text()).toBe(montagem.nome)
    expect(workspace.get('[data-workspace-status]').text()).toBe(i18n.global.t(`drafts.status.${status}`))
    expect(workspace.get('[data-action-group="primary"]').findAll('button').length).toBeLessThanOrEqual(1)
    if (status === 'Finalizada' || status === 'Cancelada') {
      expect(workspace.get('[data-action-group="primary"]').find('button').exists()).toBe(false)
      expect(workspace.get('[data-action-group="danger"]').find('button').exists()).toBe(false)
    } else {
      expect(workspace.get('[data-action-group="danger"] button').attributes('data-variant')).toBe('destructive')
    }
    wrapper.unmount()
  })

  it('uses the application landmark and workspace identity only once', () => {
    expect(DraftsViewSource).not.toMatch(/<main\b/)
    expect(DraftVisualBoardSource).not.toContain('{{ localMontagem.nome }}')
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
  })

  it('preserves confirmation, cancellation, and both close-presence payloads through the preparation panel', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('confirm-presence')
    await flushPromises()
    expect(serviceMocks.confirmDraftMontagemPresence).toHaveBeenCalledWith('montagem-1')

    panel.vm.$emit('cancel-presence')
    await flushPromises()
    expect(serviceMocks.cancelDraftMontagemPresence).toHaveBeenCalledWith('montagem-1')

    panel.vm.$emit('close-presence', false)
    await flushPromises()
    panel.vm.$emit('close-presence', true)
    await flushPromises()
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenNthCalledWith(1, 'montagem-1', false, 5)
    expect(serviceMocks.closeDraftMontagemPresence).toHaveBeenNthCalledWith(2, 'montagem-1', true, 5)
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

    publications.vm.$emit('republish', 'TimesDefinidos')
    await flushPromises()
    expect(wrapper.get('[role="dialog"]').text()).toContain('Republicar times')
    wrapper.unmount()
  })

  it('does not expose management actions after permission denial', async () => {
    authMock.canManageDrafts = false
    const wrapper = await mountView()

    expect(wrapper.getComponent({ name: 'DraftPreparationPanel' }).props('canManage')).toBe(false)
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
    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual(['jogador-1'])
    wrapper.unmount()
  })

  it('ignores captain toggles outside the closed-presence state', async () => {
    const wrapper = await mountView()
    const panel = wrapper.getComponent({ name: 'DraftPreparationPanel' })

    panel.vm.$emit('toggle-captain', 'jogador-1')

    expect((wrapper.vm as unknown as { captainSelection: string[] }).captainSelection).toEqual([])
    wrapper.unmount()
  })
})

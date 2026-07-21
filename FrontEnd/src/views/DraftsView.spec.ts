// @vitest-environment happy-dom
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n } from '@/i18n'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

import DraftsView from './DraftsView.vue'
import DraftsViewSource from './DraftsView.vue?raw'

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
  cancelDraftMontagemPresence: vi.fn(),
  closeDraftMontagemPresence: vi.fn(),
  confirmDraftMontagemPresence: vi.fn(),
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
    expect(selected.publicacoesDiscord[0]?.id).toBe('publicacao-admin')
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
    expect(wrapper.text()).toContain('cancelado')
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
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('does not let an older mutation admin refresh overwrite a newer realtime refresh', async () => {
    const wrapper = await mountView()
    let resolveMutationRefresh!: (value: DraftMontagemAdmin) => void
    let resolveRealtimeRefresh!: (value: DraftMontagemAdmin) => void
    serviceMocks.cancelDraftMontagem.mockResolvedValueOnce({ ...montagem, status: 'Cancelada' })
    serviceMocks.getDraftMontagemAdminById
      .mockImplementationOnce(() => new Promise((resolve) => { resolveMutationRefresh = resolve }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveRealtimeRefresh = resolve }))

    const mutation = confirmReasonAction(wrapper, 'Cancelar', 'mutacao antiga')
    await vi.waitFor(() => expect(resolveMutationRefresh).toBeTypeOf('function'))
    const realtime = emitRealtime('montagem-1', { ...montagem, status: 'Finalizada' })
    resolveRealtimeRefresh(adminProjection('Finalizada', 'realtime novo'))
    await realtime
    resolveMutationRefresh(adminProjection('Cancelada', 'refresh antigo da mutacao'))
    await mutation

    const selected = (wrapper.vm as unknown as { selectedMontagem: DraftMontagemAdmin }).selectedMontagem
    expect(selected.status).toBe('Finalizada')
    expect(selected.acoesAdministrativas[0]?.motivo).toBe('realtime novo')
    wrapper.unmount()
  })

  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('republishes presence with its current status and exact reason', async () => {
    const wrapper = await mountView()

    await openReasonDialog(wrapper, 'Republicar presença')
    expect(wrapper.text()).toContain('Status atual: falhou')
    await wrapper.get('textarea').setValue('canal corrigido')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

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
})

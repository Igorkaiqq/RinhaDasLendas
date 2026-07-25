# UI Contracts: Redesenho do Fluxo de Draft

## Regra de fronteira

Todos os componentes descritos aqui são de apresentação. Eles não importam serviços, não consultam autenticação e não decidem regras de negócio. `DraftsView.vue` fornece dados, permissões e disponibilidade e processa todos os eventos.

## `DraftNavigator`

Responsabilidade: filtros, lista, seleção, loading, falha e vazio.

```ts
type DraftNavigatorItem = Omit<DraftMontagemResumo, 'status'> & {
  status: string
}

interface DraftNavigatorProps {
  drafts: readonly DraftNavigatorItem[]
  selectedDraftId: string | null
  searchTerm: string
  selectedStatus: DraftMontagemStatus | ''
  statusOptions: readonly DraftMontagemStatus[]
  loading: boolean
  loadFailed: boolean
  hasKnownDrafts: boolean
  canCreate: boolean
}

defineEmits<{
  'update:searchTerm': [value: string]
  'update:selectedStatus': [value: DraftMontagemStatus | '']
  select: [draftId: string]
  reset: []
  retry: []
  create: []
}>()
```

Garantias:

- item selecionado usa `aria-current`;
- status desconhecido é neutro;
- os sete status conhecidos usam variantes semânticas e status desconhecido usa variante neutra;
- data ausente é localizada;
- opções de filtro incluem os sete status suportados, na ordem do ciclo, incluindo `OrdemDefinida` e `Cancelada`;
- loading ou falha de atualização preservam itens conhecidos com feedback não bloqueante; skeleton é exclusivo da ausência de dados conhecidos;
- zero resultados por filtro oferece limpeza dos filtros sem criação;
- coleção genuinamente vazia oferece criação somente quando autorizada ou orientação neutra quando não autorizada.

## `DraftWorkspaceHeader`

Responsabilidade: contexto estável do draft e progresso.

```ts
type DraftWorkspacePresentation = Omit<DraftMontagem, 'status'> & {
  status: string
}

interface DraftWorkspaceHeaderProps {
  draft: DraftWorkspacePresentation
  confirmedCount: number
  finalTeamsPublicationStatus: DraftMontagemPublicacaoDiscordStatus | null
}
```

Slots:

- `primary-action` para no máximo uma ação primária;
- `secondary-actions` para ações auxiliares;
- `danger-action` para cancelamento.

Garantias:

- nome, data, status e contadores permanecem visíveis em todas as etapas;
- o componente posiciona os grupos separadamente; a view preenche no máximo um controle em `primary-action`, validado por teste;
- nome longo não sobrepõe métricas ou ações.

## `DraftPreparationPanel`

Responsabilidade: presença aberta, seleção de capitães e definição de ordem.

```ts
interface EligiblePresencePlayer {
  id: string
  nomeExibicao: string
}

interface DraftPreparationPanelProps {
  draft: DraftMontagem
  confirmedPresences: readonly DraftMontagemPresenca[]
  currentUserHasPresence: boolean
  canManage: boolean
  saving: boolean
  captainSelection: readonly string[]
  manualPresenceSearch: string
  selectedManualPresencePlayerId: string
  availableManualPresencePlayers: readonly EligiblePresencePlayer[]
}

defineEmits<{
  'confirm-presence': []
  'cancel-presence': []
  'close-presence': [continueWithLess: boolean]
  'update:manualPresenceSearch': [value: string]
  'search-manual-presence': []
  'update:selectedManualPresencePlayerId': [value: string]
  'add-manual-presence': []
  'remove-manual-presence': [jogadorId: string, jogadorNome: string]
  'toggle-captain': [jogadorId: string]
  'define-captains': []
  'draw-order': []
}>()
```

Garantias:

- participante apresenta identidade, origem e ação como regiões distintas;
- seleção de capitão usa estado pressionado acessível;
- eventos preservam IDs e nomes esperados pela view;
- nenhum serviço é chamado diretamente.

## `DraftDiscordPublicationPanel`

Responsabilidade: estados Discord subordinados e ações de republicação.

```ts
interface DraftPublicationPresentation {
  tipo: DraftMontagemPublicacaoDiscordTipo | string
  status: DraftMontagemPublicacaoDiscordStatus | string | null
}

interface DraftDiscordPublicationPanelProps {
  publications: readonly DraftPublicationPresentation[]
  canManage: boolean
  saving: boolean
}

defineEmits<{
  republish: [tipo: DraftMontagemPublicacaoDiscordTipo]
}>()
```

Garantias:

- status ausente ou desconhecido é neutro;
- republicação nunca usa variante primária da etapa;
- conteúdo permanece localizado.

## `DraftStateRail` e `DraftRail`

Contrato preservado:

- status do draft;
- status opcional da publicação final;
- lista ordenada de etapas no componente de layout.

Mudanças de contrato:

- estado aceita também `terminal` e `unknown`;
- etapa ativa recebe `aria-current="step"`;
- cancelamento não possui etapa operacional ativa;
- status desconhecido não é convertido para presença.

Mapeamento canônico:

| Status | Etapa marcada atual | Próxima ação fora do rail |
|--------|---------------------|---------------------------|
| `PresencaAberta` | Presença aberta | Confirmar ou encerrar presença |
| `PresencaEncerrada` | Presença encerrada | Definir capitães |
| `CapitaesDefinidos` | Capitães | Definir ordem |
| `OrdemDefinida` | Ordem | Iniciar escolhas conforme fluxo atual |
| `Aberta` | Escolhas | Escolher jogador ou finalizar |
| `Finalizada` | Finalização terminal | Nenhuma ação de avanço |
| `Cancelada` | Nenhuma; cancelamento terminal | Nenhuma ação de avanço |
| Outro | Nenhuma; estado neutro | Nenhuma inferida |

Discord é paralelo à progressão e nunca recebe `aria-current`.

## `DraftVisualBoard`

Props preservadas e complementares:

- `montagem`, `saving`, `canManage`, `currentPlayerId`;
- `canCurrentUserPick` recebe somente autorização personalizada;
- `serverClockOffsetMs` recebe a diferença calculada entre `serverNow` personalizado e `Date.now()`.

Eventos preservados:

- `save`, `startRealtime`, `pick`, `substituteReserve`, `drawCaptains`, `finalize`, `cancel`.

Garantias adicionais:

- `pick` continua emitindo somente `jogadorId`;
- `save` preserva formato e ordem funcional do payload;
- ordenação visual usa cópias por `time.ordem`;
- finalizado e cancelado não exibem controles mutáveis;
- progresso das escolhas usa `montagem.escolhas` sem alterar dados recebidos;
- preferências de rota permanecem visíveis nos jogadores disponíveis e detalhes, inclusive em layouts compactos;
- identidade geral do draft não é duplicada no board;
- broadcasts SignalR são apenas notificações de mudança: a view ignora sua autorização e projeção, consulta `getDraftMontagemRealtimeState` para o draft ativo e aplica o retorno personalizado sob as proteções de geração e versão de requisição;
- estado inicial, mutações realtime e reconexões também atualizam `montagem`, `canCurrentUserPick` e o offset do relógio a partir do retorno personalizado;
- o board calcula tempo restante e expiração com `Date.now() + serverClockOffsetMs`;
- antes de preservar o payload `pick(jogadorId)`, a view exige status e modo ativos, autorização personalizada, time atual existente, capitão do time igual a `turnoAtualCapitaoId` e ao jogador autenticado, jogador livre elegível e `turnoExpiraEm` posterior ao horário ajustado do servidor.

## Atualizações

Contrato editorial de `2026.07.3`:

- topo do registro;
- uma categoria `fix` e uma área `drafts`;
- um detalhe `selected-weekday-feedback`;
- link `AppRoutes.Settings`;
- somente `2026.07.3` destacada;
- mesmas folhas de tradução em `pt.json` e `en.json`;
- conteúdo não menciona o redesenho ainda não publicado.

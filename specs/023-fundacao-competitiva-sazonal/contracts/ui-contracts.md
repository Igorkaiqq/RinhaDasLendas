# UI Contracts: Fundação Competitiva Sazonal

## Rotas e navegação

| Rota | Tela | Acesso |
| --- | --- | --- |
| `/temporadas` | `SeasonsView` | Autenticado |
| `/series` | `SeriesView` | Autenticado |
| `/series/:serieId` | `SeriesDetailView` | Autenticado |
| `/partidas/:partidaId` | `MatchDetailView` | Autenticado |

`/partidas` deixa de usar `PlaceholderView` e redireciona para a listagem operacional de Séries ou apresenta a mesma área com foco nas Partidas, sem criar dois estados concorrentes. Navegação, títulos e breadcrumbs usam `appRoutes` e i18n.

## `SeasonScopeSelector`

Props:

```ts
type SeasonOption = {
  id: string
  nome: string
  estado: 'Planejada' | 'Ativa' | 'Encerrada'
}

type SeasonScope =
  | { mode: 'current' }
  | { mode: 'selected'; seasonIds: string[] }
  | { mode: 'all' }

seasons: SeasonOption[]
modelValue: SeasonScope
calendarConfigured: boolean
loading?: boolean
```

Emits:

```ts
'update:modelValue': [scope: SeasonScope]
```

Regras:

- `selected` exige ao menos um ID distinto;
- `all` e IDs explícitos nunca coexistem;
- o valor é serializado na URL com `temporadaIds` repetível ou `todas=true`;
- omissão representa `current`;
- sem Season ativa, `current` mostra estado de calendário não configurado e não faz fallback;
- detalhe por ID não aplica nem exibe filtro implícito sobre o recurso;
- controle completo exige no máximo três ações e uma confirmação;
- labels, descrição e nome acessível usam i18n.

## `SeasonsView`

Responsabilidades:

- carregar Seasons paginadas e a versão do calendário;
- destacar a Season ativa sem depender apenas de cor;
- separar planejadas, ativa e encerradas por filtros claros, sem esconder histórico;
- mostrar competições e rodadas da Season selecionada;
- expor criar, editar, ativar e encerrar somente quando as capabilities correspondentes existirem;
- manter dados conhecidos durante refresh e conflito.

Estados obrigatórios:

- skeleton inicial;
- calendário não configurado;
- vazio sem Seasons, com CTA para quem possui `CanManageSeasons`;
- vazio por filtro;
- erro recuperável com retry;
- `401` orienta autenticação;
- `403` remove somente controles não autorizados;
- `409` recarrega a versão e exige nova confirmação da ação destrutiva.

## `SeasonFormDrawer`

Campos verticais, com no máximo duas colunas no desktop:

```ts
nome: string
ano: number
ordemNoAno: number
dataInicio: string
dataFimExclusiva: string
```

Regras:

- criação começa em `Planejada` sem controle editável de estado;
- fim é apresentado como limite exclusivo em texto de ajuda localizado;
- salvar envia `Idempotency-Key`; editar também envia `If-Match`;
- erros de campo usam as mensagens localizadas do envelope vigente e o `messageCode` estável para o resumo;
- drawer fecha somente após resposta confirmada.

## `SeasonTransitionDialog`

Props:

```ts
action: 'activate' | 'close'
season: SeasonSummary
currentSeason?: SeasonSummary | null
calendarVersion: number
```

Emits:

```ts
confirm: []
cancel: []
```

Ativação informa explicitamente quando encerrará a Season atual. Encerramento informa que novas Séries oficiais não poderão ser confirmadas. Ambas enviam a ETag do calendário, nunca uma versão inferida.

## `CompetitionPanel`

Responsabilidades:

- listar competições da Season selecionada;
- publicar regra geral da Season para amistosos sem competição;
- identificar a competição do circuito diário;
- criar e editar competição com `CanManageCompetitions`;
- criar e reordenar rodadas;
- publicar versão de regras MD3 ou MD5, padrão ou Fearless;
- exibir número e instante da versão sem permitir edição retroativa.

Reordenação usa controles acessíveis por teclado além de drag-and-drop. Evento de quatro times nunca oferece Fearless.

## `SeriesView`

Responsabilidades:

- aplicar `SeasonScopeSelector` e filtros de tipo/estado;
- listar natureza, Season, competição/rodada, formato, modo, horário, lados, placar e estado;
- identificar amistoso textualmente e em badge;
- mostrar `calendarioConfigurado=false` sem misturar histórico;
- abrir `SeriesFormDrawer` com `CanManageMatches`.

No mobile, cada linha vira card mantendo Season, natureza, lados, placar, horário e estado. Alvos interativos têm pelo menos 44 px.

## `SeriesFormDrawer`

O formulário adapta campos pelo tipo:

```ts
seasonId: string
tipo: 'DiariaTemporaria' | 'ConfrontoOficial' | 'Amistoso'
competicaoId?: string
rodadaId?: string
versaoRegrasId: string
eventoId?: string
agendadaPara: string
dataLocal?: string
draftMontagemId?: string
ladoOrigemIds: [string, string]
```

Regras:

- oficial exige competição e rodada da mesma Season; pode ser agendado para Season planejada, mas iniciar ou confirmar resultado exige Season ativa;
- `DiariaTemporaria` mostra somente Drafts finalizados e obtém lados/capitães da origem;
- `ConfrontoOficial` mostra dois Times oficiais distintos;
- `Amistoso` sinaliza inelegibilidade oficial antes da confirmação;
- Evento restringe lados aos quatro Times e fixa draft padrão;
- formato e modo são exibidos a partir da versão publicada, não recalculados pelo cliente;
- não existe dependência de lista de presença.

## `SeriesDetailView`

Áreas:

- cabeçalho com natureza, Season, competição/rodada, versão de regras e estado;
- placar MD3/MD5 com vitórias necessárias;
- dois cards de lado com snapshot e capitães quando aplicável;
- lista ordenada de Partidas;
- painel Fearless com campeões bloqueados para ambos os lados;
- histórico de correções para `CanViewCompetitiveAudit`;
- ações permitidas pelo estado e capabilities.

Regras operacionais:

- iniciar/cancelar/anular usa `If-Match` e chave idempotente;
- criar próxima Partida não aparece depois de vencedor definido;
- `revisaoNecessaria` exibe alerta persistente e bloqueia confirmação/conclusão;
- amistoso mantém aviso permanente de que não alimenta projeções oficiais;
- Evento mostra regra de draft padrão sem toggle Fearless.

## `MatchDetailView` e `MatchOperationPanel`

Fluxo mínimo:

1. Selecionar cinco campeões distintos por lado usando `championId`.
2. Validar localmente duplicidade apenas para feedback imediato.
3. Enviar picks ao backend, que valida Fearless como fonte de verdade.
4. Confirmar vencedor e motivo `Normal` ou `Surrender`, ou registrar remake.
5. Atualizar placar, bloqueio e ETags a partir da resposta.

Remake exige escolha explícita entre preservar e desconsiderar picks, justificativa e confirmação. Correção exige justificativa, apresenta antes/depois e avisa que partidas posteriores podem ficar inconsistentes. Ao corrigir Série concluída de forma que deixe de existir vencedor, o dialog exige confirmação explícita de que a Série será anulada; sem essa confirmação, o envio é recusado.

## Capabilities

| Controle | Capability | SuperAdmin | Presidente | VicePresidente | Admin | Moderador | Capitão/Jogador |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Criar/editar/ativar/encerrar Season | `CanManageSeasons` | Sim | Sim | Não | Não | Não | Não |
| Criar competição/rodada/regra/evento | `CanManageCompetitions` | Não | Sim | Não | Condicional | Não | Não |
| Agendar/iniciar/cancelar Série; adicionar Partida/picks | `CanManageMatches` | Condicional | Sim | Não | Sim | Condicional | Não |
| Confirmar resultado, remake, anular ou corrigir | `CanFinalizeMatches` | Condicional | Sim | Não | Sim | Condicional | Não |
| Consultar auditoria detalhada | `CanViewCompetitiveAudit` | Sim | Sim | Não | Condicional | Não | Não |

O frontend usa capabilities somente para apresentação. O backend autoriza todas as requisições. VicePresidente não recebe capability esportiva automaticamente enquanto sua governança estiver pendente.

`Condicional` segue `research.md`: SuperAdmin somente em correção/anulação técnica; Admin configura estrutura antes de Série iniciada e consulta auditoria somente de Série/Partida; Moderador opera/finaliza apenas `DiariaTemporaria` e `Amistoso`, sem correção/anulação. Cada DTO retorna `acoesPermitidas`; a UI não deduz condição por papel.

Toda mutação de Partida envia a ETag da Série e recebe a nova ETag da Série. O cliente invalida simultaneamente detalhe da Partida, placar e Fearless; não mantém versões independentes capazes de confirmar duas Partidas concorrentes.

## Erros, concorrência e idempotência

- `400`: manter formulário e associar erros localizados aos campos;
- `401`: preservar dados não sensíveis e orientar login;
- `403`: remover a capability correspondente e manter leitura autorizada;
- `404`: apresentar recurso indisponível sem revelar existência protegida;
- `409` de versão: recarregar o agregado Série ou recurso correspondente, mostrar diferenças relevantes e pedir nova ação;
- `409` de idempotência: não reenviar com a mesma chave e conteúdo alterado;
- replay idempotente: aceitar a resposta como sucesso sem toast ou item duplicado.

## Acessibilidade, responsividade e design

- usar `AppShell`, `PageFrame`, `PageHeader`, cards, badges, drawer e dialogs existentes;
- usar apenas tokens de `docs/design/DESIGN_TOKENS.md`;
- não depender apenas de cor para estado, natureza ou conflito;
- foco visível, navegação por teclado e anúncio de erro/atualização;
- tabelas densas viram cards em 320-767 px;
- reduzir movimentos quando solicitado pelo sistema;
- fonte mono somente para placar, versão e IDs curtos;
- skeleton em carga inicial; evitar spinner como estado principal;
- não criar identidade visual competitiva paralela.

## Internacionalização

Todas as chaves adicionadas em `FrontEnd/src/i18n/locales/pt.json` devem existir em `en.json`, incluindo:

- navegação e títulos;
- labels, placeholders e ajuda dos formulários;
- estados, tipos, formatos e modos de draft;
- badges e avisos de amistoso/Fearless;
- confirmações de ativação, encerramento, cancelamento, remake, anulação e correção;
- vazios, skeleton labels, retry, toasts e erros;
- nomes acessíveis e instruções de reordenação.

Português deve usar acentuação correta. Nenhum enum ou `messageCode` deve ser exibido diretamente.

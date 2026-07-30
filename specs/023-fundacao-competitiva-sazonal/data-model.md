# Data Model: Fundação Competitiva Sazonal

## Visão geral

O modelo usa UUID, FKs explícitas, nomes `snake_case`, timestamps UTC e concorrência otimista. Datas de Season e `DiariaTemporaria` representam o calendário em `America/Sao_Paulo`. Enums são persistidos como strings estáveis. Entidades do domínio não dependem de EF Core, HTTP ou localização.

```text
CalendarioCompetitivo 1 --- N Season 1 --- N Competicao 1 --- N Rodada
                                  |               |
                                  N VersaoRegras  N VersaoRegras
                                  |
                                  N Serie 1 --- 2 LadoSerie
                                    |  |
                                    |  N Partida 1 --- N PickPartida
                                    |
                                    0..N RegistroCorrecaoCompetitiva

Season 1 --- N EventoCompetitivo 1 --- 4 EventoTime
EventoCompetitivo 1 --- N Serie
Serie N --- 0..1 DraftMontagem
LadoSerie N --- 0..1 Time
```

## Agregados

### CalendarioCompetitivo

Raiz técnica pequena que serializa a ativação do calendário.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Identificador singleton conhecido pela aplicação |
| `seasonAtivaId` | UUID? | FK para Season; nulo entre ciclos |
| `versao` | long | Incrementada em toda ativação ou encerramento que altere o calendário |
| `atualizadoEm` | instant | UTC |
| `atualizadoPorUsuarioId` | UUID | Ator autenticado |

Invariantes:

- existe no máximo um registro;
- `seasonAtivaId`, quando presente, aponta para Season `Ativa`;
- ativação exige a versão observada e altera calendário e Seasons na mesma transação.

### Season

Raiz do recorte competitivo temático.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na criação |
| `nome` | string | Obrigatório, trim, 1 a 120 caracteres |
| `ano` | int | 2009 a 9999 |
| `ordemNoAno` | int | Maior que zero; única por ano |
| `dataInicio` | date | Data local inclusiva |
| `dataFimExclusiva` | date | Deve ser posterior ao início |
| `estado` | `Planejada`, `Ativa`, `Encerrada` | Nova Season começa `Planejada` |
| `versao` | long | Concorrência otimista |
| `criadaEm`, `atualizadaEm` | instant | UTC |
| `ativadaEm`, `encerradaEm` | instant? | UTC, conforme transições |
| `criadaPorUsuarioId`, `atualizadaPorUsuarioId` | UUID | Ator autenticado |

Constraints:

- `unique (ano, ordem_no_ano)`;
- período `[data_inicio, data_fim_exclusiva)` não sobrepõe outra Season;
- índice único parcial garante no máximo uma linha com `estado='Ativa'`;
- Season encerrada não retorna ao fluxo normal;
- Season com Série confirmada não pode ter período alterado para excluir a Série.

Transições:

```text
Planejada -> Ativa -> Encerrada
```

Ativar uma planejada encerra a ativa anterior atomicamente. Encerramento manual limpa `seasonAtivaId`. Reabertura exige feature futura de correção.

### Competicao

Raiz organizacional dentro de uma Season.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na criação |
| `seasonId` | UUID | FK obrigatória |
| `nome` | string | Obrigatório, trim, 1 a 120 caracteres |
| `codigo` | string | Obrigatório, trim, 1 a 40; único na Season |
| `circuitoDiario` | bool | No máximo uma competição marcada por Season |
| `versao` | long | Concorrência otimista |
| `criadaEm`, `atualizadaEm` | instant | UTC |
| `criadaPorUsuarioId`, `atualizadaPorUsuarioId` | UUID | Ator autenticado |

Invariantes:

- pertence a exatamente uma Season;
- `DiariaTemporaria` só usa a competição `circuitoDiario=true` da mesma Season;
- não pode trocar de Season;
- regras publicadas e rodadas pertencem à própria Competição.

### Rodada

Entidade da Competição.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na criação |
| `competicaoId` | UUID | FK obrigatória |
| `nome` | string | Obrigatório, trim, 1 a 80 caracteres |
| `ordem` | int | Maior que zero e única na Competição |
| `versao` | long | Concorrência otimista |
| `criadaEm`, `atualizadaEm` | instant | UTC |

Reordenar atualiza de forma atômica todas as ordens afetadas e recusa versão obsoleta. Séries continuam ligadas ao mesmo `rodadaId`.

### VersaoRegras

Entidade imutável publicada no escopo da Season, opcionalmente para uma Competição específica.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na publicação |
| `seasonId` | UUID | FK obrigatória |
| `competicaoId` | UUID? | FK opcional; quando presente, deve pertencer à mesma Season |
| `numero` | int | Sequencial e único no escopo geral da Season ou da Competição |
| `formato` | `Md3`, `Md5` | Formato permitido |
| `modoDraft` | `Padrao`, `Fearless` | Regra aplicável a séries diretas |
| `publicadaEm` | instant | UTC |
| `publicadaPorUsuarioId` | UUID | Ator autenticado |

Depois da publicação não é editada. Uma mudança cria novo número. Série oficial exige regra da própria Competição; amistoso com competição usa regra dessa Competição e amistoso sem competição usa regra geral da Season. A Série referencia a versão escolhida e também captura formato/modo efetivos para leitura histórica simples.

### EventoCompetitivo

Raiz que organiza confrontos de quatro Times oficiais.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na criação |
| `seasonId` | UUID | FK obrigatória |
| `nome` | string | Obrigatório, trim, 1 a 120 caracteres |
| `modoDraft` | `Padrao` | Valor fixo |
| `versao` | long | Concorrência otimista |
| `criadoEm`, `atualizadoEm` | instant | UTC |
| `criadoPorUsuarioId`, `atualizadoPorUsuarioId` | UUID | Ator autenticado |

`EventoTime` contém `eventoId`, `timeId`, `ordem` e snapshot de nome/tag. Exatamente quatro Times ativos e distintos devem ser informados. Toda Série associada:

- pertence à mesma Season;
- usa lados contidos no Evento quando forem Times oficiais;
- usa `modoDraft=Padrao` e `fearlessHabilitado=false`;
- ainda não possui picks incompatíveis no momento da associação.

### Serie

Raiz operacional de uma disputa direta.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado na criação |
| `seasonId` | UUID | FK obrigatória e imutável |
| `competicaoId` | UUID? | Obrigatória para tipos oficiais |
| `rodadaId` | UUID? | Obrigatória para tipos oficiais |
| `versaoRegrasId` | UUID | Regra publicada aplicável |
| `eventoId` | UUID? | Contexto opcional |
| `draftMontagemId` | UUID? | Obrigatório somente em `DiariaTemporaria` |
| `tipo` | `DiariaTemporaria`, `ConfrontoOficial`, `Amistoso` | Discriminador estável |
| `formato` | `Md3`, `Md5` | Define 2 ou 3 vitórias necessárias |
| `modoDraft` | `Padrao`, `Fearless` | Evento força `Padrao` |
| `fearlessHabilitado` | bool | Derivado do modo e contexto |
| `estado` | enum | Ver transições |
| `agendadaPara` | instant | UTC |
| `dataLocal` | date? | Obrigatória em `DiariaTemporaria` |
| `ladoVencedorId` | UUID? | Derivado de partidas confirmadas |
| `revisaoNecessaria` | bool | Derivado de conflitos após correção |
| `versao` | long | Concorrência otimista |
| `criadaEm`, `atualizadaEm`, `concluidaEm` | instant | UTC conforme estado |
| `criadaPorUsuarioId`, `atualizadaPorUsuarioId` | UUID | Ator autenticado |

Invariantes por tipo:

| Tipo | Lados | Competição/rodada | Origem |
| --- | --- | --- | --- |
| `DiariaTemporaria` | Dois temporários | Obrigatórias e da mesma Season; competição é circuito diário | Draft finalizado, dois capitães e data local |
| `ConfrontoOficial` | Dois Times oficiais distintos | Obrigatórias e da mesma Season | Independente de presença |
| `Amistoso` | Dois lados compatíveis | Opcionais; quando presentes, pertencem à mesma Season | Independente de presença |

Outras invariantes:

- lados temporários e oficiais não são misturados sem contrato futuro explícito;
- formato e modo correspondem à `VersaoRegras`;
- Série pode ser agendada para Season `Planejada`, mas não pode iniciar nem confirmar resultado oficial antes de a Season estar `Ativa`;
- a data de `agendadaPara` em `America/Sao_Paulo` pertence a `[Season.dataInicio, Season.dataFimExclusiva)`; em `DiariaTemporaria`, ela é igual a `dataLocal`;
- Fearless só é permitido fora de Evento;
- apenas partidas `Confirmada` contam para placar;
- nenhuma partida competitiva adicional é confirmada depois do match point vencedor;
- Série com conflito Fearless não aceita nova confirmação nem conclusão;
- elegibilidade oficial é derivada do tipo e estado, nunca editável.

Transições:

```text
Agendada -> EmAndamento -> Concluida
Agendada -> Cancelada
EmAndamento -> Cancelada
Agendada|EmAndamento|Concluida -> Anulada (somente correção auditada)
```

Estados finais não retornam ao fluxo normal.

Correção não reabre estado final. Se fatos corrigidos de uma Série `Concluida` ainda produzirem vencedor, ela permanece concluída com resultado reconstruído. Se deixarem de produzir vencedor, a correção exige confirmação explícita e leva a Série diretamente a `Anulada`; uma operação inconclusiva sem essa confirmação é recusada.

Anular diretamente uma Partida decisiva de Série concluída segue a mesma regra: o request deve confirmar a anulação da Série quando o resultado deixar de ser conclusivo. Sem confirmação, a operação inteira é recusada e nenhum fato muda.

### LadoSerie

Entidade imutável de snapshot, exatamente duas por Série.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Identidade do lado dentro da Série |
| `serieId` | UUID | FK obrigatória |
| `ordem` | 1 ou 2 | Única na Série |
| `tipo` | `Temporario`, `TimeOficial` | Compatível com o tipo da Série |
| `origemId` | UUID | `DraftMontagemTime` ou `Time` |
| `nomeSnapshot` | string | Obrigatório |
| `tagSnapshot` | string? | Quando disponível |
| `capitaoJogadorId` | UUID? | Obrigatório em `DiariaTemporaria` |
| `capitaoNomeSnapshot` | string? | Histórico de apresentação |

`ParticipanteEsperadoSerie` registra `ladoSerieId`, `jogadorId`, `nomeSnapshot` e `ordem`. É obrigatório para `DiariaTemporaria`; em Time oficial representa somente a composição conhecida na criação e não substitui a futura escalação por Partida.

### Partida

Entidade interna da Série nesta feature.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado ao adicionar à Série |
| `serieId` | UUID | FK obrigatória e única associação |
| `ordem` | int | Maior que zero e única na Série |
| `estado` | `Rascunho`, `Confirmada`, `Remake`, `Anulada` | Estado competitivo mínimo |
| `ladoVencedorId` | UUID? | Obrigatório em confirmada; nulo em remake/anulada |
| `motivoTermino` | `Normal`, `Surrender`? | Presente em confirmada |
| `decisaoPicksRemake` | `PreservarPicks`, `DesconsiderarPicks`? | Obrigatória em remake antes da próxima confirmação |
| `conflitoFearless` | bool | Derivado após reconstrução |
| `versao` | long | Versão interna; mutações HTTP usam a versão do agregado Série |
| `criadaEm`, `atualizadaEm`, `confirmadaEm` | instant | UTC conforme estado |

Transições:

```text
Rascunho -> Confirmada
Rascunho -> Remake
Rascunho|Confirmada|Remake -> Anulada (correção auditada)
```

Uma Partida `Confirmada` registra exatamente um vencedor entre os lados. `Remake` não possui vencedor. `Anulada` não contribui para placar nem Fearless.

### PickPartida

Fato confirmado associado à Partida.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Gerado no registro |
| `partidaId` | UUID | FK obrigatória |
| `ladoSerieId` | UUID | Um dos dois lados da Série |
| `championId` | int | Positivo; chave numérica canônica da Riot |
| `ordem` | int | 1 a 5 por lado |
| `versaoFato` | int | Incrementada por correção |
| `valido` | bool | Versão substituída permanece histórica com `false` |
| `registradoEm` | instant | UTC |

Constraints da versão válida:

- exatamente cinco picks distintos por lado ao confirmar a Partida;
- campeão não se repete entre os dez picks;
- em Fearless, campeão não aparece no conjunto efetivo de partidas anteriores;
- hover ou intenção não é persistido como pick confirmado.

## Projeções derivadas

### ResultadoSerie

Calculado a partir de partidas `Confirmada`:

- vitórias por lado;
- vitórias necessárias (`2` para MD3, `3` para MD5);
- vencedor quando alcança o limite;
- quantidade de partidas válidas;
- elegibilidade oficial.

Não é fonte de verdade editável.

### BloqueioFearless

Conjunto reconstruído por Série e por ordem da próxima Partida:

- inclui picks válidos de partidas `Confirmada` anteriores;
- inclui picks de `Remake` anterior somente com `PreservarPicks`;
- exclui partidas `Anulada` e remake com `DesconsiderarPicks`;
- vale para ambos os lados;
- nunca atravessa Séries.

Pode ser materializado para leitura, mas a reconstrução a partir dos fatos é autoritativa.

### SelecaoSazonal

Value object de consulta:

- `Padrao`: Season ativa; sem ativa, resultado vazio e `calendarioConfigurado=false`;
- `Especifica`: um ou mais IDs distintos;
- `Todas`: sem filtro de Season.

`Especifica` e `Todas` são mutuamente exclusivos. Detalhes e auditoria por ID não aplicam seleção implícita.

## Auditoria e idempotência

### RegistroAuditoriaCompetitiva

Registro append-only para mudanças sensíveis.

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Identificador |
| `recursoTipo`, `recursoId` | string, UUID | Recurso alterado |
| `acao` | string code | Código estável |
| `atorUsuarioId` | UUID | Derivado da autenticação |
| `capacidade` | string | Policy avaliada |
| `justificativa` | string? | Obrigatória em correção, anulação e decisão excepcional |
| `valorAnterior`, `valorPosterior` | dados estruturados redigidos | Snapshot mínimo do fato alterado |
| `correlationId` | UUID | Caso de uso |
| `ocorridoEm` | instant | UTC |

Não contém texto localizado, segredo ou payload externo bruto. A consulta exige `CanViewCompetitiveAudit`.

### OperacaoIdempotente

| Campo | Tipo | Regra |
| --- | --- | --- |
| `id` | UUID | Identificador |
| `atorUsuarioId` | UUID | Parte da identidade da chave |
| `metodo`, `rota`, `chave` | string | Combinação única com ator |
| `requestHash` | string | Hash do conteúdo canônico |
| `statusCode` | int | Resposta original |
| `recursoTipo`, `recursoId` | string, UUID? | Referência do resultado |
| `respostaMinima` | string | Representação necessária ao replay, sem segredo |
| `criadaEm`, `expiraEm` | instant | Retenção de 90 dias |

Reutilizar a chave com hash diferente gera conflito. A limpeza só remove registros expirados.

## Eventos de domínio fundamentais

- `TemporadaCriada`
- `TemporadaAtivada`
- `TemporadaEncerrada`
- `CompeticaoCriada`
- `RodadaCriada`
- `RodadasReordenadas`
- `RegrasCompeticaoPublicadas`
- `EventoCriado`
- `SerieCriada`
- `SerieAdicionadaAoEvento`
- `SerieIniciada`
- `PartidaAdicionada`
- `PicksPartidaRegistrados`
- `PartidaConfirmada`
- `PartidaMarcadaComoRemake`
- `ResultadoSerieConfirmado`
- `SerieCancelada`
- `SerieAnulada`
- `FatoCompetitivoCorrigido`

`SerieCriada` contém `eventoId` opcional para preservar o contexto inicial. `SerieAdicionadaAoEvento` registra associações posteriores sem reinterpretar a criação da Série.

Eventos de domínio não carregam mensagens localizadas. Não há evento de integração/outbox nesta fatia sem consumidor assíncrono.

## Regras de exclusão e histórico

- não há exclusão física de Season, Competição, Rodada, Evento, Série, Partida, Pick confirmado ou Auditoria;
- alterações históricas usam correção versionada ou estado final;
- mudança de nome, membros ou capitão da origem não altera snapshots;
- arquivar `DraftMontagem` não rompe nem reativa o vínculo histórico;
- nenhuma mudança da Season ativa reatribui Série existente.

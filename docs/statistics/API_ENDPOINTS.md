# Proposta de endpoints REST v1 para o domínio competitivo

## Escopo e convenções

Esta referência propõe contratos para as features 023 e posteriores. A especificação e o plano de cada feature devem confirmar apenas o subconjunto que será implementado.

- Base: `/api/v1`.
- Rotas usam substantivos no plural.
- Identificadores são UUID.
- Datas e instantes usam ISO 8601; instantes são retornados em UTC.
- Comandos e consultas permanecem separados na Application.
- Controllers recebem, autorizam, encaminham e mapeiam respostas; não contêm regras competitivas.
- Entidades, agregados, eventos internos e payloads brutos de provedores nunca são retornados diretamente.
- Mensagens e códigos de erro seguem o envelope localizado vigente.
- `401` identifica ausência ou invalidade de autenticação; `403`, falta de capacidade; `404`, recurso inexistente ou deliberadamente oculto; `409`, conflito de estado, versão ou idempotência; `400`, entrada inválida.
- Endpoints de leitura autenticados podem receber `ETag`; mutações concorrentes usam `If-Match` quando alteram recurso versionado.

## Paginação, ordenação e filtros

Toda coleção usa:

```text
page=1
pageSize=20
```

- `page` começa em 1.
- `pageSize` aceita de 1 a 100.
- Ordenação usa `sort` com campos permitidos por endpoint e prefixo `-` para ordem descendente, por exemplo `sort=-dataInicio,nome`.
- Filtros desconhecidos ou valores inválidos retornam validação; não são ignorados silenciosamente.

Envelope padrão:

```json
{
  "page": 1,
  "pageSize": 20,
  "items": [],
  "totalItems": 0,
  "totalPages": 0
}
```

O contrato público usa `PaginatedResponseDto<TItem>`. `items` contém somente DTOs de projeção.

## Seleção sazonal comum

Consultas sazonais de coleção, agregação, ranking e histórico seguem um único contrato:

- omitir `temporadaIds` e `todas` seleciona a Season atual quando ela existe;
- `temporadaIds` é uma lista repetível, por exemplo `temporadaIds={id1}&temporadaIds={id2}`;
- `todas=true` remove o recorte de Season;
- `todas=true` e qualquer `temporadaIds` são mutuamente exclusivos e, quando combinados, retornam `400` localizado;
- `todas=false` sem `temporadaIds` equivale à omissão e seleciona a Season atual;
- resultados com uma ou mais Seasons retornam `seasonsIncluidas`, com `id`, `nome`, `versaoSeason` e `versaoRegras` de cada recorte;
- cada item ou série temporal identifica sua Season e as versões de regra e cálculo usadas, sem combinar versões silenciosamente.

Quando não existe Season ativa e a consulta omite `temporadaIds` e `todas`, a API retorna coleção vazia com `calendarioConfigurado=false` e `temporadaAtual=null`. Ela não usa a última Season, não infere Season pela data e não faz fallback para `todas=true`. A consulta histórica exige `temporadaIds` explícito ou `todas=true`.

Toda resposta sazonal de coleção acrescenta ao envelope `calendarioConfigurado` e `temporadaAtual`. Quando há Season ativa, `temporadaAtual` identifica `id`, `nome`, `versaoSeason` e `versaoRegras`; o campo não altera `seasonsIncluidas` de uma seleção explícita.

Rotas aninhadas em `/temporadas/{temporadaId}/...` são exclusivamente single-season: o identificador do caminho define o único recorte e os parâmetros `temporadaIds` e `todas` são rejeitados com `400`. Quando uma consulta precisa combinar Seasons, usa a coleção top-level correspondente, nunca repete a rota aninhada no cliente para montar uma resposta aparentemente única.

Endpoints de detalhe, correção e auditoria por ID, como `/series/{serieId}`, `/partidas/{partidaId}` e `/series/{serieId}/auditoria`, resolvem o recurso pelo identificador e **não** aplicam Season atual como filtro implícito. Quando autorizados, retornam a Season histórica do recurso e suas versões. Parâmetros sazonais nesses endpoints são rejeitados em vez de ocultar um recurso válido de outra Season.

## Escopo oficial

- `oficial=true` inclui somente partidas oficiais, encerradas, válidas e consolidadas no recorte solicitado.
- Rankings, rating e agregados oficiais aplicam esse escopo de forma obrigatória no servidor; o cliente não consegue habilitar amistosos nesses cálculos.
- Endpoints de histórico podem aceitar `tipo=oficial|amistoso|todos`. Cada item informa `tipoPartida`.
- Amistosos nunca alteram totais, rankings, elegibilidade, votação oficial ou rating oficial.

## Temporadas e competições

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/temporadas` | `PaginatedResponseDto<TemporadaResumoDto>` | autenticado |
| `GET` | `/temporadas/{temporadaId}` | `TemporadaDetalheDto` | autenticado |
| `POST` | `/temporadas` | `TemporadaDetalheDto` | `CanManageSeasons` |
| `PATCH` | `/temporadas/{temporadaId}` | `TemporadaDetalheDto` | `CanManageSeasons` |
| `POST` | `/temporadas/{temporadaId}/aberturas` | `TemporadaDetalheDto` | `CanManageSeasons` |
| `POST` | `/temporadas/{temporadaId}/encerramentos` | `TemporadaDetalheDto` | `CanManageSeasons` |
| `GET` | `/temporadas/{temporadaId}/competicoes` | `PaginatedResponseDto<CompeticaoResumoDto>` | autenticado |
| `POST` | `/temporadas/{temporadaId}/competicoes` | `CompeticaoDetalheDto` | `CanManageCompetitions` |
| `GET` | `/competicoes` | `PaginatedResponseDto<CompeticaoResumoDto>` | autenticado |
| `GET` | `/competicoes/{competicaoId}` | `CompeticaoDetalheDto` | autenticado |
| `PATCH` | `/competicoes/{competicaoId}` | `CompeticaoDetalheDto` | `CanManageCompetitions` |

Filtros de temporadas: `estado`, `dataDe`, `dataAte`, `search`. Filtros de competições: `formato`, `estado`.

O formato da competição declara `fearlessHabilitado`. A validação aceita Fearless somente em série direta entre dois lados. Evento com quatro times exige `fearlessHabilitado=false`.

`GET /temporadas/{temporadaId}/competicoes` usa somente a Season do caminho. `GET /competicoes` aceita a seleção sazonal comum para consultas multiseason. `CompeticaoResumoDto`, `CompeticaoDetalheDto` e `RodadaDto` identificam `seasonId`, `versaoSeason` e `versaoRegras`.

## Elencos, escalações e transferências

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/temporadas/{temporadaId}/elencos` | `PaginatedResponseDto<ElencoResumoDto>` | autenticado |
| `GET` | `/elencos` | `PaginatedResponseDto<ElencoResumoDto>` | autenticado |
| `GET` | `/elencos/{elencoId}` | `ElencoDetalheDto` | autenticado |
| `POST` | `/temporadas/{temporadaId}/elencos` | `ElencoDetalheDto` | `CanManageRosters` |
| `PATCH` | `/elencos/{elencoId}` | `ElencoDetalheDto` | `CanManageRosters` |
| `GET` | `/elencos/{elencoId}/movimentacoes` | `PaginatedResponseDto<MovimentacaoElencoDto>` | autenticado |
| `POST` | `/elencos/{elencoId}/movimentacoes` | `MovimentacaoElencoDto` | `CanManageTransfers` |
| `POST` | `/movimentacoes-elenco/{movimentacaoId}/aprovacoes` | `MovimentacaoElencoDto` | `CanApproveTransfers` |
| `POST` | `/movimentacoes-elenco/{movimentacaoId}/rejeicoes` | `MovimentacaoElencoDto` | `CanApproveTransfers` |
| `GET` | `/partidas/{partidaId}/escalacoes` | `EscalacoesPartidaDto` | autenticado |
| `PUT` | `/partidas/{partidaId}/escalacoes/{ladoId}` | `EscalacaoPartidaDto` | `CanManageLineups` |
| `GET` | `/jogadores/{jogadorId}/progresso-qualificacao` | `ProgressoQualificacaoJogadorDto` | autenticado |

`GET /temporadas/{temporadaId}/elencos` usa somente a Season do caminho e rejeita `temporadaIds` e `todas`. `GET /elencos` aceita a seleção sazonal comum, além de `timeId`, `jogadorId`, `estado`, `vigenteEm`, `dataDe` e `dataAte`.

Um Time oficial válido possui um capitão titular entre exatamente cinco titulares, um por rota, até três reservas e no máximo oito jogadores. Cada jogador mantém no máximo um vínculo oficial ativo no mesmo instante. Movimentações respeitam janelas configuradas em `America/Sao_Paulo`.

`ProgressoQualificacaoJogadorDto` retorna `seriesDiariasConfirmadas`, `seriesDiariasNecessarias=2`, `progresso`, `qualificado` e referências às séries qualificadoras. A qualificação é global, histórica e não consumível: somente séries `DiariaTemporaria` oficiais, concluídas e confirmadas contam; amistosos, cancelamentos e anulações não avançam o progresso. Atingir `2/2` permanece verdadeiro e não é vinculado, reservado, decrementado nem consumido por solicitação de contratação, transferência ou vínculo.

Uma movimentação exige `Idempotency-Key`, registra vigência e nunca altera o vínculo histórico capturado em partidas já consolidadas. Repetição idêntica devolve a movimentação original; a mesma chave com conteúdo divergente retorna `409` e não altera vínculos ou progresso.

## Partidas, séries e estatísticas

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/partidas` | `PaginatedResponseDto<PartidaResumoDto>` | autenticado |
| `GET` | `/partidas/{partidaId}` | `PartidaDetalheDto` | autenticado |
| `PATCH` | `/partidas/{partidaId}` | `PartidaDetalheDto` | `CanManageMatches` |
| `POST` | `/partidas/{partidaId}/encerramentos` | `PartidaDetalheDto` | `CanFinalizeMatches` |
| `POST` | `/partidas/{partidaId}/cancelamentos` | `PartidaDetalheDto` | `CanManageMatches` |
| `GET` | `/series/{serieId}` | `SerieDetalheDto` | autenticado |
| `GET` | `/series/{serieId}/auditoria` | `PaginatedResponseDto<SerieAuditoriaDto>` | `CanViewCompetitiveAudit` |
| `GET` | `/series` | `PaginatedResponseDto<SerieResumoDto>` | autenticado |
| `POST` | `/series` | `SerieDetalheDto` | `CanManageMatches` |
| `GET` | `/series/{serieId}/partidas` | `PaginatedResponseDto<PartidaResumoDto>` | autenticado |
| `POST` | `/series/{serieId}/partidas` | `PartidaDetalheDto` | `CanManageMatches` |
| `POST` | `/partidas/{partidaId}/picks` | `PicksPartidaDto` | `CanManageMatches` |
| `POST` | `/partidas/{partidaId}/resultados` | `ResultadoPartidaDto` | `CanFinalizeMatches` |
| `GET` | `/series/{serieId}/resultado` | `ResultadoSerieDto` | autenticado |
| `GET` | `/eventos/{eventoId}` | `EventoDetalheDto` | autenticado |
| `POST` | `/eventos` | `EventoDetalheDto` | `CanManageCompetitions` |
| `GET` | `/eventos/{eventoId}/series` | `PaginatedResponseDto<SerieResumoDto>` | autenticado |
| `GET` | `/partidas/{partidaId}/estatisticas` | `EstatisticasPartidaDto` | autenticado |
| `GET` | `/partidas/{partidaId}/integridade` | `IntegridadePartidaDto` | `CanReviewImports` |
| `POST` | `/partidas/{partidaId}/consolidacoes` | `ConsolidacaoPartidaDto` | `CanConsolidateOfficialStats` |
| `POST` | `/partidas/{partidaId}/correcoes` | `RevisaoPartidaDto` | `CanReviewImports` |

Filtros da listagem: seleção sazonal comum, `competicaoId`, `timeId`, `jogadorId`, `tipo`, `estado`, `dataDe`, `dataAte`, `consolidada`.

O encerramento captura escalações e vínculos válidos naquele instante. A consolidação exige integridade aprovada e é idempotente por versão dos dados normalizados.

`tipoSerie` é um discriminador obrigatório com valores `DiariaTemporaria`, `ConfrontoOficial` e `Amistoso`. `Evento` é um contexto separado que agrupa Séries e nunca é valor de `tipoSerie`.

- toda Série oficial exige `competicaoId` e `rodadaId` pertencentes à mesma Season da Série;
- `DiariaTemporaria` é Série oficial e exige `competicaoId`, `rodadaId`, `dataLocal`, dois capitães, `draftMontagemId` de origem e snapshots de dois lados temporários;
- `ConfrontoOficial` exige `competicaoId`, `rodadaId` e dois Times oficiais;
- `Amistoso` direto entre dois lados e fora de Evento pode definir `fearlessHabilitado=true`, mas permanece inelegível para votação, Score, Rating, rankings, recordes e qualquer projeção oficial;
- um Evento de quatro times contém referências às Séries pertencentes e impõe draft padrão a todas elas; toda Série com `eventoId` exige `fearlessHabilitado=false`, independentemente de seu tipo ou de possuir dois lados.

A `Serie` agrupa zero ou mais `Partidas`; cada `Partida` é a unidade individual da disputa e pertence a no máximo uma Série. Novas Partidas de uma Série são criadas somente por `/series/{serieId}/partidas`; `/partidas/{partidaId}` consulta ou altera a unidade já identificada. A feature 023 mantém o contrato mínimo de Série, Partidas, placar, resultado e picks confirmados necessários ao Fearless. A feature 025 amplia cada `PartidaDetalheDto` e seus participantes com dados técnicos e importados sem criar outra Série, outro resultado ou outra lista Fearless.

`PicksPartidaDto` contém somente picks confirmados por lado e a lista Fearless resultante. Hovers não são persistidos como bloqueio. `ResultadoPartidaDto` registra vencedor, placar e estado mínimo da Partida individual; `ResultadoSerieDto` deriva das Partidas válidas e preserva seus resultados individuais.

## Consultas estatísticas

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/estatisticas/resumo` | `EstatisticasResumoDto` | autenticado |
| `GET` | `/estatisticas/jogadores` | `PaginatedResponseDto<EstatisticaJogadorResumoDto>` | autenticado |
| `GET` | `/estatisticas/jogadores/{jogadorId}` | `EstatisticaJogadorDetalheDto` | autenticado |
| `GET` | `/estatisticas/times` | `PaginatedResponseDto<EstatisticaTimeResumoDto>` | autenticado |
| `GET` | `/estatisticas/times/{timeId}` | `EstatisticaTimeDetalheDto` | autenticado |
| `GET` | `/estatisticas/comparacoes/jogadores` | `ComparacaoJogadoresDto` | autenticado |

Parâmetros comuns: seleção sazonal comum, `competicaoId`, `rodadaDe`, `rodadaAte`, `funcao`, `lado`. O servidor fixa `oficial=true`.

A comparação recebe `jogadorId` repetido, de duas a quatro vezes. Todos os DTOs informam `amostra`, `elegivel`, `motivosInelegibilidade`, `atualizadoEm` e `versaoCalculo`.

## Votação MVP/SVP por série

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/series/{serieId}/votacao` | `VotacaoSerieDto` | participante autenticado ou gestor autorizado |
| `POST` | `/series/{serieId}/votacao/aberturas` | `VotacaoSerieDto` | `CanManageVoting` |
| `POST` | `/series/{serieId}/votacao/turnos/{turno}/votos` | `VotoSerieConfirmadoDto` | `CanVoteSeriesAwards` e participante elegível |
| `POST` | `/series/{serieId}/votacao/turnos/{turno}/encerramentos` | `ApuracaoTurnoDto` | `CanManageVoting` |
| `GET` | `/series/{serieId}/votacao/votos` | `PaginatedResponseDto<VotoSerieModeracaoDto>` | `CanModerateSeriesVotes` |
| `POST` | `/series/{serieId}/votacao/votos/{votoId}/invalidacoes` | `VotoSerieInvalidadoDto` | `CanInvalidateSeriesVotes` |
| `GET` | `/series/{serieId}/votacao/resultado` | `ResultadoVotacaoSerieDto` | autenticado, após publicação |
| `POST` | `/series/{serieId}/votacao/publicacoes` | `ResultadoVotacaoSerieDto` | `CanManageVoting` |

- Existe uma única votação por série concluída e validada; partidas da série não criam votações próprias.
- Somente quem participou de ao menos uma Partida válida da Série pode votar. Candidatos a MVP vêm do lado vencedor e candidatos a SVP, do lado derrotado; substitutos que participaram são incluídos e integrante sem participação é excluído.
- Autovoto é permitido. Cada eleitor tem uma escolha por categoria e turno.
- Empate no primeiro turno abre segundo turno somente entre empatados. Persistindo o empate, vence a maior média de Score elegível nas Partidas da Série; igualdade exata ou comparação inconclusiva produz co-vencedores.
- O voto exige `Idempotency-Key` e usa exclusivamente a identidade do principal autenticado.
- Admin+ pode invalidar um voto com motivo obrigatório. A invalidação preserva o voto no histórico, registra ator, turno, categoria, instante e impacto, e gera nova versão da apuração quando necessário.
- A consulta administrativa de votos é exclusiva de Admin+, exige `CanModerateSeriesVotes` e gera auditoria de acesso. `VotoSerieModeracaoDto` retorna `votoId` opaco, categoria, turno, candidato, estado e identidade mínima do eleitor somente para finalidade de moderação. Essa projeção não é pública, não integra resultados e não pode ser armazenada em cache compartilhado.
- A invalidação recebe o `votoId` opaco retornado pela consulta administrativa; não aceita identidade do eleitor como substituto do identificador.
- A resposta nunca retorna votos de terceiros nem parciais antes da publicação.
- O contrato da feature 026 deve refletir a decisão pendente sobre mudança de voto enquanto a janela estiver aberta.

## Rating

| Método | Rota | Resposta | Capacidade |
| --- | --- | --- | --- |
| `GET` | `/ratings/jogadores` | `PaginatedResponseDto<RatingJogadorResumoDto>` | autenticado |
| `GET` | `/ratings/jogadores/{jogadorId}` | `RatingJogadorDetalheDto` | autenticado |
| `GET` | `/ratings/jogadores/{jogadorId}/lancamentos` | `PaginatedResponseDto<LancamentoRatingDto>` | autenticado |
| `GET` | `/ratings/metodologia` | `MetodologiaRatingDto` | autenticado |
| `POST` | `/ratings/recalculos` | `RecalculoRatingDto` | `CanRecalculateRatings` |
| `GET` | `/ratings/recalculos/{recalculoId}` | `RecalculoRatingDto` | `CanRecalculateRatings` |

Parâmetros: seleção sazonal comum, `competicaoId`, `funcao`, `elegivel`, `faixa`. Nenhum endpoint retorna thresholds, amostra mínima ou soft reset como definitivos antes de sua aprovação.

O Rating-base é calculado e lançado por Partida oficial. Cada lançamento-base referencia obrigatoriamente `partidaId`. Ajustes derivados do resultado ou contexto da Série usam lançamentos separados e referenciam obrigatoriamente `serieId`; nunca são incorporados silenciosamente ao lançamento da Partida. Correções geram lançamentos compensatórios vinculados ao mesmo tipo de origem e à versão do algoritmo.

## Importação e Collector

| Método | Rota | Resposta | Capacidade ou escopo |
| --- | --- | --- | --- |
| `GET` | `/importacoes-partidas` | `PaginatedResponseDto<ImportacaoPartidaResumoDto>` | `CanReviewImports` |
| `GET` | `/importacoes-partidas/{importacaoId}` | `ImportacaoPartidaDetalheDto` | `CanReviewImports` |
| `POST` | `/importacoes-partidas` | `ImportacaoPartidaDetalheDto` | `CanImportMatchData` |
| `POST` | `/importacoes-partidas/{importacaoId}/validacoes` | `ValidacaoImportacaoDto` | `CanReviewImports` |
| `POST` | `/importacoes-partidas/{importacaoId}/consolidacoes` | `ConsolidacaoImportacaoDto` | `CanConsolidateOfficialStats` |
| `POST` | `/collector/importacoes-partidas` | `ColetaAceitaDto` | escopo `collector:match-import:create` |
| `GET` | `/collector/importacoes-partidas/{importacaoId}` | `ColetaEstadoDto` | escopo `collector:match-import:read-own` |

- `POST /collector/importacoes-partidas` exige `Idempotency-Key`, autenticação própria da credencial Collector e limite de taxa separado.
- A credencial não lista temporadas, jogadores, times, ratings, usuários ou importações de outros coletores.
- O Collector informa referências externas e payload de coleta; o servidor resolve identidade e autorização. Ele não escolhe ator humano nem marca dados como oficiais.
- A consolidação é sempre uma ação humana ou técnica com policy separada e auditada.

## DTOs de referência

### Temporada

- `TemporadaResumoDto`: `id`, `nome`, `dataInicio`, `dataFim`, `estado`, `quantidadeCompeticoes`, `versao`.
- `TemporadaDetalheDto`: campos do resumo, calendário, regras publicadas, instantes de abertura/encerramento e auditoria resumida.
- Requests: `CriarTemporadaRequestDto`, `AtualizarTemporadaRequestDto`.

### Elenco

- `ElencoResumoDto`: `id`, `temporadaId`, `versaoSeason`, `time`, `vigenteDesde`, `vigenteAte`, `quantidadeTitulares`, `quantidadeReservas`, `estado`, `versao`.
- `ElencoDetalheDto`: resumo, cinco titulares por rota, até três reservas, capitão titular, máximo de oito membros, janelas e movimentações recentes.
- `MovimentacaoElencoDto`: `id`, `jogador`, `timeOrigem`, `timeDestino`, `tipo`, `vigencia`, `estado`, `solicitadoPor`, `decididoPor`, `versao`.

### Partida e série

- `SerieResumoDto`: `id`, `tipoSerie`, Season e versões, `competicaoId`, `rodadaId`, estado, lados, placar, `eventoId`, `fearlessHabilitado` e versão.
- `SerieDetalheDto`: resumo, contagem e links de Partidas, resultado agregado, picks confirmados e bloqueios Fearless; para `DiariaTemporaria`, também data local, capitães, `draftMontagemId` e lados temporários.
- `EventoDetalheDto`: contexto próprio com quatro times, referências às Séries pertencentes, regra obrigatória de draft padrão, Season e versões. Não é uma Série e não possui `tipoSerie`.
- `PartidaResumoDto`: `id`, Season e versões, competição, rodada, tipo, estado, lados, placar, início, duração, consolidação e versão.
- `PartidaDetalheDto`: resumo da Partida individual, referência à Série, escalações e participantes capturados, picks, resultado, fonte, revisão e links permitidos. Não contém coleção de Partidas.
- `EstatisticasPartidaDto`: estatísticas normalizadas por time e jogador, objetivos e metadados de cálculo.

### Estatísticas e rating

- DTOs de resumo retornam valores já calculados, amostra e elegibilidade; nunca retornam entidades de partida para o cliente recalcular regras.
- DTOs detalhados incluem séries temporais limitadas, lista paginável de partidas de sustentação e versão do cálculo.
- `MetodologiaRatingDto` contém somente parâmetros publicados e vigentes, com versão e data de efeito.
- `LancamentoRatingDto` discrimina `Partida` e `AjusteSerie`: lançamento-base contém `partidaId` e `serieId=null`; ajuste de série contém `serieId` e `partidaId=null`. Cada efeito permanece em lançamento separado.

### Votação

- `VotacaoSerieDto`: série, categorias, turno atual, prazo, eleitores elegíveis, candidatos por lado e estado; não contém escolhas de terceiros.
- `VotoSerieConfirmadoDto`: voto, série, categoria, turno, candidato, instante e indicador de repetição idempotente.
- `VotoSerieModeracaoDto`: `votoId` opaco, série, categoria, turno, candidato, estado, identidade mínima do eleitor e metadados de moderação; disponível somente na projeção administrativa auditada.
- `ResultadoVotacaoSerieDto`: série, turno decisivo, votos válidos e invalidados agregados, critério de desempate, versão de Score, vencedor ou co-vencedores e versão da apuração.

### Importação

- `ImportacaoPartidaResumoDto`: `id`, chave externa mascarada, fonte, estado, totais de erros/avisos, criada em, criada por e versão.
- `ImportacaoPartidaDetalheDto`: resumo, correspondências normalizadas, diferenças, códigos de validação e histórico. Não inclui segredo, token ou payload bruto irrestrito.
- `ColetaAceitaDto`: `importacaoId`, `estado`, `recebidaEm`, `repetida`.

## Idempotência HTTP

- `Idempotency-Key` é obrigatória em importação Collector, movimentação de elenco, voto e demais comandos declarados pela feature.
- A chave é vinculada a credencial, rota e hash canônico do request.
- Repetição com a mesma chave, o mesmo ator e conteúdo canônico idêntico retorna exatamente o resultado original e cabeçalho `Idempotency-Replayed: true`, sem novo evento, voto, importação, movimentação ou auditoria equivalente.
- Repetição da mesma chave com qualquer conteúdo canônico divergente retorna `409`, identifica conflito de idempotência no envelope localizado e não executa nem altera a operação original.
- A retenção da chave deve cobrir a janela operacional da feature; o período exato pertence ao plano de cada feature e não autoriza retenção indefinida de payload bruto.

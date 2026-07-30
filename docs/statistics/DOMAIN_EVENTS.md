# Eventos de domínio da fundação competitiva

## Objetivo

Definir fatos de negócio, limites de transação e requisitos de idempotência para temporadas, elencos, partidas, votação, estatísticas e rating. Nomes representam eventos ocorridos no passado e não comandos.

## Regras arquiteturais

- Eventos de domínio nascem no agregado que protege a invariante.
- Domain não conhece HTTP, EF Core, PostgreSQL, MediatR, Riot, Discord ou DTOs.
- A Application coordena comandos e consultas e persiste o agregado uma única vez por caso de uso.
- Efeitos externos usam eventos de integração gravados em outbox na mesma transação da mudança de negócio.
- Publicação ocorre somente depois do commit.
- Consumidores devem ser idempotentes; entrega é pelo menos uma vez, não exatamente uma vez.
- Eventos persistidos são imutáveis. Correções produzem novos fatos compensatórios.
- Mensagens ao usuário não ficam no evento. Erros e notificações usam recursos localizados nas bordas da aplicação.

## Envelope de integração

Todo evento destinado a consumidores fora da transação contém:

| Campo | Finalidade |
| --- | --- |
| `eventId` | UUID único da ocorrência |
| `eventType` | nome estável e versionado, por exemplo `competitivo.partida-consolidada.v1` |
| `aggregateId` | UUID do agregado de origem |
| `aggregateType` | tipo lógico do agregado |
| `aggregateVersion` | versão confirmada após a mudança |
| `occurredAt` | instante UTC do fato |
| `correlationId` | correlação do caso de uso |
| `causationId` | comando ou evento que causou o fato |
| `actor` | referência mínima ao usuário, serviço ou sistema |
| `payload` | dados mínimos necessários ao consumidor |

O envelope não inclui token, e-mail, IP irrestrito, segredo, payload bruto de provedor nem texto localizado.

## Catálogo fundamental da feature 023

O catálogo implementado pela fundação competitiva sazonal é:

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

Eventos adicionais descritos neste documento pertencem às evoluções posteriores de elencos, ingestão, votação, estatísticas e rating.

## Temporada e competição

### `TemporadaCriada`

Ocorre após criação válida com nome e período. Não abre automaticamente a temporada.

### `TemporadaAtivada`

Ocorre quando a temporada passa a aceitar competições, elencos e operação oficial conforme suas regras publicadas.

### `TemporadaEncerrada`

Ocorre quando novas operações oficiais ficam bloqueadas. Consumidores podem iniciar consolidações finais, mas não devem alterar o fato de encerramento.

### `CompeticaoCriada`

Registra formato e vínculo com temporada. Fearless só pode constar habilitado para Série direta entre dois lados que não pertença a Evento.

### `RegrasCompeticaoPublicadas`

Registra uma versão imutável das regras que passam a orientar partidas posteriores. Mudança futura cria nova versão e não reinterpreta partidas consolidadas silenciosamente.

### `EventoCriado`

Registra contexto independente com Season, quatro times e regra obrigatória de draft padrão. Evento não é Série e não participa do discriminador `tipoSerie`.

### `SerieAdicionadaAoEvento`

Vincula uma Série ao Evento e impõe `fearlessHabilitado=false`. A associação é recusada se a Série já contém picks ou configuração Fearless incompatível; nenhuma Série interna pode excepcionar o draft padrão do Evento.

## Elencos, escalações e transferências

### `ElencoRegistrado`

Cria o vínculo sazonal entre time e seus membros iniciais. Um elenco válido possui capitão titular, exatamente cinco titulares por rota, até três reservas e no máximo oito jogadores.

### `JogadorAdicionadoAoElenco`

Registra função, vigência e origem da movimentação. Um jogador não pode possuir vínculos oficiais conflitantes no mesmo período, salvo regra futura explicitamente aprovada.

### `ProgressoQualificacaoGlobalAtualizado`

Registra a conclusão confirmada de uma das duas Séries diárias oficiais exigidas para qualificação global. O evento identifica jogador, Série qualificadora e progresso histórico `0..2`; amistoso, Série cancelada ou anulada não o produz. Atingir `2/2` é permanente, não consumível e independente de solicitação, transferência, vínculo ou Season posterior.

### `JogadorRemovidoDoElenco`

Encerra a vigência sem apagar o vínculo histórico.

### `TransferenciaSolicitada`

Registra intenção de movimentação. Não concede elegibilidade ao destino.

### `TransferenciaAprovada`

Confirma a vigência e produz as mudanças coerentes nos elencos envolvidos na mesma unidade transacional.

### `TransferenciaRejeitada`

Encerra a solicitação sem alterar vínculos.

### `EscalacaoPartidaConfirmada`

Captura jogadores, funções, lado e time representado para uma partida. A captura é histórica e não muda com transferências posteriores.

## Partidas, séries e dados competitivos

### `SerieCriada`

Registra o discriminador `DiariaTemporaria`, `ConfrontoOficial` ou `Amistoso`, a Season e suas versões, lados, formato e Partidas previstas. Evento é contexto separado e pode ser referenciado por `eventoId`, sem alterar o tipo da Série.

- toda Série oficial registra `competicaoId` e `rodadaId` da mesma Season;
- `DiariaTemporaria` é oficial e exige `competicaoId`, `rodadaId`, data local em `America/Sao_Paulo`, dois capitães, origem `DraftMontagem` e snapshots de lados temporários;
- `ConfrontoOficial` vincula `competicaoId`, `rodadaId` e Times oficiais;
- `Amistoso` direto e fora de Evento pode habilitar Fearless e produzir bloqueios operacionais de picks, mas permanece fora de votação, Score, Rating, rankings, recordes e todas as projeções oficiais.
- toda Série pertencente a Evento nasce com draft padrão e `fearlessHabilitado=false`.

Fora de evento, Fearless pertence somente a Série direta de dois lados e é derivado dos picks confirmados das Partidas válidas.

### `PartidaAdicionada`

Registra a adição da Partida ao agregado Série, com seu identificador e ordem.

### `PartidaIniciada`

Confirma escalações válidas e impede alterações incompatíveis durante o jogo.

### `PicksPartidaRegistrados`

Registra o conjunto confirmado de campeões, lados, Partida e Série. Em Série Fearless elegível, atualiza a projeção bilateral de bloqueios; hover ou intenção não produz este evento. Partida de qualquer Série pertencente a Evento não executa regra Fearless.

### `PartidaConfirmada`

Registra resultado operacional. Ainda não significa que estatísticas estejam validadas ou consolidadas.

### `ResultadoSerieConfirmado`

Deriva placar e vencedor das Partidas válidas sem apagar resultados individuais. O resultado mínimo pertence à fundação 023; a importação e as estatísticas detalhadas da 025 apenas o enriquecem ou corrigem por fluxo versionado.

### `PartidaCancelada`

Encerra a operação sem gerar estatísticas oficiais ou rating.

### `DadosPartidaImportados`

Registra recebimento e normalização inicial. Importação não torna dados oficiais.

### `DadosPartidaValidados`

Registra versão validada, avisos aceitos e correspondências resolvidas.

### `PartidaConsolidada`

Declara uma versão de dados apta a alimentar projeções. Para uma partida amistosa, consumidores oficiais devem ignorar o evento para estatísticas, ranking, votação e rating oficiais.

### `PartidaCorrigida`

Registra nova versão consolidável e referência à versão substituída. Consumidores recalculam por compensação ou reconstrução controlada, sem apagar lançamentos anteriores.

## Votação MVP/SVP por série

### `VotacaoSerieAberta`

Registra a única votação da Série concluída e validada, o primeiro turno, seu prazo, participantes eleitores e candidatos por lado. MVP recebe candidatos do lado vencedor; SVP, do lado derrotado. Somente quem participou de ao menos uma Partida válida pode votar ou ser candidato, e autovoto é permitido.

### `VotoSerieRegistrado`

Registra uma escolha por categoria e turno associada ao participante autenticado. O evento é confidencial, idempotente e não é distribuído a projeções públicas.

### `TurnoVotacaoSerieEncerrado`

Bloqueia novos votos no turno e fixa o conjunto contabilizável. Empate na primeira colocação abre segundo turno apenas para os candidatos empatados, sem transportar os votos anteriores.

### `SegundoTurnoVotacaoSerieAberto`

Registra candidatos empatados, categorias reabertas, eleitores participantes e novo prazo. Cada eleitor recebe uma nova escolha por categoria reaberta.

### `VotoSerieInvalidado`

Registra invalidação por Admin+ com voto, categoria, turno, ator, instante, motivo obrigatório e impacto na apuração. O voto permanece imutável no histórico e deixa de contar; a invalidação não cria voto em nome do eleitor.

### `VotosSerieConsultadosParaModeracao`

Registra acesso sensível de Admin+ à projeção administrativa, com ator, Série, instante, correlação, finalidade e quantidade consultada. O evento de auditoria não replica escolhas ou identidade do eleitor e não é publicado para consumidores públicos.

### `ResultadoVotacaoSeriePublicado`

Publica somente resultado agregado, participação e critério aplicado. Após empate no segundo turno, usa a média de Score elegível do candidato nas Partidas da Série; igualdade exata ou comparação inconclusiva gera co-vencedores. Não publica autoria individual.

## Rating e projeções

### `CalculoRatingSolicitado`

Evento de integração interno que referencia uma Partida oficial consolidada por `partidaId`, Season e versão do algoritmo.

### `RatingJogadorAtualizado`

Registra valor anterior, variação, valor novo, `partidaId` da Partida oficial de origem e versão do algoritmo. O lançamento-base ocorre por Partida, usa `serieId=null` e nunca representa um total da Série.

### `AjusteRatingSerieAplicado`

Registra bônus, reembolso ou outro ajuste de Série aprovado como lançamento separado, com `serieId`, `partidaId=null`, regra e versão próprias. Não modifica nem substitui lançamentos-base das Partidas da Série.

### `RatingJogadorCompensado`

Corrige efeito de partida revisada ou invalidada sem editar o lançamento original.

### `ClassificacaoRatingAtualizada`

Atualiza uma projeção de ranking após os lançamentos necessários. Faixas S-F só são emitidas quando thresholds estiverem formalmente aprovados e versionados.

### `ProjecaoEstatisticaAtualizada`

Indica conclusão da projeção para um recorte e versão. Não é fonte de verdade para invariantes de partida.

## Fluxos principais

### Consolidação de partida oficial

```text
DadosPartidaImportados
  -> DadosPartidaValidados
  -> PartidaConsolidada
  -> ProjecaoEstatisticaAtualizada
  -> CalculoRatingSolicitado
  -> RatingJogadorAtualizado (um lançamento-base por partida e participante elegível)
  -> AjusteRatingSerieAplicado (lançamento separado, quando aplicável)
  -> ClassificacaoRatingAtualizada
```

### Partida amistosa

```text
PartidaConfirmada
  -> DadosPartidaImportados
  -> DadosPartidaValidados
  -> PartidaConsolidada
  -> histórico identificado como amistoso
```

Não são gerados efeitos em estatísticas oficiais, votação oficial ou rating oficial.

Uma Série `Amistoso` pode agrupar várias Partidas amistosas; processar qualquer Partida ou o resultado agregado da Série mantém todo o grupo fora de Score, Rating, MVP/SVP, rankings e recordes oficiais.

### Correção

```text
PartidaCorrigida
  -> reconstrução da projeção afetada
  -> RatingJogadorCompensado
  -> novos lançamentos, se aplicável
  -> ClassificacaoRatingAtualizada
```

## Idempotência e ordenação

### Produção

- O agregado rejeita comando repetido quando o estado pretendido já foi confirmado.
- A transação grava estado, versão, auditoria e outbox de forma atômica.
- Restrição única impede mais de um evento de integração para a mesma combinação de agregado, versão e tipo.
- Importações usam chave composta por credencial, rota, `Idempotency-Key` e hash canônico.
- Repetição com a mesma chave, ator e conteúdo canônico idêntico retorna o resultado confirmado sem produzir novo fato equivalente.
- A mesma chave com conteúdo canônico divergente retorna conflito e não modifica estado, auditoria, outbox ou resultado original.

### Consumo

- Cada consumidor mantém inbox por `consumerName` e `eventId`.
- Evento já concluído retorna sucesso sem repetir efeito.
- Evento com versão anterior à aplicada é ignorado com registro técnico.
- Lacuna de versão interrompe o consumidor daquele agregado e agenda nova tentativa; não aplica fora de ordem.
- Falha não marca inbox como concluída.
- Efeitos em lote usam chave determinística por evento e participante, evitando duplicação parcial no retry.

### Reconstrução

- Projeções estatísticas podem ser reconstruídas a partir de partidas oficiais consolidadas e versões publicadas.
- Backfill não reutiliza identidade de eventos históricos nem finge ter ocorrido no passado; emite correlação própria e preserva proveniência.
- A política efetiva de backfill permanece uma decisão pendente registrada em `OPEN_DECISIONS.md`.

## Observabilidade e auditoria

- Logs técnicos registram `eventId`, `correlationId`, tipo, agregado, versão, tentativa e resultado.
- Métricas mínimas: atraso da outbox, tentativas, dead letters, lacunas de versão e duração de projeção.
- A auditoria humana registra ação sensível, ator confiável, recurso, instante e resultado.
- Nenhum log contém credencial Collector, token, payload bruto irrestrito ou voto individual.

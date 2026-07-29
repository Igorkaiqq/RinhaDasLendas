# Rinha Rating e Elos

## Objetivo

O Rinha Rating representa progressão competitiva acumulada. Ele é diferente do Rinha Score: Score mede performance contextual; Rating mantém um saldo histórico movimentado principalmente pelo resultado de cada partida oficial, com ajustes de força e impacto.

## Ledger imutável

Toda alteração ocorre por lançamento em ledger. O Rating atual é a soma ordenada dos lançamentos válidos, respeitando resets e piso.

Um lançamento deve identificar:

- jogador;
- partida, série e Season de origem, quando aplicável;
- tipo do lançamento;
- `variacaoNominal` assinada;
- `variacaoAplicada` assinada;
- `parcelaLimitadaPeloPiso` não negativa;
- componentes de resultado, força e impacto;
- versão da fórmula;
- saldo anterior e posterior projetados;
- correção ou lançamento substituído;
- instante e motivo administrativo, quando houver.

Lançamentos confirmados não são editados nem excluídos. Correções usam estorno e novo lançamento ou outra compensação versionada.

## Movimento principal por partida

Cada partida oficial elegível produz um lançamento principal para cada participante:

- vitória na partida: ganho entre **+10 e +20**;
- derrota na partida: perda entre **-5 e -20**.

Essas faixas são decisões aprovadas para `variacaoNominal`. Força e impacto somente escolhem o valor nominal dentro da faixa correspondente. Uma derrota individual sempre tem `variacaoNominal` entre `-5` e `-20` e nunca vira ganho.

Ao aplicar o piso zero:

- `variacaoAplicada` é a parcela da variação nominal que efetivamente altera o saldo;
- `parcelaLimitadaPeloPiso` registra a magnitude que não pôde ser aplicada;
- em vitória, `variacaoAplicada = variacaoNominal` e `parcelaLimitadaPeloPiso = 0`;
- em derrota com saldo suficiente, `variacaoAplicada = variacaoNominal` e `parcelaLimitadaPeloPiso = 0`;
- em derrota com Rating baixo, a magnitude de `variacaoAplicada` pode ser menor que a nominal, e a diferença fica em `parcelaLimitadaPeloPiso`;
- para variações negativas, a identidade contábil é `variacaoNominal = variacaoAplicada - parcelaLimitadaPeloPiso`.

Proposta versionada para calibração:

### Vitória

`ganho = 10 + força_do_adversário + impacto`, limitado a `+20`.

- força do adversário: `0..6`, premiando vitória contra lado mais forte;
- impacto: `0..4`, derivado de Score confiável da partida.

### Derrota

`perda = -(5 + expectativa + impacto_negativo)`, limitada a `-20`.

- expectativa: `0..9`, aumentando a perda quando o lado era claramente favorito e reduzindo-a contra lado mais forte;
- impacto negativo: `0..6`, reduzido por boa performance individual confiável.

As fórmulas, escalas internas e distribuição entre força e impacto são propostas versionadas para calibração. As faixas finais por Partida são decisões aprovadas. Consulte [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md).

## Força do confronto

A força compara Ratings anteriores à Partida, usando as escalações confirmadas nela e sem incorporar o próprio resultado em avaliação. A versão deve declarar como agrega jogadores, trata reservas e lida com Rating ausente.

Não se usa elo nominal sozinho para calcular força; o elo é derivado do Rating.

## Impacto

Impacto usa Rinha Score da partida somente quando a confiança atende ao mínimo da versão. Sem Score confiável, aplica-se componente neutro, nunca uma penalidade por ausência de dados.

O impacto ajusta a variação dentro da faixa, mas não converte derrota em ganho nem vitória em perda.

## Lançamentos posteriores de série

Depois que todos os lançamentos principais das partidas forem confirmados, uma série pode produzir lançamentos adicionais de:

- **bônus**, para vitória em condição definida pelo regulamento;
- **reembolso**, para reduzir parte de uma perda diante de contexto ou impacto excepcional.

Invariantes:

- bônus e reembolso são lançamentos separados e auditáveis;
- bônus e reembolso não alteram retroativamente os lançamentos por partida;
- bônus de série e fórmula de elegibilidade são propostas versionadas;
- reembolso reduz o saldo negativo de uma série perdida e pode levá-lo a zero;
- o saldo líquido de uma série perdida, somando partidas e ajustes posteriores, nunca pode ser positivo;
- se o subtotal das partidas de uma série perdida já for positivo, nenhum bônus ou reembolso positivo é aplicado e o fechamento versionado da série deve limitá-lo a zero por lançamento auditável;
- uma derrota individual preserva `variacaoNominal` negativa e nunca produz `variacaoAplicada` positiva, mesmo quando existe reembolso posterior da Série;
- reembolso não apaga o lançamento original;
- a mesma condição não pode ser aplicada duas vezes.

## Piso e queda automática

O Rating possui piso absoluto **0**. Nenhum lançamento ou reset produz saldo negativo; a parte excedente da perda é registrada em `parcelaLimitadaPeloPiso`. Piso e queda automática são decisões aprovadas.

O elo é recalculado automaticamente após cada lançamento. Ao cruzar um limite para baixo, ocorre queda automática para o elo correspondente, sem partida de proteção implícita.

## Elos oficiais

| Elo | Rating |
|---|---:|
| Ovo Quebrado II | 0-99 |
| Ovo Quebrado I | 100-199 |
| Pinto de Briga II | 200-299 |
| Pinto de Briga I | 300-399 |
| Galo de Quintal II | 400-499 |
| Galo de Quintal I | 500-599 |
| Galo de Rinha II | 600-699 |
| Galo de Rinha I | 700-799 |
| Galo Lendário II | 800-899 |
| Galo Lendário I | 900 ou mais, sem limite superior |

Os nomes, limites, piso zero e ausência de teto para Galo Lendário I são decisões aprovadas, não parâmetros de calibração.

## Soft reset versionado

Cada transição de Season pode aplicar soft reset por lançamento explícito e versionado. A regra de versionar o reset é aprovada; sua fórmula interna permanece proposta para calibração em [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md):

`rating_novo = max(0, âncora + fator_de_retenção × (rating_anterior - âncora))`

Âncora, fator, arredondamento, data de corte e elegibilidade pertencem à versão do reset. O reset:

- não apaga o ledger anterior;
- registra a diferença como lançamento;
- pode provocar queda automática de elo;
- deve ser reproduzível;
- não pode ser reaplicado para a mesma Season e versão.

Os valores da âncora e do fator permanecem propostas para calibração.

## Elegibilidade e reconstrução

Somente Partidas de Séries oficiais válidas são elegíveis, incluindo `DiariaTemporaria` e `ConfrontoOficial`. Nenhum amistoso, agendado, agrupado ou isolado, inclusive Série amistosa com Fearless entre Times oficiais, altera Rating, elo, ranking ou recorde oficial.

O saldo pode ser reconstruído do ledger. Se uma série for invalidada, lançamentos compensatórios removem seu efeito sem reescrever a história. Rankings devem informar Season, versão e instante da reconstrução.

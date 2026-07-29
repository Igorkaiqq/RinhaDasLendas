# Catálogo de Métricas

## Objetivo

Padronizar significado, unidade, granularidade, elegibilidade e qualidade antes de qualquer visualização. Este catálogo não define componentes de interface.

## Classes de dado

- **Fato de origem**: valor observado ou informado, como kills ou ouro.
- **Métrica derivada**: cálculo reproduzível e versionado, como KDA ou participação em abates.
- **Score composto**: Rinha Score de 0 a 100.
- **Projeção competitiva**: Rating, elo e ranking.

Toda métrica derivada deve registrar fórmula, versão, granularidade, campos necessários e tratamento de ausência.

## Dimensões comuns

As consultas devem suportar, conforme disponibilidade:

- jogador;
- rota efetivamente jogada;
- campeão;
- Partida e Série;
- Time oficial ou equipe temporária;
- natureza oficial ou amistosa;
- Competição e rodada, obrigatórias para Série oficial e opcionais ou ausentes somente para amistoso conforme contrato;
- subtipo da Série oficial: `DiariaTemporaria` ou `ConfrontoOficial`;
- formato de draft, incluindo Fearless, independente da natureza oficial ou amistosa;
- Season e patch;
- vitória ou derrota;
- lado.

Season atual é o padrão. Recortes multisseason e `Todas` devem manter identificável a composição do período e a versão das métricas.

## Métricas de resultado e participação

| Métrica | Unidade | Granularidade mínima | Regra |
|---|---:|---|---|
| Partidas jogadas | contagem | jogador/Partida | Participação válida com resultado confirmado |
| Vitórias | contagem | jogador/Partida | Lado vencedor confirmado |
| Derrotas | contagem | jogador/Partida | Lado derrotado confirmado |
| Win rate | percentual | recorte | `vitórias / partidas válidas` |
| Séries jogadas | contagem | jogador/Série | Ao menos uma Partida válida na Série |
| Séries vencidas | contagem | jogador/Série | Regra do resultado da Série |

Cancelamentos, Partidas invalidadas e remakes sem resultado competitivo não entram nos denominadores.

## Combate

| Métrica | Unidade | Observação |
|---|---:|---|
| Kills, mortes e assistências | contagem | Fatos por jogador e Partida |
| KDA | razão | Fórmula proposta para calibração: `(kills + assistências) / max(1, mortes)` |
| Participação em abates | percentual | `(kills + assistências) / abates do time`; indisponível se o total do time faltar |
| Dano a campeões | valor e por minuto | Exige duração válida |
| Dano recebido | valor e por minuto | Interpretado por rota, não como qualidade isolada |
| First blood | indicador | Distinguir autor e assistência quando a fonte permitir |

A fórmula interna de KDA não é decisão definitiva desta fundação. Ela deve possuir versão, dados de referência e calibração antes de ser usada no Score ou em comparação oficial. Sua ratificação permanece em [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md).

## Economia e rota

| Métrica | Unidade | Observação |
|---|---:|---|
| Farm | contagem e por minuto | Não preencher com zero quando ausente |
| Ouro | valor e por minuto | Manter valor final e curva somente quando disponível |
| Diferença de ouro | valor | Marcos temporais devem informar o minuto |
| Diferença de experiência | valor | Marcos temporais devem informar o minuto |
| Presença em rota | percentual | Candidata; depende de telemetria posicional confiável |

## Objetivos e mapa

| Métrica | Unidade | Observação |
|---|---:|---|
| Participação em dragões, Barão, Arauto e torres | contagem/percentual | Exige eventos ou critério de participação versionado |
| Controle de objetivo | índice normalizado | Métrica derivada candidata, não fato bruto |
| Pressão de mapa | índice normalizado | Candidata; requer definição por rota e fonte suficiente |
| Estruturas destruídas | contagem | Separar torres e inibidores quando disponível |

## Visão e utilidade

| Métrica | Unidade | Observação |
|---|---:|---|
| Vision score | valor e por minuto | Comparar por rota e duração |
| Wards colocadas e removidas | contagem | Ausência não equivale a zero |
| Controle de visão | índice normalizado | Candidato, derivado de visão e contexto |
| Controle de grupo | duração/contagem | Depende da fonte e da versão de definição |
| Cura e escudo em aliados | valor | Relevante principalmente para Support e campeões utilitários |

## Elegibilidade

- Toda Série oficial válida alimenta estatísticas, Score, Rating, MVP/SVP, rankings e recordes oficiais.
- `DiariaTemporaria` alimenta projeções oficiais mesmo usando equipes temporárias, pois pertence obrigatoriamente à Competição e à rodada do circuito diário.
- `ConfrontoOficial` alimenta projeções oficiais como subtipo exclusivo entre Times oficiais.
- Todo amistoso pode alimentar apenas histórico e agregados amistosos identificados.
- Nenhum amistoso alimenta Score, Rating, MVP/SVP, rankings ou recordes oficiais.
- Fearless em Série amistosa não altera sua inelegibilidade oficial.
- Agrupar amistosos em série ou evento não altera sua inelegibilidade oficial.
- Dados insuficientes podem produzir estatística parcial, acompanhada de cobertura, sem fabricar Score completo.
- Partida corrigida provoca reconstrução dos agregados afetados.

## Qualidade

Cada resultado agregado deve expor:

- cobertura de Partidas e campos;
- origem manual ou integrada;
- versão das fórmulas;
- confiança;
- quantidade de observações;
- exclusões por inelegibilidade;
- instante da última reconstrução.

## Governança do catálogo

Adicionar ou alterar métrica exige definir nome, propósito, fórmula, unidade, dimensões, fonte, ausência, versão e testes de referência. Mudar significado sem elevar versão é proibido.

Métricas candidatas neste documento não se tornam obrigatórias enquanto a fonte não oferecer qualidade suficiente e uma especificação não aprovar a fórmula.

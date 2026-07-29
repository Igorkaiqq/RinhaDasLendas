# Design: Fundação Competitiva Sazonal e Módulo de Estatísticas

## Contexto

O RinhaDasLendas já organiza jogadores, times, presença e drafts, mas ainda não possui domínio de Seasons, séries, partidas ou estatísticas. O novo módulo deve servir às séries diárias formadas por capitães, aos confrontos entre times oficiais e à evolução futura para coleta local e Riot API.

A pesquisa completa, as evidências e os contratos conceituais estão indexados em [`docs/statistics/README.md`](../../statistics/README.md). A primeira especificação executável está em [`specs/023-fundacao-competitiva-sazonal/spec.md`](../../../specs/023-fundacao-competitiva-sazonal/spec.md).

## Decisão de produto

O trabalho será decomposto em incrementos independentes. A feature 023 cria apenas a fundação sazonal e operacional de séries. Elencos e transferências, partidas detalhadas, votação, Score e Rating permanecem em features posteriores.

Toda integração externa possui alternativa manual. O LCU é uma fonte útil, mas não suportada oficialmente e incapaz de determinar sozinho todos os fatos competitivos com confiança.

## Fronteiras

O monólito manterá quatro fronteiras conceituais:

1. **Competição**: Seasons, eventos, confrontos, séries e regulamentos.
2. **Partidas**: fatos confirmados, participantes, resultados e snapshots.
3. **Ingestão**: fontes, payloads redigidos, matching, revisão e auditoria.
4. **Analytics**: métricas, Score, Rating, rankings e snapshots.

Partidas confirmadas são fatos históricos. Métricas e rankings são projeções reconstruíveis e versionadas.

## Contextos competitivos

- Equipes temporárias são formadas por capitães no `DraftMontagem` e existem no contexto da série diária.
- Times oficiais possuem identidade e elenco persistentes, sem serem confundidos com os lados temporários.
- Confrontos oficiais e amistosos entre times são agendados fora da lista recorrente de presença.
- Todo amistoso permanece separado e não gera Score, Rating, MVP/SVP, ranking ou recorde oficial.
- Fearless vale para série direta de dois lados e bloqueia para ambos os lados todo campeão usado anteriormente na série.
- Evento com quatro times usa draft padrão e não compartilha bloqueios globais.

## Seasons

Cada Season temática da Riot representa uma Season da Rinha. A Season ativa é o filtro padrão, enquanto consultas podem selecionar uma, várias ou todas. Encerrar uma Season congela seu contexto, sem apagar histórico. O soft reset futuro será um lançamento versionado no ledger de Rating.

## Dados e confiança

A validação local da partida customizada `3266170515` comprovou que o LCU pode retornar estatísticas finais e timeline de custom game. Também comprovou que rota automática e assistências de objetivos podem ser inconsistentes. Por isso, cada campo possui origem, disponibilidade e confiança; campos experimentais não participam de decisões oficiais.

## Score, Rating e votação

- Rinha Score será numérico de 0 a 100, explicado por categorias e versão do algoritmo.
- Rating usa ledger imutável, piso 0, queda automática e faixas de elos aprovadas.
- Vitória varia de +10 a +20; derrota, de -5 a -20, com força e impacto limitados.
- MVP e SVP são decididos por participantes em votação, não automaticamente pelo Score.
- Score só desempata após segundo turno empatado; igualdade exata produz co-vencedores.

## Segurança e autorização

A ordem administrativa é `SuperAdmin > Presidente > VicePresidente > Admin > Moderador > Capitão > Jogador`. Hierarquia não substitui capabilities explícitas. Collector futuro será identidade de serviço de escopo mínimo e nunca enviará credenciais LCU ao backend.

## Testes

Regras de domínio, validators, casos de uso, autorização, concorrência, idempotência, contratos de importação, reconstrução de projeções, i18n e acessibilidade serão verificadas nos níveis apropriados. Fixtures externas serão redigidas e versionadas.

## Decomposição aprovada

1. `023-fundacao-competitiva-sazonal`
2. `024-elencos-escalacoes-transferencias`
3. `025-partidas-estatisticas-basicas`
4. `026-votacao-destaques-series`
5. `027-rinha-score-rating`
6. Perfis, times, coleta automatizada e inteligência competitiva em incrementos posteriores.

## Pendências

As decisões ainda abertas estão em [`docs/statistics/OPEN_DECISIONS.md`](../../statistics/OPEN_DECISIONS.md). Elas não autorizam suposições silenciosas durante planejamento ou implementação.

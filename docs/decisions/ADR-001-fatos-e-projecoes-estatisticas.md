# ADR-001: Separar fatos competitivos de projeções estatísticas

## Status

Aceito

## Data

2026-07-27

## Contexto

Partidas, resultados, participantes e picks podem sofrer revisão administrativa. Métricas, notas, ratings e rankings também evoluem quando fórmulas são corrigidas ou versionadas. Se valores derivados forem tratados como fatos definitivos, uma correção exigirá alterações destrutivas e poderá produzir históricos incompatíveis.

## Decisão

Partidas confirmadas e decisões administrativas serão fatos históricos auditáveis. Métricas, Rinha Score, Rinha Rating consolidado, rankings e recordes serão projeções reconstruíveis desses fatos e de versões explícitas de algoritmo.

Correções não apagam o fato anterior. Elas registram nova versão, estorno ou compensação, conforme o tipo de dado. Consultas identificam o escopo sazonal e a versão aplicável.

## Alternativas consideradas

### Armazenar somente os agregados atuais

- Vantagem: leitura inicial simples.
- Desvantagem: impossibilita explicar ou reproduzir valores antigos.
- Rejeição: conflita com auditoria, correções e versionamento de nota.

### Recalcular tudo sob demanda

- Vantagem: elimina projeções persistidas.
- Desvantagem: custo imprevisível e ausência de snapshots históricos de ranking.
- Rejeição: inadequado para páginas públicas internas e fechamento de Season.

## Consequências

- O modelo precisa preservar fatos suficientes para reconstrução.
- Projetores devem ser idempotentes e determinísticos.
- Fechamento de Season cria snapshots, sem eliminar a capacidade de reconstrução.
- Correções exigem reprocessamento e auditoria.
- Interfaces devem distinguir dado confirmado, derivado, provisório e indisponível.

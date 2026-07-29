# ADR-003: Separar times oficiais de equipes temporárias de draft

## Status

Aceito

## Data

2026-07-27

## Contexto

A Rinha possui dois conceitos chamados informalmente de time:

- equipes temporárias formadas diariamente por capitães após a lista de presença;
- times oficiais com capitão, elenco fixo, titulares, reservas, escalações e transferências.

Misturar os conceitos faria uma escolha diária alterar histórico de time oficial, atribuir estatísticas ao time errado e aplicar janelas de transferência a participantes temporários.

## Decisão

`Time` continuará representando a organização oficial persistente. O lado temporário de uma série será um snapshot contextual originado do `DraftMontagem`, sem adquirir identidade de time oficial.

Uma série diária usa equipes temporárias. Um confronto entre times usa times oficiais e agendamento próprio, sem depender da lista recorrente de presença. A partida preserva snapshot do lado e pode manter referência opcional à origem.

Fearless é regra da série direta e pode existir nos dois contextos, sem unificar suas identidades. A exceção obrigatória é o evento com quatro times: o evento e todos os confrontos que pertencem a ele usam draft padrão, sem Fearless. Todo amistoso entre times permanece em histórico separado e não afeta projeções oficiais.

## Alternativas consideradas

### Reutilizar `Time` para qualquer lado

- Vantagem: menos nomes no modelo.
- Desvantagem: cria times descartáveis, polui elencos e confunde estatísticas históricas.
- Rejeição: identidade persistente e composição contextual possuem ciclos de vida diferentes.

### Guardar apenas nomes dos lados

- Vantagem: baixa complexidade inicial.
- Desvantagem: perde vínculo auditável com draft, capitães e times oficiais.
- Rejeição: snapshots precisam preservar origem sem depender da mutabilidade atual.

## Consequências

- Estatísticas de time oficial nunca agregam equipes temporárias.
- Elegibilidade de contratação pode contar séries diárias do jogador sem criar vínculo oficial.
- `DraftMontagem` não é reativado, restaurado ou alterado quando uma série é criada.
- A feature de elencos precisa evoluir o `Time` atual de cinco para até oito integrantes.
- Consultas devem permitir filtrar contexto diário, confronto oficial e amistoso.

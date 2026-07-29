# Relatório da Task 6

## Status

Implementada a escolha de modo e o board manual da feature 028 no frontend.

## Entregas

- Contratos frontend atualizados para `modo` anulável, `cicloVersao`, `capitaesElegiveisIds` e `novoCapitaoId` opcional na substituição.
- Serviço `chooseDraftMontagemMode` usando `PATCH /api/v1/draft-montagens/{id}/modo`.
- Capacidade do novo ciclo restrita explicitamente às roles `Admin` e `SuperAdmin`, além da permissão existente.
- Escolha Manual/TempoReal exibida somente para `PresencaEncerrada`, modo nulo e ciclo `ModoPosPresenca`.
- Fluxo legado preservado sem retornar à escolha de modo.
- Seleção realtime limitada aos capitães elegíveis projetados pelo backend.
- Criação direta sem configuração de capitães, mantendo `sortearCapitaes: false` e `capitaesIds: []` para compatibilidade do contrato.
- Board manual v2 sem capitães, sorteio, ordem, início realtime ou histórico de picks; layouts salvam `capitaoId: null`.
- Finalização manual habilitada somente após a projeção apresentar todos os times completos e nenhum titular livre; o backend continua responsável pela validação definitiva.
- Início realtime disponível explicitamente em `OrdemDefinida`.
- Rail e header adaptados ao modo e à versão do ciclo.
- O novo diálogo de substituição não foi implementado; somente o tipo exigido pelo contrato atual foi incorporado.

## TDD

- RED de serviço: 1 falha esperada, `chooseDraftMontagemMode is not a function`.
- RED de componentes: 16 falhas esperadas cobrindo panel, setup, board, view, rail, header e i18n.
- GREEN focado: 8 arquivos e 285 testes aprovados antes da regressão adicional de início realtime na view.
- GREEN final: 38 arquivos e 499 testes aprovados.

## Verificação

- `npm run lint:check`: aprovado.
- `npm test`: 38 arquivos, 499 testes aprovados.
- `npm run build`: aprovado.
- `git diff --check`: aprovado.
- Build manteve avisos preexistentes de anotações `PURE` em dependências e chunk principal acima de 500 kB.

## Screenshots

O navegador não foi executado nesta task; nenhuma screenshot foi gerada.

## Auditoria de internacionalização

- Sim: nenhum texto visível novo ficou hardcoded nos componentes de draft.
- Sim: nenhum hardcode de mensagem foi introduzido no backend.
- Sim: `pt.json` e `en.json` possuem chaves equivalentes e o teste de sincronização passou.
- Sim: resources backend permanecem adequados; esta task não adicionou mensagem de backend.
- Sim: acentuação em português foi revisada, incluindo “Próxima”, “capitães”, “histórico” e “não”.
- Sim: títulos, botões, badges, mensagens de estado e textos vazios afetados foram revisados.
- Sim: os novos estados e feedbacks frontend usam Vue I18n; não houve nova validação backend.
- Sim: todos os arquivos alterados respeitam o padrão de internacionalização.

## Preocupações

- O build continua reportando o bundle principal com aproximadamente 735 kB antes de gzip; não foi introduzida dependência nova nesta task.
- A validação visual em navegador ficou para uma etapa posterior, conforme a ausência de execução de browser nesta task.

# Relatório de verificação: Redesenho do Fluxo de Draft

## Ambiente

- Data local da execução: 2026-07-25.
- Sistema: Linux 6.6.114.1-microsoft-standard-WSL2 x86_64.
- Node.js: 22.22.1.
- npm: 9.2.0.
- Vitest: 4.1.8.

## Baseline focado

- Revisão: `d367b7bd7135c7f06b28b0d3c2204db01bf01266` (`chore: ignorar worktrees locais`).
- Horário local da execução: 2026-07-25 16:05:40.
- Comando: `npm test --prefix FrontEnd -- src/views/DraftsView.spec.ts src/components/drafts/DraftStateRail.spec.ts src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/views/SystemUpdatesView.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 7 arquivos aprovados, 58 testes aprovados e 0 falhas.
- Resumo exato: `DraftsView.spec.ts` 25; `DraftStateRail.spec.ts` 1; `systemUpdates.spec.ts` em `constants` 5; `systemUpdates.spec.ts` em `services` 8; `SystemUpdatesView.spec.ts` 7; `SystemUpdateCard.spec.ts` 4; `i18n.spec.ts` 8.

## T003 - Ordem canônica dos status

### RED

- Comando, executado em `FrontEnd/`: `npm test -- src/constants/draftMontagemStatus.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 2 arquivos com falha; 3 testes falharam e 8 passaram. O teste de status falhou porque o valor esperado `OrdemDefinida` não existia entre `CapitaesDefinidos` e `Aberta`.

### GREEN

- Comando, executado em `FrontEnd/`: `npm test -- src/constants/draftMontagemStatus.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 2 arquivos aprovados, 11 testes aprovados e 0 falhas.

## T004 - Auditoria de textos visíveis do Draft

### RED original

- Comando, executado em `FrontEnd/`: `npm test -- src/constants/draftMontagemStatus.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: o scanner ampliado encontrou o marcador visível hardcoded `C` em `DraftBoard.vue`; na mesma execução, os contratos ainda ausentes de status e traduções totalizaram 3 falhas e 8 aprovações.

### RED dos casos de regressão

- Comando, executado em `FrontEnd/`: `npm test -- src/i18n/i18n.spec.ts`.
- Resultado: 22 testes executados; 11 falharam e 11 passaram. As falhas reproduziram truncamento após `<template>` aninhado, texto visível de um caractere, atributos estáticos com aspas simples, `alt`, literais em atributos vinculados, literal de toast e literal de notificação.
- Comando após implementar o scanner, antes de localizar os marcadores: `npm test -- src/i18n/i18n.spec.ts`.
- Resultado: 22 testes executados; 1 falhou e 21 passaram. Todos os fixtures foram aprovados e a auditoria real encontrou os marcadores `C` ainda hardcoded.
- Comando após adicionar o fixture para `>` em expressão de atributo: `npm test -- src/i18n/i18n.spec.ts`.
- Resultado: 23 testes executados; 2 falharam e 21 passaram. O fixture e `DraftReasonDialog.vue` demonstraram o falso positivo causado por `=>` dentro de atributo entre aspas.

### GREEN

- Comando, executado em `FrontEnd/`: `npm test -- src/i18n/i18n.spec.ts`.
- Resultado: 1 arquivo aprovado, 23 testes aprovados e 0 falhas. O scanner percorreu o template SFC externo completo, aceitou operadores em atributos e rejeitou todos os fixtures proibidos.

## T007-T013 - Hierarquia e progresso do workspace

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T007 | Concluída | Matriz dos seis estados operacionais, cancelamento, desconhecido, Discord e `aria-current` em `DraftStateRail.spec.ts`. |
| T008 | Concluída | Contexto, data, status, métricas, nomes longos, slots e pluralização PT/EN em `DraftWorkspaceHeader.spec.ts`. |
| T009 | Concluída | Matriz integrada dos sete estados, ações e remoção de identidade/cancelamento duplicados em `DraftsView.spec.ts`. |
| T010 | Concluída | `DraftRail.vue` aceita `terminal` e `unknown` e aplica `aria-current="step"` somente quando indicado. |
| T011 | Concluída | Rail usa a ordem canônica sem `Cancelada`, fallback neutro, cancelamento terminal e matriz completa do indicador Discord. |
| T012 | Concluída | Cabeçalho estável apresenta identidade, data, status, métricas e grupos distintos de ação. |
| T013 | Concluída | `AppShell` é o único landmark `main`; o board não repete identidade nem cancelamento e mantém o contrato público de emits. |

### RED inicial

- Comando, executado em `FrontEnd/`: `npm test -- src/components/drafts/DraftStateRail.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 arquivos com falha; 20 testes falharam e 25 passaram.
- Motivos esperados: ausência do cabeçalho de workspace, falta de `aria-current`, estados terminal e desconhecido não implementados, cancelamento ativando presença, indicador Discord sem identificação paralela, `<main>` interno em `DraftsView.vue` e identidade geral repetida no board.

### GREEN funcional inicial

- Comando, executado em `FrontEnd/`: `npm test -- src/components/drafts/DraftStateRail.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 arquivos aprovados, 48 testes aprovados e 0 falhas.
- Limitação encontrada na revisão: o gate de build ainda falhava por tipagem estrita introduzida anteriormente no scanner de i18n; por isso, esta execução não encerrou T007-T013.

### RED da revisão

- Comando, executado em `FrontEnd/`: `npm run build`.
- Resultado: falha com 8 erros TypeScript em `src/i18n/i18n.spec.ts`, causados por `String.at` fora do target configurado e acessos de índice/capturas sem invariantes explícitas para `noUncheckedIndexedAccess`.
- Comando, executado em `FrontEnd/`: `npm test -- src/components/layout/AppShell.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 4 arquivos com falha; 8 testes falharam e 52 passaram.
- Motivos esperados: dois landmarks `main` renderizados, `RequerReconciliacao` e `EmAndamento` tratados como pendentes, ordem do rail duplicada localmente, cancelamento ainda disponível no board e métricas sem singular/plural independente em PT/EN.

### GREEN final

- Comando focado, executado em `FrontEnd/`: `npm test -- src/components/layout/AppShell.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 5 arquivos aprovados, 88 testes aprovados e 0 falhas.
- Comando completo, executado em `FrontEnd/`: `npm test`.
- Resultado: 33 arquivos aprovados, 240 testes aprovados e 0 falhas.
- Comando de build, executado em `FrontEnd/`: `npm run build`.
- Resultado: build concluído; 2.755 módulos transformados. Permanecem apenas avisos não bloqueantes de anotações `PURE` em dependências e tamanho de chunk.
- Comando de i18n, executado em `FrontEnd/`: `npm test -- src/i18n/i18n.spec.ts`.
- Resultado: 1 arquivo aprovado, 28 testes aprovados e 0 falhas.
- Comando de lint, executado em `FrontEnd/`: `npm run lint:check`.
- Resultado: aprovado sem erros ou avisos do ESLint.
- Comando de whitespace, executado na raiz: `git diff --check`.
- Resultado: aprovado sem saída.

## T014-T019 - Lista de presença e publicações no Discord

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T014 | Concluída | `DraftPreparationPanel.spec.ts` cobre listas com 0, 1, 10, 14 e 30 participantes e valida identidade, origem, ação aplicável e estrutura de cada linha, além de busca agrupada, metadados dos campos, payloads e `aria-pressed`. |
| T015 | Concluída | `DraftDiscordPublicationPanel.spec.ts` cobre renderização da matriz recebida, tipos desconhecidos neutros sem ação, localização, disponibilidade legada de republicação, permissão, `saving` e os três tipos exatos. |
| T016 | Concluída | `DraftsView.spec.ts` cobre projeções Discord vazias/parciais e `IntegracaoLegada` em PT/EN, deduplicação, preservação de dados e de `null`, integração, concorrência, permissão e fallbacks. |
| T017 | Concluída | O roster separa identidade, origem e ações em colunas estruturais consistentes e reúne busca, seleção e inclusão manual em um grupo operável; largura visual real permanece para T030. |
| T018 | Concluída | O painel Discord é subordinado, usa somente ações secundárias, localiza estados e mantém fallback desconhecido neutro. |
| T019 | Concluída | `DraftsView.vue` é a única origem da matriz Discord: três linhas canônicas seguidas dos primeiros registros não canônicos preservados; painel, ações e diálogo compartilham essa projeção. |

### RED

- Comando, executado em `FrontEnd/`: `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 suítes falharam antes da implementação porque `DraftPreparationPanel.vue` e `DraftDiscordPublicationPanel.vue` ainda não existiam; nenhum teste foi executado.
- Motivo esperado: os contratos de apresentação e a integração exigidos por T014-T019 ainda estavam ausentes.

### GREEN focado

- Comando, executado em `FrontEnd/`: `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 arquivos aprovados, 58 testes aprovados e 0 falhas (`DraftPreparationPanel`: 12; `DraftDiscordPublicationPanel`: 8; `DraftsView`: 38).

### Ledger de evidências

| Critério | Evidência desta fase | Estado |
|----------|----------------------|--------|
| SC-003 | Matriz automatizada 0/1/10/14/30 verifica identidade, origem, ação e a mesma estrutura em cada linha, incluindo nomes longos. | Cobertura estrutural aprovada; largura real e inspeção em 320px permanecem em T030. |
| SC-005 | Testes focados cobrem duplicidade rápida de confirmar/encerrar/remover/republicar, perda de permissão administrativa, payloads exatos e validação defensiva de capitães. | Aprovado para presença/Discord; ações de US3 permanecem fora desta fase. |
| SC-007 | PT/EN possuem fallback neutro para status Discord desconhecido tanto no painel quanto no diálogo; o scanner não encontrou texto visível hardcoded. | Aprovado para T014-T019. |
| SC-010 | Busca, seleção e inclusão manual formam um grupo estrutural único com nomes e `autocomplete`; remoção e avanço mantêm seus eventos. | Cobertura estrutural parcial; jornada extensa e rolagem real permanecem em T030. |

### Gates da fase

- Suíte completa: `npm test`; 35 arquivos aprovados, 264 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído. Permanecem avisos não bloqueantes de anotações `PURE` em dependências e tamanho de chunk.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 1 arquivo aprovado, 28 testes aprovados e 0 falhas.
- Dependências: os filhos não importam `@/services/`; serviços e autorização permanecem em `DraftsView.vue`.
- Textos visíveis: placeholders, botões, títulos, badges, estados vazios e rótulos acessíveis foram revisados em PT/EN; acentuação portuguesa revisada.
- Backend: nenhuma mensagem, validação ou recurso backend foi alterado.

### Revisão dos achados de T014-T019

#### RED

- Comando: `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts`.
- Resultado isolado: 4 arquivos com falha; 8 testes falharam e 91 passaram.
- Motivos esperados: projeções Discord vazias/parciais não mantinham três linhas; campos manuais não tinham `name`/`autocomplete`; status desconhecido vazava chave técnica no diálogo; e o pai aceitava capitão sem validar permissão, estado ou presença confirmada.
- Os novos testes de duplicidade rápida e perda de permissão para confirmar, encerrar, remover e republicar já passaram no RED, comprovando os guards existentes sem exigir alteração artificial de produção.

#### GREEN

- Comando focado: `npm test -- src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 4 arquivos aprovados, 99 testes aprovados e 0 falhas (`DraftPreparationPanel`: 13; `DraftDiscordPublicationPanel`: 10; `DraftReasonDialog`: 31; `DraftsView`: 45).
- Suíte completa: `npm test`; 35 arquivos aprovados, 276 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído, com os mesmos avisos não bloqueantes de dependências e tamanho de chunk.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados e 0 falhas.

### Fechamento do status ausente no Discord

#### RED

- Comando: `npm test -- src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 arquivos com falha; 5 testes falharam e 85 passaram.
- Motivos esperados: o filho ainda normalizava dados, a view fornecia projeção esparsa e convertia registro ausente para `Pendente`, e o diálogo ocultava contexto quando `publicationStatus` era `null`.

#### GREEN

- Comando focado: `npm test -- src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 3 arquivos aprovados, 90 testes aprovados e 0 falhas (`DraftDiscordPublicationPanel`: 10; `DraftReasonDialog`: 33; `DraftsView`: 47).
- Projeções vazias e parciais foram verificadas em português e inglês; registros ausentes permanecem `null` na matriz, na ação pendente e no diálogo.
- Suíte completa: `npm test`; 35 arquivos aprovados, 280 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído, com os avisos não bloqueantes já registrados.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados e 0 falhas.

### Fechamento de publicações Discord não canônicas

#### RED

- Comando: `npm test -- src/views/DraftsView.spec.ts`.
- Resultado: 1 arquivo com falha; 2 testes falharam e 47 passaram.
- Motivo esperado: `discordPublicationMatrix` mantinha somente os três tipos canônicos e descartava `IntegracaoLegada`.

#### GREEN

- Comando focado: `npm test -- src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 2 arquivos aprovados, 59 testes aprovados e 0 falhas (`DraftDiscordPublicationPanel`: 10; `DraftsView`: 49).
- Evidência: linhas canônicas aparecem primeiro; tipos canônicos e não canônicos repetidos usam o primeiro registro; `IntegracaoLegada` mantém os dados originais, usa fallback neutro localizado em PT/EN e não recebe ação.
- Suíte completa: `npm test`; 35 arquivos aprovados, 282 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído, com os avisos não bloqueantes já registrados.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados e 0 falhas.

## T020-T023 - Capitães, ordem, escolhas e resultado

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T020 | Concluída | `DraftVisualBoard.spec.ts` cobre clone local imutável, cópia ordenada, payload, progresso, preferências, leitura terminal, autorização personalizada, turno válido e expiração calculada pelo relógio ajustado do servidor, lock local e filtros de rota em inglês. |
| T021 | Concluída | `DraftsView.spec.ts` cobre payload, estados, duplicidade, escolha inválida, cancelamento obsoleto, broadcast não personalizado seguido de GET personalizado, offset em mutação/reconexão e rejeições independentes por time, capitão, jogador livre e expiração. |
| T022 | Concluída | O board preserva props/emits, recebe autorização e offset opcionais, ordena cópias, mantém rotas, restringe picks ao turno oficial ativo e usa lock resetado por ciclo de salvamento ou nova projeção. |
| T023 | Concluída | A view mantém serviços e payloads, trata SignalR somente como notificação, aplica estado personalizado sob geração/versão e revalida autorização, identidade, elegibilidade e expiração antes do pick. |

### RED

- Primeiro comando, executado em `FrontEnd/`: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts`.
- Primeiro resultado: 2 arquivos com falha; 8 testes falharam e 54 passaram.
- Motivos esperados: ausência de ordenação visual identificável, ordem/capitão/progresso/sequência explícitos, regiões claras de turno/pool e leitura terminal sem renomeação; pick e finalização ainda aceitavam eventos duplicados, e a view não validava a identidade do capitão da vez.
- Comando RED ampliado após adicionar as matrizes de estado: o mesmo comando executou 64 testes; 10 falharam e 54 passaram.
- Motivos adicionais esperados: capitães e ordem eram aceitos fora das etapas correspondentes, e intenções do board eram encaminhadas antes de o draft estar aberto.
- RED de timeout, executado em `FrontEnd/`: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`; 1 teste falhou e 5 passaram porque o timeout era contado como jogador escolhido (`3 / 4` em vez de `2 / 4`).

### GREEN focado inicial

- Comando: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts`.
- Resultado: 2 arquivos aprovados, 64 testes aprovados e 0 falhas (`DraftVisualBoard`: 6; `DraftsView`: 58).
- O teste de payload confirma que a apresentação usa `team-a`, `team-b` por `ordem`, enquanto `save` mantém a ordem funcional recebida `team-b`, `team-a` e os mesmos `jogadorId`.
- O teste realtime inicial confirmou o bloqueio duplicado na integração da view; a revisão posterior acrescentou lock independente no próprio board.
- A escolha rejeitada pelo serviço mantém turno, pool e projeção atuais, apresenta o erro retornado e não interfere nas proteções existentes contra refresh obsoleto.
- Timeouts permanecem na sequência auditável, mas somente registros com `jogadorId` avançam o progresso de jogadores escolhidos.

### Ledger de evidências

| Critério | Evidência desta fase | Estado |
|----------|----------------------|--------|
| SC-001 | `Finalizada` e `Cancelada` mantêm resultado, ordem e capitães, sem renomear, arrastar, substituir, salvar, escolher, finalizar ou cancelar. | Aprovado estruturalmente para o board; inspeção visual permanece em T030. |
| SC-005 | Matriz cobre cancelamento revalidado após transição terminal, capitães, ordem, pick e finalização com permissão/identidade, estado válido e bloqueio de duplicidade rápida na view e no board. | Aprovado para as ações automatizadas de US3. |
| SC-007 | Dez strings adicionadas nesta fase possuem equivalentes PT/EN; ordem, progresso, sequência, timeout e seis filtros de rota usam i18n e o scanner completo foi aprovado. | Aprovado para T020-T023. |
| SC-009 | Props/emits, `jogadorId`, ordem do payload, projeção inválida, autorização oficial e identidade realtime permanecem preservados nos testes focados e completos. | Aprovado para a jornada automatizada de US3; jornada real permanece em T030. |

### Gates da fase

- Suíte completa: `npm test`; 36 arquivos aprovados, 297 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído. Permanecem somente os avisos não bloqueantes já conhecidos de anotações `PURE` em dependências e tamanho de chunk.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados e 0 falhas.
- Whitespace: `git diff --check`; aprovado sem saída.
- Fronteiras: nenhum serviço, backend, contrato HTTP, dependência, token ou regra de domínio foi alterado.
- Textos visíveis: ordem dos times, progresso, sequência e timeout foram adicionados em PT/EN; preferências, botões, títulos, badges, estados vazios e mensagens de validação existentes foram revisados.
- Backend: nenhuma mensagem ou resource foi alterado; nenhuma atualização é necessária para esta fase exclusivamente frontend.

### Revisão pós-T023

#### RED

- Cancelamento obsoleto: `npm test -- src/views/DraftsView.spec.ts`; 2 testes falharam e 58 passaram. `confirmReasonAction` ainda chamava o serviço depois de uma atualização realtime mudar o draft para `Finalizada` ou `Cancelada`.
- Autorização e lock de pick: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts`; 9 testes falharam e 64 passaram. A flag `canCurrentUserPick` era descartada, turno expirado ou sem atores válidos ainda oferecia pick, eventos rápidos duplicavam o emit local e a view não rejeitava a negação oficial mais recente.
- Filtros em inglês: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`; 1 teste falhou e 11 passaram porque `TODAS AS ROTAS` e `SUPORTE` eram renderizados diretamente dos valores internos.
- Capitão incompatível com o time atual: o mesmo teste focado executou 13 casos; 1 falhou e 12 passaram porque um capitão existente em outro time ainda qualificava o turno.

#### GREEN

- Cancelamento obsoleto: `DraftsView.spec.ts` passou 60/60 após a revalidação terminal limpar a ação sem mutação.
- Focado final: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`; 3 arquivos e 103 testes aprovados (`DraftVisualBoard`: 13; `DraftsView`: 62; i18n: 28).
- A autorização oficial é preservada no carregamento realtime inicial, reconexão e respostas de iniciar, escolher e substituir; broadcasts SignalR não são fonte de autorização e exigem nova consulta personalizada antes da aplicação.
- O board exige time atual existente, `capitaoId` igual a `turnoAtualCapitaoId` e participante correspondente com `capitao === true`, além de tempo positivo calculado pelo relógio ajustado do servidor; seu lock bloqueia emits rápidos e é liberado somente após ciclo de salvamento ou atualização da projeção.
- Os rótulos dos seis filtros usam chaves PT/EN, enquanto `DraftRouteFilterValues` e `DRAFT_MONTAGEM_ROUTE_BY_FILTER` permanecem inalterados para seleção e filtragem.

#### Gates corrigidos

- Suíte completa: `npm test`; 36 arquivos aprovados, 308 testes aprovados e 0 falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído, com os avisos não bloqueantes já conhecidos.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados e 0 falhas.
- Whitespace: `git diff --check`; aprovado sem saída.
- Escopo: nenhum serviço, backend, contrato HTTP, dependência, token ou regra de domínio foi alterado.

### Revisão realtime complementar

#### RED

- Fluxo personalizado e relógio: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts`; 6 testes falharam e 75 passaram. O callback ainda aplicava o broadcast diretamente, offset não era propagado por mutação/reconexão e view/board aceitavam turno expirado apenas no relógio do servidor.
- Time atual ausente: `npm test -- src/views/DraftsView.spec.ts -t "rejects a parent pick when current team is missing"`; 1 teste falhou e 65 foram ignorados, confirmando que a view ainda encaminhava o pick sem time ativo.
- Capitão do time e jogador livre elegível: `npm test -- src/views/DraftsView.spec.ts -t "captain mismatches the current team|requested player is not eligible and free"`; 2 testes falharam e 66 foram ignorados quando os dois guardas mínimos estavam ausentes.

#### GREEN

- Cada broadcast do draft ativo dispara `getDraftMontagemRealtimeState`; nenhum campo do payload SignalR é aplicado, e somente o retorno personalizado atual pode atualizar projeção, autorização e offset.
- Estado inicial, mutações e reconexões atualizam o offset como `Date.parse(serverNow) - Date.now()` sob as proteções existentes de draft, geração e versão de requisição.
- View e board usam `Date.now() + serverClockOffsetMs` e exigem que o participante correspondente do time tenha `capitao === true`; a view também exige a cadeia de identidade do capitão, jogador com estado `Livre` e expiração futura.
- Payloads, endpoints, serviços, backend e regras de domínio permanecem inalterados.

#### Gates finais

- Focado: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`; 3 arquivos e 112 testes aprovados (`DraftVisualBoard`: 15; `DraftsView`: 69; i18n: 28).
- Suíte completa: `npm test`; 36 arquivos e 317 testes aprovados, sem falhas.
- Build: `npm run build`; 2.761 módulos transformados e build concluído, somente com os avisos não bloqueantes já conhecidos de anotações `PURE` e chunk acima de 500 kB.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- src/i18n/i18n.spec.ts`; 28 testes aprovados, sem falhas.
- Whitespace: `git diff --check`; aprovado sem saída.

### Fechamento final T020-T023

#### RED

- Capitão apenas por identidade: `npm test -- src/components/drafts/visual/DraftVisualBoard.spec.ts`; 1 teste falhou e 14 passaram. Mesmo com `capitaoId` e jogador atual coincidentes, o board ainda exibia a ação quando o participante correspondente tinha `capitao === false`.

#### GREEN

- `currentTurnCaptain` somente resolve o participante quando identidade e `capitao === true` coincidem, exatamente como a validação independente da view.
- `DraftVisualBoard.spec.ts` passou 15/15: no estado inválido não há ação nem emissão; após corrigir apenas o flag na projeção local, sem ciclo de saving ou troca de projeção, o pick é emitido, comprovando que a tentativa negada não ativou o lock.

## T024-T026 - Navegação entre drafts

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T024 | Concluída | `DraftNavigator.spec.ts` cobre os sete status na ordem canônica, variantes semânticas, `v-model` de busca/status, seleção e `aria-current`, reset, retry, criação autorizada, data ausente, status desconhecido, skeleton sem dados, feedback não bloqueante, transições loading/falha/sucesso, vazio real, zero filtrado, nome longo, expansão compacta local e fronteira sem serviços/autorização. |
| T025 | Concluída | `DraftNavigator.vue` implementa o contrato tipado com status aberto a strings, preserva itens conhecidos sob loading/falha, reserva skeleton para ausência de dados, diferencia coleção vazia de filtros sem resultado e mantém fallbacks PT/EN e expansão compacta no filho. |
| T026 | Concluída | `DraftsView.vue` fornece dados filtrados, conhecimento da coleção, identidade selecionada independente do detalhe e usada também pelo auto-open, permissões e estados de carregamento/falha; seleção, detalhe, realtime, criação, reset, retry e guardas obsoletos permanecem na view. |

### RED

- Componente isolado: `npm test -- --run src/components/drafts/DraftNavigator.spec.ts`; 1 suíte falhou antes de executar testes porque `DraftNavigator.vue` ainda não existia, exatamente a unidade exigida por T025.
- Integração: `npm test -- --run src/views/DraftsView.spec.ts`; 5 testes falharam e 69 passaram porque a view ainda renderizava filtros/lista diretamente e não expunha `DraftNavigator` nem estado independente de falha da listagem.
- Reset integrado: `npm test -- --run src/views/DraftsView.spec.ts -t "resets both navigator filters"`; 1 teste falhou e 75 foram ignorados depois da remoção do binding ainda não coberto. Busca e status permaneceram preenchidos e nenhuma recarga sem filtro ocorreu.
- Revisão dos achados: `npm test -- --run src/components/drafts/DraftNavigator.spec.ts src/views/DraftsView.spec.ts`; 17 testes falharam e 82 passaram. As falhas reproduziram lista escondida por loading/falha, seleção perdida quando o detalhe era `null`, ausência de conhecimento da coleção, criação incorreta em zero filtrado e falta de variantes semânticas.
- Fechamento final dos achados: o mesmo comando executou 101 testes; 3 falharam e 98 passaram. As falhas reproduziram auto-open baseado em `selectedMontagem` após detalhe falho e zero filtrado concorrendo com feedback de loading/falha no filho e na integração.

### GREEN

- Componente isolado: `DraftNavigator.spec.ts` passou 9/9 após a implementação mínima do contrato visual.
- Integração inicial: `npm test -- --run src/components/drafts/DraftNavigator.spec.ts src/views/DraftsView.spec.ts`; 2 arquivos e 84 testes aprovados (`DraftNavigator`: 9; `DraftsView`: 75).
- Focado da primeira entrega: `npm test -- --run src/views/DraftsView.spec.ts src/components/drafts/DraftNavigator.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts src/i18n/i18n.spec.ts`; 8 arquivos e 174 testes aprovados (`DraftsView`: 76; `DraftNavigator`: 9; i18n: 28).
- GREEN da revisão: `npm test -- --run src/components/drafts/DraftNavigator.spec.ts src/views/DraftsView.spec.ts`; 2 arquivos e 99 testes aprovados (`DraftNavigator`: 20; `DraftsView`: 79).
- Focado final corrigido: o comando dos oito arquivos acima aprovou 188 testes (`DraftsView`: 79; `DraftNavigator`: 20; i18n: 28).
- GREEN do fechamento final: `npm test -- --run src/components/drafts/DraftNavigator.spec.ts src/views/DraftsView.spec.ts`; 2 arquivos e 101 testes aprovados (`DraftNavigator`: 21; `DraftsView`: 80).
- Focado final: o comando dos oito arquivos acima aprovou 190 testes (`DraftsView`: 80; `DraftNavigator`: 21; i18n: 28).
- A listagem usa versão de requisição própria: uma falha antiga não substitui o resultado nem os estados `loading`/`loadFailed` de uma tentativa mais nova.
- Falha e retry da lista não limpam itens conhecidos, identidade ativa, metadados personalizados, conexão realtime ou erros de ação; auto-open consulta a identidade ativa, não o detalhe anulável, e respostas obsoletas não iniciam novo detalhe.

### Ledger de evidências

| Critério | Evidência desta fase | Estado |
|----------|----------------------|--------|
| SC-002 | Estrutura compacta, expansão local e quebra de nome em duas linhas estão implementadas no filho. | Cobertura estrutural aprovada; alvos de toque, screenshots e overflow real permanecem em T029-T030. |
| SC-004 | Os sete status usam a ordem canônica e variantes `info`, `warning`, `success` ou `danger`; qualquer string desconhecida recebe variante `neutral` e rótulo localizado. | Aprovado para o navegador. |
| SC-005 | Seleção, criação somente no vazio real autorizado, limpeza de filtros e retry preservam payloads, permissão e bloqueios da view; falha concorrente obsoleta é ignorada e não dispara auto-open. | Aprovado para ações de navegação automatizadas. |
| SC-007 | As 12 chaves de loading/refresh, falha, zero filtrado, vazio e expansão existem em PT/EN; data e status desconhecido foram verificados nos dois idiomas. | Aprovado para T024-T026. |
| SC-009 | A identidade selecionada e sua orquestração permanecem ativas quando o detalhe está nulo e após sucessos/falhas obsoletas da lista; loading/falha têm feedback exclusivo e zero filtrado aparece somente no sucesso assentado. | Aprovado estruturalmente; jornada autenticada permanece em T030. |

### Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner em `DraftsView.vue` ou `components/drafts/**/*.vue`.
- Sincronização `pt.json`/`en.json`: aprovada; as 12 chaves de `drafts.navigator` existem em ambos.
- Acentuação portuguesa: revisada em loading, refresh, falha, zero filtrado, vazio, expansão e recolhimento.
- Placeholders, botões, títulos, badges, toasts, validações e empty states: revisados; o navegador usa somente chaves de tradução.
- Validações frontend: nenhuma validação nova; mensagens existentes permanecem localizadas.
- Backend: nenhuma mensagem, validação ou resource alterado; atualização não necessária.
- Novos arquivos: `DraftNavigator.vue` e `DraftNavigator.spec.ts` respeitam o padrão de internacionalização e a fronteira de apresentação.

### Gates finais

- Suíte completa: `npm test`; 37 arquivos e 349 testes aprovados, sem falhas.
- Build: `npm run build`; 2.764 módulos transformados e build concluído. Permanecem somente os avisos não bloqueantes já conhecidos de anotações `PURE` em dependências e chunk acima de 500 kB.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos após adotar o padrão local de tipagem de eventos DOM.
- Internacionalização: `npm test -- --run src/i18n/i18n.spec.ts`; 28 testes aprovados, sem falhas.
- Whitespace: `git diff --check`; aprovado sem saída antes do fechamento documental; reexecutado após a atualização do relatório.
- Escopo: nenhum serviço, backend, contrato HTTP, dependência, token ou regra de domínio foi alterado.

## T027-T029 - Responsividade e acessibilidade estrutural

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T027 | Concluída | Os sete arquivos de especificação exigidos e as coberturas complementares de `DraftVisualSetup` e `DraftReasonDialog` verificam ordem, regiões, estados textuais/ARIA, controles nomeados, alvos e rolagem estrutural. |
| T028 | Concluída | `main.css` recebeu estilos tardios escopados por `.drafts-page` para shell, navegador, workspace, cabeçalho, preparação, Discord, rail e board; seletores genéricos permanecem intactos para o `DraftBoard` legado e outras páginas. |
| T029 | Concluída | O shell usa `260px minmax(0, 1fr)` acima de 1024px, navegador horizontal entre 769px e 1024px, seleção compacta até 768px, proteção de overflow, controles/links/labels acionáveis de 44px, nomes quebráveis e movimento reduzido. |
| T030 | Concluída | Chromium real via `agent-browser` aprovou os quatro viewports, oito estados, listas 0/1/10/14/30, teclado, alvos, semântica, PT/EN, movimento reduzido e console; evidências detalhadas na seção própria de T030. |

### RED

- Comando: `npm test -- --run src/views/DraftsView.spec.ts src/components/drafts/DraftNavigator.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts`.
- Resultado: 7 arquivos falharam como esperado; 7 testes novos falharam e os 162 testes anteriores passaram.
- Motivos esperados: shell e board ainda não tinham nomes programáticos; não existiam contratos escopados para layout lateral/horizontal/compacto, roster auto-fit, ações semânticas, status Discord, rail conectado, alvos de 44px, quebra segura ou movimento reduzido.
- RED complementar do board: `npm test -- --run src/components/drafts/visual/DraftVisualBoard.spec.ts`; 2 testes falharam e 15 passaram por ausência de `touch-action`, ativação por Espaço e ocultação dos avatares decorativos.

### GREEN

- Focado inicial: os sete arquivos foram aprovados com 169 testes e 0 falhas.
- Board complementar: `DraftVisualBoard.spec.ts` foi aprovado com 17 testes e 0 falhas após oferecer ativação por Espaço, iniciais decorativas ocultas e manipulação por toque.
- A ordem de leitura permanece navegador, contexto/progresso/ações e conteúdo operacional; o shell e o board usam rótulos localizados existentes.
- Correção pós-revisão: o rail operacional é conectado horizontalmente acima de 1024px e verticalmente até 1024px; Discord agora é um bloco irmão paralelo, fora do `<ol>` e de seus conectores.
- Correção pós-revisão: etapas e integração exibem estado localizado e expõem o mesmo significado por `aria-label`; cor deixou de ser o único sinal de estado.
- Correção pós-revisão: roster, pool, times e grades do setup não criam overflow vertical interno; o formulário do setup é a única região vertical própria do modal.

### Ledger de evidências

| Critério | Evidência desta fase | Estado |
|----------|----------------------|--------|
| SC-002 | Contratos de CSS cobrem 260px/minmax, breakpoints de 1024px e 768px, coluna única, `overflow-x: clip` e filhos com `min-width: 0`. | Aprovado estruturalmente; medição e screenshots reais permanecem em T030. |
| SC-003 | Roster usa `auto-fit` com mínimo limitado ao contêiner e uma coluna até 768px; nomes usam `overflow-wrap: anywhere`. | Aprovado estruturalmente; inspeção de 0/1/10/14/30 em 320px permanece em T030. |
| SC-006 | Progresso usa lista ordenada somente para etapas, estados têm texto e ARIA em PT/EN, campos possuem nomes, e botões, links, campos e label do checkbox têm alvo mínimo de 44px. | Aprovado estruturalmente; jornada completa por Tab/Shift+Tab permanece em T030. |
| SC-010 | Roster, pool e grades do setup usam overflow vertical visível; o formulário do setup concentra sua única rolagem e o tablet mantém apenas navegação horizontal contida. | Aprovado estruturalmente; rolagem real com lista extensa permanece em T030. |

### Pré-requisitos e bloqueio de T030

- O devcontainer `rinhadaslendas_devcontainer-app-1` e o PostgreSQL saudável estão ativos, mas o frontend Vite em `http://localhost:5173` não foi iniciado nesta fase.
- É necessária uma sessão dedicada autenticada com perfil de jogador e permissão Moderador+, sem registrar credenciais nos comandos ou artefatos.
- São necessários drafts representativos dos sete status, fallback desconhecido, nomes longos e listas com 0, 1, 10, 14 e 30 participantes.
- Depois desses dados, T030 exige `agent-browser` real em 1440x900, 1024x768, 768x900 e 320x844, jornada por Tab/Shift+Tab/Enter/Espaço, checagem de `scrollWidth`, movimento reduzido, console e screenshots locais.
- Até essas condições serem atendidas e a execução real ser registrada, T030 permanece desmarcada.

### Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner em `DraftsView.vue` ou `components/drafts/**/*.vue`.
- Sincronização `pt.json`/`en.json`: aprovada; `progress.attention`, `accessibility.stateLabel`, `visualBoard.playerSearchLabel` e `visualBoard.teamNameLabel` existem nos dois idiomas.
- Backend e resources: nenhuma mensagem, validação ou resource alterado; atualização não necessária.
- Acentuação portuguesa: revisada em “Atenção”, “Nome do time” e “Buscar jogadores disponíveis”.
- Placeholders, botões, títulos, badges, toasts, validações e empty states: revisados; nenhuma nova mensagem visível foi introduzida.
- Validações frontend e backend: nenhuma validação nova; mensagens existentes permanecem localizadas.
- Novos arquivos: `DraftVisualSetup.spec.ts` não contém texto de produção e respeita o padrão de internacionalização.

### Gates antes da revisão pós-T029

- Suíte focada: 7 arquivos e 170 testes aprovados, sem falhas.
- Suíte completa: 37 arquivos e 357 testes aprovados, sem falhas.
- Build: 2.764 módulos transformados e build concluído; permanecem somente os avisos não bloqueantes conhecidos de anotações `PURE` em dependências e chunk acima de 500 kB.
- Lint: `npm run lint:check` aprovado sem erros ou avisos.
- Internacionalização: `i18n.spec.ts` aprovado com 28 testes e 0 falhas.
- Whitespace e diff: `git diff --check` aprovado sem saída; somente os 12 arquivos esperados de T027-T029 e suas evidências foram alterados.

### Revisão pós-T029

#### RED

- Rail e Discord: `DraftStateRail.spec.ts` executou 19 testes e todos falharam porque não havia rótulo textual/ARIA de estado e Discord ainda era anexado ao `<ol>` operacional.
- Setup, diálogo e board: o comando focado dos três componentes executou 56 testes; 7 falharam e 49 passaram por grades com overflow próprio, checkbox sem label de 44px, diálogo portado sem escopo, links sem alvo mínimo e campos sem nomes localizados.
- Textarea: o teste focado de alvos executou 1 caso falho e ignorou os outros 19, confirmando que a regra genérica ainda omitia `textarea`.
- Autocomplete dos times: 2 casos PT/EN falharam e 18 foram ignorados porque os campos recém-nomeados ainda não declaravam `autocomplete="off"`.

#### GREEN

- O `<ol>` contém somente etapas operacionais canônicas; o bloco Discord é irmão posterior com `role="status"`, texto localizado, `aria-label` equivalente e nenhum conector sequencial.
- `completed`, `current`, `pending`, `attention`, `terminal` e `unknown` possuem texto visível e nome acessível equivalente em português e inglês.
- Campos de nome de time e busca de jogadores têm `name`, `autocomplete="off"` e `aria-label` localizados; links de detalhes, diálogo portado, checkbox e demais controles possuem contratos de alvo mínimo.
- `DraftVisualSetup` concentra overflow vertical no formulário; grades de jogadores usam `max-height: none` e `overflow: visible`.
- Suíte focada ampliada: 10 arquivos e 241 testes aprovados, sem falhas.
- T030 continua pendente; nenhuma evidência de navegador, screenshot ou medição real foi criada nesta revisão.

#### Gates finais pós-revisão

- Suíte focada: 10 arquivos e 241 testes aprovados, sem falhas.
- Suíte completa: 38 arquivos e 367 testes aprovados, sem falhas.
- Build: 2.764 módulos transformados e build concluído; permanecem somente os avisos não bloqueantes conhecidos de anotações `PURE` e chunk acima de 500 kB.
- Lint: `npm run lint:check` aprovado sem erros ou avisos.
- Internacionalização: `i18n.spec.ts` aprovado com 28 testes e 0 falhas; scanner de texto hardcoded e paridade PT/EN aprovados.
- Diff: `git diff --check` aprovado sem saída antes do fechamento documental.

### Fechamento final de T027-T029

#### RED

- `DraftReasonDialog.spec.ts` executou o caso focado com 1 falha e 33 testes ignorados: o diálogo portado renderizava três botões, incluindo o fechamento `icon-sm`, mas o contrato CSS garantia somente altura mínima.

#### GREEN

- O teste renderizado confirma os três botões e identifica explicitamente `button[data-slot="dialog-close"]`.
- `.draft-reason-dialog button` agora impõe `min-width: 44px` e `min-height: 44px`; o contrato separado de `textarea` preserva sua altura mínima.
- T030 permanece pendente; nenhuma validação em navegador, screenshot ou medição real foi executada neste fechamento.

#### Gates finais do fechamento

- Suíte focada: 10 arquivos e 241 testes aprovados, sem falhas.
- Suíte completa: 38 arquivos e 367 testes aprovados, sem falhas.
- Build: 2.764 módulos transformados e build concluído; permanecem somente os avisos não bloqueantes conhecidos de anotações `PURE` e chunk acima de 500 kB.
- Lint: `npm run lint:check` aprovado sem erros ou avisos.
- Internacionalização: `i18n.spec.ts` aprovado com 28 testes e 0 falhas; nenhuma chave ou mensagem localizada foi alterada neste fechamento.
- Diff: `git diff --check` aprovado sem saída antes da atualização final deste relatório.

## T031-T034 - Correção dos dias selecionados em Atualizações

### Progresso

| Tarefa | Estado | Evidência |
|--------|--------|-----------|
| T031 | Concluída | Registro e serviço exigem `2026.07.3` no topo, busca localizada, histórico completo, `.2` sem destaque e `.3` como única release destacada. |
| T032 | Concluída | Card, hero e i18n exigem data, ordem, metadados e conteúdo PT/EN exatos da correção. |
| T033 | Concluída | `presence-schedule-weekday-selection-fix` foi adicionado com categoria `fix`, área `drafts`, detalhe `selected-weekday-feedback` e link `AppRoutes.Settings`; todo o histórico anterior foi preservado. |
| T034 | Concluída | Título, resumo e detalhe orientados a benefício foram adicionados em português e inglês sem mencionar o redesenho ainda não publicado. |

### RED

- Comando: `npm test -- --run src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 5 arquivos falharam como esperado; 12 testes falharam e 41 passaram.
- Motivos esperados: `2026.07.3` ainda não existia, `.2` permanecia destacada, latest e busca não encontravam a correção, card e hero ainda apresentavam `.2`, e as chaves/editorial PT/EN de `.3` estavam ausentes.

### GREEN

- Comando focado: `npm test -- --run src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 5 arquivos e 53 testes aprovados, sem falhas (`constants`: 6; `services`: 8; card: 4; view: 7; i18n: 28).
- A ordem exata contém dez releases, com `.3` no topo e `.2` imediatamente depois; os detalhes completos de `.2` e `.1` permanecem protegidos por testes.
- A busca localizada por “dias selecionados” encontra somente `.3` com filtro de correção; a busca por Discord continua encontrando `.2`.

### Ledger de evidências

| Critério | Evidência desta fase | Estado |
|----------|----------------------|--------|
| SC-008 | Registro, serviço, card e hero confirmam `2026.07.3` no topo, data `2026-07-25`, único destaque e link interno para Configurações. | Aprovado por testes automatizados; inspeção autenticada permanece no T030. |
| FR-025 | O histórico descreve a confirmação visual dos dias selecionados com categoria `fix` e área `drafts`. | Aprovado. |
| Preservação histórica | Testes exatos mantêm as dez releases, `.2` com cinco detalhes e `.1` com quinze detalhes. | Aprovado. |
| Segurança editorial | PT/EN usam somente benefícios visíveis e rejeitam menções a “redesenho” ou “redesign”. | Aprovado. |

### Gates

- Suíte focada: 5 arquivos e 53 testes aprovados, sem falhas.
- Suíte completa: `npm test`; 38 arquivos e 368 testes aprovados, sem falhas.
- Build: `npm run build`; 2.764 módulos transformados e build concluído. Permanecem somente os avisos não bloqueantes já conhecidos de anotações `PURE` em dependências e chunk acima de 500 kB.
- Lint: `npm run lint:check`; aprovado sem erros ou avisos.
- Internacionalização: `npm test -- --run src/i18n/i18n.spec.ts`; 28 testes aprovados, sem falhas.
- Whitespace: `git diff --check`; aprovado sem saída antes do fechamento documental.

### Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: não encontrados; o conteúdo editorial está exclusivamente em `pt.json` e `en.json`.
- Textos visíveis hardcoded no backend: não encontrados; backend não foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada por paridade integral de folhas e pelo contrato exato de `2026_07_3`.
- Resources backend: nenhuma atualização necessária; nenhuma mensagem ou validação backend foi alterada.
- Acentuação portuguesa: revisada em “agendamentos”, “presença”, “Confirmação”, “selecionados” e “dúvidas”.
- Placeholders, botões, títulos, badges, toasts e estados vazios: revisados; somente título, resumo e detalhe editorial foram adicionados.
- Validações frontend e backend: nenhuma validação nova; mensagens existentes permanecem localizadas.
- Novos arquivos: nenhum; todos os arquivos alterados respeitam o padrão de internacionalização.

## T030 - Validação real no Chromium

### Escopo reexecutado

- Reexecução final: 2026-07-26, `agent-browser 0.29.1`, Chromium 150, Node.js 22.22.1 e npm 9.2.0.
- Aplicação real: Vite em `http://127.0.0.1:4174`, usando router, `AppShell`, autenticação, `DraftsView`, serviços e componentes de produção nas rotas `/drafts`, `/atualizacoes` e `/configuracoes`.
- Backend temporário: HTTP/SignalR determinístico em `http://127.0.0.1:4311`, exclusivamente sob `/tmp/opencode/feature021-e2e`; nenhum código alternativo foi criado no repositório.
- `run-all.sh` executa cleanup inicial, setup de processos, toda a validação de navegador, gates, verificador de evidências e cleanup final. A execução final terminou com `28` assertions aprovadas e `0` falhas em `evidence-summary.json`.
- Scripts reproduzíveis: `setup.sh`, `cleanup.sh`, `browser-helpers.sh`, `run-keyboard-traversal.sh`, `run-bilingual-journeys.sh`, `run-roster-validation.sh`, `run-viewport-actions.sh`, `run-cross-checks.sh`, `run-gates.sh`, `verify-evidence.js` e `run-all.sh`.

### Teclado sem foco forçado

- Nenhum script contém `agent-browser focus`, alias equivalente ou chamada DOM `.focus()`. O verificador lê todos os scripts `.sh` e rejeita qualquer ocorrência.
- Cada cenário navega novamente, espera `document.activeElement === document.body`, registra o body inicial e avança exclusivamente com `Tab` ou `Shift+Tab`. Ações são ativadas somente com Enter ou Espaço; texto no campo já focado usa `keyboard type`.
- Foram preservados `417` registros em `keyboard-traversal.jsonl`: `361` são transições Tab/Shift+Tab e `12` são checkpoints após a conclusão de comandos. Cada registro contém cenário, sequência, evento, tag, id, name, role, test ID, classe, texto, outline calculado, box shadow e dimensões.
- Todo registro posterior a uma ação possui `body: false` e `outline.style` diferente de `none`. Os 12 checkpoints pós-comando usam outline `solid`: confirmar presença, incluir/remover presença por diálogo, encerrar presença, definir capitães, definir ordem, escolher, finalizar, republicar e cancelar.
- A travessia alcançou e operou navegador compacto, busca, filtro de status, confirmação, busca/seleção/inclusão manual, diálogos, remoção, encerramento, dois capitães, definição de capitães, ordem, pick realtime, finalização, republicação Discord e cancelamento.
- O backend foi resetado antes de cada cenário funcional. Os cenários de diálogo esperam também a remoção real do overlay antes de continuar.
- Evidência bruta: `keyboard-traversal.jsonl`, `logs/keyboard-command.log`, `evals/keyboard-*.json.txt` e `screenshots/keyboard-final-cancelled.png`.

### Correção de restauração de foco

- Causa raiz: controles de comando eram removidos ou substituídos após mutações bem-sucedidas; sem sucessor explícito, a restauração do opener do diálogo e o foco do botão removido terminavam em `body`.
- RED dos componentes: `DraftWorkspaceHeader.spec.ts` e `DraftReasonDialog.spec.ts` executaram 46 testes; 3 falharam porque `focusStage()` e o tratamento de `close-auto-focus` ainda não existiam.
- RED integrado: `DraftsView.spec.ts` executou 90 testes; 8 falharam reproduzindo `document.activeElement === document.body` após confirmação, inclusão, remoção, republicação, cancelamento, encerramento, capitães, ordem, pick e finalização. O caso de SignalR passivo já passou, comprovando que atualização remota não deve mover foco.
- GREEN focado: `DraftWorkspaceHeader.spec.ts`, `DraftReasonDialog.spec.ts` e `DraftsView.spec.ts` aprovaram 136 testes. O cabeçalho expõe `focusStage()`, prioriza a ação principal habilitada da etapa e usa o próprio cabeçalho focável como fallback visível.
- `DraftReasonDialog` intercepta `close-auto-focus` somente após confirmação válida concluída; cancelamento preserva a restauração padrão do opener. `DraftsView` solicita restauração somente após mutações locais bem-sucedidas e não o faz em callbacks SignalR.

### Seis jornadas em PT e EN

`run-bilingual-journeys.sh` reseta o backend antes de cada jornada e executa interações reais por ponteiro nos dois idiomas. `bilingual-journeys.jsonl` contém `28` checkpoints visíveis.

| Jornada | Interações executadas em cada idioma | Evidência visível retida |
|---------|--------------------------------------|--------------------------|
| Presença | confirmar, buscar, selecionar, adicionar, remover e encerrar | toasts, diálogos de inclusão/remoção, contagens e status encerrado |
| Capitães | selecionar dois jogadores e definir capitães | status `Capitães definidos` / `Captains defined` |
| Ordem | sortear ordem | status `Ordem definida` / `Order defined` |
| Pick | configurar realtime e escolher jogador | progresso `2 / 8 escolhas` / `2 / 8 picks` |
| Finalização | finalizar board manual | status `Finalizada` / `Finished` |
| Discord e cancelamento | republicar presença, confirmar motivo, cancelar e confirmar motivo | dois diálogos localizados, toast de republicação e status cancelado |

- Nenhum checkpoint exibiu chave técnica. O verificador exige as seis jornadas para `pt` e `en`, conteúdo visível em todos os registros e diálogos/status terminais nos dois idiomas.
- `requests.jsonl` contém `41` mutações nesta execução e os dez métodos/caminhos funcionais distintos: confirmar, incluir, remover, encerrar, capitães, ordem, pick, finalizar, republicar e cancelar.
- Evidência bruta: `bilingual-journeys.jsonl`, `requests.jsonl`, `logs/bilingual-command.log` e `screenshots/bilingual-final.png`.

### Roster extenso e rolagem

- A matriz reproduzível configurou e mediu exatamente `0`, `1`, `10`, `14` e `30` linhas em 320x844, sem overflow horizontal nem scroller vertical interno concorrente.
- A jornada extensa iniciou com 30 linhas, adicionou presença manual para 31, removeu para 30 e encerrou mantendo 30.
- Em cada uma das quatro fases foram retidos nome e largura de todas as linhas, não apenas mínimo/máximo. Todas as linhas mediram 212px antes da inclusão, após inclusão, após remoção e após encerramento.
- A lista de scrollers internos permaneceu vazia nas quatro fases.
- Evidência bruta: `roster-matrix.jsonl`, `roster-30-journey.jsonl`, `logs/roster-command.log` e `screenshots/roster-*.png`.

### Ações por viewport

| Viewport | Teclado | Ponteiro | Alvo após ação | Overflow/scroller após ação |
|----------|---------|----------|-----------------|-----------------------------|
| 1440x900 | confirmar presença por Tab + Enter | confirmar presença por click | 176,375×44px | 0 / 0 |
| 1024x768 | confirmar presença por Tab + Enter | confirmar presença por click | 176,375×44px | 0 / 0 |
| 768x900 | confirmar presença por Tab + Enter | confirmar presença por click | 512,094×44px | 0 / 0 |
| 320x844 | confirmar presença por Tab + Enter | confirmar presença por click | 212×44px | 0 / 0 |

- Cada método usa reset próprio e registra estado antes/depois, alvo, toast, contagem, `scrollWidth/clientWidth` e scrollers internos.
- Evidência bruta: `viewport-actions.jsonl`, `logs/viewport-actions-command.log` e `screenshots/viewport-action-*.png`.

### Matriz, Atualizações e preferências

- A matriz estática complementar percorreu oito estados nos quatro viewports: 32 rotas, evals, snapshots e screenshots. Todos os casos mantiveram `AppShell`, um `main`, zero overflow horizontal, zero scroller interno, zero alvo abaixo de 44px, zero nome fora da caixa, zero chave técnica e zero alerta.
- `/atualizacoes` foi aberta e interagida em PT e EN a 1440px. `2026.07.3` permaneceu no hero e primeiro card, com um único destaque; o detalhe foi expandido e o link foi clicado, chegando realmente a `/configuracoes` com títulos “Configurações” e “Settings”.
- O menu de perfil aberto teve todos os cinco botões medidos e zero violação de 44px.
- Movimento reduzido correspondeu à media query; `scroll-behavior` foi `auto`, e animações/transições máximas foram 1ms.
- Evidência bruta: `evals/<viewport>-<estado>.json.txt`, `snapshots/<viewport>-<estado>.txt`, `screenshots/<viewport>-<estado>.png`, `updates-validation.jsonl`, `evals/profile-targets.json.txt` e `evals/reduced-motion.json.txt`.

### Console, rede e processos

- `setup.sh` grava PIDs, aguarda health checks, abre a sessão e limpa console/erros. `cleanup.sh` fecha a sessão e encerra os grupos de processo do Vite e backend; `run-all.sh` usa trap para executar cleanup também em falha.
- `final-errors.txt` está vazio. `final-console.txt` contém somente mensagens de conexão do Vite.
- `final-network.txt` contém 15.655 registros; nenhuma resposta da API ou do hub foi 4xx/5xx. A única resposta não 2xx foi o pedido automático opcional de `/favicon.ico` (404).
- O fake backend implementa também os reads de Discord/agendamentos usados após o click real de Atualizações, evitando mascarar falhas de rede da rota de destino.

### Gates e logs brutos

| Gate | Resultado | Log bruto |
|------|-----------|-----------|
| Suíte focada | 8 arquivos, 192 testes, 0 falhas | `logs/test-focused.log` |
| Suíte completa | 38 arquivos, 382 testes, 0 falhas | `logs/test-full.log` |
| Build | 2.764 módulos, concluído | `logs/build.log` |
| Lint | aprovado | `logs/lint.log` |
| i18n | 28 testes, 0 falhas | `logs/i18n.log` |
| Audit | 0 vulnerabilidades | `logs/audit.log` |
| Whitespace | sem saída | `logs/diff-check.log` |

- O build mantém apenas os avisos já conhecidos de anotações `PURE` em dependências e chunk acima de 500kB.
- `logs/verify-evidence.log` registra o resumo final `28` aprovadas e `0` falhas; detalhes e valores usados pelo verificador estão em `evidence-summary.json`.

### Ledger conservador T030

| Critério | Evidência desta reexecução | Estado |
|----------|----------------------------|--------|
| SC-001 | Matriz de estados e jornadas de avanço/terminal; zero ou uma ação conforme projeção. | Aprovado localmente. |
| SC-002 | 32 casos estáticos mais 8 ações reais antes/depois nos quatro viewports. | Aprovado localmente. |
| SC-003 | Matriz 0/1/10/14/30 e jornada 30→31→30→30 com largura de cada linha. | Aprovado localmente. |
| SC-004 | Rail/`aria-current` medidos nos oito estados pela matriz estática. | Aprovado localmente. |
| SC-005 | 41 requests de mutação reais nesta execução; guards continuam cobertos pela suíte automatizada. | Aprovado localmente; backend produtivo não foi exercitado. |
| SC-006 | 361 transições Tab/Shift+Tab sem foco forçado, 12 checkpoints pós-comando, zero foco no body e zero `outlineStyle: none` após ações. | Aprovado localmente. |
| SC-007 | Seis jornadas PT/EN com feedback visível mais scanner/paridade de 28 testes. | Aprovado localmente. |
| SC-008 | Card `.3` expandido em PT/EN e link clicado até Configurações/Settings. | Aprovado localmente. |
| SC-009 | Seis jornadas funcionais repetidas com interações reais nos dois idiomas. | Aprovado localmente contra backend determinístico. |
| SC-010 | Inclusão, remoção e avanço com roster extenso; scrollers medidos após cada etapa. | Aprovado localmente. |

### Defeitos e limites

- A reexecução rejeitada revelou perda de foco para `body` após comandos que removiam o controle ativo. O defeito foi reproduzido por testes RED e corrigido com restauração centralizada para a ação principal da etapa ou para o cabeçalho do workspace.
- O click inicial do link de Atualizações falhou apenas no harness em 320px porque o texto inline quebrado tinha o centro geométrico no espaço entre linhas. A mesma interação real foi estabilizada em 1440px, onde o hit target é contínuo, e navegou para `/configuracoes` nos dois idiomas.
- Duas lacunas do fake backend para a rota Configurações e uma corrida de fechamento de overlay foram corrigidas somente nos scripts temporários.
- Esta aprovação é local e autenticada contra backend determinístico; não afirma validação contra backend ou deploy produtivo.

### Auditoria de internacionalização T030

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner nem nas jornadas reais.
- Textos visíveis hardcoded no backend: nenhum arquivo backend foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente pelos 28 testes de i18n.
- Resources backend: nenhuma atualização necessária.
- Acentuação portuguesa: revisada nos feedbacks, diálogos, status, botões, títulos e Atualizações capturados.
- Placeholders, botões, títulos, badges, toasts e estados vazios: exercitados ou revisados em PT e EN.
- Validações frontend/backend: nenhuma validação nova; mensagens existentes continuam localizadas.
- Novos arquivos duráveis: nenhum. As alterações de produção e testes não introduzem texto visível nem novas chaves de tradução.

## T035-T040 - Fechamento local e publicação editorial do redesenho

### Ambiente e revisão inicial

- Execução: 2026-07-26, entre 02:01 e 02:14 no fuso `-03:00`.
- Revisão inicial: `7fbde8bb30a2099c4735eb3cbbc545775330aa6d` em `feature/021-redesenho-fluxo-draft`, com worktree limpa.
- Sistema: Linux 6.6.114.1-microsoft-standard-WSL2 x86_64.
- Node.js: 22.22.1; npm: 9.2.0; Vitest: 4.1.8.
- Checklist de requisitos: 16 itens concluídos, 0 pendentes.
- SC-001 a SC-010 já estavam aprovados localmente pelas evidências reproduzíveis de T030 antes da criação da entrada editorial.

### Gates antes das alterações

| Gate | Comando na raiz | Resultado |
|------|-----------------|-----------|
| Baseline focado | `npm test --prefix FrontEnd -- src/views/DraftsView.spec.ts src/components/drafts/DraftStateRail.spec.ts src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/views/SystemUpdatesView.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/i18n/i18n.spec.ts` | 7 arquivos, 164 testes aprovados, 0 falhas |
| Atualizações | `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/views/SystemUpdatesView.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/i18n/i18n.spec.ts` | 5 arquivos, 53 testes aprovados, 0 falhas |
| Jornadas de Draft | `npm test --prefix FrontEnd -- src/views/DraftsView.spec.ts src/components/drafts/DraftNavigator.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts` | 7 arquivos, 188 testes aprovados, 0 falhas |
| Suíte completa | `npm test --prefix FrontEnd` | 38 arquivos, 382 testes aprovados, 0 falhas |
| Build | `npm run build --prefix FrontEnd` | 2.764 módulos transformados; concluído |
| Lint não destrutivo | `npm run lint:check --prefix FrontEnd` | aprovado sem erros ou avisos |
| i18n | `npm test --prefix FrontEnd -- src/i18n/i18n.spec.ts` | 1 arquivo, 28 testes aprovados, 0 falhas |
| Dependências | `npm audit --prefix FrontEnd -- --audit-level=moderate` | 0 vulnerabilidades |
| Diff | `git diff --check` | aprovado sem saída |

- O build apresentou somente os avisos não bloqueantes já conhecidos de anotações `PURE` em dependências e chunk acima de 500 kB.

### Revisão de responsabilidades, duplicações, regressões e design

| Área | Evidência | Resultado |
|------|-----------|-----------|
| Responsabilidades | `DraftsView.vue` continua responsável por serviços, autorização, concorrência, realtime, notificações e diálogos; filhos de `components/drafts/` recebem dados e emitem intenções. | Conforme; nenhum deslocamento necessário. |
| Dependências | Componentes do fluxo não importam serviços executáveis; os únicos imports encontrados em filhos são tipos existentes de jogador. | Conforme. |
| Duplicações | Navegação, contexto, presença, Discord e board permanecem em regiões coesas; nenhuma repetição concreta justificou nova abstração. | Conforme; nenhuma refatoração especulativa. |
| Regressões | Matriz de 188 testes de Draft e suíte completa cobrem estados, permissões, duplicidade, realtime, foco, roster e responsividade estrutural. | Conforme. |
| Design system | CSS do fluxo permanece escopado por `.drafts-page`, reutiliza tokens oficiais, breakpoints de 1024/768px, alvos de 44px e movimento reduzido. | Conforme; nenhum token ou estilo paralelo. |
| Histórico editorial | A cobertura anterior congelava apenas ID e IDs de detalhes de `.2`. | Follow-up concreto resolvido com snapshots exatos de metadados e conteúdo PT/EN. |

- Nenhuma alteração foi feita em `DraftsView.vue`, `components/drafts/` ou `main.css`, pois a revisão não encontrou defeito concreto não coberto pelos gates.

### TDD da versão 2026.07.4

#### RED

- Comando: `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 5 arquivos falharam; 10 testes falharam e 46 passaram.
- Motivos esperados: `.4` ausente, `.3` ainda destacada, hero/card/serviço apontando para `.3`, quatro detalhes ausentes e conteúdo PT/EN de `.4` inexistente.
- O novo congelamento exato de metadados e conteúdo PT/EN de `2026.07.2` passou já no RED, comprovando preservação histórica sem exigir mudança de produção.
- RED complementar: `npm test --prefix FrontEnd -- src/services/systemUpdates.spec.ts -t "accepts sequential releases published on the same day"`; 1 teste falhou e 8 foram ignorados porque a validação rejeitava a data compartilhada por `.4` e `.3`.

#### GREEN

- Comando focado final: o mesmo comando dos cinco arquivos aprovou 57 testes e 0 falhas (`constants`: 7; `services`: 9; card: 4; view: 7; i18n: 30).
- `2026.07.4` foi adicionada no topo com data `2026-07-25`, categoria `improvement`, área `drafts`, destaque único e quatro detalhes ligados a `AppRoutes.Draft`.
- O conteúdo PT/EN descreve benefícios de hierarquia operacional, roster de presença, clareza de etapas/acessibilidade e operação responsiva/mobile, sem expor detalhes técnicos.
- Releases sequenciais na mesma data agora são válidas; unicidade de ID e versão, validade da data e ordem cronológica continuam verificadas.
- `2026.07.3` foi preservada integralmente por snapshot exato e alterada somente para `featured: false`.
- `2026.07.2` passou a ter metadados completos e todo o conteúdo editorial PT/EN congelados por testes exatos.

### Gates finais

| Gate | Comando na raiz | Resultado final |
|------|-----------------|-----------------|
| Baseline focado | `npm test --prefix FrontEnd -- src/views/DraftsView.spec.ts src/components/drafts/DraftStateRail.spec.ts src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/views/SystemUpdatesView.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/i18n/i18n.spec.ts` | 7 arquivos, 168 testes aprovados, 0 falhas |
| Atualizações | `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts` | 5 arquivos, 57 testes aprovados, 0 falhas |
| Jornadas de Draft | `npm test --prefix FrontEnd -- src/views/DraftsView.spec.ts src/components/drafts/DraftNavigator.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftStateRail.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts` | 7 arquivos, 188 testes aprovados, 0 falhas |
| Suíte completa | `npm test --prefix FrontEnd` | 38 arquivos, 386 testes aprovados, 0 falhas |
| Build | `npm run build --prefix FrontEnd` | 2.764 módulos transformados; concluído |
| Lint não destrutivo | `npm run lint:check --prefix FrontEnd` | aprovado sem erros ou avisos |
| i18n | `npm test --prefix FrontEnd -- src/i18n/i18n.spec.ts` | 1 arquivo, 30 testes aprovados, 0 falhas |
| Dependências | `npm audit --prefix FrontEnd -- --audit-level=moderate` | 0 vulnerabilidades |
| Diff | `git diff --check` | aprovado sem saída |

### Ledger final

| Critério | Evidência acumulada e de fechamento | Estado |
|----------|-------------------------------------|--------|
| SC-001 | Matriz automatizada dos sete estados, ações e terminais mais T030. | Aprovado localmente. |
| SC-002 | Contratos responsivos e 32 casos reais nos quatro viewports em T030. | Aprovado localmente. |
| SC-003 | Testes 0/1/10/14/30 e jornada real 30→31→30→30. | Aprovado localmente. |
| SC-004 | Rail conhecido/desconhecido/cancelado, texto e `aria-current`. | Aprovado localmente. |
| SC-005 | Testes de ações, permissões, locks e 41 mutações reais em T030. | Aprovado localmente. |
| SC-006 | Estrutura acessível e travessia real de 361 transições de foco. | Aprovado localmente. |
| SC-007 | Scanner, paridade integral e 30 testes de i18n; conteúdo `.4` exato em PT/EN. | Aprovado localmente. |
| SC-008 | `.3` continua preservada no histórico; `.4` assume topo e destaque após aprovação local. | Aprovado localmente. |
| SC-009 | Seis jornadas PT/EN e suíte final de 386 testes. | Aprovado localmente contra backend determinístico. |
| SC-010 | Inclusão, remoção e avanço com roster extenso sem scroller concorrente. | Aprovado localmente. |

### Auditoria completa de internacionalização

- Textos visíveis hardcoded no frontend: não encontrados em `DraftsView.vue` ou `components/drafts/**/*.vue`; scanner aprovado.
- Textos visíveis hardcoded no backend: não encontrados nas alterações; nenhum arquivo backend foi modificado.
- Sincronização `pt.json`/`en.json`: sim, paridade integral aprovada por `i18n.spec.ts`.
- Conteúdo editorial PT/EN: sim, `.4`, `.3` e `.2` possuem contratos exatos e equivalentes.
- Resources backend: sim, permanecem conformes; nenhuma atualização foi necessária.
- Acentuação portuguesa: sim, revisada em “presença”, “próxima ação”, “inclusão”, “fáceis”, “navegação”, “operação” e “preferências”.
- Placeholders, botões, títulos, badges, toasts e estados vazios: sim, revisados; a mudança adiciona somente título, resumo e detalhes editoriais localizados.
- Validações frontend e backend: sim, nenhuma mensagem de validação nova; as existentes continuam em i18n/resources.
- Novos arquivos: sim, conformes; nenhum arquivo novo foi criado.

### Limites restantes

- A validação autenticada em produção permanece posterior à integração em `main`, conforme o quickstart; este fechamento comprova os gates locais.
- O build mantém o aviso conhecido de chunk principal acima de 500 kB e avisos `PURE` de dependências; não foram introduzidos por esta release editorial.

## Revisão pós-T040 - Data real e ordem de releases no mesmo dia

### Preservação do histórico

- Esta seção complementa e supersede somente o estado final descrito no fechamento T035-T040 anterior; as evidências de `.3` no topo registradas em T031-T034 e T030 permanecem inalteradas porque comprovam corretamente o estágio anterior à publicação do redesenho.
- SC-008 passa a ser verificado em duas etapas: `.3` foi topo e destaque único antes do gate de FR-027; após a aprovação local, `.4` assume topo e destaque, enquanto `.3` permanece imutável exceto por `featured: false`.
- A data real de publicação de `2026.07.4` é `2026-07-26`. A versão permanece `2026.07.4`; nenhum conteúdo editorial PT/EN ou dado histórico de `.3` e `.2` foi reescrito.

### TDD dos achados

#### RED

- Comando: `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts`.
- Resultado: 4 arquivos falharam e 1 passou; 6 testes falharam e 52 passaram.
- Motivos esperados: registro, card e hero ainda usavam `2026-07-25`; datas renderizadas ainda eram “25 de julho de 2026” e “July 25, 2026”; a validação aceitava `.3` antes de `.4` quando ambas compartilhavam uma data.

#### GREEN

- O mesmo comando aprovou 5 arquivos e 58 testes, sem falhas (`constants`: 7; `services`: 10; card: 4; view: 7; i18n: 30).
- `publishedAt` de `.4` e as expectativas exatas de registro, `datetime`, português e inglês foram atualizados para `2026-07-26`.
- Para datas iguais e versões válidas `AAAA.MM.N`, a validação compara ano, mês e sequência numericamente e exige ordem estritamente decrescente.
- O teste válido de releases sequenciais na mesma data continua aprovado; unicidade de ID/versão, formato de versão, validade de data, ordem cronológica, traduções, links e demais validações anteriores permanecem cobertos.
- O caso invertido `.3`, `.4` na mesma data falha com `Releases on the same date must use descending version sequence`.

### Gates finais corrigidos

| Gate | Comando na raiz | Resultado final |
|------|-----------------|-----------------|
| Atualizações | `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts` | 5 arquivos, 58 testes aprovados, 0 falhas |
| Suíte completa | `npm test --prefix FrontEnd` | 38 arquivos, 387 testes aprovados, 0 falhas |
| Build | `npm run build --prefix FrontEnd` | 2.764 módulos transformados; concluído |
| Lint não destrutivo | `npm run lint:check --prefix FrontEnd` | aprovado sem erros ou avisos |
| i18n | `npm test --prefix FrontEnd -- src/i18n/i18n.spec.ts` | 1 arquivo, 30 testes aprovados, 0 falhas |
| Dependências | `npm audit --prefix FrontEnd -- --audit-level=moderate` | 0 vulnerabilidades |
| Diff | `git diff --check` | aprovado sem saída |

### Correção do ledger

| Critério | Evidência em duas etapas | Estado |
|----------|--------------------------|--------|
| SC-008 estágio 1 | T031-T034 e T030 registram `.3` no topo, destaque único, conteúdo PT/EN e navegação para Configurações antes da publicação do redesenho. | Aprovado historicamente. |
| SC-008 estágio 2 | Testes finais registram `.4` no topo, destaque único, data `2026-07-26`, conteúdo PT/EN e links para Drafts; snapshots exatos mantêm `.3` inalterada exceto pelo destaque. | Aprovado localmente após FR-027. |
| T035-T040 | Gates corrigidos, auditoria e regra de ordenação reexecutados; nota pós-T040 adicionada em `tasks.md`. | Permanecem concluídas. |

### Auditoria de internacionalização da revisão

- Textos visíveis hardcoded no frontend: não introduzidos; scanner de Draft e testes de Atualizações permanecem aprovados.
- Textos visíveis hardcoded no backend: nenhum arquivo backend foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente; as folhas de tradução não precisaram de alteração nesta revisão.
- Conteúdo `.4`, `.3` e `.2`: preservado nos dois idiomas por contratos exatos.
- Datas renderizadas: `26 de julho de 2026` e `July 26, 2026` aprovadas no card; `datetime="2026-07-26"` aprovado no card e hero.
- Resources backend: nenhuma atualização necessária.
- Acentuação portuguesa, títulos, detalhes, badges, links, toasts, placeholders e estados vazios: permanecem conformes.
- Validações frontend/backend: a nova validação é interna e não adiciona mensagem visível ao usuário; mensagens existentes continuam localizadas.
- Novos arquivos: nenhum.

### Limites preservados

- O build mantém somente os avisos conhecidos de anotações `PURE` em dependências e chunk principal acima de 500 kB.
- A validação autenticada pós-deploy continua pendente conforme o quickstart; as duas etapas editoriais e todos os gates locais estão documentados separadamente.

## Fechamento final de consistência dos artefatos T035-T040

### Achado e correção

- A revisão final encontrou uma inconsistência exclusivamente documental: SC-008 e o quickstart já definiam os dois estágios, mas o teste independente e os cenários da User Story 6, FR-026, `data-model.md` e `contracts/ui-contracts.md` ainda descreviam somente `.3` no topo e destacada.
- Os requisitos históricos de `.3` foram preservados integralmente como estágio 1, anterior ao gate de FR-027.
- O estágio final foi explicitado nos quatro artefatos: `.4` no topo e destacada após a validação do redesenho; `.3` imutável exceto por `featured: false`.
- `data-model.md` e o contrato de UI registram a metadata final de `.4`: ID `clearer-draft-operation`, versão `2026.07.4`, data `2026-07-26`, categoria `improvement`, área `drafts` e quatro detalhes ligados a `AppRoutes.Draft`.
- Nenhuma regra de produção, tradução, teste ou evidência histórica anterior foi alterada neste fechamento.

### Matriz de consistência final

| Artefato | Estágio 1 | Estágio final | Estado |
|----------|-----------|---------------|--------|
| `spec.md` User Story 6 | `.3` no topo/destaque, correção e Configurações antes de FR-027 | `.4` no topo/destaque em `2026-07-26`, Drafts e `.3` preservada | Consistente |
| `spec.md` FR-026 e SC-008 | Requisitos históricos de `.3` mantidos | Transição pós-FR-027 definida explicitamente | Consistente |
| `data-model.md` | Metadata e invariantes de `.3` mantidos | Metadata, detalhes, links e invariantes de `.4` definidos | Consistente |
| `contracts/ui-contracts.md` | Contrato anterior ao gate preservado | Contrato final e invariantes compartilhadas definidos | Consistente |
| `quickstart.md` | Validação pré-FR-027 de `.3` | Validação pós-FR-027 de `.4` | Consistente |

### Escopo de verificação

- Gate obrigatório: `git diff --check`; aprovado sem saída.
- Regressão editorial: `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/components/updates/SystemUpdateCard.spec.ts src/views/SystemUpdatesView.spec.ts src/i18n/i18n.spec.ts`; 5 arquivos e 58 testes aprovados, 0 falhas.
- Internacionalização incluída na regressão editorial: `i18n.spec.ts` aprovou 30 testes, 0 falhas.
- Locales `pt.json` e `en.json`: não alterados; nenhuma execução adicional separada de i18n é necessária além da suíte focada solicitada.

## Correções da revisão final de 2026-07-26

### Escopo e TDD

- Nenhuma tarefa foi reaberta ou remarcada. Esta seção acrescenta somente evidência dos achados finais sobre a implementação já concluída.
- RED principal: `npm test -- --run src/components/drafts/visual/DraftVisualBoard.spec.ts src/components/drafts/DraftWorkspaceHeader.spec.ts src/components/drafts/DraftNavigator.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftDiscordPublicationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`; 8 arquivos falharam, 67 testes falharam e 182 passaram. As falhas reproduziram semântica interativa aninhada, ausência de capabilities, guards de substituição, contrato de `dataRinha`, ação Discord fragmentada, alternativas de movimento, anúncios, `aria-pressed` e ajustes de design/i18n.
- RED complementar do destino inválido: o teste focado removeu `available-1` ao receber `missing-team`; 1 falha e 25 testes ignorados.
- GREEN complementar do destino inválido: o mesmo teste passou após validar o destino antes de remover o jogador.
- RED complementar de ação obsoleta: os dois cenários de cancelamento terminal falharam porque `detailRequestVersion` avançava de 3 para 4 antes da rejeição.
- GREEN complementar de ação obsoleta: os dois cenários passaram após revalidar a capability antes de criar o contexto de atualização.
- GREEN focado final: os oito arquivos principais aprovaram 250 testes antes dos dois casos complementares; a suíte final abaixo inclui ambos.

### Correções comprovadas

- `DraftVisualBoard` usa botão nativo separado para detalhes e controles irmãos para pick/substituição, contém bubbling de teclado, bloqueia substituições duplicadas e mantém drag-and-drop junto de 33 selects localizados de destino no cenário real.
- `DraftsView` revalida substituição por permissão, salvamento, status, time, titular e reserva elegível; a primeira chamada ativa `saving` antes da segunda chamada rápida.
- O cabeçalho recebe `dataRinha` do resumo selecionado pelo mesmo ID e usa o encerramento da presença somente como fallback; seu título é `h2`.
- Filtros de rota expõem `aria-pressed`; turno e progresso realtime usam região `aria-live="polite"`.
- `DraftPreparationPanel` e `DraftDiscordPublicationPanel` não importam serviços/autenticação nem derivam autorização de ação; recebem capabilities explícitas calculadas e revalidadas no pai.
- Republicação usa somente `{ type: 'republishDiscord', publicationType, publicationStatus }` entre painel, diálogo e handler, com matriz dos três tipos.
- Diálogo não aplica autofocus em viewport mobile e contém overscroll. Navegador usa exemplo localizado com `…`, status com `autocomplete="off"` e `--font-data`.
- Animações alteradas usam `transform`/`opacity`; `theme-color` escuro foi adicionado. O `overflow-x: clip` foi removido somente depois da medição real descrita abaixo.

### Validação real do overflow removido

- Harness reutilizado: `/tmp/opencode/feature021-e2e`, aplicação real em `http://127.0.0.1:4174/drafts?draftId=open` e backend determinístico em `http://127.0.0.1:4311`.
- `1440x900`: `scrollWidth=1440`, `clientWidth=1440`, overflow falso, zero scroller vertical interno e zero alvo menor que 44px.
- `1024x768`: `scrollWidth=1024`, `clientWidth=1024`, overflow falso, zero scroller vertical interno e zero alvo menor que 44px.
- `768x900`: `scrollWidth=768`, `clientWidth=768`, overflow falso, zero scroller vertical interno e zero alvo menor que 44px.
- `320x844`: `scrollWidth=320`, `clientWidth=320`, overflow falso, zero scroller vertical interno e zero alvo menor que 44px.
- Inspeção DOM em 320px: zero controles interativos aninhados, zero linhas com `role="button"`, 33 botões de detalhes, 33 controles de destino, filtros com estados `true,false,false,false,false,false` e live region `polite`.
- Evidências locais: `evals/review-fix-overflow-*.json` e `screenshots/review-fix-open-320x844.png`.

### Gates finais da revisão

| Gate | Resultado final |
|------|-----------------|
| Suíte completa | 38 arquivos, 405 testes aprovados, 0 falhas |
| Build | 2.764 módulos transformados; concluído |
| Lint não destrutivo | aprovado sem erros ou avisos |
| i18n | 31 testes aprovados, 0 falhas |
| Dependências | 0 vulnerabilidades em nível moderado ou superior |

- O build mantém somente os avisos conhecidos de anotações `PURE` em dependências e chunk principal acima de 500 kB.
- Auditoria arquitetural: `DraftsView` preserva serviços, autorização, concorrência e guards; filhos permanecem de apresentação. Nenhuma mudança ocorreu em backend, bot, banco, contratos HTTP ou dependências.

### Auditoria de internacionalização da revisão final

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner do fluxo de Draft.
- Textos visíveis hardcoded no backend: não encontrados; backend não foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente por 31 testes.
- Resources backend: nenhuma atualização necessária.
- Acentuação portuguesa: revisada em detalhes, destinos, anúncios, placeholders e feedbacks de carregamento/salvamento.
- Placeholders, botões, títulos, badges, toasts e estados vazios: revisados; novos textos usam somente chaves equivalentes PT/EN.
- Validações frontend/backend: mensagens existentes continuam localizadas; nenhuma validação backend foi alterada.
- Novos arquivos: nenhum; todos os arquivos alterados respeitam o padrão.

### Limites restantes

- A validação de navegador usa backend determinístico local, não backend ou deploy produtivo.
- Permanecem apenas os avisos de build já conhecidos; refatorações de ownership CSS, cards de time, arquivos de teste e migração inalterada de `PlayerDetailsDrawer` continuaram fora do escopo.

## Fechamento dos achados remanescentes da revisão final

### TDD e correções

- RED: `npm test -- --run src/views/DraftsView.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts src/i18n/i18n.spec.ts`; 4 arquivos falharam, 8 testes falharam e 188 passaram.
- Motivos esperados: a data selecionada dependia da lista filtrada; o diálogo movia foco no mobile e não limitava a altura; controles de movimento não possuíam `name`/`autocomplete`, restauração de foco ou anúncio; a região realtime existia em modo manual; a nova tradução e quatro reticências tipográficas estavam ausentes.
- GREEN focado final: o mesmo comando aprovou 4 arquivos e 196 testes, sem falhas (`DraftsView`: 100; `DraftReasonDialog`: 38; `DraftVisualBoard`: 27; i18n: 31).
- `DraftsView` captura e atualiza `dataRinha` quando o resumo selecionado está disponível, preserva o valor quando filtros posteriores excluem o item e mantém o fallback do detalhe para deep links sem resumo.
- `DraftReasonDialog` impede o autofocus padrão e consulta `matchMedia` em cada evento `open-auto-focus`: foca o motivo no desktop e preserva o opener no mobile. O conteúdo limita a altura ao viewport, rola verticalmente e contém overscroll.
- `DraftVisualBoard` identifica todos os selects de movimento, desativa autocomplete, restaura foco dentro da linha movida, anuncia jogador e destino em PT/EN e renderiza a região de status realtime somente no modo realtime.
- O contrato de UI registra `DraftVisualBoard` como exceção explícita de estado interativo local, sem alterar a fronteira de serviços, autenticação ou autorização.

### Navegador real

- Harness autenticado determinístico reutilizado: `/tmp/opencode/feature021-e2e`, frontend em `http://127.0.0.1:4174` e backend em `http://127.0.0.1:4311`.
- Filtro server-side `Finalizada` excluiu visualmente o draft ativo `open`, mas o workspace preservou `Rinha: 26/07/2026` e a mesma identidade selecionada.
- Em modo manual, `[data-realtime-announcement]` não foi renderizado. O controle `draft-move-player-201` apresentou `autocomplete="off"`; após mover para `open-team-a`, o jogador apareceu no time, o foco foi restaurado ao botão de detalhes da mesma linha e o live region anunciou `Invocador 201 foi movido para Time Alpha de Nome Extraordinariamente Longo.`.
- Em `1440x900`, abrir o diálogo focou `#draft-reason`; estilos calculados confirmaram `overflow-y: auto` e `overscroll-behavior: contain`.
- Em viewport curto `320x300`, abrir o diálogo preservou foco no botão `Cancelar`; o diálogo ficou contido em 268px, com `scrollHeight=345`, `overflow-y: auto`, `overscroll-behavior: contain` e `max-height=268px`.
- Console apresentou somente mensagens de conexão do Vite; a coleção de erros do navegador permaneceu vazia.

### Gates finais

| Gate | Comando | Resultado |
|------|---------|-----------|
| Suíte focada | `npm test -- --run src/views/DraftsView.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts src/i18n/i18n.spec.ts` | 4 arquivos, 196 testes aprovados, 0 falhas |
| Suíte completa | `npm test` | 38 arquivos, 409 testes aprovados, 0 falhas |
| Build | `npm run build` | 2.764 módulos transformados; concluído |
| Lint não destrutivo | `npm run lint:check` | aprovado sem erros ou avisos |
| Internacionalização | incluída na suíte focada | 31 testes aprovados, 0 falhas |
| Dependências | `npm audit -- --audit-level=moderate` | 0 vulnerabilidades |
| Diff | `git diff --check` | aprovado sem saída após a atualização deste relatório |

- O build mantém somente os avisos conhecidos de anotações `PURE` em dependências e chunk principal acima de 500 kB.

### Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner do fluxo de Draft.
- Textos visíveis hardcoded no backend: não encontrados; nenhum arquivo backend foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente por 31 testes.
- Resources backend: nenhuma atualização necessária.
- Acentuação portuguesa: revisada, incluindo o anúncio de movimento e `Ex.:` no placeholder de jogadores.
- Placeholders, botões, títulos, badges, toasts, estados vazios e mensagens de validação: revisados; textos não URL usam `…` e os quatro exemplos literais de OP.GG/DeepLoL preservam `...` como parte da URL.
- Validações frontend/backend: mensagens existentes continuam localizadas; nenhuma validação backend foi alterada.
- Novos arquivos: nenhum; todos os arquivos alterados respeitam o padrão de internacionalização.

## Fechamento do foco após movimento para linha filtrada

### Causa e TDD

- Causa raiz: depois do movimento, `DraftVisualBoard` procurava foco somente na nova linha do jogador. Quando busca ou rota ativa excluía essa linha do pool renderizado, o select original já havia sido removido e nenhum destino recebia foco, deixando `document.activeElement` em `body`.
- RED: `npm test -- --run src/components/drafts/visual/DraftVisualBoard.spec.ts`; 1 arquivo falhou, 2 testes falharam e 27 passaram.
- Os dois casos exatos moveram um jogador de `team-a` para `livres` sob busca ativa e para `reservas` sob filtro ADC ativo. Em ambos, movimento e anúncio já funcionavam, a linha ficava corretamente oculta e o foco caía em `body`.
- GREEN: o mesmo comando aprovou 1 arquivo e 29 testes, sem falhas.
- Correção mínima: quando a linha movida está renderizada, o foco continua dentro dela; quando está filtrada, o foco vai para `draft-player-search`, controle estável e visível. Busca, rota selecionada e anúncio localizado permanecem inalterados.
- O contrato de UI foi atualizado para explicitar o fallback sem limpeza de filtros.

### Gates finais

| Gate | Comando | Resultado |
|------|---------|-----------|
| Suíte focada | `npm test -- --run src/components/drafts/visual/DraftVisualBoard.spec.ts` | 1 arquivo, 29 testes aprovados, 0 falhas |
| Suíte completa | `npm test` | 38 arquivos, 411 testes aprovados, 0 falhas |
| Build | `npm run build` | 2.764 módulos transformados; concluído |
| Lint não destrutivo | `npm run lint:check` | aprovado sem erros ou avisos |
| Internacionalização | `npm test -- --run src/i18n/i18n.spec.ts` | 1 arquivo, 31 testes aprovados, 0 falhas |
| Diff | `git diff --check` | aprovado sem saída após esta atualização documental |

- O build mantém somente os avisos conhecidos de anotações `PURE` em dependências e chunk principal acima de 500 kB.

### Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: nenhum introduzido; o anúncio existente continua localizado.
- Textos visíveis hardcoded no backend: nenhum; backend não foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente por 31 testes; locales não precisaram de alteração.
- Resources backend: nenhuma atualização necessária.
- Acentuação portuguesa, placeholders, botões, títulos, badges, toasts, estados vazios e mensagens de validação: permanecem conformes e revisados.
- Validações frontend/backend: nenhuma mensagem ou validação foi alterada.
- Novos arquivos: nenhum.

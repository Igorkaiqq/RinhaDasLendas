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

### Ambiente e composição real

- Execução final: 2026-07-26, Node.js 22.22.1, npm 9.2.0, `agent-browser 0.29.1`, Chromium 150 e sessão isolada `feature021-real`.
- Aplicação real iniciada por Vite em `http://127.0.0.1:4174`; todas as inspeções usaram as rotas autenticadas reais `/drafts` e `/atualizacoes`, o `AppShell`, o router, os serviços e os componentes de produção.
- Um backend HTTP/SignalR determinístico externo ao repositório foi iniciado em `http://127.0.0.1:4311`. Ele implementa refresh de autenticação, permissões Moderador/Jogador, perfil, listagem e detalhe dos drafts, estado realtime, jogadores elegíveis e mutações. Nenhum token ou dado pessoal real foi usado.
- Scripts, backend, logs de comandos, requests, snapshots, avaliações e screenshots foram preservados em `/tmp/opencode/feature021-e2e`; os pontos de entrada reproduzíveis são `run-commands.sh`, `collect-matrix.sh` e `run-interactions.sh`.
- O Vite foi reiniciado antes da coleta final para garantir que o Chromium recebesse o CSS final.

### Matriz real de estados e viewports

O script `collect-matrix.sh` percorreu os oito estados abaixo em cada viewport, totalizando 32 navegações completas e 32 screenshots PNG.

| Estado | Ação primária PT | Ação primária EN | Rail esperado |
|--------|------------------|------------------|---------------|
| `PresencaAberta` | Confirmar presença | Confirm presence | `active` e cinco `pending` |
| `PresencaEncerrada` | Definir capitães | Define captains | `done`, `active` e quatro `pending` |
| `CapitaesDefinidos` | Sortear ordem de escolha | Draw pick order | dois `done`, `active` e três `pending` |
| `OrdemDefinida` | nenhuma | nenhuma | três `done`, `active` e dois `pending` |
| `Aberta` | Finalizar | Finalize | quatro `done`, `active` e `pending` |
| `Finalizada` | nenhuma | nenhuma | cinco `done` e `terminal` atual |
| `Cancelada` | nenhuma | nenhuma | `terminal`, sem etapa atual |
| `EstadoLegadoInesperado` | nenhuma | nenhuma | `unknown`, sem etapa atual |

| Viewport | Casos | Overflow horizontal | Scroll vertical interno | Violações de alvo na aplicação | Screenshots |
|----------|-------|---------------------|-------------------------|----------------------------------|-------------|
| 1440x900 | 8/8 | 0 | 0 | 0 | `screenshots/1440x900-*.png` |
| 1024x768 | 8/8 | 0 | 0 | 0 | `screenshots/1024x768-*.png` |
| 768x900 | 8/8 | 0 | 0 | 0 | `screenshots/768x900-*.png` |
| 320x844 | 8/8 | 0 | 0 | 0 | `screenshots/320x844-*.png` |

- Todos os 32 casos apresentaram exatamente um landmark `main`, `AppShell` real, `scrollWidth === clientWidth`, zero alerta, zero chave técnica, zero nome longo fora da caixa e zero controle visível abaixo de 44px.
- O progresso manteve um único `<ol>` com apenas etapas operacionais; Discord permaneceu irmão com `role="status"`, e publicações permaneceram em `<section>` independente.
- `file /tmp/opencode/feature021-e2e/screenshots/{1440x900,1024x768,768x900,320x844}-*.png` confirmou as dimensões exatas dos 32 PNGs.
- Snapshots brutos estão em `snapshots/<viewport>-<estado>.txt`; medições completas estão em `evals/<viewport>-<estado>.json.txt`.

### Listas 0/1/10/14/30

- O endpoint de teste configurou o mesmo draft `PresencaAberta` com 0, 1, 10, 14 e 30 presenças; cada variante foi recarregada na rota real em 320x844.
- As cinco variantes renderizaram contagens exatas e, para cada participante, a mesma quantidade de regiões de identidade, origem e ação.
- A variante 0 exibiu o empty state; as demais não exibiram vazio.
- Todas apresentaram largura de linha estável de 212px, zero overflow horizontal, zero scroll interno concorrente, zero alvo abaixo de 44px e zero nome longo fora da caixa.
- Evidências: `evals/roster-{0,1,10,14,30}-320x844.json.txt` e `screenshots/roster-{0,1,10,14,30}-320x844.png`.

### Teclado e mutações reais

- No navegador compacto, foco por teclado apresentou outline de 2px. `Enter` expandiu de 1 para 8 drafts e atualizou `aria-expanded`; `Tab` percorreu busca e status, `Shift+Tab` retornou à busca e Espaço recolheu a lista.
- A jornada de presença confirmou a presença própria, buscou jogador elegível, adicionou e removeu presença manual por diálogo, e encerrou a presença. Autofoco, retorno por `Shift+Tab`, foco de botões e ausência de scroll interno no diálogo foram registrados.
- A jornada seguinte selecionou dois capitães com Espaço, definiu capitães e sorteou a ordem com Enter.
- O board em modo realtime escolheu `player-201` com Enter, reduziu o pool para 29 ações e avançou o progresso para `2 / 8 escolhas`; em uma projeção restaurada, finalizou o draft com Enter.
- A jornada final abriu o diálogo de republicação, enviou o motivo, abriu o cancelamento com Espaço e cancelou o draft; o resultado foi `Cancelada` sem etapa atual.
- `requests.jsonl` comprova respostas reais para `POST /presencas/confirmar`, `POST /presencas/manual`, `DELETE /presencas/eligible-manual`, `POST /encerrar-presenca`, `POST /capitaes`, `POST /ordem-escolha`, `POST /picks`, `PATCH /finalizar`, `POST /discord/publicacoes/republicar` e `PATCH /cancelar`.
- Evidências: `interactions.log`, `snapshots/interaction-*.txt`, `snapshots/dialog-*.txt`, `screenshots/interactions-final-cancelled.png` e o log bruto de requests.

### Defeitos encontrados e RED/GREEN

- A primeira inspeção da rota real encontrou alvos de 32px a 40px no toggle/brand/navegação lateral, busca/link/ícones do topbar e gatilho de perfil. O teste `keeps every AppShell interaction target at least 44px on desktop and mobile` falhou antes da correção e passou após os mínimos de 44px em `main.css`.
- Uma segunda reprodução, ao trocar o idioma com o menu aberto, mediu cinco botões do painel de perfil com 40px. A nova assertion para `.profile-menu__panel button` falhou no RED com 1 teste falho e 3 aprovados; após adicionar somente `min-height: 44px`, `AppShell.spec.ts` passou 4/4.
- A coleta final pós-restart confirmou zero violação no shell fechado, no painel de perfil aberto e nos 32 cenários do Draft.

### PT/EN e Atualizações

- Os oito estados foram percorridos por navegação SPA em inglês em 320x844. Todos exibiram `Draft list`, status e ações localizados, zero alerta, zero chave técnica, zero overflow e zero alvo abaixo de 44px; evidências em `evals/en-<estado>-320x844.json.txt`.
- Em português, a matriz completa exibiu as ações e status acentuados esperados. Nenhum cenário PT/EN exibiu padrões `drafts.*` ou `updates.*`.
- `/atualizacoes` foi inspecionada autenticada em PT e EN: `2026.07.3` aparece no hero e como primeiro card, `presence-schedule-weekday-selection-fix` é o primeiro ID, existe exatamente um `.system-update-card--latest`, os títulos são “Dias selecionados mais claros” e “Clearer selected weekdays”, e o detalhe aponta para `/configuracoes`.
- Evidências: `evals/updates-{pt,en}.json.txt`, `snapshots/updates-{pt,en}.txt` e `screenshots/updates-{pt,en}-320x844.png`.

### Movimento, console e rede

- Com `prefers-reduced-motion: reduce`, a media query correspondeu, `scroll-behavior` foi `auto`, e as maiores durações calculadas de animação e transição foram 1ms; não houve overflow. Evidência: `evals/reduced-motion.json.txt` e `screenshots/reduced-motion-open-320x844.png`.
- Depois de limpar console e erros antes da matriz final, `final-console.txt` contém somente mensagens de debug de conexão do Vite e `final-errors.txt` está vazio.
- `final-network.txt` registrou zero resposta 4xx/5xx da API e do hub. A única resposta não 2xx da sessão inteira foi a consulta automática e não funcional do Chromium a `/favicon.ico` (404).

### Gates finais

- Suíte focada: 8 arquivos e 181 testes aprovados, sem falhas.
- Suíte completa: 38 arquivos e 369 testes aprovados, sem falhas.
- Build: 2.764 módulos transformados e build concluído; permaneceram somente avisos conhecidos de anotações `PURE` em dependências e chunk acima de 500 kB.
- Lint: `npm run lint:check` aprovado sem erros ou avisos.
- Internacionalização: `i18n.spec.ts` aprovado com 28 testes; scanner de hardcodes e paridade integral PT/EN aprovados.
- Dependências: `npm audit -- --audit-level=moderate` retornou 0 vulnerabilidades.
- Whitespace: `git diff --check` aprovado sem saída antes da atualização documental.

### Ledger de evidências T030

| Critério | Evidência objetiva | Estado |
|----------|--------------------|--------|
| SC-001 | Matriz real PT/EN dos oito estados, zero ou uma ação primária e nenhum avanço terminal/desconhecido. | Aprovado. |
| SC-002 | 32 screenshots reais e igualdade de largura nos quatro viewports. | Aprovado. |
| SC-003 | Variantes 0/1/10/14/30 reais em 320px com estrutura, largura e nomes preservados. | Aprovado. |
| SC-004 | Sete estados canônicos, cancelado e desconhecido com rail e `aria-current` esperados. | Aprovado. |
| SC-005 | Dez mutações reais, payloads registrados, resultado atualizado e guards automatizados na suíte. | Aprovado. |
| SC-006 | Tab, Shift+Tab, Enter e Espaço, foco visível e zero alvo abaixo de 44px após RED/GREEN. | Aprovado. |
| SC-007 | Matrizes PT/EN, scanner e paridade integral sem chave técnica ou hardcode visível. | Aprovado. |
| SC-008 | `/atualizacoes` real em PT/EN com `.3` no topo, um destaque e link para Configurações. | Aprovado. |
| SC-009 | Jornadas reais de presença, capitães, ordem, escolha, finalização, Discord e cancelamento; estados completos PT/EN. | Aprovado. |
| SC-010 | Zero região vertical interna concorrente, inclusive inclusão/remoção/avanço e lista de 30. | Aprovado. |

### Auditoria de internacionalização T030

- Textos visíveis hardcoded no frontend: não encontrados pelo scanner nem nas rotas exercitadas.
- Textos visíveis hardcoded no backend: não encontrados; nenhum arquivo backend foi alterado.
- Sincronização `pt.json`/`en.json`: aprovada integralmente pelo gate de 28 testes.
- Resources backend: nenhuma atualização necessária; não houve mudança backend.
- Acentuação portuguesa: revisada em status, ações, métricas, presença, Discord e Atualizações.
- Placeholders, botões, títulos, badges, toasts e estados vazios: revisados em PT e EN nas rotas reais.
- Validações frontend e backend: nenhuma validação nova; as existentes continuam usando i18n/resources.
- Novos arquivos: nenhum arquivo de produção novo; teste, CSS e documentação respeitam o padrão.

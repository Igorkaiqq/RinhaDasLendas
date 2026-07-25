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

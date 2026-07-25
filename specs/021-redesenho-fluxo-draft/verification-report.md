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

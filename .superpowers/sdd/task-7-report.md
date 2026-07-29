# Relatório da Task 7

## Status

Implementação concluída no worktree `feature-024` para a feature 028.

## RED / GREEN

### Frontend

- RED: `DraftSubstitutionDialog.spec.ts` falhou primeiro por componente ausente e, após o shell compilável, com 5 falhas comportamentais para seleção explícita, capitão, acessibilidade e foco.
- GREEN: 5 testes do diálogo passaram após a implementação com componentes shadcn-vue existentes.
- RED: `DraftVisualBoard.spec.ts` confirmou que o board ainda emitia automaticamente a primeira reserva e não permitia substituir o capitão.
- GREEN: 40 testes do board passaram com abertura do diálogo, payload `novoCapitaoId`, compatibilidade v1 e restauração de foco.
- RED: `DraftsView.spec.ts` confirmou que elegibilidade não era projetada ao board e que o payload v2 sem novo capitão não era rejeitado defensivamente.
- GREEN: 142 testes da view passaram com elegibilidade projetada e validação do time resultante.
- Regressão combinada: 220 testes focados passaram.
- Suíte completa: 39 arquivos e 514 testes passaram.

### Backend

- RED: as novas jornadas integradas reproduziram HTTP 500 em escolhas de modo concorrentes.
- Investigação: transições estruturais concorrentes podiam colidir nos índices únicos de participantes/times antes da atualização do token do agregado; essas constraints não eram classificadas como conflito de versão.
- RED focado: 2 casos de `DraftMontagemSaveConflictClassifierTests` falharam para as constraints estruturais.
- GREEN: as constraints foram classificadas como `ConflitoDeVersao`, resultando em `MV103`/HTTP 409; 7 testes focados passaram.
- Jornadas integradas: Manual v2, TempoReal v2 com timeout/picks/substituição/finalização, autorização Admin+/negações, concorrência e compatibilidade v1 passaram.
- Suíte completa: 673 testes passaram.
- Build Release: concluído com 0 warnings e 0 erros.

## Arquivos

### Criados

- `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.vue`
- `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.spec.ts`
- `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleAuthorizationIntegrationTests.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemLegacyCompatibilityIntegrationTests.cs`
- `.superpowers/sdd/task-7-report.md`

### Alterados

- `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- `FrontEnd/src/views/DraftsView.vue`
- `FrontEnd/src/views/DraftsView.spec.ts`
- `FrontEnd/src/constants/messageCode.ts`
- `FrontEnd/src/services/messageService.ts`
- `FrontEnd/src/i18n/locales/pt.json`
- `FrontEnd/src/i18n/locales/en.json`
- `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemSaveConflictClassifier.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemSaveConflictClassifierTests.cs`

## Comportamento entregue

- Reserva escolhida explicitamente; removida a seleção automática da primeira reserva.
- Saída de capitão diário v2 exige novo capitão elegível no time resultante.
- Reserva elegível não recebe capitania automaticamente.
- Payload `novoCapitaoId` preservado do diálogo ao serviço.
- Draft v1 preserva contrato legado sem exigir `novoCapitaoId`.
- Dialog com título/descrição acessíveis, validação `aria-invalid`, Escape, foco inicial desktop/mobile e restauração de foco.
- Layout vertical e conteúdo limitado à viewport móvel, sem novos tokens ou dependências.
- Conflitos estruturais concorrentes retornam conflito de estado em vez de erro interno.
- Nenhum arquivo de publicação Discord ou SignalR foi alterado.

## Auditoria de internacionalização

- Textos visíveis hardcoded no frontend: **Não encontrados**.
- Mensagens hardcoded no backend: **Não encontradas**.
- `pt.json` e `en.json` sincronizados: **Sim**, comprovado por `i18n.spec.ts` na suíte completa.
- Resources backend equivalentes: **Sim**, `MV107` a `MV110` existem em default, `pt-BR` e `en-US`.
- Códigos frontend equivalentes: **Sim**, `messageCode.ts` e `messageService.ts` atualizados em PT/EN.
- Acentuação em português revisada: **Sim**.
- Placeholders, botões, títulos, erros e mensagens revisados: **Sim**.
- Validações frontend/backend usam i18n/resources: **Sim**.
- Novos arquivos respeitam o padrão: **Sim**.

## Verificações

- `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test ... --configuration Release`: 673 aprovados.
- `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build ... --configuration Release`: sucesso, 0 warnings, 0 erros.
- `npm run lint:check`: sucesso.
- `npm test`: 514 aprovados.
- `npm run build`: sucesso.
- `git diff --check`: sucesso.

## Preocupações

- O build Vite mantém avisos não bloqueantes já emitidos por anotações `PURE` de dependências e pelo chunk principal acima de 500 kB; nenhum deles foi introduzido ou ampliado no escopo desta task.
- A responsividade e o foco mobile foram cobertos por testes automatizados de componente; não houve alteração de tokens ou CSS global.

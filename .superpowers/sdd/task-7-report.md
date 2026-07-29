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
- Investigação: transições estruturais concorrentes podiam colidir nos índices únicos de participantes/times antes da atualização do token do agregado.
- RED focado inicial: 2 casos de `DraftMontagemSaveConflictClassifierTests` falharam para as constraints estruturais.
- GREEN inicial: as colisões concorrentes passaram a resultar em `MV103`/HTTP 409 em vez de HTTP 500.
- Jornadas integradas: Manual v2, TempoReal v2 com timeout/picks/substituição/finalização, autorização Admin+/negações, concorrência e compatibilidade v1 passaram.
- Suíte completa inicial: 673 testes passaram.
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
- `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemSaveConflictClassifierTests.cs`

## Comportamento entregue

- Reserva escolhida explicitamente; removida a seleção automática da primeira reserva.
- Saída de capitão diário v2 exige novo capitão elegível no time resultante.
- Reserva elegível não recebe capitania automaticamente.
- Payload `novoCapitaoId` preservado do diálogo ao serviço.
- Draft v1 preserva contrato legado sem exigir `novoCapitaoId`.
- Dialog com título/descrição acessíveis, validação `aria-invalid`, Escape, foco inicial desktop/mobile e restauração de foco.
- Layout vertical e conteúdo limitado à viewport móvel, sem novos tokens ou dependências.
- Conflitos estruturais concorrentes retornam conflito de estado somente quando a versão original rastreada está defasada; violações estruturais sem versão stale continuam erros reais de persistência.
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

## Correções da revisão

### Classificação de concorrência

- RED: constraints estruturais ainda eram classificadas globalmente pelo nome, e uma escrita única inconsistente retornava `MV103` em vez de `DbUpdateException`.
- GREEN: o classifier apenas identifica a natureza estrutural; o repository consulta `VersaoEstado` no banco após o rollback e compara com o valor original rastreado antes de converter para conflito.
- Cobertura: concorrência estrutural stale e violação estrutural sem versão stale.

### Contexto e ciclo do diálogo

- RED: o board mantinha objetos stale, não invalidava mudanças de projeção e destruía o diálogo antes do resultado do serviço.
- GREEN: o contexto contém apenas IDs, versão e snapshots de IDs de membros/reservas; time e jogador são sempre derivados da projeção atual.
- O diálogo fecha com restauração de foco quando versão, status, membership ou reservas mudam. Quando o gatilho deixa de existir, o foco retorna ao shell do board.
- Durante o request, diálogo, reserva, capitão e motivo permanecem montados e desabilitados. Falhas reabilitam os controles para retry; sucesso fecha somente após projeção com versão avançada.

### Validação do capitão

- RED: a seleção continuava aparentemente válida depois de perder elegibilidade.
- GREEN: `selectedCaptainValid` é a fonte única para submissão, reconciliação, `FieldError`, `data-invalid` e `aria-invalid`.

### Integração e compatibilidade

- Concorrência integrada cobre início, pick contra timeout, substituição e finalizações, sempre com um vencedor e sem HTTP 500.
- V1 cobre transferência automática da capitania para a reserva quando o capitão sai sem `NovoCapitaoId` e preservação dos estados `PresencaAberta`, `PresencaEncerrada`, `Aberta`, `Finalizada` e `Cancelada`.
- Bot autenticado válido recebe exatamente HTTP 403 em todas as operações Admin+ do ciclo.

## Verificações

- Backend focado de revisão: 27 aprovados.
- `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test ... --configuration Release`: 685 aprovados.
- `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build ... --configuration Release`: sucesso, 0 warnings, 0 erros.
- `npm run lint:check`: sucesso.
- Frontend focado de revisão: 194 aprovados.
- `npm test`: 521 aprovados.
- `npm run build`: sucesso.
- `git diff --check`: sucesso.

## Preocupações

- O build Vite mantém avisos não bloqueantes já emitidos por anotações `PURE` de dependências e pelo chunk principal acima de 500 kB; nenhum deles foi introduzido ou ampliado no escopo desta task.
- A responsividade e o foco mobile foram cobertos por testes automatizados de componente; não houve alteração de tokens ou CSS global.

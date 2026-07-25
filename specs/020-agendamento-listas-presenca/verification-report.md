# Relatório de verificação final da feature 020

**Data**: 2026-07-25  
**Branch**: `feature/020-agendamento-listas-presenca`  
**Status**: frontend e contratos aprovados; risco temporal preexistente da suíte PostgreSQL completa registrado abaixo.

## Escopo

- Correção frontend por TDD do total global `activeItems` no resumo de agendas.
- Correção semântica de MV100 em português e inglês.
- Consolidação dos artefatos SDD e das evidências da Task 8.
- Nenhum arquivo de produção backend ou do bot foi alterado nesta rodada.
- `docs/prompts/` e `specs/018-importacao-partidas-lcu/` são mudanças não relacionadas e permaneceram intocados.

## Resultado consolidado

| Gate | Resultado desta rodada | Evidência |
|---|---:|---|
| RED frontend | PASS | 2 falhas esperadas: resumo `6` em vez de `20`; MV100 com significado incorreto |
| Frontend focado | PASS | 7 arquivos, 63/63 testes |
| Frontend completo | PASS | 31 arquivos, 187/187 testes |
| Frontend build | PASS | 2.756 módulos transformados |
| Frontend lint sem fix | PASS | exit 0, sem diagnósticos |
| Backend contrato | PASS | 29/29 testes de handlers e matriz HTTP |
| Backend completo | RISCO | 532 executados: 496 aprovados e 36 falhas temporais PostgreSQL após 25/07 |
| Bot | NÃO REEXECUTADO | baseline verificado: 54 testes; sem mudança nesta rodada |
| i18n frontend | PASS | 906 chaves PT e 906 EN; paridade testada |
| resources backend | PASS | 218 chaves em neutral, PT-BR e EN-US; sem mudança backend |
| Diff check | PASS | `git diff --check` sem erros antes da documentação final |

## TDD da correção

RED executado:

```bash
npm test --prefix FrontEnd -- src/components/settings/PresenceScheduleSection.spec.ts src/i18n/i18n.spec.ts
```

Resultado: 2 falhas esperadas e 31 aprovações. A primeira página tinha seis agendas ativas, `totalItems: 20` e `activeItems: 20`, mas o resumo mostrava seis. MV100 ainda dizia que a ocorrência não havia sido encontrada.

GREEN executado:

```bash
npm test --prefix FrontEnd -- src/services/presenceSchedules.spec.ts src/components/settings/PresenceScheduleSection.spec.ts src/i18n/i18n.spec.ts
```

Resultado: 3 arquivos e 40/40 testes. O envelope específico passou a exigir `activeItems`; página atual, carregar mais e refresh atualizam o total, enquanto respostas stale são ignoradas junto com seus itens.

## Frontend

```bash
npm test --prefix FrontEnd -- src/services/presenceSchedules.spec.ts src/components/settings/PresenceScheduleFormDialog.spec.ts src/components/settings/PresenceScheduleSection.spec.ts src/components/settings/PresenceScheduleOccurrenceHistoryDialog.spec.ts src/components/settings/PresenceScheduleConfirmDialog.spec.ts src/views/SettingsView.spec.ts src/i18n/i18n.spec.ts
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint --prefix FrontEnd -- --no-fix
```

Resultados: focado 63/63; completo 187/187; build e lint aprovados. O build mantém os avisos conhecidos de anotações `PURE` em dependências e chunk principal de aproximadamente 678 kB.

## Backend

Comando de contrato aprovado nesta rodada:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --no-build --filter "FullyQualifiedName~AgendamentoPresencaHandlersTests|FullyQualifiedName~EndpointCoverageIntegrationTests"
```

Resultado: 29/29. Inclui `activeItems` no handler, paginação e matriz HTTP/autorização.

A suíte completa também foi executada:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --no-restore
```

Resultado: 532 testes, 496 aprovados e 36 falhas. A causa observada é temporal e preexistente: `AgendamentoPresencaBehaviorIntegrationTests` combina marcador fixo em 23/07/2026, nomes/drafts de 24/07/2026 e `clock_timestamp()` real; ao executar em 25/07/2026, ocorrências esperadas já estavam fora da janela. O código frontend/docs desta rodada não participa dessas falhas. Nenhuma correção backend foi feita por restrição de escopo.

## Migration e PostgreSQL

O baseline de 2026-07-24 comprovou aplicação, rollback integral e reaplicação das migrations da feature em banco descartável, quatro tabelas, enums `smallint`, histórico `varchar(200)` e índices únicos. Procedimento reproduzível:

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 createdb -U postgres feature020_verification
docker.exe exec -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=feature020_verification;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api --configuration Release
docker.exe exec -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=feature020_verification;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update 20260721171829_AddDraftMontagemPublicationClaimsAndAdministrativeAudit --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api --configuration Release
docker.exe exec -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=feature020_verification;Username=postgres;Password=postgres" rinhadaslendas_devcontainer-app-1 dotnet ef database update --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api --configuration Release
docker.exe exec rinhadaslendas_devcontainer-postgres-1 dropdb -U postgres feature020_verification
```

## API e autorização

- Anônimo: `401`; Jogador: `403`; Moderador/Admin: operações permitidas.
- Agendas usam `CanManageDrafts`; configuração sensível continua em `CanManageUsers`.
- Autoria vem do JWT; DTOs não expõem claims, token, IDs Discord, payload ou detalhe técnico.
- O filtro de 29 testes acima reproduz handlers e cobertura HTTP relevantes.

## Concorrência e recuperação

O baseline anterior comprovou exactly-once, claim vencedor, rollback, claim expirado, recuperação de múltiplos dias e bloqueadas com marcador avançado. O filtro reproduzível é:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --no-build --filter "FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests|FullyQualifiedName~AgendamentoPresencaExecutionServiceTests"
```

Na data atual, esse filtro também expõe o risco temporal descrito em [Backend](#backend); não deve ser interpretado como GREEN até as fixtures de data serem estabilizadas em tarefa backend própria.

## Browser real

O baseline de 2026-07-24 usou Chromium real e comprovou Jogador/Moderador/Admin, CRUD, carregar mais, histórico, foco/Escape e ausência de overflow em `1440x900`, `768x1024`, `390x844` e `320x844`. As capturas mostravam seis cards na primeira página e sete IDs únicos após carregar mais; o novo teste automatizado cobre especificamente o resumo global 20 com apenas seis cards.

Com frontend e API locais ativos, o roteiro pode ser iniciado por:

```bash
agent-browser --session feature020 open http://localhost:5173/configuracoes
agent-browser --session feature020 snapshot -i
agent-browser --session feature020 set viewport 320 844
agent-browser --session feature020 snapshot -i
agent-browser --session feature020 close
```

Credenciais e tokens não são registrados no relatório.

## Bot

Não reexecutado porque nenhum arquivo ou contrato do bot mudou. Baseline atual informado: 54 testes. Comandos reproduzíveis:

```bash
npm test --prefix discord-bot
npm run build --prefix discord-bot
```

## Auditoria de internacionalização

- Textos hardcoded frontend encontrados: Não.
- Mensagens hardcoded backend encontradas: Não.
- `pt.json` e `en.json` sincronizados: Sim, 906 chaves em cada catálogo.
- Resources backend atualizados e sincronizados: Sim, 218 chaves em neutral/PT-BR/en-US; nenhuma mudança necessária nesta rodada.
- MV100 semanticamente equivalente aos resources: Sim.
- Acentuação portuguesa revisada: Sim.
- Placeholders, botões, títulos, badges, toasts e estados vazios revisados: Sim.
- Validações frontend usam i18n e backend usa resources: Sim.
- Novos arquivos respeitam o padrão: Sim.

Comandos:

```bash
npm test --prefix FrontEnd -- src/i18n/i18n.spec.ts
docker.exe exec rinhadaslendas_devcontainer-app-1 grep -c "<data name=" /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx
```

## Diff e escopo

```bash
git diff --check
git status --short
```

Somente frontend e documentos da feature 020 integram esta correção. Mudanças não relacionadas permanecem fora dos commits.

## Riscos

- A suíte PostgreSQL completa possui fixtures acopladas a 23/24 de julho de 2026 e falha após a virada para 25/07; requer correção backend/testes em escopo separado.
- O bundle frontend mantém chunk principal acima de 500 kB; aviso conhecido, sem regressão desta mudança.
- O bot não possui script de lint.
- A verificação browser e migration desta rodada reutiliza evidência integrada anterior; os comandos acima permitem reprodução sem depender de paths temporários ou screenshots como única prova.

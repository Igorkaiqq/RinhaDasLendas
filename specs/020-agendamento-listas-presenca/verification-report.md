# Relatório de verificação final da feature 020

**Data**: 2026-07-25
**Branch**: `feature/020-agendamento-listas-presenca`
**Status**: PASS.

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
| Frontend dependency audit | PASS | 0 vulnerabilidades após overrides patched de `brace-expansion` e `postcss` |
| Backend contrato | PASS | 29/29 testes de handlers e matriz HTTP |
| Backend completo | PASS | 532/532 testes; build Release com 0 warnings e 0 erros |
| Bot | PASS | 54/54 testes; build TypeScript aprovado |
| i18n frontend | PASS | 906 chaves PT e 906 EN; paridade testada |
| resources backend | PASS | 218 chaves em neutral, PT-BR e EN-US; sem mudança backend |
| Browser real | PASS | resumo global 20 com 6 cards; paginação deduplicada; 1280px e 320px sem overflow |
| Diff check | PASS | `git diff --check` sem erros após a documentação final |

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

O gate `npm audit --audit-level=moderate` também passou sem vulnerabilidades. As versões transitivas vulneráveis foram substituídas por `brace-expansion 5.0.8` e `postcss 8.5.23`, mantendo ESLint 9 e a compatibilidade da suíte.

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

Resultado final: 532 testes aprovados, 0 falhas e 0 ignorados; build Release com 0 warnings e 0 erros. A primeira execução em 25/07 revelou 36 fixtures temporais acopladas aos dias 23/24. O commit `1932a02` passou a derivar datas, dias ISO e janelas do relógio PostgreSQL; o commit `303929a` eliminou dois retornos antecipados que podiam concluir sem asserções. O filtro desses cenários passou com 5/5 antes da suíte completa.

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

A verificação final comprovou exactly-once, claim vencedor, rollback, claim expirado, recuperação de múltiplos dias, cursor de bloqueadas, revalidação da configuração Discord e bloqueadas com marcador avançado. O filtro reproduzível é:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --no-build --filter "FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests|FullyQualifiedName~AgendamentoPresencaExecutionServiceTests"
```

Resultado incluído na suíte final: GREEN. As fixtures usam o relógio PostgreSQL e mantêm datas históricas fixas somente nos cenários deliberados de timezone e migration.

## Browser real

O baseline de 2026-07-24 usou Chromium real e comprovou Jogador/Moderador/Admin, CRUD, carregar mais, histórico, foco/Escape e ausência de overflow em `1440x900`, `768x1024`, `390x844` e `320x844`. Em 25/07, uma rodada adicional validou a correção de `activeItems`: a primeira página exibiu resumo global 20 com seis cards; a segunda página sobreposta manteve 11 IDs únicos e atualizou o resumo conforme o envelope. Os viewports `1280x900` e `320x800` permaneceram sem overflow e sem erros no console.

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

O bot foi reexecutado após as proteções de deadline e configuração singleton: 54/54 testes e build TypeScript aprovados. Comandos reproduzíveis:

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

O intervalo final contém somente backend, frontend, bot, testes, migrations e documentos da feature 020. `docs/prompts/` e `specs/018-importacao-partidas-lcu/` permanecem fora dos commits.

## Riscos

- O bundle frontend mantém chunk principal acima de 500 kB; aviso conhecido, sem regressão desta mudança.
- O bot não possui script de lint.
- Os cursores de agendas e bloqueadas são operacionais e reiniciam com o processo; a ordenação circular e os marcadores persistidos preservam correção e progresso eventual.
- O envio ao Discord não participa da transação PostgreSQL; claims, deadline, conclusão e reconciliação protegem contra duplicação e publicação confirmada fora da janela.

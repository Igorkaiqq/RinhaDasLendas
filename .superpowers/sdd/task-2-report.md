# Relatório da Unidade 2

## Resultado

Implementado o publisher pós-commit resiliente na camada Application, com contrato sem token da request, timeout interno de cinco segundos, reload de arquivados, publicação compartilhada sem capacidade personalizada, eventos aditivos de arquivamento/restauração e absorção observável de falhas por envio.

O método personalizado anterior do notifier foi preservado para não quebrar os callers que serão migrados atomicamente na Unidade 4. A semântica de `DraftMontagemRealtimeSnapshotDto` e `CanView` não foi alterada.

## Evidências TDD

### RED

Comando:

```text
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimePublisherTests
```

Resultado esperado observado: falha de compilação porque `DraftMontagemRealtimePublisher`, `DraftMontagemAvailabilityChange`, `IDraftMontagemRealtimeTelemetry` e os novos namespaces/ports ainda não existiam.

### GREEN

Mesmo comando focado após a implementação.

Resultado:

```text
Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 5 s
```

Comportamentos cobertos:

- publicação normal do snapshot compartilhado;
- independência de token de request já cancelado;
- cancelamento pelo timeout interno de exatamente cinco segundos;
- reload normal para restauração;
- reload incluindo arquivados para arquivamento;
- absorção e telemetria de falha sem impedir o evento de disponibilidade.

### Build

Comando:

```text
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

Resultado:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Diff-check

`git diff --check` e `git diff --cached --check` concluíram sem saída e sem erro. O inventário confirmou todos os implementadores concretos de `IDraftMontagemRealtimeNotifier` atualizados.

## Arquivos

- `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimePublisher.cs`
- `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeTelemetry.cs`
- `BackEnd/src/RinhaDasLendas.Application/Services/DraftMontagemRealtimePublisher.cs`
- `BackEnd/src/RinhaDasLendas.Application/Enums/DraftMontagemAvailabilityChange.cs`
- `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeStateFactory.cs`
- `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs`
- `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimePublisherTests.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

## Self-review

- Dependências continuam apontando para dentro: a Application depende apenas de ports e repositório; SignalR, logging e métricas permanecem na API.
- O publisher não recebe nem reutiliza token da request.
- Cada reload/envio possui token interno próprio de cinco segundos.
- Falhas de reload ou transporte são absorvidas e registradas com draft, versão quando disponível, operação, duração e tipo da falha.
- O evento de disponibilidade é tentado independentemente da falha no snapshot compartilhado.
- O contrato personalizado legado e seus callers permanecem compiláveis.
- Nenhum arquivo de `tasks.md`, `plan.md` ou especificação foi alterado.
- Nenhum achado crítico, importante ou menor ficou pendente.

## Commit

Implementação: `11daa3d` (`feat: adicionar publisher pós-commit resiliente`).

## Auditoria de internacionalização

- Ausência de novos textos hardcoded no frontend: **Sim**. Nenhum arquivo frontend foi alterado.
- Ausência de novas mensagens de API hardcoded para usuário: **Sim**. O único texto novo é log operacional estruturado, não resposta ao usuário.
- `pt.json` e `en.json` sincronizados: **Sim**. Auditoria recursiva encontrou `1049` chaves em cada arquivo, sem chaves exclusivas.
- Recursos backend atualizados quando necessários: **Sim**. Não houve nova mensagem destinada ao usuário, portanto nenhum recurso precisou ser criado.
- Acentuação em português revisada: **Sim**.
- Placeholders, botões, títulos, badges, toasts e estados vazios revisados: **Sim**. Nenhum desses elementos foi alterado.
- Validações frontend/backend usam i18n/recurso: **Sim**. Nenhuma validação foi adicionada ou modificada.
- Novos arquivos respeitam o padrão de internacionalização: **Sim**.

## Correções após revisão

Foram corrigidos todos os findings da revisão da Unidade 2:

- o port de telemetria passou a receber somente `failureType`, sem transportar a exceção completa;
- o adapter registra apenas draft, versão, operação, duração e tipo da falha, sem anexar `Exception` ao logger;
- reload, factory/mapping, envios e observação ficaram dentro da fronteira best-effort pós-commit;
- falha da própria telemetria é absorvida e não interrompe o próximo envio independente;
- falhas de reload ou mapping encerram a pipeline sem qualquer envio indevido;
- o timeout interno por estágio permanece em cinco segundos.

### RED da revisão

O filtro `FullyQualifiedName~DraftMontagemRealtimePublisherTests` falhou na compilação porque o double já exigia o novo contrato seguro por `failureType`, enquanto `IDraftMontagemRealtimeTelemetry` ainda recebia `Exception`.

### GREEN da revisão

```text
Passed! - Failed: 0, Passed: 11, Skipped: 0, Total: 11, Duration: 10 s
```

Os novos testes cobrem contrato de telemetria sem exceção completa, reload com throw, timeout interno do reload, falha real na factory/mapping, telemetria que lança, ausência de exceção externa, ausência de envio após reload/mapping inválido e continuidade do evento de disponibilidade após falha observável.

### Build e diff-check da revisão

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

`git diff --check` concluiu sem erros. A busca de código confirmou ausência de logger recebendo `Exception` e ausência de `Exception` no port de telemetria.

### Commit da revisão

Mensagem: `fix: impedir escape de falhas do publisher pós-commit`.

### Auditoria de internacionalização da revisão

- Ausência de novos textos hardcoded no frontend: **Sim**.
- Ausência de novas mensagens de API hardcoded para usuário: **Sim**. O log operacional não é conteúdo de resposta.
- `pt.json` e `en.json` permanecem sincronizados: **Sim**. Nenhum locale foi alterado.
- Recursos backend atualizados quando necessários: **Sim**. Não houve mensagem nova para usuário.
- Acentuação em português revisada: **Sim**.
- Placeholders, botões, títulos, badges, toasts e estados vazios revisados: **Sim**. Não foram alterados.
- Validações frontend/backend usam i18n/recurso: **Sim**. Não foram alteradas.
- Arquivos modificados respeitam o padrão de internacionalização: **Sim**.

# Endurecimento da Feature 017 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corrigir os riscos de autorização, configuração, duplicação, concorrência, validação, exposição de dados e cobertura encontrados na auditoria da feature 017.

**Architecture:** O backend permanece como fonte de verdade para autorização, publicação, auditoria e concorrência. O bot atua como adaptador e só envia após adquirir claim atômico; API/Infrastructure encapsulam segurança de host e detalhes EF/PostgreSQL, enquanto Domain/Application recebem apenas tipos neutros.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core, PostgreSQL, MediatR, FluentValidation, SignalR, xUnit, FluentAssertions, Moq, Node.js, TypeScript, discord.js e Vue 3.

## Global Constraints

- Seguir `AGENTS.md`, `docs/architecture/*`, `docs/design/*` e `specs/017-robustecer-drafts-discord-jogadores/*`.
- Usar TDD: cada comportamento novo começa por teste falho.
- Controllers não recebem regras de negócio; Domain não depende de EF, HTTP, DTOs ou Discord.
- Todo texto visível usa i18n no frontend/bot ou `.resx` no backend, com português e inglês sincronizados.
- Não registrar tokens, motivos administrativos ou dados pessoais em métricas e logs.
- Criar um commit em português depois de cada tarefa aprovada; não fazer push.
- Não adicionar `.superpowers/` ao Git.

---

### Task 1: Tornar o teste de data determinístico

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts:305-308`
- Test: `discord-bot/src/modules/drafts/draftInteractions.spec.ts:7-13`

**Interfaces:**
- Produces: `parsePresenceClosingTime(dayInput: string, timeInput: string, now?: Date): string | null`.

- [ ] **Step 1: Alterar o teste para injetar o relógio**

```ts
const result = parsePresenceClosingTime(
  '11/07/2026',
  '19:30',
  new Date('2026-07-10T12:00:00.000Z'),
)
assert.equal(result, '2026-07-11T22:30:00.000Z')
```

- [ ] **Step 2: Executar o teste e confirmar falha de assinatura/comportamento**

Run: `npm test -- src/modules/drafts/draftInteractions.spec.ts`
Workdir: `discord-bot`
Expected: FAIL porque `parsePresenceClosingTime` ignora o terceiro argumento e usa o relógio real.

- [ ] **Step 3: Encaminhar o relógio para a validação existente**

```ts
export function parsePresenceClosingTime(dayInput: string, timeInput: string, now = new Date()) {
  const validation = validatePresenceClosingTime(dayInput, timeInput, now)
  return validation.ok ? validation.value : null
}
```

- [ ] **Step 4: Verificar teste focado e suíte do bot**

Run: `npm test -- src/modules/drafts/draftInteractions.spec.ts && npm test`
Workdir: `discord-bot`
Expected: PASS, 21 testes ou mais aprovados.

- [ ] **Step 5: Marcar T075 e criar commit**

```bash
git add discord-bot/src/modules/drafts/draftInteractions.ts discord-bot/src/modules/drafts/draftInteractions.spec.ts specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "test: tornar data do bot determinística"
```

---

### Task 2: Autorizar comandos mutáveis do Discord

**Files:**
- Create: `discord-bot/src/discord/commands/draftCommands.spec.ts`
- Modify: `discord-bot/src/discord/commands/draftCommands.ts`
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Modify: `discord-bot/src/config/env.ts`
- Modify: `discord-bot/src/shared/messages/pt-BR.ts`
- Modify: `discord-bot/src/shared/messages/en-US.ts`
- Modify: `discord-bot/.env.example`
- Modify: `.env.example`
- Modify: `docker-stack.prod.yml`
- Test: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`

**Interfaces:**
- Produces: `isDraftAdministrator(interaction: ChatInputCommandInteraction, configuredRoleIds: readonly string[]): boolean`.
- Produces: `DRAFT_ADMIN_ROLE_IDS` como string opcional separada por vírgulas.

- [ ] **Step 1: Testar permissões padrão serializadas**

```ts
const mutable = new Set(['draft-criar', 'draft-encerrar-presenca', 'draft-definir-capitaes', 'draft-definir-ordem-escolha'])
for (const command of draftCommands) {
  if (mutable.has(command.name)) assert.equal(command.default_member_permissions, PermissionFlagsBits.ManageGuild.toString())
  else assert.equal(command.default_member_permissions, undefined)
}
```

- [ ] **Step 2: Testar autorização runtime antes da API**

Cobrir três casos em `draftInteractions.spec.ts`: `ManageGuild`, cargo presente em `DRAFT_ADMIN_ROLE_IDS` e membro sem ambos. No caso negado, afirmar resposta efêmera e zero chamadas ao método mutável do `rinhaApi`.

- [ ] **Step 3: Confirmar falha dos testes**

Run: `npm test -- src/discord/commands/draftCommands.spec.ts src/modules/drafts/draftInteractions.spec.ts`
Workdir: `discord-bot`
Expected: FAIL por ausência de `default_member_permissions` e guarda runtime.

- [ ] **Step 4: Aplicar permissão padrão e helper runtime**

```ts
const mutableCommand = (builder: SlashCommandBuilder) =>
  builder.setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)

export function isDraftAdministrator(interaction: ChatInputCommandInteraction, configuredRoleIds: readonly string[]) {
  if (interaction.memberPermissions?.has(PermissionFlagsBits.ManageGuild)) return true
  const roles = interaction.member && 'roles' in interaction.member
    ? Array.from(interaction.member.roles.cache?.keys?.() ?? interaction.member.roles)
    : []
  return configuredRoleIds.some((roleId) => roles.includes(roleId))
}
```

Executar a guarda para os quatro comandos mutáveis antes de consultar configuração ou chamar a API. Responder com `t.draftAdministrationDenied` e `MessageFlags.Ephemeral`. O cancelamento de draft permanece exclusivo do site e `/draft-cancelar` não é registrado pelo bot.

- [ ] **Step 5: Adicionar configuração e mensagens equivalentes**

```ts
DRAFT_ADMIN_ROLE_IDS: z.string().optional().default(''),
```

Propagar `DRAFT_ADMIN_ROLE_IDS` nos dois `.env.example` e no serviço bot de `docker-stack.prod.yml`. Adicionar a mesma chave estrutural nos catálogos `pt-BR` e `en-US`.

- [ ] **Step 6: Verificar e commitar**

Run: `npm test && npm run build`
Workdir: `discord-bot`
Expected: PASS.

```bash
git add .env.example docker-stack.prod.yml discord-bot/.env.example discord-bot/src specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: restringir comandos administrativos do Discord"
```

---

### Task 3: Validar o token interno em produção

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Api/Services/InternalTokenSecurity.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/BotInternalAuthHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`

**Interfaces:**
- Produces: `InternalTokenSecurity.ResolveTokens(IConfiguration): IReadOnlyCollection<string>`.
- Produces: `InternalTokenSecurity.ValidateProductionTokens(IWebHostEnvironment, IReadOnlyCollection<string>, IMessageProvider): void`.
- Produces: `InternalTokenSecurity.FixedTimeEquals(string, string): bool`.

- [ ] **Step 1: Escrever testes para vazio, curto, placeholder e token forte**

```csharp
[Theory]
[InlineData("")]
[InlineData("short-token")]
[InlineData("change-me-generate-a-long-random-secret")]
public void ProductionStartup_ShouldRejectUnsafeInternalToken(string token)
{
    var action = () => InternalTokenSecurity.ValidateProductionTokens(ProductionEnvironment(), [token], Messages);
    action.Should().Throw<InvalidOperationException>().WithMessage(M(MessageCodes.BotInternalTokenNotSecurelyConfigured));
}
```

Adicionar teste positivo com 32 ou mais caracteres e teste de `FixedTimeEquals` para valor igual/diferente.

- [ ] **Step 2: Confirmar testes falhos**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~SecurityHardeningTests --configuration Release`
Expected: FAIL porque o helper e o código de mensagem não existem.

- [ ] **Step 3: Implementar resolução, validação e comparação constante**

```csharp
internal static class InternalTokenSecurity
{
    internal const int MinimumTokenLength = 32;
    private static readonly string[] Placeholders = ["change-me", "dev-only", "replace-me"];

    internal static bool FixedTimeEquals(string provided, string expected)
    {
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
```

Validar todos os tokens aceitos; ignorar somente `Development`, `Testing` e `IntegrationTesting`. Reutilizar a coleção resolvida na configuração de `BotInternalAuthOptions` e no startup.

- [ ] **Step 4: Adicionar `ME042` nos três recursos**

Usar mensagem pt-BR “O token interno do bot não está configurado com segurança.” e en-US “The bot internal token is not configured securely.”.

- [ ] **Step 5: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~SecurityHardeningTests --configuration Release`
Expected: PASS.

```bash
git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Security specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: validar token interno do bot em produção"
```

---

### Task 4: Particionar o rate limiter

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Api/Services/ApiRateLimitPartition.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Services/ApiRateLimitOptions.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- Modify: three backend `Messages*.resx` files
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`

**Interfaces:**
- Produces: `ApiRateLimitPartition.GetPartitionKey(HttpContext): string`.
- Produces: `ApiRateLimitOptions.PermitLimit` e `WindowSeconds` configuráveis.

- [ ] **Step 1: Testar chaves `bot:`, `user:` e `ip:`**

```csharp
ApiRateLimitPartition.GetPartitionKey(botContext).Should().Be("bot:discord-bot");
ApiRateLimitPartition.GetPartitionKey(userContext).Should().Be($"user:{userId}");
ApiRateLimitPartition.GetPartitionKey(anonymousContext).Should().Be("ip:203.0.113.10");
```

- [ ] **Step 2: Confirmar falha e implementar função pura**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~SecurityHardeningTests --configuration Release`
Expected: FAIL pelo tipo inexistente.

```csharp
internal static string GetPartitionKey(HttpContext context)
{
    var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (id == "discord-bot") return "bot:discord-bot";
    if (!string.IsNullOrWhiteSpace(id)) return $"user:{id}";
    return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
```

- [ ] **Step 3: Registrar política particionada e mover middleware**

Usar `options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(...))`. Mover `UseRateLimiter()` para depois de `UseAuthentication()` e antes de `UseAuthorization()`.

- [ ] **Step 4: Localizar 429 com `ME043`**

`OnRejected` deve manter a métrica e escrever `ApiErrorResponse.FromCode(messages, MessageCodes.RateLimitExceeded)`.

- [ ] **Step 5: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~SecurityHardeningTests --configuration Release`
Expected: PASS.

```bash
git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Security specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: isolar limites de requisição por cliente"
```

---

### Task 5: Preservar códigos de erro de domínio

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Exceptions/DomainException.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Filters/ApiExceptionMiddleware.cs`
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- Test: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`

**Interfaces:**
- Produces: `DomainException.MessageCode`.

- [ ] **Step 1: Criar teste HTTP que provoque `PresenceAlreadyClosed`**

Finalizar presença e repetir uma mutação. Afirmar status 400 e:

```csharp
error!.MessageCode.Should().Be(MessageCodes.PresenceAlreadyClosed);
error.Message.Should().Be(M(MessageCodes.PresenceAlreadyClosed));
```

- [ ] **Step 2: Confirmar retorno atual `ME031`**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~EndpointCoverageIntegrationTests --configuration Release`
Expected: FAIL com `messageCode` igual a `ME031`.

- [ ] **Step 3: Preservar o código na exceção e middleware**

```csharp
public sealed class DomainException(string messageCode) : Exception(messageCode)
{
    public string MessageCode { get; } = messageCode;
}
```

```csharp
catch (DomainException exception)
{
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(ApiErrorResponse.FromCode(messages, exception.MessageCode));
}
```

- [ ] **Step 4: Mapear no bot os códigos reais `MV*` usados pelo backend**

Substituir nomes simbólicos inexistentes no contrato por `MessageCodes` equivalentes constantes no módulo do bot, preservando mensagens localizadas.

- [ ] **Step 5: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~EndpointCoverageIntegrationTests --configuration Release`
Run: `npm test`
Workdir do segundo comando: `discord-bot`
Expected: PASS.

```bash
git add BackEnd discord-bot/src specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: preservar códigos de erro de domínio"
```

---

### Task 6: Modelar claims de publicação no domínio

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemPublicacaoDiscordStatus.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemPublicacaoDiscord.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`

**Interfaces:**
- Produces: status `EmAndamento` e `RequerReconciliacao`.
- Produces: `IniciarTentativa`, `RegistrarPublicada(claimId, ...)`, `RegistrarFalha(claimId, ...)` e `MarcarRequerReconciliacao`.

- [ ] **Step 1: Testar transições e rejeição de claim divergente**

Cobrir: pendente concede claim; segundo claim não altera estado; claim correto conclui; claim diferente lança código estável; tentativa expirada vai para reconciliação; reconciliação não concede claim.

- [ ] **Step 2: Confirmar testes falhos**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagemTests --configuration Release`
Expected: FAIL por estados e métodos inexistentes.

- [ ] **Step 3: Implementar estado e transições**

```csharp
public Guid? ClaimId { get; private set; }
public DateTimeOffset? ClaimExpiraEm { get; private set; }

public void IniciarTentativa(Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora)
{
    if (Status != DraftMontagemPublicacaoDiscordStatus.Pendente) throw new DomainException(MessageCodes.DiscordPublicationNotPending);
    ClaimId = claimId;
    ClaimExpiraEm = expiraEm;
    UltimaTentativaEm = agora;
    Status = DraftMontagemPublicacaoDiscordStatus.EmAndamento;
}
```

Conclusão/falha validam `ClaimId`; reconciliação limpa expiração, preserva o claim para auditoria e bloqueia reenvio automático.

- [ ] **Step 4: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagemTests --configuration Release`
Expected: PASS.

```bash
git add BackEnd/src/RinhaDasLendas.Domain BackEnd/tests/RinhaDasLendas.Tests/Domain specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "feat: modelar claims de publicação Discord"
```

---

### Task 7: Persistir e expor claims atômicos

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPublicacaoClaim.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/ClaimPublicacaoDiscordResponseDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Validators/AdquirirClaimPublicacaoDiscordDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/RegistrarPublicacaoDiscordDraftMontagemRequestDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto.cs`
- Modify: corresponding publication commands and handlers under `BackEnd/src/RinhaDasLendas.Application/`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

**Interfaces:**
- Produces: `TryClaimPublicacaoDiscordAsync(...)`, `TryConcluirPublicacaoDiscordAsync(...)`, `TryRegistrarFalhaPublicacaoDiscordAsync(...)` e `MarcarPublicacoesExpiradasParaReconciliacaoAsync(...)`.
- Produces: `POST /api/v1/draft-montagens/{id}/discord/publicacoes/claim` bot-only.

- [ ] **Step 1: Testar dois claims concorrentes e claim divergente**

Executar duas requisições simultâneas para o mesmo draft/tipo e afirmar exatamente uma resposta `Adquirido=true`. Tentar concluir com outro `ClaimId` e esperar 400 com código estável.

- [ ] **Step 2: Confirmar falha do teste**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagemBehaviorIntegrationTests --configuration Release`
Expected: FAIL por rota inexistente.

- [ ] **Step 3: Implementar operação condicional no repositório**

Executar `UPDATE draft_montagem_publicacoes_discord SET status = 'EmAndamento', claim_id = ..., claim_expira_em = ... WHERE draft_montagem_id = ... AND tipo = ... AND status = 'Pendente'`. Considerar claim adquirido somente quando uma linha for afetada. Conclusão/falha usam `WHERE claim_id = ... AND status = 'EmAndamento'`.

- [ ] **Step 4: Implementar DTOs/handlers/endpoints e validators**

```csharp
public sealed record ClaimPublicacaoDiscordResponseDto(bool Adquirido, Guid? ClaimId, DateTimeOffset? ExpiraEm, string Status);
public sealed record RegistrarPublicacaoDiscordDraftMontagemRequestDto(string Tipo, Guid ClaimId, string? DiscordGuildId, string? DiscordChannelId, string MessageId);
```

Proteger claim/conclusão/falha com `AuthenticationSchemes = BotInternalAuthOptions.SchemeName` e `CanUseDiscordBotApi`.

- [ ] **Step 5: Mapear campos EF e consolidar migrações não publicadas**

Remover as três migrações de julho e respectivos designers somente após confirmar que continuam não publicadas. Gerar uma migração `AddDraftMontagemPublicationClaimsAndAdministrativeAudit` com tabelas finais, `claim_id`, `claim_expira_em`, índice de expiração e `jogador_alvo_id`.

- [ ] **Step 6: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagemBehaviorIntegrationTests --configuration Release`
Expected: PASS.

```bash
git add BackEnd specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "feat: implementar claim atômico de publicação"
```

---

### Task 8: Migrar o polling do bot para claims

**Files:**
- Modify: `discord-bot/src/shared/api/types.ts`
- Modify: `discord-bot/src/shared/api/rinhaApi.ts`
- Test: `discord-bot/src/shared/api/rinhaApi.spec.ts`
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Test: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`

**Interfaces:**
- Produces: `runDraftPollingCycle(client: Client): Promise<void>`.
- Consumes: endpoints de claim/conclusão/falha da Task 7.

- [ ] **Step 1: Testar paths e payloads do cliente API**

Afirmar claim, conclusão e falha contendo `tipo` e o mesmo `claimId`.

- [ ] **Step 2: Testar ciclo sem envio quando claim for negado**

Adicionar casos: claim negado; adquirido e concluído; falha registrada; `RequerReconciliacao` ignorado; primeiro draft falha e o segundo conclui.

- [ ] **Step 3: Confirmar falhas**

Run: `npm test -- src/shared/api/rinhaApi.spec.ts src/modules/drafts/draftInteractions.spec.ts`
Workdir: `discord-bot`
Expected: FAIL por métodos e ciclo exportado inexistentes.

- [ ] **Step 4: Implementar cliente e ciclo aguardável**

```ts
export async function runDraftPollingCycle(client: Client) {
  const configuration = await rinhaApi.getDiscordConfiguration()
  assertDiscordBotEnabled(configuration)
  const drafts = await rinhaApi.listActiveDrafts()
  for (const candidate of publicationCandidates(drafts)) {
    try {
      const claim = await rinhaApi.claimDiscordPublication(candidate.draft.id, candidate.tipo)
      if (!claim.adquirido || !claim.claimId) continue
      await publishClaimedDraft(client, configuration, candidate, claim.claimId)
    } catch (error) {
      logger.error('Discord publication failed', error, { draftId: candidate.draft.id, tipo: candidate.tipo })
    }
  }
}
```

`startDraftPolling` apenas chama o ciclo no intervalo. Remover sets locais como fonte de decisão.

- [ ] **Step 5: Verificar e commitar**

Run: `npm test && npm run build`
Workdir: `discord-bot`
Expected: PASS.

```bash
git add discord-bot/src specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: impedir duplicação no polling do Discord"
```

---

### Task 9: Tornar presença idempotente sob concorrência

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemSaveResultado.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: handlers de confirmar/cancelar presença em `Application/Handlers/DraftMontagens`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

**Interfaces:**
- Produces: `TrySaveChangesAsync` e `ReloadByIdAsync` sem tipos EF fora de Infrastructure.

- [ ] **Step 1: Testar confirmação e cancelamento repetidos no domínio**

Confirmar duas vezes retorna a mesma presença; cancelar duas vezes mantém cancelada e não lança.

- [ ] **Step 2: Testar duas confirmações HTTP simultâneas**

As duas respostas devem ser diferentes de 500 e o banco deve conter uma única presença confirmada.

- [ ] **Step 3: Confirmar falhas**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter "FullyQualifiedName~DraftMontagemTests|FullyQualifiedName~DraftMontagemBehaviorIntegrationTests" --configuration Release`
Expected: FAIL no segundo cancelamento ou por 500 concorrente.

- [ ] **Step 4: Implementar idempotência e tradução de persistência**

Infrastructure converte `DbUpdateConcurrencyException` para `ConflitoDeVersao` e PostgreSQL `23505` dos índices de presença para `ConflitoDePresencaConfirmada`. O handler recarrega; se o estado desejado já existir, retorna sucesso, caso contrário lança código de conflito localizado.

- [ ] **Step 5: Verificar e commitar**

Run: comando do Step 3.
Expected: PASS.

```bash
git add BackEnd specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: garantir idempotência concorrente de presença"
```

---

### Task 10: Aplicar `botEnabled` e permissões de canal corretamente

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Modify: `discord-bot/src/shared/messages/pt-BR.ts`
- Modify: `discord-bot/src/shared/messages/en-US.ts`
- Test: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`

**Interfaces:**
- Produces: guarda única antes de create/cancel/close/captains/order/confirm/cancel presence.
- Produces: `getSendableChannel(..., requirements: { embed: boolean; mentionRole: boolean })`.

- [ ] **Step 1: Escrever matriz falha**

Com `botEnabled=false`, afirmar zero mutações para todos os comandos e botões. Testar ausência individual de ViewChannel, SendMessages, EmbedLinks e MentionEveryone, com/sem cargo configurado e em times finais.

- [ ] **Step 2: Confirmar falhas**

Run: `npm test -- src/modules/drafts/draftInteractions.spec.ts`
Workdir: `discord-bot`
Expected: FAIL porque mutações e `MentionEveryone` não são condicionais.

- [ ] **Step 3: Implementar guards e requisitos condicionais**

Buscar configuração e chamar `assertDiscordBotEnabled` antes de toda mutação. Exigir menção somente para CTA com `DRAFT_NOTIFY_ROLE_ID`; times finais exigem apenas view/send/embed. Lançar códigos internos distintos e mapear mensagens equivalentes nos dois idiomas.

- [ ] **Step 4: Verificar e commitar**

Run: `npm test && npm run build`
Workdir: `discord-bot`
Expected: PASS.

```bash
git add discord-bot/src specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: aplicar configuração e permissões do bot"
```

---

### Task 11: Exigir validação e autoria administrativa

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/AdicionarPresencaManualDraftMontagemRequestDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/CancelarDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/AdicionarPresencaManualDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/RemoverPresencaManualDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/RepublicarPublicacaoDiscordDraftMontagemValidator.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Validators/RegistrarPublicacaoDiscordDraftMontagemValidator.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Validators/RegistrarFalhaPublicacaoDiscordDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdicionarPresencaManualDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RemoverPresencaManualDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemValidatorTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

**Interfaces:**
- Produces: motivos não vazios com máximo 500.
- Produces: publicação com `ClaimId`, IDs até 40 e erro até 120.

- [ ] **Step 1: Testar motivos e payloads inválidos**

Criar casos nulo, vazio e whitespace; executor sem `NameIdentifier`; tipo desconhecido; `ClaimId` vazio; message ID vazio; IDs acima de 40; erro acima de 120. Esperar 400 localizado e nenhuma mutação.

- [ ] **Step 2: Confirmar falhas**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter "FullyQualifiedName~DraftMontagemValidatorTests|FullyQualifiedName~DraftMontagemBehaviorIntegrationTests" --configuration Release`
Expected: FAIL porque regras são opcionais ou viram 500.

- [ ] **Step 3: Implementar regras FluentValidation**

```csharp
RuleFor(x => x.Motivo)
    .NotEmpty().WithMessage(MessageCodes.FieldRequired)
    .MaximumLength(500).WithMessage(MessageCodes.CancellationReasonMaxLength);
```

Usar `Enum.TryParse` após validação. Nunca usar `Guid.Empty` como executor. Resolver `currentUser.UserId` antes de carregar o agregado. Adição manual recebe executor e motivo e registra ação administrativa com jogador-alvo.

- [ ] **Step 4: Verificar e commitar**

Run: comando do Step 2.
Expected: PASS.

```bash
git add BackEnd specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: validar ações administrativas e publicações"
```

---

### Task 12: Notificar publicação via SignalR

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs` se necessário para projeção pública
- Test: novos testes de handlers em `BackEnd/tests/RinhaDasLendas.Tests/Application`

**Interfaces:**
- Consumes: `IDraftMontagemRealtimeNotifier.StateUpdatedAsync`.

- [ ] **Step 1: Testar uma notificação após persistência**

Para sucesso, falha e republicação, verificar exatamente uma chamada ao notifier após repositório; em erro de persistência, verificar zero chamadas.

- [ ] **Step 2: Confirmar falhas**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagem --configuration Release`
Expected: FAIL porque os handlers não notificam.

- [ ] **Step 3: Injetar notifier e publicar estado público**

Após persistência bem-sucedida, criar estado com a factory existente e chamar `StateUpdatedAsync`. Não incluir auditoria nem IDs operacionais no payload.

- [ ] **Step 4: Verificar e commitar**

Run: comando do Step 2.
Expected: PASS.

```bash
git add BackEnd specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: notificar mudanças de publicação em tempo real"
```

---

### Task 13: Separar projeções pública, administrativa e do bot

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemResponseDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemDiscordOperationalDto.cs`
- Create: query e handler `GetDraftMontagemAdminQuery` em `Application`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- Modify: frontend service/types/view para carregar detalhes admin somente quando autorizado
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- Test: `FrontEnd/src/services/draftMontagens.spec.ts`

**Interfaces:**
- Produces: `GET /api/v1/draft-montagens/{id}/administracao` com `CanManageDrafts`.
- Produces: DTO público sem executor, motivo, guild/channel/message/error.

- [ ] **Step 1: Testar ausência e presença dos campos por projeção**

GET comum como jogador não deve serializar `acoesAdministrativas`, `discordGuildId`, `channelId`, `messageId` ou `ultimoErroCodigo`. GET administrativo como moderador deve retornar auditoria e operação.

- [ ] **Step 2: Confirmar falha**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~DraftMontagemBehaviorIntegrationTests --configuration Release`
Expected: FAIL porque GET comum expõe os campos e rota admin não existe.

- [ ] **Step 3: Separar DTOs/query/endpoint e consumo frontend**

Manter apenas `Tipo` e `Status` na publicação pública. Proteger a nova query com endpoint `CanManageDrafts`. O bot continua recebendo contrato operacional somente em endpoints bot-only.

- [ ] **Step 4: Verificar e commitar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release`
Run: `npm test && npm run build`
Workdir do segundo comando: `FrontEnd`
Expected: PASS.

```bash
git add BackEnd FrontEnd specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "fix: restringir dados operacionais de drafts"
```

---

### Task 14: Registrar métrica de cancelamento

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemMetrics.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemMetrics.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarDraftMontagemCommandHandler.cs`
- Modify: `docs/domain/DRAFT_DISCORD_OPERATIONS.md`
- Test: handler test em `BackEnd/tests/RinhaDasLendas.Tests/Application`

**Interfaces:**
- Produces: `void RecordDraftCancelled(Guid draftMontagemId)`.

- [ ] **Step 1: Testar chamada após persistência e ausência em falha**

Usar Moq para verificar uma chamada após sucesso e nenhuma quando o repositório falhar.

- [ ] **Step 2: Confirmar falha e implementar**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --filter FullyQualifiedName~CancelarDraftMontagem --configuration Release`
Expected: FAIL porque o método não existe.

```csharp
public void RecordDraftCancelled(Guid draftMontagemId) =>
    metrics.RecordDraftAction(draftMontagemId, "draft_cancelled");
```

Chamar após persistência, sem motivo ou executor como tag.

- [ ] **Step 3: Verificar e commitar**

Run: comando do Step 2.
Expected: PASS.

```bash
git add BackEnd docs/domain/DRAFT_DISCORD_OPERATIONS.md specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "feat: registrar métrica de cancelamento de draft"
```

---

### Task 15: Substituir cobertura declarativa por testes comportamentais

**Files:**
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/SecurityApiFactory.cs`
- Create/complete: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs` para respostas 401/403 localizadas
- Create: `BackEnd/src/RinhaDasLendas.Api/Filters/ApiAuthorizationMiddlewareResultHandler.cs`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: clientes anônimo, JWT por roles e bot com autenticação real.
- Produces: 401/403/429 padronizados em `ApiErrorResponse`.

- [ ] **Step 1: Criar factory com JWT real e token forte de teste**

```csharp
internal sealed class SecurityApiFactory : WebApplicationFactory<Program>
{
    internal const string BotToken = "integration-test-token-with-32-characters";
    private const string TestJwtKey = "integration-test-jwt-signing-key-with-32-characters";
    private const string TestIssuer = "RinhaDasLendas.Tests";
    private const string TestAudience = "RinhaDasLendas.Tests.Client";
    internal HttpClient CreateAnonymousClient() => CreateClient();
    internal HttpClient CreateBotClient() { var client = CreateClient(); client.DefaultRequestHeaders.Add("X-Rinha-Internal-Token", BotToken); return client; }
    internal HttpClient CreateJwtClient(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(TestIssuer, TestAudience, claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
}
```

- [ ] **Step 2: Cobrir matriz HTTP real**

Para cada endpoint crítico, testar anônimo, jogador, admin/moderador, bot válido, bot inválido e esquema errado. Cobrir motivos/payloads inválidos, dois claims, concorrência de presença, auditoria e projeções.

- [ ] **Step 3: Padronizar e localizar 401/403**

Implementar `IAuthorizationMiddlewareResultHandler`: challenge retorna `ME029`, forbid retorna `ME028`, token interno inválido retorna `MV079`; sempre usar `ApiErrorResponse` e `IMessageProvider`.

- [ ] **Step 4: Remover chaves declarativas sem execução**

`CoveredEndpointKeys` não deve listar como coberta uma rota que nenhum teste chamou. Manter apenas inventário separado, se ainda útil, sem tratá-lo como evidência de cobertura.

- [ ] **Step 5: Executar todas as verificações**

Run: `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release`
Run: `dotnet build BackEnd/RinhaDasLendas.sln --configuration Release`
Run: `npm test && npm run build`
Workdir frontend: `FrontEnd`
Run: `npm test && npm run build`
Workdir bot: `discord-bot`
Run: `git diff --check`
Expected: todos os comandos PASS e nenhum erro de whitespace.

- [ ] **Step 6: Auditar internacionalização**

Confirmar paridade `pt.json`/`en.json`, `pt-BR.ts`/`en-US.ts` e três `.resx`; revisar acentos, placeholders, botões, títulos, badges, toasts, vazios e validações.

- [ ] **Step 7: Marcar T109-T112 e criar commit final de testes**

```bash
git add BackEnd .github/workflows/ci.yml specs/017-robustecer-drafts-discord-jogadores/tasks.md
git commit -m "test: cobrir segurança e concorrência de drafts"
```

---

## Final Review

- [ ] Confirmar que T075-T112 estão marcadas somente quando sua evidência existe.
- [ ] Revisar `git status`, `git diff`, `git log --oneline -10` e garantir que `.superpowers/` não foi staged.
- [ ] Executar auditoria de código focada em autorização, claim, concorrência, recursos e migrações.
- [ ] Não fazer push, merge ou PR sem solicitação explícita.

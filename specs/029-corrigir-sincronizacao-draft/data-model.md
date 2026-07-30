# Data Model: Sincronização e Operação do Draft

## Persisted Models

### DraftMontagem

Modelo existente; nenhuma nova tabela de snapshot.

| Field | Type | Persistence | Rule |
|-------|------|-------------|------|
| `Id` | `Guid` | `draft_montagens.id` | Identidade estável. |
| `VersaoEstado` | `long` | `versao_estado` | Monotônica, incrementada por transição visível e token de concorrência. |
| `DataAtualizacao` | `DateTimeOffset` | `data_atualizacao` | Metadado informativo; não ordena aplicação no cliente. |
| `TurnoExpiraEm` | `DateTimeOffset?` | `turno_expira_em` | Base da projeção do timer. |
| `TurnoIniciadoEm` | `DateTimeOffset?` | `turno_iniciado_em` | Fallback para início do realtime. |

Qualquer SQL que altere `draft_montagem_publicacoes_discord` de forma visível também atualiza a linha pai no mesmo comando/transação e retorna `DraftMontagemVersionStamp(Guid Id, long VersaoEstado, DateTimeOffset DataAtualizacao)`. Isso vale para claim adquirido ou terminalizado, sucesso, falha e expiração; no-op retorna stamp nulo e não publica.

### DraftMontagemActor

Value object de autoria, não tabela própria.

| Field | Type | Rule |
|-------|------|------|
| `Tipo` | `DraftMontagemActorType` | `User` ou `System`. |
| `UsuarioId` | `Guid?` | Obrigatório para `User`; nulo para `System`. |

Factories: `DraftMontagemActor.User(Guid)` e `DraftMontagemActor.System()`.

### DraftMontagemAcaoAdministrativa

| Field | Type | Persistence | Rule |
|-------|------|-------------|------|
| `Id` | `Guid` | existente | UUID. |
| `ResponsavelTipo` | enum/string | nova `responsavel_tipo` | Backfill `User`; obrigatório. |
| `ResponsavelUsuarioId` | `Guid?` | FK existente tornada nullable | Presente somente quando o tipo exige usuário. |
| `Tipo` | `string` | existente | Código técnico estável da ação. |
| `RegistradoEm` | `DateTimeOffset` | existente | Horário oficial. |

Migration aditiva necessária após inspeção do modelo atual: adicionar `responsavel_tipo`, preencher registros existentes com `User`, tornar `responsavel_usuario_id` nullable e adicionar check constraint `User` com usuário não nulo ou `System` com usuário nulo. Nenhuma remoção ou reescrita de histórico.

Contrato administrativo: `DraftMontagemAcaoAdministrativaResponseDto(Guid Id, string Tipo, DraftMontagemActorType ResponsavelTipo, Guid? ResponsavelUsuarioId, Guid? JogadorAlvoId, string? Motivo, DateTimeOffset RegistradoEm)`. O frontend usa `responsavelTipo` e `responsavelUsuarioId?: string | null`; `System` é apresentado como `Sistema`/`System` por i18n.

## Transport Models

### DraftMontagemRealtimeSnapshotDto (shared)

| Field | Type | Visibility |
|-------|------|------------|
| `Montagem` | `DraftMontagemResponseDto` | Compartilhada no grupo do draft. Inclui `Id`, `VersaoEstado`, `DataAtualizacao` e estado completo exibido. |
| `ServerNow` | `DateTimeOffset` | Compartilhada; usada somente para offset temporal. |

Proibição: não inclui `CanCurrentUserPick`, roles, claims, token, permissões administrativas ou projeção derivada da identidade.

### DraftMontagemRealtimeStateDto (personalized)

| Field | Type | Visibility |
|-------|------|------------|
| `Montagem` | `DraftMontagemResponseDto` | Estado compartilhado canônico, preservando o contrato HTTP existente. |
| `ServerNow` | `DateTimeOffset` | Horário oficial para recalcular o offset da sessão. |
| `CanCurrentUserPick` | `bool` | Apenas resposta autenticada individual. |

O endpoint personalizado pode recalcular capacidade mesmo quando a versão compartilhada não mudou.

### Candidate Projections

`DraftMontagemRealtimeCandidate(Guid Id)` identifica um draft cujo filtro SQL indica turno potencialmente expirado. O comando recarrega e decide timeout versus duração máxima.

`DraftMontagemPresenceClosureCandidate(Guid Id)` identifica presença potencialmente expirada. Contagem, horário e estado são lidos apenas pelo comando após recarga do agregado.

## Ephemeral Frontend Models

### DraftSyncGeneration

| Field | Type | Rule |
|-------|------|------|
| `draftId` | `string` | Deve coincidir com a seleção atual. |
| `generation` | `number` | Incrementa em cada abertura/saída; invalida callbacks antigos. |
| `highestSharedVersion` | `number` | Maior `versaoEstado` compartilhada aplicada. |
| `passiveRequestId` | `number` | Ordena estado compartilhado de GET/fallback dentro da lane passiva. |
| `mutationRequestId` | `number` | Ordena respostas dentro da lane de mutação. |
| `personalizedSequence` | `number` | Sequência global atribuída a toda resposta HTTP personalizada, independentemente da lane. |
| `lastPersonalizedSequence` | `number` | Maior sequência personalizada aplicada. |
| `auxiliaryRequestId` | `number` | Ordena somente enriquecimento opcional e nunca invalida GET canônico. |
| `connectionStatus` | enum | `connected`, `reconnecting`, `fallback`, `disconnected`. |
| `joined` | `boolean` | `connected` exige true; falha de Join/rejoin força false e degradação. |

### LocalLayoutEdit

| Field | Type | Rule |
|-------|------|------|
| `draftId` | `string` | Pertence a uma única geração/draft. |
| `baseVersion` | `number` | Versão clonada antes da primeira alteração. |
| `localMontagem` | `DraftMontagem` | Clone editável; nunca referência direta das props. |
| `dirty` | `boolean` | Verdadeiro após mudança local e até aplicação da versão persistida correspondente. |
| `requiresReconciliation` | `boolean` | Verdadeiro ao manter edição diante de remoto maior ou após 409. |
| `pendingCanonicalSnapshot` | snapshot/null | Maior snapshot remoto retido enquanto dirty. |
| `pendingIntent` | intent/null | Uma entre aplicar remoto, trocar draft, navegar, remover/filtrar, arquivar ou sair. |
| `canonicalResetToken` | `number` | Incrementado pela view somente após descarte explícito; mudança força clone canônico e limpa dirty. |
| `acceptedSaveVersion` | `number?` | Versão retornada pela mutação de layout vigente; somente ela pode limpar dirty do save correspondente. |

Persistido versus efêmero: somente `DraftMontagem`, sua versão e auditoria são persistidos. Geração, lanes, conexão, clone, dirty, intent e snapshot pendente vivem somente na instância da tela e são descartados ao encerrar a geração após confirmação.

## Invariants

1. Um evento compartilhado nunca contém capacidade específica de usuário.
2. `VersaoEstado` é a única ordenação do estado compartilhado; `ServerNow` e `DataAtualizacao` não vencem versão.
3. Snapshot de outro draft ou geração é descartado.
4. Versão menor ou igual a `highestSharedVersion` nunca reaplica campos compartilhados.
5. Mesma versão pode atualizar capacidade/offset somente por HTTP personalizado cuja sequência global seja nova.
6. Lane passiva não invalida shared da lane de mutação, nem o inverso; `personalizedSequence` ordena metadados entre ambas.
7. 409 bloqueia nova mutação até GET personalizado concluir ou a geração encerrar.
8. Publisher executa somente após commit e não publica rejeição/no-op.
9. Falha exclusiva do publisher não altera o resultado funcional persistido.
10. Cada candidato automático é recarregado, revalidado e persistido em scope próprio.
11. Ação automática usa ator `System`, nunca GUID humano simulado.
12. Dirty só limpa por mudança de `canonicalResetToken` ou quando `acceptedSaveVersion` corresponde à resposta vigente, é maior que `baseVersion` e coincide com a prop canônica aplicada.
13. Remoto maior durante dirty não substitui `localMontagem`; fica pendente até decisão.
14. SQL de publicação visível incrementa versão/data e retorna stamp atomicamente; no-op não incrementa.
15. Usuário humano autenticado só visualiza draft não arquivado; GET e Join aplicam a mesma decisão/rejeição.
16. Publisher ignora cancelamento da request após commit e limita cada tentativa a 5 s internos.
17. `connected` implica conexão iniciada e Join vigente; falha de Join/rejoin ativa fallback/restart.

## State Transitions

### Connection

```text
disconnected --start+Join success--> connected
disconnected/reconnecting --Join failure--> fallback + controlled restart
connected --transport loss--> reconnecting
reconnecting --fallback tick--> fallback
fallback --reconnected+Join+GET--> connected
reconnecting/fallback --retry exhausted--> disconnected
disconnected --controlled retry while active--> reconnecting
any --generation closed--> disconnected (timers/callbacks cancelled)
```

### Snapshot Application

```text
received
  -> reject if draft/generation mismatch
  -> reject shared fields if version <= highest
  -> update personalized/time only if HTTP sequence > last sequence and version >= highest
  -> if version > highest and layout clean: apply shared, set highest
  -> if version > highest and layout dirty: retain as pending, request decision
```

### Layout

```text
clean --local move--> dirty(baseVersion)
dirty --remote higher--> dirty + requiresReconciliation + pending snapshot
dirty --keep editing--> dirty + requiresReconciliation
dirty --discard--> apply highest canonical + increment canonicalResetToken -> clean
dirty --save response accepted--> set acceptedSaveVersion
dirty --matching canonical prop/version--> clean(new baseVersion)
dirty --stale acceptedSaveVersion/event same version--> remain dirty
dirty --409--> dirty + requiresReconciliation + GET + decision
```

### Automatic Processing

```text
candidate projected -> new scope -> command reloads aggregate
  -> no longer eligible: no-op, no event
  -> valid transition: system audit + commit -> best-effort publish
  -> any item failure: observe item, continue next scope
  -> host cancellation requested: propagate and stop
```

### Availability

```text
archive commit -> reload including archived -> shared snapshot -> DraftMontagemArchived
restore commit -> normal reload -> shared snapshot -> DraftMontagemRestored
notification timeout/failure -> observe and preserve command success
```

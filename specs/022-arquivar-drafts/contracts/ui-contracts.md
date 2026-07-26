# UI Contracts: Arquivamento Administrativo de Drafts

## `DraftNavigator`

Novas props:

```ts
canIncludeArchived: boolean
includeArchived: boolean
```

Novo emit:

```ts
'update:includeArchived': [value: boolean]
```

Regras:

- filtro aparece somente com `canIncludeArchived`;
- item arquivado mostra badge administrativo e badge do status simultaneamente;
- desmarcar o filtro emite a intenção antes de ocultar itens;
- alvos mantêm 44px e textos usam i18n.

## `DraftWorkspaceHeader`

Usa `draft.arquivado` para exibir badge separado do status. Ações são fornecidas pelos slots existentes; arquivamento usa tratamento destrutivo e restauração usa tratamento primário.

## `DraftReasonDialog`

Novas ações:

```ts
{ type: 'archiveDraft'; draftName: string; cancelsActiveDraft: boolean }
{ type: 'restoreDraft'; draftName: string }
```

Emit:

```ts
confirm: [reason: string | null]
```

Arquivar exige textarea vazio inicialmente, trim e 1-500 caracteres. Restaurar não renderiza textarea e emite `null`. O dialog informa claramente quando a ação também cancela o draft.

## `DraftDiscordPublicationPanel`

Aceita `Cancelamento` como tipo conhecido. Em draft arquivado, somente esse tipo pode entrar em `republishableTypes`, e apenas em `Falha` ou `RequerReconciliacao`.

## `DraftsView`

- Capacidade Admin+ é independente de `canManageDrafts`.
- Listagem normal envia `includeArchived: false`.
- Arquivar envia motivo e `versaoEstado` observada, usa o resultado reduzido para confirmar a mutação e recarrega a navegação ou detalhe necessário.
- Restaurar envia `versaoEstado` observada e recarrega a navegação ou detalhe antes de atualizar o workspace.
- Após arquivar fora do filtro, remove o item, fecha realtime e seleciona próximo, anterior ou vazio.
- Com filtro ativo, o draft pode permanecer selecionado apenas sem ações operacionais.
- Restauração mantém status e seleção e atualiza lista sem reload completo.
- `401` preserva dados e orienta login; `403` remove somente capacidades de arquivo; `409` recarrega e exige nova confirmação.
- Evento `DraftMontagemArchived` contém somente ID e reconcilia lista/seleção.
- Republicação de `Cancelamento` em arquivado aparece somente para Admin+ e usa o endpoint administrativo dedicado; republicações operacionais normais mantêm endpoint e permissão de Moderador.

## Permissões

| Controle | SuperAdmin | Admin | Moderador | Capitão/Jogador |
|----------|------------|-------|-----------|-----------------|
| Incluir arquivados | Sim | Sim | Não | Não |
| Arquivar | Sim | Sim | Não | Não |
| Restaurar | Sim | Sim | Não | Não |
| Ver motivo/histórico | Sim | Sim | Não | Não |

O frontend apenas controla apresentação; todas as requisições continuam autorizadas no backend.

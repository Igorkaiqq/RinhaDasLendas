# UI Contracts: Reabertura de Presença

## DraftPreparationPanel

Nova prop:

```ts
canReopenPresence: boolean
```

Novo evento:

```ts
'reopen-presence': []
```

Com `canReopenPresence=true`, o painel mostra uma ação secundária localizada. Com `canSelectCaptains=true`, mostra `{selected} / {total} capitães` junto à ação principal; a ação de definir continua desabilitada até igualdade exata.

## DraftReasonDialog

Nova ação:

```ts
{ type: 'reopenPresence'; draftName: string }
```

A ação não solicita motivo. O diálogo apresenta título, descrição do impacto, voltar e confirmar; foco inicial vai para confirmar em desktop e voltar em mobile, seguindo o comportamento de restauração existente.

## DraftsView

Capability:

```ts
canReopenPresence = operational
  && canManageDrafts
  && status === DraftMontagemStatusValues.PresencaEncerrada
```

Antes de abrir o diálogo e antes de executar a mutação, a view revalida capability, draft selecionado e `saving`. Em sucesso, aplica a projeção retornada, limpa a seleção de capitães, mostra feedback localizado e restaura foco na etapa.

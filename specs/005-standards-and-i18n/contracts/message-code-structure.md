# Message Code Structure Contract

## Overview

The message code system centralizes user-facing messages in RinhaDasLendas. Each message has one immutable code, one category, localized text, context, and severity.

## Message Code Format

**Pattern**: `[CATEGORY_PREFIX][3-DIGIT_SEQUENCE]`

Examples: `MSIS001`, `ME020`, `MV005`.

## Official Categories and Ranges

| Prefix | Category | Use Case | Active Range |
|--------|----------|----------|--------------|
| `MI` | Mensagem Informativa | Neutral information and loading states | `001-009` |
| `MSIS` | Mensagem de Sucesso | Successful operations | `001-009` |
| `ME` | Mensagem de Erro | Unexpected errors, exceptions, and missing resources | `001-020` |
| `MV` | Mensagem de Validação | Input validation failures | `001-019` |
| `MC` | Mensagem de Confirmação | User confirmation prompts | `001-009` |
| `MA` | Mensagem de Alerta | Warnings and cautionary states | `001-009` |

The initial catalog intentionally keeps all categories inside these ranges. Future expansion may add new ranges through a documented standards update.

## Message Catalog Entry Structure

```json
{
  "code": "MSIS001",
  "category": "Success",
  "categoryPrefix": "MSIS",
  "ptBR": "Operação realizada com sucesso",
  "enUS": "Operation completed successfully",
  "description": "Generic success message for successful operations",
  "context": "Player creation, update, deletion; queue and draft operations",
  "severity": "info"
}
```

## Backend Response Structure

### Response with Message Code

```json
{
  "messageCode": "MSIS001",
  "message": "Operação realizada com sucesso",
  "messageCategory": "Success",
  "data": {}
}
```

### Error Response with Message Code

```json
{
  "messageCode": "MV001",
  "message": "Campo obrigatório",
  "messageCategory": "Validation",
  "details": {
    "field": "name",
    "rule": "required"
  }
}
```

## Backend Implementation Strategy

**Message Code Storage**: `MessageCodes.cs` contains constants only.

```csharp
public const string OperationSuccess = "MSIS001";
public const string UnexpectedError = "ME001";
```

**Message Text Storage**: `.resx` files in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/`.

- `Messages.resx`: default Portuguese text.
- `Messages.pt-BR.resx`: explicit Portuguese locale.
- `Messages.en-US.resx`: English locale.

**Message Retrieval**: `IMessageProvider` and `ResourceMessageProvider`.

```csharp
public interface IMessageProvider
{
    string GetMessage(string messageCode);
    string GetMessage(string messageCode, string cultureCode);
}
```

## Frontend Implementation Strategy

- Codes are represented as TypeScript enum or constants.
- Localized text lives in JSON locale files.
- `messageService.ts` resolves a code and locale with fallback to `pt-BR`.

## Versioning and Evolution

- Codes are immutable once released.
- New codes use the next available number in the category.
- Deprecated codes remain documented for compatibility.
- Localization text can change without changing the code.

## Catalog Maintenance

- Source of truth: `docs/messages/message-catalog.md`.
- Quick reference: `docs/messages/message-codes.md`.
- Every PR adding user-facing messages must update the catalog before implementation.

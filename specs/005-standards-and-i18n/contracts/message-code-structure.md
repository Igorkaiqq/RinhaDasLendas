# Message Code Structure Contract

## Overview

The message code system provides a centralized, standardized approach to handling all user-facing messages in RinhaDasLendas. Each message has a unique code that maps to localized text and metadata.

## Message Code Format

**Pattern**: `[CATEGORY_PREFIX][3-DIGIT_SEQUENCE]`

**Example**: `MSIS001`, `ME042`, `MV005`

## Categories

| Prefix | Category | Use Case | Count |
|--------|----------|----------|-------|
| `MI` | Mensagem Informativa | Neutral information, loading states | 001-009 |
| `MSIS` | Mensagem de Sucesso | Operation completed successfully | 001-009 |
| `ME` | Mensagem de Erro | Unexpected errors, exceptions | 001-029 |
| `MV` | Mensagem de Validação | Input validation failures | 001-019 |
| `MC` | Mensagem de Confirmação | User confirmation prompts | 001-009 |
| `MA` | Mensagem de Alerta | Warnings, cautionary messages | 001-009 |

## Message Catalog Entry Structure

```json
{
  "code": "MSIS001",
  "category": "Success",
  "categoryPrefix": "MSIS",
  "ptBR": "Operação realizada com sucesso",
  "enUS": "Operation completed successfully",
  "description": "Generic success message for successful operations",
  "context": "Player creation, update, deletion; Fila creation; Draft operations",
  "severity": "info"
}
```

## Backend Response Structure (Phase 3+)

### Response with Message Code

```typescript
{
  "messageCode": "MSIS001",
  "message": "Operação realizada com sucesso",
  "messageCategory": "Success",
  "data": {
    // Operation-specific data
  }
}
```

### Error Response with Message Code

```typescript
{
  "messageCode": "MV001",
  "message": "Campo obrigatório",
  "messageCategory": "Validation",
  "details": {
    "field": "name",
    "value": "",
    "rule": "required"
  }
}
```

## Message Code Ranges by Domain

**Player Management** (MI001-MI009, MSIS001-MSIS009, MV001-MV009, ME001-ME009)
- Player creation, update, deletion
- Validation of player data
- Status changes

**Fila/Queue** (MI010-MI019, MSIS010-MSIS019, MV010-MV019)
- Queue join/leave operations
- Player confirmation/absence

**Draft Operations** (MI020-MI029, MSIS020-MSIS029, MV020-MV029)
- Draft start, pick, ban
- Team formation

**System Messages** (MI030+, MA001-MA009, ME020-ME029)
- Loading, warnings, alerts
- System-level errors

## Backend Implementation Strategy

**Message Code Storage**: `MessageCodes.cs` (constants only)
```csharp
public const string OperationSuccess = "MSIS001";
public const string UnexpectedError = "ME001";
```

**Message Text Storage**: `.resx` files (native .NET resource files)
- `Messages.resx` (Portuguese - Brazil, default)
- `Messages.pt-BR.resx` (Portuguese - Brazil, explicit)
- `Messages.en-US.resx` (English - US)

**Message Retrieval**: `IMessageProvider` interface and `ResourceMessageProvider` implementation
```csharp
public interface IMessageProvider
{
    string GetMessage(string messageCode);
    string GetMessage(string messageCode, string cultureCode);
}
```

**Key Points**:
- Message codes (strings like "MSIS001") are constants in `MessageCodes.cs`
- Message text (localized strings) lives in `.resx` files, not in code
- `ResourceMessageProvider` uses `System.Resources.ResourceManager` and `CultureInfo` to retrieve localized text
- No hardcoded message strings in classes
- Backend supports native .NET i18n; text changes don't require code recompilation

## Frontend Implementation Strategy

**Message Code Storage**: TypeScript enums or constants
**Message Text Storage**: JSON i18n files (`locales/pt-BR.json`, `locales/en-US.json`)
**Message Retrieval**: `messageService.ts` (wrapper around i18n library)

**Note**: Frontend and backend have different storage strategies (JSON vs .resx) optimized for each platform's conventions.

## Versioning & Evolution

- Message codes are **immutable** once released
- New codes are added with the next available sequence number
- Deprecation: If a message no longer applies, mark as deprecated in catalog but keep code for backward compatibility
- Localization updates don't change code; they update the text mapping

## Catalog Maintenance

- Catalog stored in `docs/messages/message-catalog.md`
- All new codes MUST be registered before deployment
- Code review: Every PR that adds new user-facing messages MUST update the catalog
- Automation: Phase 6 will define CI/CD hook to validate all codes used in code exist in catalog

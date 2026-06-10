# Data Model: Standards and Internationalization System

## Message Catalog Data Structure

### Message Entity

Represents a single user-facing message with unique code and localizations.

```typescript
Message {
  code: string                    // Unique identifier (e.g., "MSIS001")
  category: MessageCategory       // Enum: Info, Success, Error, Validation, Confirmation, Alert
  categoryPrefix: string          // Short prefix (e.g., "MSIS", "ME", "MV")
  textPtBR: string               // Portuguese (Brazilian) message text
  textEnUS: string               // English (US) message text
  description: string            // Internal description of message purpose
  context: string[]              // Usage contexts (e.g., ["player_creation", "player_update"])
  severity: "info" | "warning" | "error"  // Display severity level
  createdAt: timestamp           // When message was added to catalog
  deprecated: boolean            // Whether this message is still in use
}
```

### MessageCategory Enum

```typescript
enum MessageCategory {
  Info = "MI"           // MI + 001-009: Neutral information
  Success = "MSIS"      // MSIS + 001-009: Operation succeeded
  Error = "ME"          // ME + 001-029: Unexpected errors
  Validation = "MV"     // MV + 001-019: Input validation failures
  Confirmation = "MC"   // MC + 001-009: User confirmation prompts
  Alert = "MA"          // MA + 001-009: Warnings and alerts
}
```

### Message Code Numbering

| Category | Prefix | Range | Typical Count | Domain |
|----------|--------|-------|---------------|--------|
| Informative | MI | 001-009 | 5-8 | Loading, neutral status |
| Success | MSIS | 001-009 | 5-8 | Operation success, confirmations |
| Error | ME | 001-029 | 15-20 | System errors, exceptions, API errors |
| Validation | MV | 001-019 | 10-15 | Form validation, input constraints |
| Confirmation | MC | 001-009 | 3-5 | User action confirmations |
| Alert | MA | 001-009 | 3-5 | Warnings, cautionary messages |

**Total Capacity**: ~80-100 message codes (sufficient for MVP + moderate feature growth)

---

## Standard Constants & Enums

### Frontend Constants (Phase 6 Output)

**AppRoutes** (TypeScript)
```typescript
export const AppRoutes = {
  HOME: '/',
  PLAYERS: '/players',
  PLAYER_DETAIL: '/players/:id',
  QUEUE: '/queue',
  DRAFT: '/draft',
  STATISTICS: '/statistics',
  SETTINGS: '/settings',
} as const
```

**PlayerStatus** (TypeScript)
```typescript
export enum PlayerStatus {
  ACTIVE = 'ativo',
  INACTIVE = 'inativo',
  ON_BREAK = 'em_recesso',
  BANNED = 'banido'
}
```

**LeagueRole** (TypeScript)
```typescript
export enum LeagueRole {
  TOP = 'top',
  JUNGLE = 'jungle',
  MID = 'mid',
  ADC = 'adc',
  SUPPORT = 'support'
}
```

**LeagueRank** (TypeScript Union Type)
```typescript
export type LeagueRank = 
  | 'iron'
  | 'bronze'
  | 'silver'
  | 'gold'
  | 'platinum'
  | 'diamond'
  | 'master'
  | 'grandmaster'
  | 'challenger'
```

**MessageCode** (TypeScript)
```typescript
export enum MessageCode {
  // Info
  LOADING = 'MI001',
  
  // Success
  OPERATION_SUCCESS = 'MSIS001',
  PLAYER_CREATED = 'MSIS002',
  
  // Validation
  FIELD_REQUIRED = 'MV001',
  EMAIL_INVALID = 'MV002',
  
  // Error
  UNKNOWN_ERROR = 'ME001',
  SERVER_ERROR = 'ME002',
  
  // ... more codes
}
```

**LocaleCode** (TypeScript)
```typescript
export type LocaleCode = 'pt-BR' | 'en-US'

export const DEFAULT_LOCALE: LocaleCode = 'pt-BR'
export const SUPPORTED_LOCALES: LocaleCode[] = ['pt-BR', 'en-US']
```

### Backend Constants & Enums (Phase 2 Output)

**MessageCodes.cs** (.NET - Code Constants Only)
```csharp
namespace RinhaDasLendas.Domain.Constants;

public static class MessageCodes
{
  // Informative (MI)
  public const string Loading = "MI001";
  public const string Processing = "MI002";
  
  // Success (MSIS)
  public const string OperationSuccess = "MSIS001";
  public const string PlayerCreated = "MSIS002";
  public const string PlayerUpdated = "MSIS003";
  
  // Validation (MV)
  public const string FieldRequired = "MV001";
  public const string EmailInvalid = "MV002";
  public const string RoleSelectionRequired = "MV003";
  
  // Error (ME)
  public const string UnexpectedError = "ME001";
  public const string ServerConnectionFailed = "ME002";
  public const string PlayerNotFound = "ME003";
  
  // Confirmation (MC)
  public const string ConfirmAction = "MC001";
  
  // Alert (MA)
  public const string WarningMessage = "MA001";
}
```

**Key Point**: `MessageCodes.cs` contains **only the codes** (string constants), never the message text. This provides type-safe references to codes while keeping text in .resx files.

**IMessageProvider.cs** (.NET Interface)
```csharp
namespace RinhaDasLendas.Application.Interfaces;

public interface IMessageProvider
{
    /// <summary>
    /// Gets a localized message by code using the current culture.
    /// </summary>
    /// <param name="messageCode">The message code (e.g., "MSIS001", "MV001")</param>
    /// <returns>The localized message text, or a fallback if not found</returns>
    string GetMessage(string messageCode);
    
    /// <summary>
    /// Gets a localized message by code using a specific culture.
    /// </summary>
    /// <param name="messageCode">The message code</param>
    /// <param name="cultureCode">The culture code (e.g., "pt-BR", "en-US")</param>
    /// <returns>The localized message text, or a fallback if not found</returns>
    string GetMessage(string messageCode, string cultureCode);
}
```

**ResourceMessageProvider.cs** (.NET Implementation)
```csharp
namespace RinhaDasLendas.Infrastructure.Messages;

using System.Globalization;
using System.Resources;
using RinhaDasLendas.Application.Interfaces;

public class ResourceMessageProvider : IMessageProvider
{
    private readonly ResourceManager _resourceManager;
    
    public ResourceMessageProvider()
    {
        // Points to Messages.resx assembly resource
        _resourceManager = new ResourceManager("RinhaDasLendas.Infrastructure.Messages.Messages", 
            typeof(ResourceMessageProvider).Assembly);
    }
    
    public string GetMessage(string messageCode)
    {
        var culture = CultureInfo.CurrentCulture;
        return GetMessage(messageCode, culture.Name);
    }
    
    public string GetMessage(string messageCode, string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            var message = _resourceManager.GetString(messageCode, culture);
            return message ?? $"[{messageCode}]"; // Fallback if key not found
        }
        catch
        {
            return $"[{messageCode}]"; // Fallback on error
        }
    }
}
```

**Resource Files (.resx)** - Located in `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/`

**Messages.resx** (Portuguese - Brazil, default)
```xml
<root>
  <data name="MI001" xml:space="preserve">
    <value>Carregando informações...</value>
  </data>
  <data name="MSIS001" xml:space="preserve">
    <value>Operação realizada com sucesso</value>
  </data>
  <data name="MSIS002" xml:space="preserve">
    <value>Jogador criado com sucesso</value>
  </data>
  <data name="MV001" xml:space="preserve">
    <value>Campo obrigatório</value>
  </data>
  <data name="ME001" xml:space="preserve">
    <value>Ocorreu um erro inesperado</value>
  </data>
</root>
```

**Messages.pt-BR.resx** (Portuguese - Brazil, explicit)
```xml
<!-- Same as Messages.resx, explicitly named for pt-BR locale -->
```

**Messages.en-US.resx** (English - US)
```xml
<root>
  <data name="MI001" xml:space="preserve">
    <value>Loading information...</value>
  </data>
  <data name="MSIS001" xml:space="preserve">
    <value>Operation completed successfully</value>
  </data>
  <data name="MSIS002" xml:space="preserve">
    <value>Player created successfully</value>
  </data>
  <data name="MV001" xml:space="preserve">
    <value>Required field</value>
  </data>
  <data name="ME001" xml:space="preserve">
    <value>An unexpected error occurred</value>
  </data>
</root>
```

**MessageCategory.cs** (.NET Enum)
```csharp
namespace RinhaDasLendas.Domain.Enums;

public enum MessageCategory
{
  Info,
  Success,
  Error,
  Validation,
  Confirmation,
  Alert
}
```

**PlayerStatus.cs** (.NET Enum, Existing)
```csharp
namespace RinhaDasLendas.Domain.Enums;

public enum PlayerStatus
{
  Active,
  Inactive,
  OnBreak,
  Banned
}
```

---

## Backend i18n Implementation Notes

- **Resource Files**: .resx files are compiled into satellite assemblies by the .NET runtime, enabling culture-specific resource loading
- **CultureInfo**: `ResourceMessageProvider` respects `System.Globalization.CultureInfo.CurrentCulture`, allowing per-request locale switching (e.g., via HTTP Accept-Language header)
- **No Recompilation**: Text changes in .resx files don't require code recompilation; simply recompile resources
- **Dependency Injection**: `IMessageProvider` is injected as a singleton or scoped service into controllers/handlers that need messages
- **Testability**: Tests can pass a mock `IMessageProvider` returning controlled message text, decoupling tests from resource files

---

## i18n Configuration Structure

### Translation File Format

**Location**: `FrontEnd/src/i18n/locales/`

**pt-BR.json**
```json
{
  "app": {
    "name": "Rinha das Lendas",
    "tagline": "Organize suas partidas de League of Legends"
  },
  "navigation": {
    "players": "Jogadores",
    "queue": "Fila",
    "draft": "Draft",
    "statistics": "Estatísticas",
    "settings": "Configurações"
  },
  "messages": {
    "MSIS001": "Operação realizada com sucesso",
    "MI001": "Carregando informações...",
    "MV001": "Campo obrigatório",
    "ME001": "Ocorreu um erro inesperado"
  },
  "player": {
    "name": "Nome",
    "email": "Email",
    "region": "Região",
    "favoriteRoles": "Rotas Preferidas"
  }
}
```

**en-US.json**
```json
{
  "app": {
    "name": "Rinha das Lendas",
    "tagline": "Organize your League of Legends matches"
  },
  "navigation": {
    "players": "Players",
    "queue": "Queue",
    "draft": "Draft",
    "statistics": "Statistics",
    "settings": "Settings"
  },
  "messages": {
    "MSIS001": "Operation completed successfully",
    "MI001": "Loading information...",
    "MV001": "Required field",
    "ME001": "An unexpected error occurred"
  },
  "player": {
    "name": "Name",
    "email": "Email",
    "region": "Region",
    "favoriteRoles": "Favorite Roles"
  }
}
```

---

## Message Catalog Entry Examples

### Information Messages (MI001-MI009)

| Code | pt-BR | en-US | Context |
|------|-------|-------|---------|
| MI001 | Carregando informações... | Loading information... | Data loading, fetching |
| MI002 | Aguardando resposta do servidor | Waiting for server response | API calls |
| MI003 | Sincronizando dados | Syncing data | Data synchronization |

### Success Messages (MSIS001-MSIS009)

| Code | pt-BR | en-US | Context |
|------|-------|-------|---------|
| MSIS001 | Operação realizada com sucesso | Operation completed successfully | Generic success |
| MSIS002 | Jogador criado com sucesso | Player created successfully | Player creation |
| MSIS003 | Jogador atualizado com sucesso | Player updated successfully | Player update |

### Validation Messages (MV001-MV019)

| Code | pt-BR | en-US | Context |
|------|-------|-------|---------|
| MV001 | Campo obrigatório | Required field | Any required field |
| MV002 | Email inválido | Invalid email | Email validation |
| MV003 | Ao menos uma rota deve ser selecionada | At least one role must be selected | Route selection |
| MV004 | Nenhuma rota pode ser duplicada | No role can be duplicated | Route validation |

### Error Messages (ME001-ME029)

| Code | pt-BR | en-US | Context |
|------|-------|-------|---------|
| ME001 | Ocorreu um erro inesperado | An unexpected error occurred | Generic error |
| ME002 | Falha ao conectar ao servidor | Failed to connect to server | Connection error |
| ME003 | Jogador não encontrado | Player not found | 404 on player fetch |

---

## Standards Documentation Outline

### docs/standards/README.md
- Overview of standardization effort
- Links to all standards documents
- Quick navigation

### docs/standards/BRANCH_NAMING.md
- Pattern: `feature/NNN-slug`
- Examples with all project features
- Rationale and alignment

### docs/standards/COMMIT_MESSAGES.md
- Format: `type: description` (Portuguese)
- Types: feat, fix, docs, refactor, test, chore
- Examples

### docs/standards/SPECS_AND_PLANNING.md
- Spec Kit workflow (Constitution → Specify → Plan → Tasks → Implement)
- Feature numbering and directory naming
- Phase breakdown

### docs/standards/PR_STANDARDS.md
- Title format, description template
- Labeling conventions
- Review expectations

### docs/standards/CONSTANTS_AND_ENUMS.md
- When to use enums (fixed set of values)
- When to use constants (non-domain fixed values)
- When to use types (TypeScript domain modeling)
- Naming conventions

### docs/standards/I18N_GUIDELINES.md
- Adding new translation keys
- Locale structure and file format
- Testing translations
- Adding new languages

### docs/messages/README.md
- Purpose of message catalog
- How to reference messages
- Versioning and deprecation policy

### docs/messages/message-catalog.md
- Master list of all message codes (organized by category)
- Each entry: code, pt-BR, en-US, context, severity

### docs/messages/message-codes.md
- Quick reference, searchable by code
- Category breakdown
- New code registration process

---

## Relationships & State Transitions

### Message Lifecycle

```
Not Planned
    ↓
In Catalog (Phase 1)
    ↓
Implemented in Backend (Phase 2)
    ↓
Used by Frontend (Phase 4+)
    ↓
Maintained (updates, translations)
    ↓
Deprecated (if replaced)
```

### Localization Dependencies

```
Message Code
    ├→ Backend: MessageProvider (Phase 2)
    ├→ Frontend: messageService (Phase 4)
    └→ Translation Files: i18n locales (Phase 5)
        ├→ pt-BR.json
        └→ en-US.json
```

---

## Validation Rules

- **Message Code Uniqueness**: No two messages share the same code
- **Prefix Consistency**: Code prefix matches category (e.g., `MV*` → Validation)
- **Sequence Continuity**: Within category, sequence numbers should be continuous (no gaps preferred, but allowed)
- **Text Completeness**: Both Portuguese and English text required (or explicitly marked "TBD" for future)
- **Context Clarity**: Each message has clear context; no ambiguous codes

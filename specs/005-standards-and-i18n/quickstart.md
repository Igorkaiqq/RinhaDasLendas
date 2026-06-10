# Quickstart: Validating Standardization & i18n Framework

## Overview

This guide provides step-by-step validation scenarios to verify that the standardization framework is correctly implemented and functioning end-to-end across documentation, backend infrastructure, and frontend services.

## Phase 1: Documentation Validation

### Scenario 1.1: Developer Onboarding (Branch Naming)

**Prerequisite**: `docs/standards/BRANCH_NAMING.md` exists

**Steps**:
1. A new developer reads `docs/standards/BRANCH_NAMING.md`
2. Developer checks existing branches to understand current naming
3. Developer creates a new feature branch following the pattern

**Expected Outcome**: ✅
- Developer correctly creates branch as `feature/NNN-feature-slug` (e.g., `feature/006-new-feature`)
- Branch naming is consistent with all existing branches
- AGENTS.md branch naming example is now accurate

**Validation Command**:
```bash
git branch | grep "feature/" | head -5
# Output should show: feature/001-..., feature/002-..., etc.
```

### Scenario 1.2: Commit Message Standards

**Prerequisite**: `docs/standards/COMMIT_MESSAGES.md` exists

**Steps**:
1. Developer reads commit message format guidelines
2. Developer makes a code change
3. Developer commits with correct format

**Expected Outcome**: ✅
- Commit message format: `type: description` (Portuguese)
- Examples include all required types (feat, fix, docs, refactor, test, chore)
- Existing commits in repo align with pattern

**Validation Command**:
```bash
git log --oneline -10
# Output should show messages in Portuguese with semantic prefixes
```

### Scenario 1.3: Message Catalog Discovery

**Prerequisite**: `docs/messages/message-catalog.md` exists

**Steps**:
1. Developer needs to find message code for "field required" validation
2. Developer searches `docs/messages/message-catalog.md`
3. Developer finds `MV001` with context and both language texts

**Expected Outcome**: ✅
- Message found: `MV001` = "Campo obrigatório" (pt-BR) / "Required field" (en-US)
- Context explains where it's used
- Message code is documented before backend implementation

**Validation Command**:
```bash
grep "MV001" docs/messages/message-catalog.md
# Output should show: MV001 | Campo obrigatório | Required field | ...
```

---

## Phase 2: Backend Infrastructure Validation

### Scenario 2.1: Message Provider Lookup (Culture-Aware)

**Prerequisites**: 
- Phase 2 backend code implemented
- `MessageCodes.cs` created with message code constants
- `IMessageProvider` interface implemented
- `ResourceMessageProvider` created and registered
- `.resx` files (Messages.resx, Messages.pt-BR.resx, Messages.en-US.resx) populated

**Steps**:
1. Backend code requests message text for code using `IMessageProvider`
2. `messageProvider.GetMessage(MessageCodes.OperationSuccess)` is called
3. ResourceMessageProvider looks up the code in .resx based on current culture
4. Returns: "Operação realizada com sucesso" (pt-BR) or "Operation completed successfully" (en-US)

**Expected Outcome**: ✅
- Message provider returns correct localized text from .resx files
- All message codes are lookupable via ResourceMessageProvider
- Culture-aware lookup works correctly (e.g., `GetMessage(code, "en-US")` returns English)
- No hardcoded message text in code (all in .resx files)

**Validation Test**:
```csharp
[Fact]
public void GetMessage_WithValidCode_ReturnsPortugueseText()
{
    var provider = new ResourceMessageProvider();
    var result = provider.GetMessage(MessageCodes.OperationSuccess);
    Assert.Equal("Operação realizada com sucesso", result);
}

[Fact]
public void GetMessage_WithCulture_ReturnsEnglishText()
{
    var provider = new ResourceMessageProvider();
    var result = provider.GetMessage(MessageCodes.OperationSuccess, "en-US");
    Assert.Equal("Operation completed successfully", result);
}
```

### Scenario 2.2: Message Codes Constants

**Prerequisites**:
- `MessageCodes.cs` created in Domain/Constants
- Message code constants defined

**Steps**:
1. Developer imports `MessageCodes`
2. Developer uses constant like `MessageCodes.OperationSuccess`
3. Code provides IntelliSense for available codes

**Expected Outcome**: ✅
- `MessageCodes.cs` contains only message code constants (strings)
- No message text is in `MessageCodes.cs` (text is in .resx files)
- Constants enable type-safe references to codes
- Code compiles without errors

**Validation Command**:
```bash
cd BackEnd && dotnet build
# Output: Build succeeded

# Verify MessageCodes.cs has only codes, not text
grep -v "public const string" BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs | grep -v "MSIS\|ME\|MV\|MI\|MC\|MA"
# Should return minimal output (no message text)
```

---

## Phase 3: Frontend Message Service Validation

### Scenario 3.1: Message Service Lookup

**Prerequisites**:
- Phase 3 frontend code implemented
- `messageService.ts` created
- Constants imported

**Steps**:
1. Frontend component needs success message after player creation
2. Component calls `messageService.getMessage('MSIS002', 'pt-BR')`
3. Service returns: "Jogador criado com sucesso"

**Expected Outcome**: ✅
- Message service returns correct text
- Service handles missing codes gracefully (fallback or error logged)
- All MSIS codes are available

**Validation Test**:
```typescript
import { messageService } from '@/services/messageService'

test('getMessage returns correct Portuguese text', () => {
  const msg = messageService.getMessage('MSIS002', 'pt-BR')
  expect(msg).toBe('Jogador criado com sucesso')
})
```

### Scenario 3.2: Constants Usage in Components

**Prerequisites**:
- Frontend constants defined (AppRoutes, PlayerStatus, LeagueRoles, etc.)
- Sidebar component refactored to use constants

**Steps**:
1. Sidebar component renders navigation items
2. Item routes reference `AppRoutes` constant instead of hardcoded strings
3. Build is executed

**Expected Outcome**: ✅
- Sidebar renders correctly with consistent routes
- No hardcoded route strings in component code
- `npm run lint` passes
- `npm run build` succeeds

**Validation Commands**:
```bash
cd FrontEnd && npm run lint
# Output: All linting rules passed

cd FrontEnd && npm run build
# Output: Build success; bundle size within limits
```

---

## Phase 4: Frontend Internationalization Validation

### Scenario 4.1: Translation Key Usage

**Prerequisites**:
- Phase 4 i18n infrastructure implemented
- Translation files exist: `pt-BR.json`, `en-US.json`
- Components updated to use i18n keys

**Steps**:
1. Sidebar is rendered with locale `pt-BR`
2. Menu item references translation key `navigation.players`
3. Text displays: "Jogadores"
4. User switches locale to `en-US`
5. Text updates to: "Players"

**Expected Outcome**: ✅
- Sidebar displays correct localized text based on current locale
- Switching locale updates all text without page reload
- No hardcoded menu labels remain

**Validation Commands**:
```bash
# Check translation files exist
ls -la FrontEnd/src/i18n/locales/
# Output: pt-BR.json, en-US.json

# Check sidebar uses i18n keys
grep "navigation\." FrontEnd/src/components/Sidebar.vue
# Output should show: $t('navigation.players'), etc.
```

### Scenario 4.2: Message Service Integration with i18n

**Prerequisites**:
- Message service and i18n both configured
- Player page displays error message

**Steps**:
1. Player form validation fails for required field
2. Frontend calls `messageService.getMessage('MV001', 'pt-BR')`
3. Message displays: "Campo obrigatório"
4. User switches locale to `en-US`
5. Message updates to: "Required field"

**Expected Outcome**: ✅
- Message service respects locale parameter
- Messages update when locale changes
- Both message service and i18n keys work together

**Validation Test**:
```typescript
test('messageService returns correct locale text', () => {
  expect(messageService.getMessage('MV001', 'pt-BR'))
    .toBe('Campo obrigatório')
  
  expect(messageService.getMessage('MV001', 'en-US'))
    .toBe('Required field')
})
```

---

## Phase 5: Backend Message Code Adoption Validation

### Scenario 5.1: Player Endpoint Returns Message Code with IMessageProvider

**Prerequisites**:
- Phase 5 backend code implemented
- Player create endpoint updated to use `IMessageProvider`
- `ResourceMessageProvider` resolves message codes to text from .resx files

**Steps**:
1. Client sends POST `/api/players` with player data
2. Server creates player successfully
3. Server uses `IMessageProvider.GetMessage(MessageCodes.PlayerCreated)` to get localized text
4. Server returns response with `messageCode: "MSIS002"` and localized message text

**Expected Outcome**: ✅
- Response includes `messageCode` field and localized message text
- Message text is retrieved from .resx files via ResourceMessageProvider
- Response also includes player data
- Existing API clients continue working (backward compatible)

**Validation Command**:
```bash
curl -X POST http://localhost:5000/api/players \
  -H "Content-Type: application/json" \
  -d '{"name":"Player1","email":"p1@example.com"}'

# Response includes:
# { 
#   "messageCode": "MSIS002", 
#   "message": "Jogador criado com sucesso", 
#   "messageCategory": "Success",
#   "data": {...} 
# }
```

### Scenario 5.2: Integration Tests Pass (Culture-Aware)

**Prerequisites**:
- Phase 5 tests written for message code responses
- Tests validate both Portuguese and English message retrieval
- No breaking changes to existing tests

**Steps**:
1. Run integration tests for player endpoints
2. Tests verify message codes and localized text
3. All tests pass
4. No existing test failures introduced

**Expected Outcome**: ✅
- xUnit tests verify message code is returned correctly
- Tests verify localized text is retrieved from .resx files
- All existing tests still pass
- No breaking changes to API contracts

**Validation Commands**:
```bash
cd BackEnd
dotnet test RinhaDasLendas.Tests.csproj

# Output: All tests passed
```

---

## Phase 6: Governance & Feature Checklist Validation

### Scenario 6.1: New Feature Uses Message Codes

**Prerequisites**:
- Phase 6 checklist created at `docs/standards/FEATURE_CHECKLIST.md`
- New feature being planned

**Steps**:
1. Developer reads feature development checklist
2. Checklist requires: update message catalog, create translations, update constants
3. Developer follows steps before implementation

**Expected Outcome**: ✅
- Checklist is clear and actionable
- New feature messages are registered in `docs/messages/`
- Translations added to i18n files
- Constants defined and used

**Validation**:
- Review PR for new feature
- Verify message catalog updated
- Verify translations exist
- Verify constants defined and used

---

## End-to-End Validation

### Full Feature Test: Create New Player with i18n & Messages

**Prerequisites**: All phases complete

**Test Scenario**:
1. User loads player page (sidebar/topbar localized in pt-BR)
2. User switches locale to en-US (all text updates)
3. User fills player form and submits
4. Validation fails for required field
5. Error message displays: "Required field" (from `MV001`)
6. User corrects input and resubmits
7. Success message displays: "Player created successfully" (from `MSIS002`)
8. Player page updates with new player data

**Expected Outcome**: ✅
- Entire workflow works end-to-end
- Messages use codes consistently
- Localization works across all components
- No hardcoded strings in codebase
- `npm run lint` passes
- `npm run build` succeeds
- `dotnet build` succeeds
- All tests pass

---

## Deployment Checklist

Before marking Phase complete, verify:

- [ ] `docs/standards/` fully populated with 7 documents
- [ ] `docs/messages/` contains catalog with 50+ codes
- [ ] Branch naming pattern used for this feature: `feature/005-standards-and-i18n`
- [ ] Commits follow Portuguese semantic format
- [ ] All required artifacts created (research.md, data-model.md, etc.)
- [ ] No implementation code written (Phase 1 documentation only)
- [ ] Team has reviewed and approved documentation
- [ ] Code builds without errors (no changes required)
- [ ] Tests pass (no new tests in Phase 1)

---

## Next Steps

After Phase 1 validation passes:
1. Request task generation for Phase 2
2. Begin backend infrastructure implementation
3. Continue phases sequentially with validation after each

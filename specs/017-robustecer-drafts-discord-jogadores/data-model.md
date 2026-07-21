# Data Model: Robustecer Drafts, Discord e Jogadores

## Draft

Represents a rinha/draft lifecycle.

**Relevant fields**:
- Identifier
- Name
- Status
- Presence closing time
- Team size
- Team/reserve counts
- Discord integration metadata
- Version/state update marker

**State transitions**:
- Presence open → presence closed
- Presence closed → captains defined
- Captains defined → open for draft
- Open for draft → finalized
- Any non-finalized state → cancelled

## Presence

Represents one player intent/confirmation in a draft.

**Relevant fields**:
- Identifier
- Draft identifier
- User identifier
- Player identifier
- Discord user identifier when applicable
- Origin: site, Discord or manual
- Status: confirmed or cancelled
- Confirmation order
- Final order when applicable

**Validation rules**:
- A draft cannot have more than one confirmed presence for the same player.
- Presence changes are allowed only while presence is open.
- Manual presence can only include eligible players.

## Discord Publication

Represents an operational publication attempt/result for a draft.

**Relevant fields**:
- Identifier
- Draft identifier
- Publication type: presence list, presence CTA, final teams
- Guild identifier
- Channel identifier
- Message identifier
- Status: pending, published, failed, skipped
- Last error code/message for operators
- Published at / last attempted at

**Validation rules**:
- Each draft should have at most one active publication record per publication type.
- Republishing updates or supersedes the previous publication state.
- Failed publication must not block manual site flow.

## Eligible Player Result

Represents player options shown to administrators for manual presence.

**Relevant fields**:
- Player identifier
- Display name
- Discord name/link status summary
- Player status
- Eligibility reason when not selectable

**Validation rules**:
- Search results for adding manual presence include active players with user profile and not already confirmed.
- Ineligible players are either excluded or clearly marked depending on UI mode.

## Administrative Draft Action

Represents sensitive manual operation performed by an administrator.

**Relevant fields**:
- Identifier
- Draft identifier
- Action type
- Responsible user identifier
- Target player identifier when applicable
- Reason when applicable
- Created at

**Validation rules**:
- Responsible user is required.
- Reason is required for cancellation when UI prompts for it; backend accepts localized validation if mandatory for an action.

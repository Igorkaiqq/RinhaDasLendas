# Research: Cadastro de Time

## Decision: Time and membership modeled as domain entities

**Rationale**: The feature has business invariants that must be enforced independently from API, database and frontend: active team uniqueness by name/tag, no duplicate players in the same team, maximum five main members, captain must belong to the team and inactive players cannot be added to active teams.

**Alternatives considered**: Storing team composition as JSON was rejected because database guidelines prefer relational modeling and explicit foreign keys for business data.

## Decision: Use logical status instead of physical deletion

**Rationale**: The spec requires inactive teams to remain consultable for historical and audit usage. A `TimeStatus` enum with active/inactive states matches the existing player status pattern and future draft/match history needs.

**Alternatives considered**: Physical delete was rejected because future matches and drafts may reference teams. A boolean `ativo` was considered, but an enum is clearer for future states.

## Decision: Enforce active team uniqueness by normalized name and tag

**Rationale**: FR-007 only blocks duplicates among active teams. Using normalized fields for uniqueness makes comparisons predictable and allows inactive historical records with reused names/tags if needed.

**Alternatives considered**: Global uniqueness across active and inactive teams was rejected because it unnecessarily blocks historical reuse.

## Decision: Team composition is limited to five main members in the MVP

**Rationale**: The constitution and spec define standard matches with up to five players per team. The domain should reject more than five main members and the frontend should prevent the attempt before submission.

**Alternatives considered**: Allowing unlimited members with a separate main-roster flag was deferred because the current feature describes a standard team composition, not club management or reserve rosters.

## Decision: API exposes REST endpoints under `/api/v1/times`

**Rationale**: This follows API standards: noun routes, DTOs, proper status codes, pagination for list endpoints, conflict responses for duplicate active name/tag, and validation responses for invalid composition.

**Alternatives considered**: RPC-style endpoints such as `/createTime` were rejected by API standards.

## Decision: Commands and queries remain separated

**Rationale**: The existing backend already uses commands, queries and handlers for players. Team creation, update, inactivation and reactivation are commands; list/detail lookups are queries.

**Alternatives considered**: A single application service was rejected because it would diverge from the documented CQRS guidance.

## Decision: Frontend uses the existing layout and design system

**Rationale**: The design system identifies cards as the primary presentation element and uses modals mainly for confirmation. The Teams page should mirror the productivity-focused Players experience while adding team-specific cards, status badges, member chips and a compact form.

**Alternatives considered**: A custom visual language for teams was rejected because the project already has documented design tokens and UI guidelines.

## Decision: No external integrations in this phase

**Rationale**: Team registration is a manual internal workflow and must work without Discord or Riot API. Future integrations can consume active teams later.

**Alternatives considered**: Importing teams from Discord roles or Riot data was rejected as premature for the MVP.

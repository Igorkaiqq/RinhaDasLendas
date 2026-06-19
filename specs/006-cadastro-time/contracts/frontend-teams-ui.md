# Frontend UI Contract: Times

## Route

- Path: `/times`
- Navigation label key: `navigation.teams`
- Main view: `FrontEnd/src/views/TeamsView.vue`

## Page Responsibilities

- Show empty state when no teams exist.
- Show paginated/listed team cards with name, tag, status, captain, quantity and members.
- Allow search by team name, tag or member.
- Allow status filter between all, active and inactive.
- Open creation/edit form for organizer workflows.
- Ask confirmation before inactivation.
- Allow reactivation from inactive team actions when backend accepts it.

## Components

| Component | Responsibility |
|-----------|----------------|
| TeamList | Render list and list states |
| TeamCard | Show team summary and actions |
| TeamFormModal | Capture name, tag, notes, members and captain |
| TeamStatusBadge | Render active/inactive status consistently |
| TeamMemberPicker | Select active players returned by players service/API |
| TeamDeleteDialog | Confirm inactivation, not physical deletion |

## Interaction Rules

- Creation form requires name and tag before submit.
- Member picker should prevent selecting the same player twice.
- Member picker should prevent selecting more than five players.
- Captain selector should only show selected members.
- If the captain is removed from members, clear the captain selection.
- Backend remains source of truth for all validation and conflict checks.

## i18n Keys

Required key groups:

```text
teams.title
teams.subtitle
teams.actions.create
teams.actions.edit
teams.actions.inactivate
teams.actions.reactivate
teams.filters.searchPlaceholder
teams.filters.status
teams.empty.title
teams.empty.description
teams.form.name
teams.form.tag
teams.form.notes
teams.form.members
teams.form.captain
teams.status.active
teams.status.inactive
teams.messages.created
teams.messages.updated
teams.messages.inactivated
teams.messages.reactivated
teams.validation.nameRequired
teams.validation.tagRequired
teams.validation.membersRequired
teams.validation.maxMembers
```

## Design Rules

- Use existing dark-first layout and cards as the primary information surface.
- Use existing spacing, colors, typography and badges from design docs.
- Do not invent new design tokens.
- Keep mobile usable with stacked filters and cards.

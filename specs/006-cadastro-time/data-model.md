# Data Model: Cadastro de Time

## Entity: Time

Represents a reusable team in RinhaDasLendas.

### Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| id | UUID | Yes | Stable primary key |
| nome | string | Yes | Display name shown to users |
| nome_normalizado | string | Yes | Used for active uniqueness/search |
| tag | string | Yes | Short identifier shown in lists/cards |
| tag_normalizada | string | Yes | Used for active uniqueness/search |
| observacoes | string | No | Optional internal notes |
| status | TimeStatus | Yes | Active or inactive |
| capitao_id | UUID | No | Must reference a current team member when present |
| data_cadastro | datetime | Yes | Audit field |
| data_atualizacao | datetime | Yes | Audit field |

### Relationships

- One Time has many TimeMembro records.
- A Time captain references one Jogador through `capitao_id` and must also exist in the team's members.

### Validation Rules

- nome is required and cannot be blank.
- tag is required and cannot be blank.
- Active teams cannot duplicate another active team's normalized nome.
- Active teams cannot duplicate another active team's normalized tag.
- Active teams can only include active players.
- A team can have at most five main members.
- A player cannot appear more than once in the same team.
- Captain must be one of the linked members.
- When a captain is removed from composition, captain must be cleared or replaced.

### State Transitions

```text
Active -> Inactive: allowed through inactivation command
Inactive -> Active: allowed when required data and composition remain valid
```

## Entity: TimeMembro

Represents a player's membership in a team.

### Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| id | UUID | Yes | Stable primary key |
| time_id | UUID | Yes | FK to Time |
| jogador_id | UUID | Yes | FK to Jogador |
| principal | boolean | Yes | True for standard team composition |
| data_cadastro | datetime | Yes | Audit field |

### Relationships

- Many TimeMembro records belong to one Time.
- Many TimeMembro records reference one Jogador across different teams.

### Validation Rules

- `(time_id, jogador_id)` must be unique.
- jogador_id must reference an existing player.
- For active teams, referenced players must be active.
- The number of principal members in a team must not exceed five.

## Entity: Jogador

Existing player entity used as a dependency.

### Feature-Specific Rules

- Only active players can be linked to active teams.
- Player display data is returned through team DTOs for list/detail views.
- Player deletion/inactivation should not physically remove historical team references.

## Enum: TimeStatus

| Value | Meaning |
|-------|---------|
| Ativo | Team can be used in main future flows |
| Inativo | Team remains consultable but should not be offered in new primary flows |

## Database Notes

- Tables: `times`, `time_membros`.
- Use UUID primary keys.
- Use explicit FKs from `time_membros.time_id` to `times.id` and from `time_membros.jogador_id` to `jogadores.id`.
- Use snake_case column names.
- Add indexes for `status`, `nome_normalizado`, `tag_normalizada`, `time_id`, `jogador_id`.
- Prefer filtered/partial unique indexes for active normalized name/tag if supported by the EF/PostgreSQL mapping.

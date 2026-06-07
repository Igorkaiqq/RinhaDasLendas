# Data Model: Cadastro de Jogadores e Preferências de Rotas

**Feature**: Cadastro de Jogadores e Preferências de Rotas
**Created**: 2026-06-06

## Entities

### Jogador
- `Id` (UUID / GUID) - PK
- `NomeExibicao` (string, required)
- `NomeReal` (string, optional)
- `Discord` (string, optional)
- `RiotId` (string, optional)
- `OpGgUrl` (string, optional)
- `DeepLolUrl` (string, optional)
- `Elo` (string, optional)
- `Status` (enum: Ativo, Inativo) - default Ativo
- `DataCadastro` (datetime)
- `DataAtualizacao` (datetime)

Indexes:
- PK on `Id`

DB mapping notes:
- Table name MUST be `jogadores` (snake_case) and columns MUST use snake_case
  per Database Guidelines (e.g. `nome_exibicao`, `data_cadastro`). Use EF Core
  Fluent API to map property names to snake_case column names and configure
  UUID PK generation.

### PreferenciaRota
- `Id` (UUID) - PK
- `JogadorId` (UUID) - FK -> Jogador(Id)
- `Rota` (enum: Top, Jungle, Mid, Adc, Support)
- `Prioridade` (int) - 1..5
- `NaoJogoNemLascando` (bool) - default false

Constraints & Indexes:
- Unique index on (`jogador_id`, `rota`) at DB level
- Unique index on (`jogador_id`, `prioridade`) at DB level
- FK constraint `preferencias_rotas.jogador_id` -> `jogadores.id`

DB mapping notes:
- Table name MUST be `preferencias_rotas` and columns snake_case. Add a
  CHECK constraint for `prioridade` between 1 and 5 and consider a partial
  unique index or an application-level check to ensure at most one
  `nao_jogo_nem_lascando = true` per jogador.

## Validation Rules

- `NomeExibicao` is required and must be between 1 and 100 characters.
- Exactly five `PreferenciaRota` rows MUST exist per jogador in normal flows
  (creation endpoint should accept all five; UI enforces it).
- `Prioridade` values MUST be unique per jogador and in range 1..5.
- At most one `PreferenciaRota` per jogador MAY have `NaoJogoNemLascando = true`.
- OP.GG and Deeplol URLs MUST match basic URL pattern and domain when provided.

Notes on constraints placement:
- Important invariants MUST be enforced at the domain and application layers.
- Where possible, also enforce constraints at the DB level (unique indexes,
  FK constraints, and CHECK constraints) to protect data integrity.

## Sample SQL Migration Sketch

- Create table `jogadores` with columns above.
- Create table `preferencias_rotas` with FK to jogadores and unique indexes
  for (`JogadorId`, `Rota`) and (`JogadorId`, `Prioridade`).


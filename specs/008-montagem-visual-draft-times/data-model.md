# Data Model: Montagem Visual de Draft e Times

## DraftMontagem

Representa uma sessao visual de montagem de times.

**Fields**:

- `Id`: UUID.
- `Nome`: nome exibido da montagem.
- `Observacoes`: texto opcional.
- `Status`: `Aberta`, `Finalizada`, `Cancelada`.
- `TamanhoEquipe`: quantidade desejada por time.
- `QuantidadeTimes`: quantidade calculada de times completos.
- `QuantidadeReservas`: quantidade calculada de reservas.
- `CriterioCapitaes`: `Manual` ou `Sorteio`.
- `MotivoCancelamento`: texto opcional.
- `DataCadastro`: data de criacao.
- `DataAtualizacao`: data de ultima alteracao.

**Relationships**:

- Possui muitos `DraftMontagemTime`.
- Possui muitos `DraftMontagemParticipante`.

**Validation**:

- Nome obrigatorio, maximo 120 caracteres.
- TamanhoEquipe entre 1 e 5 no MVP.
- QuantidadeTimes deve ser pelo menos 1.
- Status finalizada bloqueia mudancas de layout.

## DraftMontagemTime

Representa um time dinamico dentro da montagem.

**Fields**:

- `Id`: UUID.
- `DraftMontagemId`: UUID da montagem.
- `Nome`: nome editavel do time.
- `Ordem`: posicao visual.
- `Cor`: chave visual ou cor definida pela aplicacao.
- `CapitaoId`: jogador capitao do time.

**Relationships**:

- Pertence a `DraftMontagem`.
- Possui participantes associados por `TimeId`.

**Validation**:

- Uma montagem deve ter exatamente `QuantidadeTimes` times.
- Cada time deve ter exatamente um capitao para finalizar.
- Capitao deve ser participante da montagem e membro do proprio time.

## DraftMontagemParticipante

Representa um jogador cadastrado dentro da montagem.

**Fields**:

- `Id`: UUID.
- `DraftMontagemId`: UUID da montagem.
- `JogadorId`: UUID do jogador cadastrado.
- `TimeId`: UUID opcional do time.
- `Estado`: `Livre`, `Reserva`, `Time`.
- `Capitao`: booleano.
- `RotaContextual`: `Top`, `Jungle`, `Mid`, `Adc`, `Support` ou nulo.
- `Ordem`: posicao visual dentro da area/time.
- `DataCadastro`: data de entrada na montagem.
- `DataAtualizacao`: ultima alteracao.

**Relationships**:

- Pertence a `DraftMontagem`.
- Referencia `Jogador`.
- Opcionalmente pertence a `DraftMontagemTime`.

**Validation**:

- Um jogador aparece no maximo uma vez por montagem.
- Se `Estado = Time`, `TimeId` e obrigatorio.
- Se `Estado = Livre` ou `Reserva`, `TimeId` deve ser nulo.
- Time nao pode exceder `TamanhoEquipe`.
- RotaContextual deve ser rota valida quando informada.

## State Transitions

### DraftMontagem

- `Aberta` → `Finalizada`: permitido quando times validos, capitaes definidos e duplicidades inexistentes.
- `Aberta` → `Cancelada`: permitido com motivo opcional.
- `Finalizada` → edicao: fora do MVP, requer fluxo explicito futuro.

### DraftMontagemParticipante

- `Livre` → `Time`: jogador movido para time com vaga.
- `Time` → `Livre`: jogador removido do time.
- `Reserva` → `Time`: reserva promovido para time com vaga.
- `Time` → `Reserva`: jogador enviado para reserva.
- `Livre` ↔ `Reserva`: permitido para organizacao manual.

## Derived Calculations

- `QuantidadeTimes = floor(totalParticipantes / TamanhoEquipe)`.
- `QuantidadeReservas = totalParticipantes % TamanhoEquipe`.
- `CapitaesObrigatorios = QuantidadeTimes`.

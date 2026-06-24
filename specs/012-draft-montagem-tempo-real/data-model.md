# Data Model: Draft em Tempo Real na Montagem de Times

## DraftMontagem

Montagem visual existente usada para formar times.

**Novos campos relevantes**:

- `Modo`: Manual ou TempoReal.
- `TurnoAtualTimeId`: time que possui a vez atual, quando houver turno ativo.
- `TurnoAtualCapitaoId`: jogador capitão autorizado a escolher no turno atual.
- `TurnoSequencia`: número sequencial do turno atual.
- `TurnoIniciadoEm`: início oficial do turno.
- `TurnoExpiraEm`: expiração oficial do turno.
- `DuracaoTurnoSegundos`: duração configurada para turnos, padrão 30.
- `VersaoEstado`: versão incremental para detectar concorrência.

**Relacionamentos**:

- Possui muitos `DraftMontagemTime`.
- Possui muitos `DraftMontagemParticipante`.
- Possui muitas `DraftMontagemEscolha`.
- Possui muitas `DraftMontagemSubstituicao`.

**Validações**:

- Draft em tempo real só inicia se status for Aberta.
- Deve haver capitão em cada time elegível.
- Capitães devem ser jogadores ativos vinculados a usuários ativos e com papel elegível.
- Turno ativo só existe enquanto a montagem estiver aberta e em modo TempoReal.
- Modo manual não pode aceitar picks em tempo real.

## DraftMontagemTime

Time existente dentro da montagem.

**Campos relevantes**:

- `Id`: UUID.
- `DraftMontagemId`: UUID.
- `Nome`: nome exibido.
- `Ordem`: ordem usada para alternar turnos.
- `Cor`: cor visual definida pela montagem.
- `CapitaoId`: jogador capitão do time.

**Validações**:

- Time completo não recebe turno.
- O capitão deve ser participante do próprio time.
- O time não pode exceder `TamanhoEquipe`.

## DraftMontagemParticipante

Participante existente da montagem.

**Campos relevantes**:

- `JogadorId`: UUID.
- `Estado`: Livre, Time ou Reserva.
- `TimeId`: time associado quando estado for Time.
- `Capitao`: indica capitão.
- `Ordem`: ordem dentro da lista/time/reservas.
- `RotaContextual`: rota destacada no contexto da montagem.

**Validações**:

- Participante Reserva não pode ser escolhido por capitão.
- Participante Livre pode ser escolhido se estiver elegível e houver vaga no time do capitão da vez.
- Participante Time não pode ser escolhido novamente.
- Capitão já começa associado ao seu time.

## DraftMontagemEscolha

Registro auditável de escolhas e timeouts do draft em tempo real.

**Campos**:

- `Id`: UUID.
- `DraftMontagemId`: UUID.
- `Sequencia`: sequência do turno/registro.
- `TimeId`: time que possuía a vez.
- `CapitaoId`: jogador capitão da vez.
- `JogadorId`: jogador escolhido, nulo em timeout.
- `Tipo`: Escolha ou Timeout.
- `RegistradoEm`: data/hora oficial.

**Relacionamentos**:

- Pertence a `DraftMontagem`.
- Relaciona time, capitão e, quando aplicável, jogador escolhido.

**Validações**:

- `Sequencia` é única por montagem.
- `JogadorId` é obrigatório quando tipo for Escolha.
- `JogadorId` deve ser nulo quando tipo for Timeout.

## DraftMontagemSubstituicao

Registro de entrada emergencial de reserva no lugar de jogador que saiu ou não compareceu.

**Campos**:

- `Id`: UUID.
- `DraftMontagemId`: UUID.
- `TimeId`: UUID.
- `JogadorSaiuId`: jogador removido do time.
- `ReservaEntrouId`: jogador reserva que entra no time.
- `Motivo`: justificativa opcional limitada.
- `ResponsavelUsuarioId`: usuário que executou a substituição.
- `RegistradoEm`: data/hora oficial.

**Validações**:

- Apenas reservas podem entrar por substituição.
- Jogador que sai precisa estar no time informado.
- Substituição não pode exceder tamanho máximo do time.
- Não pode usar reserva já consumido por outra substituição ativa.

## State Transitions

### Manual Aberta → TempoReal Aberta

1. Organizador inicia draft em tempo real.
2. Sistema valida capitães, participantes e reservas.
3. Sistema cria o primeiro turno para o primeiro time elegível.
4. Estado oficial é publicado para usuários conectados.

### Turno Ativo → Escolha Registrada

1. Capitão da vez escolhe jogador livre antes da expiração.
2. Sistema valida capitão, turno, jogador livre e vaga.
3. Participante muda de Livre para Time.
4. Escolha é registrada.
5. Próximo turno é calculado ou a montagem é concluída.

### Turno Ativo → Timeout Registrado

1. Tempo oficial chega ao fim sem escolha válida.
2. Sistema registra timeout.
3. Próximo turno é calculado ignorando times completos.
4. Estado atualizado é publicado.

### Reserva → Time por Substituição Emergencial

1. Organizador seleciona time, jogador que saiu e reserva que entra.
2. Sistema valida reserva e vaga de substituição.
3. Jogador que saiu deixa o time ou perde posição ativa conforme regra implementada.
4. Reserva passa a integrar o time.
5. Substituição é registrada e publicada.

## Indexes and Constraints

- Índice por `draft_montagem_id` em escolhas e substituições.
- Índice único por `draft_montagem_id` + `sequencia` em escolhas.
- Foreign keys explícitas para montagem, time, capitão, jogador escolhido, reserva e responsável.
- Colunas em snake_case e UUID como identificadores.
- Migration obrigatória para novos campos e tabelas.

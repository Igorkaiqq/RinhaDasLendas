# Data Model: Discord Bot e Lista de Presença no DraftMontagem

## DraftMontagem

Draft oficial do sistema, agora iniciado por lista de presença.

**Campos novos/alterados**:

- `HorarioEncerramentoPresenca`: data/hora planejada para encerramento da presença.
- `DiscordGuildId`: guild Discord associada quando criado ou publicado via Discord.
- `DiscordPresenceMessageId`: mensagem da lista de presença no Discord.
- `OrdemEscolhaModo`: `Manual` ou `Sorteado` no MVP; `Snake` fica preparado para futuro.
- `PresencaContinuadaManualmente`: indica autorização administrativa para continuar com menos de 10 jogadores.

**Status oficial**:

- `PresencaAberta`
- `PresencaEncerrada`
- `CapitaesDefinidos`
- `OrdemDefinida`
- `Aberta`
- `Finalizada`
- `Cancelada`

**Regras**:

- Criação não exige capitães.
- Confirmações só são aceitas em `PresencaAberta`.
- Encerramento automático não avança se houver menos de 10 confirmados.
- Capitães só podem ser definidos após `PresencaEncerrada`.
- Quantidade de capitães deve ser igual à quantidade de times.
- Participantes da montagem são derivados das presenças confirmadas.

## DraftMontagemPresenca

Registro de presença de um jogador em um DraftMontagem.

**Campos**:

- `Id`: UUID.
- `DraftMontagemId`: UUID.
- `UsuarioId`: UUID.
- `JogadorId`: UUID.
- `DiscordUserId`: string opcional.
- `OrigemConfirmacao`: `Web` ou `Discord`.
- `Status`: `Confirmada` ou `Cancelada`.
- `ConfirmadoEm`: data/hora.
- `CanceladoEm`: data/hora opcional.
- `OrdemConfirmacao`: inteiro sequencial.
- `OrdemManual`: inteiro opcional.
- `OrdemFinal`: inteiro opcional.
- `DataCadastro`: data/hora.
- `DataAtualizacao`: data/hora.

**Índices e constraints**:

- Índice por `draft_montagem_id`.
- Índice por `usuario_id`.
- Índice por `jogador_id`.
- Único filtrado para presença ativa por `draft_montagem_id + usuario_id`.
- Único filtrado para presença ativa por `draft_montagem_id + jogador_id`.

## DiscordServerConfiguration

Configuração dos canais Discord por guild.

**Campos**:

- `Id`: UUID.
- `GuildId`: string obrigatória.
- `PresenceChannelId`: string obrigatória.
- `NewsChannelId`: string obrigatória.
- `AdminChannelId`: string obrigatória.
- `DraftChannelId`: string obrigatória.
- `MatchResultChannelId`: string obrigatória.
- `BotEnabled`: boolean.
- `CreatedAt`: data/hora.
- `UpdatedAt`: data/hora.

**Regras**:

- Apenas Admin e SuperAdmin alteram configuração.
- IDs de canais devem ter formato numérico/string válido.
- Permissão real de publicação é validada pelo bot ao tentar publicar.

## ExternalAccount

Vínculo genérico já existente usado para localizar conta interna a partir do Discord User Id.

**Regras adicionais**:

- Discord Bot consulta vínculo por `Provider = Discord` e `ProviderUserId = discordUserId` ativo.
- Vínculo legado pode ser considerado somente como fallback de leitura enquanto houver dados antigos.

## DraftMontagemTime

Time dentro da montagem.

**Alteração de fluxo**:

- Times podem ser criados após encerramento de presença, usando quantidade calculada ou autorizada manualmente.
- Capitão é definido após encerramento da presença.

## DraftMontagemParticipante

Participante gerado a partir das presenças confirmadas.

**Regras**:

- Jogadores confirmados podem virar capitães, livres ou reservas.
- Reservas são excedentes após cálculo da quantidade de times.
- Capitães iniciam em seus respectivos times.

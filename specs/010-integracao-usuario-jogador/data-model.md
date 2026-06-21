# Data Model: Integração Usuário-Jogador

## Usuario

Conta autenticada já existente.

**Fields relevant to this feature**:

- `Id`: UUID.
- `Nome`: nome exibido da conta.
- `Email`: e-mail único.
- `Ativo`: usuário pode autenticar e usar a plataforma.
- `Roles`: inclui `Jogador` por padrão no cadastro público.
- `JogadorId`: valor derivado quando existe jogador vinculado.

**Relationships**:

- Pode possuir zero ou um `Jogador` vinculado.

**Validation**:

- Usuário pode existir sem jogador.
- Usuário desativado não deve ser considerado participante válido em fluxos dependentes de conta ativa.

## Jogador

Perfil de jogo usado em listagem, draft e preferências.

**Fields relevant to this feature**:

- `Id`: UUID.
- `UsuarioId`: UUID opcional, único quando preenchido.
- `NomeExibicao`: obrigatório.
- `Discord`: obrigatório no fluxo self-service.
- `RiotId`: obrigatório no fluxo self-service.
- `OpGgUrl`: obrigatório no fluxo self-service.
- `DeepLolUrl`: obrigatório no fluxo self-service.
- `Elo`: obrigatório no fluxo self-service.
- `Divisao`: obrigatório no fluxo self-service.
- `Status`: ativo após conclusão do perfil.
- `DataCadastro`: criação do perfil.
- `DataAtualizacao`: última alteração.

**Relationships**:

- Pode pertencer a um `Usuario`.
- Possui exatamente cinco `PreferenciaRota`.
- Continua relacionado a times, drafts e montagens existentes conforme modelo atual.

**Validation**:

- `UsuarioId` não pode ser associado a mais de um jogador.
- O self-service só pode criar jogador para o próprio usuário autenticado.
- Jogador criado por self-service inicia ativo.
- Jogadores legados podem continuar sem `UsuarioId`.

## PreferenciaRota

Preferência ordenada do jogador para as cinco rotas.

**Fields relevant to this feature**:

- `Rota`: Top, Jungle, Mid, Adc ou Support.
- `Prioridade`: número de 1 a 5.
- `NaoJogoNemLascando`: booleano.

**Relationships**:

- Pertence a um `Jogador`.

**Validation**:

- Deve existir exatamente uma preferência para cada uma das cinco rotas.
- Prioridades devem ser únicas e estar entre 1 e 5.
- No máximo uma rota pode ser marcada como “não jogo nem lascando”.

## Perfil de Jogador Pendente

Estado derivado, não necessariamente persistido em tabela própria.

**Definition**:

- Usuário autenticado ativo com role `Jogador` e sem jogador vinculado.

**Behavior**:

- Deve visualizar CTA ou tela para completar perfil.
- Não aparece como jogador em listagem/draft.
- Pode se tornar jogador ativo ao concluir o perfil obrigatório.

## State Transitions

### Usuario Sem Jogador → Usuario Com Jogador

1. Usuário cadastra conta.
2. Usuário autentica.
3. Sistema detecta ausência de jogador vinculado.
4. Usuário completa perfil obrigatório.
5. Sistema cria jogador ativo com `UsuarioId` do usuário.
6. Perfil autenticado passa a retornar `JogadorId`.

### Jogador Ativo Vinculado → Jogador Ativo Atualizado

1. Usuário autenticado acessa edição do próprio perfil de jogador.
2. Usuário envia dados válidos.
3. Sistema atualiza dados básicos e preferências.
4. Jogador permanece ativo e vinculado ao mesmo usuário.

### Tentativa Duplicada de Completar Perfil

1. Usuário já possui jogador vinculado.
2. Usuário tenta concluir perfil novamente.
3. Sistema rejeita a criação duplicada e orienta usar edição do perfil existente.

## Indexes and Constraints

- Manter índice único filtrado em `jogadores.usuario_id` quando não nulo.
- Manter constraints de prioridade de rota.
- Manter índices existentes para busca/listagem de jogadores.

## Data Migration

- Não é esperada nova coluna para o vínculo, pois `UsuarioId` já existe.
- Pode ser necessária migration apenas se a implementação identificar ausência de constraint obrigatória no schema atual.
- Nenhum jogador legado deve ser migrado automaticamente para usuário.

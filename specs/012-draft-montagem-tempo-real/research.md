# Research: Draft em Tempo Real na Montagem de Times

## Decisão: Evoluir `DraftMontagem`, não `DraftSessao`

**Rationale**: A rota `/drafts` usa o fluxo visual de `DraftMontagem`, com `DraftVisualBoard`, `DraftVisualSetup` e serviços `draftMontagens`. Implementar tempo real nesse modelo evita criar uma experiência paralela e mantém a tela principal alinhada com o fluxo usado pelo produto.

**Alternatives considered**:

- Evoluir `DraftSessao`: rejeitado porque não é o fluxo renderizado atualmente na rota de drafts.
- Criar terceiro modelo de draft: rejeitado por duplicar conceitos e aumentar complexidade.

## Decisão: Manter modo manual e adicionar modo tempo real

**Rationale**: A montagem visual atual permite drag and drop e ajustes administrativos. A nova feature precisa de capitães escolhendo em turnos. Separar modo evita quebrar comportamento existente e deixa regras críticas claras.

**Alternatives considered**:

- Substituir o modo manual pelo tempo real: rejeitado porque remove funcionalidade existente.
- Permitir drag and drop durante o tempo real: rejeitado porque conflita com ordem oficial e timer.

## Decisão: Backend como fonte oficial do estado e timer

**Rationale**: Regras críticas não podem depender do frontend. O backend deve validar capitão, turno, reserva, elegibilidade, expiração e concorrência. O frontend exibe estado e envia intenções.

**Alternatives considered**:

- Timer no frontend: rejeitado por permitir divergência entre clientes.
- Validação compartilhada no frontend/backend: rejeitada para regras críticas, embora o frontend possa bloquear ações por UX.

## Decisão: SignalR para sincronização de estado

**Rationale**: O requisito exige sincronização em tempo real no stack .NET/Vue. SignalR se integra ao ASP.NET Core e permite grupos por montagem, notificando todos os clientes quando uma escolha, timeout, cancelamento ou substituição ocorrer.

**Alternatives considered**:

- Polling: rejeitado por maior latência e experiência inferior.
- Server-Sent Events: rejeitado por suportar apenas fluxo servidor-cliente e não ser o padrão esperado pelo requisito.

## Decisão: Comandos continuam via HTTP; SignalR faz join e broadcast

**Rationale**: O projeto já usa controllers, MediatR, validators, DTOs e tratamento padronizado de erro. Manter comandos HTTP reaproveita padrões existentes e facilita testes. SignalR fica responsável pela presença no grupo e entrega de eventos.

**Alternatives considered**:

- Executar picks diretamente pelo hub: rejeitado para MVP por duplicar pipeline de validação/erro.

## Decisão: Serviço de timer em background para timeouts

**Rationale**: Um serviço em background consegue avançar turnos expirados usando o estado persistido e publicar atualização para os clientes. É suficiente para uma primeira versão interna em uma instância.

**Alternatives considered**:

- Timers em memória por draft: rejeitado por maior risco de perda em restart e divergência.
- Scheduler distribuído: rejeitado no MVP por complexidade desnecessária.

## Decisão: Reservas não entram no pool de escolha

**Rationale**: Reserva é complemento emergencial para substituir participante confirmado ausente ou removido. Capitães escolhem apenas jogadores livres. Reservas ficam visíveis em área separada e só podem entrar por ação administrativa de substituição.

**Alternatives considered**:

- Permitir capitão escolher reserva: rejeitado porque contradiz a regra do produto.
- Transformar sobras em reserva somente no final: rejeitado porque o produto já separa reservas na montagem.

## Decisão: Registrar escolhas, timeouts e substituições em entidades próprias

**Rationale**: Histórico auditável é necessário para ordem de escolha, turnos expirados e substituições emergenciais. Entidades relacionais respeitam o padrão do projeto e evitam JSON para regras de negócio.

**Alternatives considered**:

- Reconstituir histórico a partir dos participantes: rejeitado porque perde timeouts e contexto de sequência.
- Guardar log JSON: rejeitado pelas diretrizes de banco.

## Decisão: Concorrência otimista com versão de estado

**Rationale**: Pick e timeout podem disputar o mesmo turno. Uma versão de estado na montagem permite detectar alteração concorrente e recarregar/recusar a operação sem duplicar escolhas.

**Alternatives considered**:

- Lock distribuído: rejeitado no MVP.
- Ignorar concorrência: rejeitado por risco de dois jogadores no mesmo turno.

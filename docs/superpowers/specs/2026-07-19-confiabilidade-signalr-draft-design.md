# Design: Confiabilidade do Draft em Tempo Real

## Contexto

Jogadores relatam que a vez do draft demora para aparecer ou não atualiza até recarregar a página. A investigação identificou falhas determinísticas no fluxo atual, além de oportunidades de reduzir carga:

- algumas transições persistem estado sem emitir atualização SignalR;
- o frontend busca o estado antes de entrar no grupo, criando uma janela de perda de eventos;
- snapshots não carregam versão e podem ser aplicados fora de ordem;
- a reconexão automática pode se encerrar sem fallback;
- o relógio ignora o horário fornecido pelo servidor;
- a substituição do capitão atual não atualiza `TurnoAtualCapitaoId`;
- o timer consulta agregados completos a cada segundo;
- o ambiente de produção registra repetidamente as consultas do timer.

Produção usa uma única réplica do backend. Redis backplane não resolve as falhas atuais e fica fora deste escopo.

## Objetivo

Garantir que todos os participantes visualizem o estado e a vez corretos sem recarregar a página, mesmo durante reconexões, eventos simultâneos ou perda temporária do WebSocket.

## Critérios de Sucesso

- Mudanças de turno aparecem para clientes conectados em até 2 segundos em condições normais.
- Uma conexão interrompida recupera o estado correto em até 5 segundos após disponibilidade do backend.
- Nenhum snapshot com versão anterior substitui estado mais novo no frontend.
- Nenhuma transição relevante do draft deixa clientes conectados sem notificação.
- Pick concorrente com timeout converge para um estado canônico e força sincronização do cliente que perdeu a concorrência.
- Substituir o capitão da vez transfere imediatamente a permissão de pick para o novo capitão.
- O timer não carrega o agregado completo quando apenas precisa localizar drafts expirados.
- O fluxo continua funcional sem Redis e com uma única réplica.

## Fora de Escopo

- Redis backplane ou aumento de réplicas.
- Event sourcing.
- Outbox transacional genérico para toda a aplicação.
- Redesenho visual da Mesa de Draft.
- Alteração das regras de pick, duração ou ordem do draft.
- Eventos incrementais por entidade; o snapshot completo permanece como contrato nesta etapa.

## Arquitetura

### Snapshot Canônico Versionado

`DraftMontagemResponseDto` expõe `VersaoEstado`. A versão já existe no domínio e no mapeamento EF como token de concorrência. Ela passa a integrar o contrato HTTP e SignalR.

O backend continua enviando snapshots completos para simplificar reconciliação. Todo snapshot inclui:

- identificador do draft;
- `VersaoEstado` monotônica;
- `DataAtualizacao`;
- estado completo da montagem;
- `ServerNow` no envelope realtime.

O frontend mantém a maior versão aplicada para o draft selecionado. Um snapshot só é aceito quando:

- pertence ao draft atualmente selecionado; e
- sua versão é maior que a versão aplicada, ou é a primeira carga canônica.

Respostas HTTP e eventos SignalR passam pelo mesmo aplicador versionado. Isso impede que uma resposta antiga substitua um evento recente.

### Conexão sem Janela de Perda

A abertura de um draft segue esta ordem:

1. criar e iniciar a conexão;
2. entrar no grupo do draft;
3. buscar `GET /realtime-state`;
4. aplicar o snapshot se sua versão for atual;
5. iniciar o monitor de saúde.

Se um evento chegar entre o Join e o GET, a comparação de versão preserva o estado mais novo. A conexão deve estar pronta para receber eventos antes da reconciliação HTTP.

### Reconexão e Fallback

A conexão usa política explícita de retry com atrasos limitados, por exemplo `0`, `2`, `5`, `10` e `15` segundos. Os callbacks têm responsabilidades distintas:

- `onreconnecting`: marca estado degradado e inicia fallback HTTP;
- `onreconnected`: entra novamente no grupo, busca snapshot canônico e interrompe fallback;
- `onclose`: mantém tentativa controlada de restabelecimento enquanto a tela estiver montada e mantém fallback.

O fallback consulta `GET /realtime-state` a cada 5 segundos somente enquanto SignalR não estiver conectado. Não existe polling permanente durante uma conexão saudável.

Uma geração de conexão identifica cada draft aberto. Callbacks, respostas HTTP e timers de uma geração antiga são descartados quando o usuário troca de draft ou sai da tela.

### Status de Conexão

O frontend representa os estados:

- `connected`;
- `reconnecting`;
- `fallback`;
- `disconnected`.

A Mesa de Draft exibe uma indicação localizada e discreta quando não está conectada normalmente. O estado conectado não adiciona ruído visual. O texto informa que a sincronização está sendo recuperada e não atribui erro ao usuário.

### Horário do Servidor

Ao receber `ServerNow`, o frontend calcula um deslocamento entre servidor e navegador. A contagem regressiva usa `Date.now() + offset`, e não o relógio local bruto.

Cada reconciliação atualiza o offset. Isso reduz divergências quando o dispositivo do usuário está adiantado ou atrasado.

## Cobertura de Eventos Backend

Toda mutação que altera informação exibida no draft deve publicar o snapshot após persistência:

- confirmar ou cancelar presença;
- adicionar ou remover presença manual;
- encerrar presença manual ou automaticamente;
- definir ou sortear capitães;
- definir ordem de escolha;
- iniciar tempo real;
- registrar pick;
- avançar por timeout;
- substituir reserva;
- salvar layout;
- finalizar ou cancelar draft;
- registrar publicação, falha ou republicação Discord.

Handlers reutilizam `IDraftMontagemRealtimeNotifier`. O envio ocorre depois de `SaveChangesAsync` e da recarga do estado canônico.

Falha de SignalR não desfaz a transação já persistida. Ela é registrada em log/métrica, e clientes se recuperam pelo fallback ou próxima reconciliação.

## Concorrência

`VersaoEstado` continua como token de concorrência. `DbUpdateConcurrencyException` em mutações de realtime recebe tratamento explícito:

- retorna erro estruturado de conflito;
- não expõe detalhes técnicos;
- orienta o frontend a buscar imediatamente o estado canônico;
- não produz segundo evento para a operação rejeitada.

O frontend, ao receber conflito em pick ou timeout acionado por ação do usuário, busca `/realtime-state` antes de exibir o estado atualizado.

## Substituição do Capitão Atual

Ao substituir um capitão cujo time está no turno atual:

- o novo capitão passa a ser `TurnoAtualCapitaoId`;
- o capitão removido perde permissão imediatamente;
- o turno, sequência e expiração são preservados;
- o snapshot atualizado é emitido ao grupo.

A autorização de pick continua validada pelo domínio/backend.

## Otimização do Timer

O timer deixa de buscar até 25 agregados completos por segundo. O repositório retorna uma projeção mínima com:

- `DraftMontagemId`;
- `TurnoExpiraEm`;
- instante de início relevante para duração máxima.

O handler carrega o agregado completo apenas para IDs realmente expirados que serão processados. Índices existentes sobre status/modo/expiração devem ser verificados; uma migration só será criada se o plano de consulta demonstrar necessidade.

O intervalo permanece em 1 segundo para cumprir a meta de atualização de turno em até 2 segundos.

## Observabilidade

Adicionar métricas e logs estruturados sem dados sensíveis:

- conexão entrou/saiu do grupo;
- evento emitido e versão;
- duração entre persistência e emissão;
- reconexão e ativação do fallback;
- conflito de versão;
- timeout processado;
- falha de notificação.

Em produção, logs de comandos SQL rotineiros do EF Core devem ficar em nível `Warning`. Logs de domínio, falhas e métricas permanecem disponíveis.

## Segurança

- O Hub permanece autenticado.
- `JoinDraftMontagem` valida que o draft existe e que o usuário pode visualizá-lo antes de entrar no grupo.
- IDs de grupos e versões não expõem tokens ou dados adicionais.
- Tokens não devem aparecer em logs de conexão ou URLs registradas pela aplicação.
- O fallback usa o mesmo endpoint autenticado e as mesmas regras de autorização.

## Internacionalização

Novos estados de conexão, mensagens de conflito e orientações de recuperação usam:

- `pt.json` e `en.json` no frontend;
- resources português e inglês no backend.

Não haverá texto visível hardcoded em Hub, handlers, middlewares, serviços, componentes ou view.

## Testes

### Domínio

- substituição do capitão atual transfere `TurnoAtualCapitaoId`;
- capitão removido não pode escolher;
- novo capitão pode escolher mantendo o turno.

### Aplicação e Backend

- cada handler de mutação relevante chama o notifier com a versão persistida;
- encerramento automático também notifica;
- conflito otimista retorna resposta estruturada;
- query do timer usa projeção mínima;
- Hub rejeita Join de draft inexistente ou inacessível;
- snapshot inclui versão monotônica e `ServerNow`.

### Frontend

- conexão faz Join antes da reconciliação HTTP;
- evento recebido entre Join e GET não é sobrescrito;
- snapshot antigo é ignorado;
- troca rápida de draft descarta callbacks antigos;
- `onreconnecting` inicia fallback;
- `onreconnected` refaz Join, sincroniza e para fallback;
- `onclose` tenta restabelecer sem duplicar timers;
- fallback consulta a cada 5 segundos apenas desconectado;
- offset usa `ServerNow` no relógio;
- status de conexão usa i18n.

### Integração

- dois clientes no mesmo grupo recebem a nova vez após pick;
- timeout atualiza os dois clientes sem reload;
- perda e retorno da conexão convergem para a versão do backend;
- pick concorrente com timeout termina em um único estado válido.

### Performance

- medir tamanho do snapshot e tempo de emissão;
- verificar que o timer não carrega coleções do agregado na busca de expirados;
- validar atualização normal em até 2 segundos;
- validar recuperação em até 5 segundos.

## Implantação

- Não exige Redis nem novo serviço.
- Contratos são aditivos: clientes antigos ignoram `VersaoEstado`.
- Backend deve ser implantado antes ou junto do frontend versionado.
- Métricas devem ser observadas durante uma rinha real antes de considerar escalonamento horizontal.
- Se o backend for escalado para mais de uma réplica no futuro, Redis backplane ou serviço equivalente se torna obrigatório.

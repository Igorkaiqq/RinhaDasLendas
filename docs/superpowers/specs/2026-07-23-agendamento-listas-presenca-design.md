# Agendamento Recorrente De Listas De Presença

**Data:** 2026-07-23
**Feature:** `020-agendamento-listas-presenca`
**Branch:** `feature/020-agendamento-listas-presenca`

## Objetivo

Permitir que usuários com permissão de gerenciamento de drafts configurem, em `/configuracoes`, agendas semanais que criam drafts com presença aberta e publicam automaticamente suas listas no Discord. A solução deve sobreviver a reinícios, múltiplas réplicas, indisponibilidade temporária e falhas de publicação sem criar drafts ou mensagens duplicadas.

## Público E Autorização

- Moderador, Admin e SuperAdmin podem listar, criar, editar, pausar, reativar e excluir agendas.
- A autorização usa `CanManageDrafts`, que já representa Moderador+ no backend e frontend.
- A configuração sensível de guild e canais continua usando `CanManageUsers`, restrita a Admin e SuperAdmin.
- Moderadores não recebem acesso para alterar guild, canais, token ou ativação global do bot.
- Todos os endpoints de agenda exigem JWT e `CanManageDrafts`.
- Entidades de domínio nunca são retornadas diretamente pela API.

## Decisões Aprovadas

- O backend é a fonte de verdade do agendamento.
- O bot permanece adaptador de publicação e não recebe regra de recorrência.
- Cada ocorrência cria um novo `DraftMontagem` e uma publicação de presença pendente.
- O fuso é sempre `America/Sao_Paulo`.
- Publicação e encerramento acontecem no mesmo dia local.
- O encerramento deve ser posterior à publicação.
- A agenda repete até ser pausada ou excluída.
- Nome e observação são configuráveis; demais parâmetros usam os padrões atuais do bot.
- O draft terá times de cinco jogadores e nome no formato `Nome configurado - dd/MM/yyyy`.
- Edições, pausas e exclusões afetam somente ocorrências ainda não criadas.
- Se o sistema recuperar antes do encerramento, a ocorrência atrasada ainda será criada.
- Depois do encerramento, a ocorrência é registrada como perdida e nenhum draft é criado.

## Arquitetura

### Backend

O backend concentra persistência, autorização, cálculo temporal, deduplicação, auditoria e criação transacional dos drafts.

Unidades principais:

- agregado `AgendamentoPresenca` no Domain;
- entidade `AgendamentoPresencaDiaSemana` para os dias selecionados;
- entidade `OcorrenciaAgendamentoPresenca` para cada data esperada;
- entidade `HistoricoAgendamentoPresenca` para auditoria administrativa;
- commands e queries separados no Application;
- FluentValidation para entrada;
- repositório PostgreSQL para operações atômicas;
- `BackgroundService` na API apenas para disparar o caso de uso periódico;
- endpoints REST finos protegidos por `CanManageDrafts`.

### Bot Discord

O bot não consulta agendas e não calcula horários. Ele continua:

1. listando drafts operacionalmente acionáveis;
2. adquirindo claim persistido por publicação;
3. publicando a presença no canal configurado;
4. concluindo, registrando falha ou deixando resultado incerto para reconciliação.

Um draft criado por agenda é indistinguível de outro draft para o protocolo de publicação existente.

### Frontend

O frontend oferece a central de automações em `/configuracoes`, consome os endpoints e mantém somente estado de interação. Regras de autorização, recorrência e deduplicação permanecem no backend.

## Modelo De Domínio

### AgendamentoPresenca

Campos:

- `Id`: UUID;
- `Nome`: obrigatório, normalizado, entre 3 e 100 caracteres;
- `Observacao`: opcional, até 500 caracteres;
- `HorarioPublicacaoLocal`: hora e minuto;
- `HorarioEncerramentoLocal`: hora e minuto;
- `Status`: `Ativo`, `Pausado` ou `Arquivado`;
- `AtivadoEm`: instante da ativação mais recente;
- `PausadoEm`: instante opcional;
- `ArquivadoEm`: instante opcional;
- `UltimaDataAvaliada`: última data local já analisada pelo scheduler;
- `CriadoPorUsuarioId`: UUID;
- `CriadoEm`, `AtualizadoEm`: instantes UTC;
- coleção de dias da semana;
- coleção de ocorrências;
- coleção de registros de histórico.

Invariantes:

- deve possuir ao menos um dia;
- dias não podem se repetir;
- horários têm precisão de minutos;
- encerramento deve ser posterior à publicação;
- arquivado não pode ser reativado ou editado;
- pausar uma agenda já pausada é idempotente;
- reativar uma agenda ativa é idempotente;
- alterações não modificam ocorrências já criadas.

### AgendamentoPresencaDiaSemana

Cada dia é persistido como linha relacional:

- `AgendamentoPresencaId`;
- `DiaSemana`: enum ISO de segunda a domingo.

Constraint única em `agendamento_presenca_id + dia_semana`.

### OcorrenciaAgendamentoPresenca

Campos:

- `Id`: UUID;
- `AgendamentoPresencaId`: FK;
- `DataLocal`: data em `America/Sao_Paulo`;
- `PublicacaoPrevistaEm`: instante UTC;
- `EncerramentoPrevistoEm`: instante UTC;
- `Status`: `Processando`, `Bloqueada`, `Criada`, `Perdida` ou `Falha`;
- `DraftMontagemId`: FK opcional;
- `CodigoFalha`: código público opcional, sem detalhe técnico;
- `UltimaTentativaEm`: instante opcional;
- `CriadaEm`, `AtualizadaEm`: instantes UTC.

Constraint única em `agendamento_presenca_id + data_local` garante uma ocorrência por agenda e data.

Transições:

- inexistente para `Processando` quando a agenda vence e a infraestrutura está configurada;
- inexistente ou `Bloqueada` para `Bloqueada` quando bot/configuração impedem execução;
- `Bloqueada` para `Processando` se a configuração voltar antes do encerramento;
- `Processando` para `Criada` junto da criação transacional do draft;
- inexistente ou `Bloqueada` para `Perdida` após o encerramento;
- `Processando` para `Falha` apenas para erro conhecido terminal;
- falha transitória de infraestrutura não confirma transição e será tentada novamente.

### HistoricoAgendamentoPresenca

Registra:

- `Id`;
- `AgendamentoPresencaId`;
- ação `Criado`, `Editado`, `Pausado`, `Reativado` ou `Arquivado`;
- `ResponsavelUsuarioId`;
- `RegistradoEm`;
- resumo estrutural dos campos alterados, sem dados sensíveis.

O histórico é administrativo e não é exposto a usuários sem `CanManageDrafts`.

## Persistência

Novas tabelas em snake_case:

- `agendamentos_presenca`;
- `agendamentos_presenca_dias_semana`;
- `ocorrencias_agendamentos_presenca`;
- `historicos_agendamentos_presenca`.

Requisitos:

- UUIDs como chaves;
- FKs explícitas;
- índices por status e horários de execução;
- índices por agenda e data local;
- delete restrito para relações auditáveis;
- exclusão da agenda implementada como arquivamento lógico;
- migration EF Core obrigatória.

## Processamento Periódico

### Serviço

`AgendamentoPresencaExecutionService` executa em intervalo padrão de 30 segundos. O serviço de host:

- cria escopo de DI;
- obtém o relógio;
- envia um command MediatR;
- registra logs estruturados e métricas;
- não contém regra de recorrência ou acesso direto ao EF.

### Cálculo Temporal

- dias e horários são interpretados com o timezone IANA `America/Sao_Paulo`;
- instantes calculados são persistidos em UTC;
- o relógio é injetável para testes determinísticos;
- não se usa deslocamento fixo `UTC-3` como regra de domínio;
- a implementação usa `TimeZoneInfo` com `America/Sao_Paulo`;
- um horário local inválido ou ambíguo não é ajustado silenciosamente: a ocorrência fica `Falha` com código localizado e nenhum draft é criado.

### Execução Exatamente Uma Vez

Para cada agenda e data local devida:

1. o caso de uso confirma que a agenda está ativa e que `AtivadoEm` não é posterior ao horário previsto, impedindo recuperação indevida após reativação tardia;
2. o repositório tenta criar/adquirir atomicamente a ocorrência;
3. a constraint única e lock transacional impedem dois processadores vencedores;
4. configuração Discord e `botEnabled` são avaliados;
5. dentro da janela, o caso de uso cria o draft e vincula a ocorrência na mesma transação;
6. a publicação `Presenca` fica pendente para o protocolo atual do bot;
7. o commit torna ocorrência, draft e publicação visíveis juntos.

Se houver crash antes do commit, nada é confirmado e uma nova execução pode tentar novamente. Se houver commit, a ocorrência única impede novo draft.

## Recuperação E Indisponibilidade

### Bot Desativado Ou Configuração Incompleta

- nenhum draft é criado;
- a ocorrência fica `Bloqueada`;
- o scheduler reavalia até o encerramento;
- se a configuração voltar dentro da janela, cria o draft atrasado;
- se a janela terminar, marca `Perdida`.

### API Reiniciada Ou Indisponível

- ao reiniciar, agendas que permaneceram ativas são recalculadas;
- o scheduler percorre as datas posteriores a `UltimaDataAvaliada` até a data local atual, sem horizonte arbitrário;
- datas selecionadas que já encerraram são registradas como `Perdida`, inclusive após indisponibilidade de vários dias;
- `UltimaDataAvaliada` avança somente depois que todas as ocorrências esperadas daquela data forem confirmadas ou classificadas;
- ocorrências do dia ainda dentro da janela são criadas;
- ocorrências vencidas são registradas como `Perdida`;
- reativar uma agenda depois do horário de publicação não recupera a ocorrência daquele dia;
- recuperação atrasada vale apenas quando a agenda permaneceu ativa no horário previsto.

### Falha De Publicação No Discord

Depois da criação do draft, falhas de envio seguem o fluxo existente:

- `Falha` conhecida permite republicação administrativa;
- resultado incerto vira `RequerReconciliacao`;
- a mensagem principal e a CTA mantêm estados independentes;
- a agenda nunca cria outro draft para compensar falha de envio.

## API REST

Base: `/api/v1/discord/agendamentos-presenca`.

Endpoints:

- `GET /`: lista agendas não arquivadas, próxima execução e última ocorrência;
- `POST /`: cria agenda ativa;
- `GET /{id}`: retorna detalhe administrativo;
- `PUT /{id}`: edita nome, observação, dias e horários futuros;
- `POST /{id}/pausar`: pausa idempotentemente;
- `POST /{id}/reativar`: reativa idempotentemente;
- `DELETE /{id}`: arquiva logicamente;
- `GET /{id}/ocorrencias`: lista histórico paginado de ocorrências.

Contratos nunca retornam IDs de mensagem, claims, tokens ou falhas técnicas do Discord. Motivos visíveis usam `messageCode` e recursos localizados.

Status HTTP:

- `200`: consulta, edição, pausa ou reativação concluída;
- `201`: agenda criada;
- `204`: arquivamento concluído;
- `400`: payload ou regra temporal inválida;
- `401`: não autenticado;
- `403`: sem `CanManageDrafts`;
- `404`: agenda inexistente ou arquivada;
- `409`: conflito de concorrência conhecido;
- respostas de erro seguem o contrato padrão da API.

## Interface Em `/configuracoes`

### Visibilidade

- a seção `Listas de presença` aparece para `CanManageDrafts`;
- a seção atual de configuração sensível Discord permanece separada e exige `CanManageUsers`;
- um Moderador vê agendas, mas não os IDs de guild/canais.

### Central De Automações

Layout aprovado:

- eyebrow `Automações`;
- título `Listas de presença`;
- descrição curta do comportamento;
- botão `Novo agendamento`;
- resumo de agendas ativas, próxima execução e fuso Brasília;
- cards em ordem de próxima execução;
- card mostra nome, observação, dias, intervalo, status, próxima execução e resultado recente;
- ações `Editar`, `Pausar`, `Reativar` e `Excluir` conforme estado;
- estado vazio com ação para criar a primeira agenda.

### Modal De Criação/Edição

Campos:

- nome;
- observação opcional;
- chips de segunda a domingo;
- horário de publicação;
- horário de encerramento;
- resumo `Horário de Brasília · Times de 5 · Repete até ser pausado`.

Comportamento:

- validação localizada em linha;
- fechamento por botão, `Escape` e ação cancelar;
- foco preso e restaurado ao gatilho;
- envio bloqueado durante processamento;
- edição usa o mesmo formulário;
- confirmação contextual para pausar e excluir;
- excluir explica que drafts já criados não serão alterados.

### Responsividade

- desktop usa cards com resumo e ações alinhadas;
- tablet reduz colunas de resumo;
- mobile usa uma coluna, chips roláveis/quebráveis e ações de largura confortável;
- sem tabela horizontal obrigatória;
- alvos de toque seguem o mínimo do design system;
- sem overflow a partir de 320px.

## Internacionalização

- todo texto frontend usa `pt.json` e `en.json`;
- mensagens backend usam recursos `.resx` em português e inglês;
- códigos novos entram no catálogo e constantes de mensagens;
- bot só recebe texto novo se o protocolo existente exigir feedback adicional;
- nomes, observações e dados inseridos pelo usuário não são traduzidos;
- datas e dias da semana são formatados pelo locale ativo;
- português deve ter acentuação revisada.

## Observabilidade

Métricas/logs estruturados sem dados pessoais:

- agendas avaliadas;
- ocorrências criadas;
- ocorrências bloqueadas;
- ocorrências perdidas;
- falhas terminais por código;
- duração do ciclo;
- conflito de aquisição de ocorrência.

Logs não incluem observação, nome personalizado, token, payload Discord, IDs de mensagem ou motivo administrativo livre.

## Testes

### Domain

- ao menos um dia;
- dias únicos;
- encerramento posterior à publicação;
- precisão de minuto;
- pausa/reativação idempotentes;
- arquivado imutável;
- transições de ocorrência válidas e inválidas;
- edição não altera ocorrência criada.

### Application

- validators FluentValidation;
- commands e queries;
- autoria e histórico;
- cálculo de próxima execução;
- recuperação dentro e fora da janela;
- agenda reativada depois do horário não recupera ocorrência;
- bot/configuração indisponível bloqueia sem criar draft;
- criação transacional de ocorrência, draft e publicação.

### Infrastructure E Integração

- migration e modelo EF;
- constraint única por agenda/data;
- duas execuções concorrentes geram uma ocorrência e um draft;
- rollback não deixa ocorrência parcial;
- endpoints 401, 403, 400, 404 e sucesso;
- projeções não expõem dados operacionais;
- timezone Brasília convertido corretamente para UTC.

### Background Service

- relógio determinístico;
- ciclo de 30 segundos sem sobreposição local;
- falha em uma agenda não interrompe as demais;
- logs e métricas sem dados sensíveis;
- cancelamento do host respeitado.

### Bot

- draft agendado entra no polling existente;
- claim continua impedindo mensagem duplicada;
- CTA mantém recuperação independente;
- nenhuma regra de agenda é adicionada ao bot.

### Frontend

- seção visível para Moderador+ e oculta para usuário comum;
- Admin/SuperAdmin mantêm acesso às duas seções;
- listagem, estado vazio e próxima execução;
- criação, edição, pausa, reativação e exclusão;
- validações localizadas;
- modal por teclado e foco;
- estados bloqueado, perdido e falha;
- paridade PT/EN;
- desktop, tablet, mobile e 320px.

### Browser Real

- login como Moderador;
- acesso a `/configuracoes` sem configuração sensível de canais;
- criação e edição de agenda;
- pausa, reativação e arquivamento;
- resposta 403 para usuário comum;
- execução controlada com relógio/ambiente de teste;
- publicação processada pelo bot em ambiente integrado quando disponível.

## Documentação E Histórico Do Produto

- atualizar `docs/domain/DRAFT_DISCORD_OPERATIONS.md`;
- documentar operação e recuperação do scheduler;
- atualizar exemplos de configuração quando houver novo intervalo configurável;
- adicionar entrada `2026.07.2` ao histórico do sistema;
- descrever visualmente agendamentos, dias/horários e recuperação, sem expor detalhes internos de locks ou segurança;
- atualizar checklist e catálogos conforme padrões existentes.

## Fora De Escopo

- timezone configurável por guild ou agenda;
- calendário mensal;
- período inicial/final da recorrência;
- criação automática de capitães ou jogadores;
- tamanho de equipe configurável;
- múltiplos canais por agenda;
- alteração ou cancelamento de drafts já criados ao editar a agenda;
- geração de cron arbitrário;
- dependência Quartz ou Hangfire;
- notificações adicionais fora da lista e CTA atuais;
- sincronização com calendários externos.

## Critérios De Conclusão

- Moderador+ gerencia agendas em `/configuracoes` sem acessar configuração sensível do Discord.
- Cada combinação agenda/data produz no máximo uma ocorrência, um draft e uma publicação principal.
- Agenda ativa cria draft e publicação dentro da janela no fuso de Brasília.
- Recuperação antes do encerramento publica atrasado; depois dele registra perda sem criar draft.
- Bot desativado ou configuração incompleta não cria draft invisível.
- Pausa, edição e exclusão não alteram drafts existentes.
- Publicações continuam usando claims, falha e reconciliação existentes.
- Todos os endpoints aplicam `CanManageDrafts` e contratos localizados.
- Backend, frontend e bot mantêm catálogos sincronizados nos idiomas suportados.
- Testes de domínio, aplicação, integração, concorrência, frontend e bot aprovam.
- Build, lint, migrations, auditorias i18n e browser desktop/mobile aprovam.
- Histórico de atualizações inclui a entrega `2026.07.2`.

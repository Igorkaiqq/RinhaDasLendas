# Estado atual do RinhaDasLendas

## Resumo executivo

Em 2026-07-27, o RinhaDasLendas possui fluxos reais para autenticação, jogadores, times, drafts, presença e integração operacional com Discord. Partidas e estatísticas ainda não possuem domínio, persistência, endpoints ou telas funcionais. As rotas frontend `/partidas` e `/estatisticas` existem, mas apontam para uma tela genérica de placeholder.

A feature 018 descreve uma possível importação pós-partida via coletor local e LCU, porém permanece com status `Draft`, não integra este worktree e não deve ser interpretada como comportamento entregue. Consulte [Fontes de dados](DATA_SOURCES.md) para viabilidade e [Conformidade](RIOT_LCU_COMPLIANCE.md) para restrições.

## Backend

### O que existe

- Solução .NET 10 separada em API, Application, Domain, Infrastructure e Tests.
- ASP.NET Core Web API, MediatR, FluentValidation, Entity Framework Core e PostgreSQL.
- Autenticação, autorização por papéis e permissões, recursos localizados e respostas por DTOs.
- Entidades e persistência para jogadores, preferências de rota, times, membros de times, drafts, montagens de draft, presenças, escolhas, substituições, publicações Discord, ações administrativas e agendamentos de presença.
- Repositórios dedicados para jogadores, times, drafts e montagens de draft.
- SignalR para atualização de montagem de draft e serviços de execução em segundo plano para presença, tempo de turno e reconciliação de publicação.
- Permissão `CanManageMatches` já aparece na configuração de autorização e protege operações de times, mas sua existência não representa um módulo de partidas implementado.

### O que não existe

- Não há `DbSet`, entidade, repositório, command, query ou controller de partida no contexto e nas camadas examinadas.
- Não há persistência de times de uma partida, participantes, placar, objetivos, itens, runas, eventos ou frames de timeline.
- Não há importação por Riot API, Match V5, Tournament API, LCU, Live Client Data, ROFL ou OCR.
- Não há cálculo de estatísticas agregadas por jogador, time, campeão, rota, temporada ou draft.
- Não há contrato aprovado de arquivo de importação. A extensão e o schema imaginados na feature 018 são requisitos em rascunho, não interface pública.

## Frontend

### O que existe

- Aplicação Vue 3 com TypeScript, Composition API, Vue Router, serviços HTTP e internacionalização.
- Telas funcionais de jogadores, times, drafts, perfil, usuários, configurações e atualizações do sistema.
- A rota `/jogadores` usa `PlayersView`; `/times` usa `TeamsView`; `/drafts` usa `DraftsView`.
- Rotas `/partidas` e `/estatisticas` estão registradas, exigem autenticação e possuem títulos localizados.

### Lacuna atual

- `/partidas` e `/estatisticas` renderizam `PlaceholderView`.
- Não existem serviços, tipos ou componentes de produção para upload, prévia, confirmação, listagem ou detalhe de partidas.
- Não existe painel de estatísticas por jogador ou time.
- O frontend, portanto, não consome nem apresenta nenhuma das métricas da [matriz de disponibilidade](FIELD_AVAILABILITY_MATRIX.md).

## Discord

### O que existe

- Bot em Node.js/TypeScript com `discord.js`.
- Comandos e interações para criar e listar drafts e operar presença.
- Publicações de presença, chamada de presença, times definidos e cancelamento.
- Claims persistidos, prevenção de duplicidade, estados de falha/reconciliação e polling operacional.
- Configuração de canais, inclusive canal de resultado de partida, e proteção por token interno e permissões administrativas.

### O que não existe

- O canal configurável de resultado não equivale a ingestão ou persistência de resultado.
- O bot não recebe callback da Tournament API, não consulta Match V5 e não lê LCU.
- O bot não publica placar detalhado, estatísticas individuais ou agregações históricas.
- O bot não deve receber senha do `lockfile`, arquivo bruto do cliente ou token Riot.

## Jogadores

### Estado funcional

- Cadastro com nome de exibição, dados opcionais, Discord, Riot ID, links externos, elo, divisão e status.
- Cinco preferências de rota com prioridades e no máximo uma rota bloqueada.
- Inativação lógica, reativação, consulta e vínculo opcional com usuário.
- O Riot ID atual pode servir como pista de associação futura, mas não há regra aprovada de normalização, unicidade global ou resolução de renome.

### Limites para estatísticas

- Nenhuma partida está vinculada a jogador.
- Elo cadastrado é informação do cadastro; não é métrica calculada e não deve ser confundido com desempenho interno.
- Preferência de rota é intenção do jogador; não prova a rota efetivamente jogada.
- Um vínculo futuro deve preservar o snapshot da partida e permitir participante externo ou não vinculado, conforme o rascunho 018, sem reescrever o fato histórico.

## Times

### Estado funcional

- Cadastro de time com nome, tag, status, observações, capitão opcional e membros.
- Validações de composição, duplicidade, capitão pertencente ao time e uso de jogadores ativos.
- Inativação e reativação sem exclusão do histórico cadastral.

### Limites para estatísticas

- Time cadastrado é uma composição reutilizável, não necessariamente o lado que disputou uma partida.
- Não existe snapshot de time por partida nem vínculo entre resultado e time cadastrado.
- Alterar membros de um time atual não pode alterar retroativamente uma futura composição histórica.

## Drafts e montagens

### Estado funcional

- Sessões de draft e montagens visuais com presença, titulares, reservas, capitães, ordem de escolha, picks, times e estados operacionais.
- Fluxo realtime, temporização de turno, substituições, cancelamento, arquivamento, restauração, auditoria e integração Discord.
- Publicações Discord de presença, chamada, times definidos e cancelamento.
- Interface testada em diferentes estados e tamanhos de tela segundo o relatório da feature 021.

### Limites para partidas

- Draft e montagem não são partida concluída.
- Não existe vínculo persistido entre um draft e um identificador de partida Riot.
- Composição planejada pode divergir da composição efetiva por substituição, erro de associação ou partida criada fora do Rinha.
- Qualquer vínculo futuro deve comparar participantes e lados e deixar divergências explícitas; o rascunho 018 não define ainda a equivalência final.

## Partidas e estatísticas

| Capacidade | Estado em 2026-07-27 |
|---|---|
| Cadastro manual de partida | Não implementado, embora exigido pela Constituição como fallback futuro. |
| Importação oficial Riot | Não implementada. |
| Importação local LCU | Não implementada. |
| Listagem e detalhe de partidas | Placeholder frontend; sem backend. |
| Estatísticas finais por participante | Não persistidas. |
| Timeline e eventos | Não persistidos. |
| Estatísticas agregadas | Não calculadas. |
| Consentimento de custom match | Não modelado. |
| Origem e confiabilidade por campo | Não modeladas. |
| Distinção entre zero e ausente | Não modelada para partidas. |

## Situação da feature 018

A feature `018-importacao-partidas-lcu` foi encontrada no workspace principal com estas características:

- Status declarado: `Draft`.
- Objetivo proposto: coletor local Windows, arquivo sanitizado e versionado, upload autenticado, prévia humana, confirmação e persistência relacional.
- Fonte local proposta: endpoint de histórico do LCU não oficialmente suportado.
- Fallback obrigatório proposto: entrada manual.
- Questões ainda abertas: permissões, expiração da prévia, assinatura ou hash, modos aceitos, regra de vínculo de Riot ID, equivalência com draft, correções manuais, retenção e visibilidade das estatísticas.
- Ausência neste worktree: não há diretório `specs/018-importacao-partidas-lcu`, código, plano, tarefas, contratos nem migrations integrados.

Conclusão: a feature 018 é insumo de pesquisa e especificação. Nenhum nome de campo, formato de arquivo, endpoint ou entidade citado nela deve ser tratado como contrato vigente.

## Dependências para evolução

Antes de implementar estatísticas, o projeto precisa aprovar, no fluxo SDD:

1. Política de privacidade e opt-in para custom matches.
2. Fonte inicial e fallback manual.
3. Definição de partida confirmada e sua relação com draft, jogador e time.
4. Representação de origem, ausência, zero e confiabilidade por campo.
5. Lista mínima de métricas oficiais e métricas experimentais excluídas.
6. Idempotência, auditoria e correção de vínculos.
7. Registro do produto e revisão das políticas Riot aplicáveis.

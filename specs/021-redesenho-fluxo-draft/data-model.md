# Data Model: Redesenho do Fluxo de Draft

## Escopo de dados

Nenhuma entidade persistida, migração ou contrato backend será alterado. A feature reorganiza a apresentação dos modelos existentes e adiciona uma entrada editorial estática.

## Draft selecionado

Representa o contexto operacional já retornado ao frontend.

Campos consumidos pela nova apresentação:

- `id`, `nome`, `status` e data relevante;
- `quantidadeTimes`, `quantidadeReservas` e `tamanhoEquipe`;
- `presencas`, `times`, `livres` e `reservas`;
- `turnoAtualTimeId`, `turnoAtualCapitaoId`, `turnoSequencia` e tempos do turno;
- `escolhas`, `substituicoes` e publicações Discord;
- motivo de cancelamento quando disponível na projeção administrativa.

Validações de apresentação:

- status conhecido usa rótulo e variante semântica;
- status desconhecido usa fallback neutro;
- data ausente usa texto localizado;
- nomes longos quebram em até duas linhas nas listas compactas e livremente no cabeçalho;
- arrays recebidos não são mutados para ordenar a apresentação.

## Participante da presença

Dados existentes:

- identidade da presença e do jogador;
- nome de exibição;
- origem e status da confirmação;
- ordem e horários de confirmação;
- informação administrativa adicional quando autorizada.

Estados visuais derivados:

- confirmado;
- selecionado como capitão;
- ação de remoção disponível;
- ação indisponível durante salvamento ou fora da etapa.

As permissões e a validade da remoção continuam determinadas pelo fluxo existente.

## Etapa do draft

Modelo derivado e não persistido:

| Status do draft | Etapa atual | Etapas anteriores | Estado terminal |
|-----------------|-------------|-------------------|-----------------|
| `PresencaAberta` | Presença aberta | Nenhuma | Não |
| `PresencaEncerrada` | Presença encerrada | Presença aberta | Não |
| `CapitaesDefinidos` | Capitães | Presença aberta, presença encerrada | Não |
| `OrdemDefinida` | Ordem | Presença aberta, presença encerrada, capitães | Não |
| `Aberta` | Escolhas | Presença aberta, presença encerrada, capitães e ordem | Não |
| `Finalizada` | Finalização | Presença aberta, presença encerrada, capitães, ordem e escolhas | Sim |
| `Cancelada` | Cancelamento | Somente histórico conhecido | Sim |
| Outro | Estado indisponível | Nenhuma inferida | Neutro |

Cada etapa derivada contém:

- identificador estável;
- rótulo localizado;
- estado `done`, `active`, `pending`, `attention`, `terminal` ou `unknown`;
- indicação programática da etapa atual somente quando aplicável.

A integração Discord é um indicador paralelo acrescentado após a sequência, sem receber `aria-current`. Em cancelamento, nenhuma etapa operacional fica ativa. Em status desconhecido, somente o estado neutro localizado é apresentado.

## Publicação Discord

Dados existentes:

- tipo `Presenca`, `ChamadaPresenca` ou `TimesDefinidos`;
- status conhecido ou ausente.

Apresentação derivada:

- rótulo localizado por tipo e status;
- variante neutra, informativa, de sucesso, atenção ou erro;
- republicação disponível conforme permissão e regras atuais.

## Atualizações editoriais em duas etapas

### Estágio 1: correção `2026.07.3`

Entrada estática:

- `id`: `presence-schedule-weekday-selection-fix`;
- `version`: `2026.07.3`;
- `publishedAt`: `2026-07-25`;
- `featured`: `true`;
- `categories`: `fix`;
- `areas`: `drafts`;
- detalhe `selected-weekday-feedback` com link para Configurações;
- título, resumo e detalhe equivalentes em português e inglês.

Invariantes:

- ID, versão e detalhe são únicos;
- registro permanece em ordem cronológica decrescente;
- existe exatamente uma release destacada;
- `2026.07.2` permanece imutável, exceto por `featured: false`.

### Estágio final: redesenho `2026.07.4`

Entrada estática criada somente após a aprovação de SC-001 a SC-010 e do gate de FR-027:

- `id`: `clearer-draft-operation`;
- `version`: `2026.07.4`;
- `publishedAt`: `2026-07-26`;
- `featured`: `true`;
- `categories`: `improvement`;
- `areas`: `drafts`;
- detalhes `operational-hierarchy`, `presence-roster`, `stage-accessibility-clarity` e `responsive-mobile-operation`, todos com link para `AppRoutes.Draft`;
- título, resumo e quatro detalhes equivalentes em português e inglês.

Invariantes finais:

- `2026.07.4` ocupa o topo e é a única release destacada;
- `2026.07.3` mantém ID, versão, data, categoria, área, detalhe, link e conteúdo localizados, alterando somente `featured` para `false`;
- `2026.07.2` e todas as releases anteriores permanecem imutáveis;
- IDs, versões e detalhes permanecem únicos;
- o registro permanece em ordem cronológica decrescente e, para datas iguais com versões válidas `AAAA.MM.N`, em ordem numérica decrescente de versão.

# Estratégia de testes do domínio competitivo

## Objetivo

Garantir que regras sazonais, dados oficiais, autorização, importação, votação, estatísticas e rating evoluam sem contaminação por amistosos, duplicidade ou regressão histórica.

## Princípios

- Testar regra no nível mais baixo que ofereça confiança suficiente.
- Escrever teste junto da feature e antes da implementação da regra correspondente.
- Usar relógio, geradores de UUID e provedores externos controláveis.
- Não depender de ordem entre testes nem de banco compartilhado.
- Manter fixtures pequenas, legíveis, versionadas e sem dados pessoais reais.
- Comparar invariantes e contratos; evitar testes acoplados a detalhes internos sem valor de negócio.
- Toda correção de bug recebe teste de regressão.

## Pirâmide e responsabilidades

### Domínio

Ferramentas: xUnit e FluentAssertions.

Cobertura obrigatória:

- ciclo de vida de temporada e competição;
- conflito de vigência de elenco;
- captura histórica de escalação;
- estados de partida, série e consolidação;
- discriminadores de Série limitados a `DiariaTemporaria`, `ConfrontoOficial` e `Amistoso`, rejeitando `Evento`;
- Evento como contexto separado que agrupa Séries;
- toda Série oficial exigindo `competicaoId` e `rodadaId` da mesma Season;
- Série diária exigindo `competicaoId`, `rodadaId`, data local, capitães, origem `DraftMontagem` e lados temporários;
- Partidas mínimas, placar, resultado derivado e picks confirmados sem hovers na lista Fearless;
- distinção `Oficial` e `Amistoso`;
- Fearless aceito em Série direta `DiariaTemporaria`, `ConfrontoOficial` ou `Amistoso`, desde que não pertença a Evento;
- Evento de quatro times e todas as Séries internas recusando Fearless e usando draft padrão;
- capitão titular, cinco titulares por rota, até três reservas, máximo de oito e vínculo ativo único;
- janelas configuradas e qualificação global histórica somente após duas Séries diárias oficiais confirmadas, sem consumo por solicitação;
- uma votação por série, eleitor participante, candidatos por lado, autovoto, turnos, invalidação e elegibilidade;
- lançamentos imutáveis e compensação de rating;
- lançamento-base de Rating por partida e ajuste de série separado;
- idempotência por estado e versão do agregado.

Os testes de domínio não usam banco, HTTP, EF Core ou API externa.

### Validators

Ferramentas: xUnit, FluentAssertions e FluentValidation TestHelper quando adotado pelo projeto.

Cobertura obrigatória:

- campos obrigatórios, UUID, datas, intervalos e limites de paginação;
- `competicaoId` e `rodadaId` obrigatórios e pertencentes à Season em toda Série oficial;
- formato e cardinalidade de lados e escalações;
- combinações inválidas de competição e Fearless;
- cardinalidade de elenco, capitão titular, rotas, reservas, janela e vínculo único;
- data local, capitães, `draftMontagemId` e lados temporários de série diária;
- turno, categoria, candidato por lado e motivo obrigatório para invalidar voto;
- filtros e ordenações permitidos;
- seleção sazonal: omissão, lista repetível `temporadaIds`, `todas=true` e exclusão mútua;
- `Idempotency-Key` nos comandos que a exigem;
- mensagens por código de recurso, sem texto de usuário hardcoded;
- limites exatos: mínimo, máximo, um abaixo e um acima.

### Application

Ferramentas: xUnit, FluentAssertions e Moq.

Cobertura obrigatória:

- handlers coordenam repositório, autorização contextual, relógio e outbox;
- comandos não publicam efeitos externos antes do commit;
- query oficial fixa exclusão de amistosos no servidor;
- omissão de filtro sazonal resolve a Season atual e consultas multisseason preservam versões;
- ausência de Season ativa retorna coleção vazia, `calendarioConfigurado=false` e `temporadaAtual=null`, sem fallback para todas;
- rotas `/temporadas/{temporadaId}/...` usam exclusivamente a Season do caminho e rejeitam `temporadaIds` e `todas`;
- listagens top-level `/competicoes` e `/elencos` aplicam corretamente a seleção multiseason;
- detalhe e auditoria por ID não aplicam Season atual como filtro implícito;
- repetição idempotente com chave, ator e conteúdo idênticos devolve o resultado original sem novo efeito;
- mesma chave com conteúdo divergente retorna conflito sem modificar o resultado original;
- apuração abre segundo turno entre empatados, usa média de Score no empate persistente e produz co-vencedores em igualdade exata ou comparação inconclusiva;
- invalidação por Admin+ preserva voto, exige motivo e reconstrói a apuração;
- correção gera compensação e preserva histórico;
- ator é obtido do contexto autenticado, não do request;
- cancelamento interrompe consumidores posteriores;
- falha de dependência não deixa estado parcial.

### Integração

Ferramentas: xUnit, FluentAssertions, PostgreSQL real em ambiente isolado e fábrica da API.

Cobertura obrigatória:

- migrations, FKs, índices, constraints e nomes `snake_case`;
- atomicidade entre agregado, auditoria e outbox;
- concorrência otimista e resposta `409`;
- unicidade de idempotência e inbox sob requisições paralelas;
- paginação com total e ordenação determinística;
- omissão selecionando Season atual, `temporadaIds` repetível selecionando múltiplas Seasons e `todas=true` mutuamente exclusivo com a lista;
- omissão sem Season ativa retornando metadados de calendário e zero itens, sem consultar histórico implicitamente;
- rotas sazonais aninhadas recusando filtros sazonais adicionais e coleções top-level aceitando multiseason;
- respostas sazonais identificando Seasons, `versaoSeason`, `versaoRegras` e versões de cálculo;
- detalhe e auditoria por ID retornando recurso histórico fora da Season atual quando autorizado;
- isolamento de amistosos em todas as projeções oficiais;
- Série `Amistoso` direta com Fearless aplicando bloqueios de picks sem produzir votação, Score, Rating, ranking ou recorde oficial;
- constraint de vínculo oficial único e limites de cinco titulares, três reservas e oito membros;
- contagem idempotente das duas Séries diárias necessárias à qualificação global, preservada após criação, aprovação, rejeição e cancelamento de solicitações;
- qualificação somando Séries válidas de Seasons diferentes e retornando o mesmo resultado global sem filtro sazonal;
- `Serie` agrupando `Partidas`, cada `Partida` como unidade individual pertencente a no máximo uma Série e `PartidaDetalheDto` sem coleção de Partidas;
- uma votação persistida por série, uma escolha por categoria e turno e invalidação auditável por Admin+;
- consulta administrativa de votos restrita a Admin+, com `votoId` opaco, identidade mínima do eleitor e auditoria de acesso sensível;
- reconstrução de projeção a partir de partidas consolidadas;
- credencial Collector limitada à própria origem;
- localização dos envelopes de erro em português e inglês.

### Contratos

Validar OpenAPI e DTOs contra consumidores reais.

Cobertura obrigatória:

- rotas sob `/api/v1`;
- métodos, status codes, campos obrigatórios e nulabilidade;
- `PaginatedResponseDto<T>` com `page`, `pageSize`, `items`, `totalItems` e `totalPages`;
- nenhuma entidade, campo de persistência, segredo ou payload bruto exposto;
- compatibilidade de enumerações e versionamento de evento;
- contrato comum de seleção sazonal e rejeição da combinação `temporadaIds` com `todas=true`;
- ausência de filtro sazonal implícito em detalhe e auditoria por ID;
- `/series/{serieId}/partidas` como coleção das Partidas da Série e `/partidas/{partidaId}` como detalhe da unidade individual;
- criação de Partida somente por `POST /series/{serieId}/partidas`, sem contrato concorrente em `POST /partidas`;
- `PartidaDetalheDto` sem campo `jogos` ou coleção de Partidas;
- `LancamentoRatingDto` exigindo `partidaId` e `serieId=null` para lançamento-base, e `serieId` com `partidaId=null` para ajuste de Série;
- Série discriminada sem `Evento`, Evento em contrato próprio e requisitos exclusivos de `DiariaTemporaria`;
- toda Série oficial com `competicaoId` e `rodadaId` coerentes com a Season;
- votação exclusivamente sob `/series/{serieId}`, com turnos, invalidação e co-vencedores;
- idempotência distinguindo replay idêntico de chave reutilizada com conteúdo divergente;
- exemplos de `401`, `403`, `404`, `409` e validação;
- contratos Collector separados dos contratos humanos.

Mudança incompatível exige nova versão ou migração de consumidor documentada.

### Frontend

Ferramentas: Vitest, Vue Test Utils e happy-dom.

Cobertura obrigatória:

- carregamento, sucesso, parcial, vazio, filtro vazio, proibido, conflito e retry;
- filtros sincronizados com URL;
- paginação e ordenação enviadas corretamente;
- ações visíveis somente com capacidade, sem tratar isso como segurança suficiente;
- alerta explícito de amistoso e exclusão do escopo oficial;
- estados de elegibilidade de voto e rating;
- progresso global `0/2`, `1/2` e `2/2`, sem redução após uso em contratação ou transferência;
- candidatos de MVP e SVP separados por lado, autovoto, segundo turno e co-vencedores;
- controle de invalidação visível somente a Admin+, exigindo motivo e mostrando a nova versão da apuração;
- tabelas convertidas em cards no contrato responsivo;
- gráficos com alternativa textual ou tabular;
- bloqueio de envio duplicado;
- textos longos em português e inglês.

### Internacionalização

- Verificar paridade estrutural de `pt.json` e `en.json`.
- Falhar se componente novo contiver texto visível hardcoded.
- Falhar se validator, handler, endpoint ou middleware novo contiver mensagem de usuário fora de resources.
- Testar títulos, labels, placeholders, tooltips, badges, estados vazios, toasts, confirmações e erros.
- Renderizar ambos os locales e detectar chave técnica, interpolação ausente e pluralização inválida.
- Revisar acentuação do português brasileiro.

### Acessibilidade

Ferramentas: testes de componente e automação em Chromium.

Cobertura obrigatória:

- navegação completa por teclado e ordem de foco lógica;
- foco visível em todos os controles;
- nome, papel e estado acessíveis;
- etapa, seleção e ordenação anunciadas programaticamente;
- contraste conforme tokens oficiais;
- estado não dependente apenas de cor;
- alvos de toque mínimos de 44 por 44 pixels;
- ausência de overflow horizontal em 320, 480, 768, 1024, 1280 e 1440 px;
- preferência por movimento reduzido;
- tabela ou texto equivalente para visualizações gráficas.

### Segurança

- Matriz completa por identidade: anônimo, Jogador, Capitão, Moderador, Admin, VicePresidente, Presidente, SuperAdmin e Collector.
- `CanManageSeasons` permitida a SuperAdmin e Presidente, negada a Admin abaixo e negada ao VicePresidente enquanto a condição fina não for aprovada.
- Testes de wrong-role, wrong-team, wrong-player, wrong-season e wrong-collector.
- Tentativas de impersonation por `userId`, `actorId`, `teamId` e claims no corpo.
- Escopo Collector ausente, trocado, expirado e revogado.
- Rate limit e tamanho máximo de importação.
- Reuso de chave com payload diferente.
- Replay da mesma chave e conteúdo idêntico sem duplicação.
- Invalidação de voto por Moderador, Capitão, Jogador ou Collector recusada.
- Consulta de votos individuais por Moderador, Capitão, Jogador, participante ou Collector recusada sem revelar `votoId` ou eleitor.
- Consulta de moderação por cada papel de Admin+ retornando somente projeção mínima e criando auditoria sensível.
- Endpoint público, resultado e cache compartilhado sem identidade do eleitor ou voto individual.
- Invalidação aceitando `votoId` opaco e recusando identidade do eleitor como localizador.
- Tentativa de voto por não participante, em nome de terceiro ou em candidato do lado incorreto.
- Enumeração de recursos sensíveis por ID.
- Injeção em busca e ordenação, upload malformado e conteúdo não confiável.
- Logs sem token, segredo, voto individual ou payload bruto sensível.
- CSRF, CORS e autenticação conforme o mecanismo vigente.

### Testes baseados em propriedades

Usar geração de dados para invariantes com grande espaço de combinações:

- nenhuma coleção oficial contém partida amistosa;
- soma de vitórias e derrotas é coerente entre os dois lados de partidas válidas;
- transferência não cria sobreposição de vínculo oficial para o mesmo jogador;
- elenco nunca excede cinco titulares, três reservas ou oito membros, e o capitão sempre é titular;
- somente a segunda Série diária oficial, concluída e confirmada leva a qualificação global a `2/2`;
- qualificação em `2/2` permanece após qualquer quantidade de solicitações, vínculos ou transferências;
- ordenar e paginar não duplica nem omite item quando o conjunto é estável;
- seleção sazonal omitida equivale à Season atual e nunca à lista completa;
- reprocessar o mesmo evento não altera o resultado;
- eventos fora de ordem não avançam a versão;
- compensar e reaplicar uma partida retorna ao resultado calculado para o conjunto vigente;
- Fearless nunca é habilitado para cardinalidade diferente de dois lados;
- voto válido pertence a participante da série, categoria e turno válidos; autovoto permanece permitido;
- empate no segundo turno sempre converge para maior média de Score ou co-vencedores.

Seeds devem ser reproduzíveis e impressos na falha.

### Golden fixtures

Manter fixtures versionadas para entradas e saídas de cálculo:

```text
fixtures/competitivo/<versao>/
  partida-oficial-completa/
  partida-amistosa/
  serie-amistosa-com-multiplas-partidas/
  serie-direta-fearless/
  evento-quatro-times-sem-fearless/
  series-internas-de-evento-sem-fearless/
  serie-diaria-origem-draft/
  elenco-cinco-mais-tres/
  qualificacao-global-duas-series-diarias/
  votacao-serie-segundo-turno/
  votacao-serie-co-vencedores/
  moderacao-votos-admin-mais/
  importacao-duplicada/
  idempotencia-conteudo-divergente/
  correcao-com-compensacao/
  rating-partida-e-ajuste-serie/
  score-extremos-e-elos/
  nomes-longos-e-dados-parciais/
```

Cada fixture contém:

- entrada mínima normalizada e anonimizada;
- versão do schema e algoritmo;
- resultado esperado de validação;
- projeção estatística esperada;
- lançamentos esperados quando parâmetros estiverem formalmente definidos;
- explicação humana do caso.

Atualizar golden exige revisão explícita do diff. Nunca regenerar snapshots em massa para fazer teste passar.

## Matriz de cenários críticos

| Cenário | Domain | Validator | Application | Integração | Contrato | Frontend | Segurança |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Amistoso fora de estatística oficial | X |  | X | X | X | X | X |
| Série amistosa com múltiplas Partidas fora de toda projeção oficial | X |  | X | X | X | X | X |
| Série direta com Fearless | X | X | X | X | X | X |  |
| Série `Amistoso` direta com Fearless e sem projeções oficiais | X | X | X | X | X | X | X |
| Evento separado de Série | X | X | X | X | X | X |  |
| Evento de quatro times com todas as Séries internas sem Fearless | X | X | X | X | X | X | X |
| Elenco cinco titulares, três reservas e capitão titular | X | X | X | X | X | X | X |
| Qualificação global por duas Séries diárias | X | X | X | X | X | X | X |
| Importação repetida | X | X | X | X | X | X | X |
| Mesma chave com conteúdo divergente | X | X | X | X | X | X | X |
| Correção e compensação | X |  | X | X | X | X | X |
| Votação única por série e segundo turno | X | X | X | X | X | X | X |
| Autovoto e candidato por lado | X | X | X | X | X | X | X |
| Invalidação Admin+ com motivo | X | X | X | X | X | X | X |
| Consulta administrativa auditada de votos |  |  | X | X | X | X | X |
| Rating por partida e ajuste de série separado | X |  | X | X | X | X |  |
| Collector fora do escopo |  |  | X | X | X |  | X |
| Transferência concorrente | X | X | X | X | X | X | X |

## Testes negativos mínimos por endpoint mutável

Cada endpoint mutável deve provar:

- `401` sem autenticação;
- `403` com identidade sem capacidade;
- negativa por limite de recurso;
- `400` para request inválido;
- `404` para recurso inexistente ou oculto;
- `409` para versão antiga, estado incompatível e chave idempotente conflitante;
- nenhuma alteração no banco, auditoria ou outbox após falha;
- nenhum efeito externo antes do commit;
- mensagem localizada sem detalhe interno.

## Casos numéricos obrigatórios de Score e Rating

### Score

- entrada elegível que normaliza no extremo inferior retorna exatamente `0`, nunca valor negativo;
- entrada elegível que normaliza no extremo superior retorna exatamente `100`, nunca valor superior;
- arredondamento de exibição não altera a precisão usada por desempate ou Rating;
- Score indisponível não é convertido em `0`.

### Limites de elo

Testar cada limite nos dois sentidos e o valor imediatamente anterior:

| Limite | Elo abaixo | Elo no limite |
| --- | --- | --- |
| `100` | Ovo Quebrado II em `99` | Ovo Quebrado I |
| `200` | Ovo Quebrado I em `199` | Pinto de Briga II |
| `300` | Pinto de Briga II em `299` | Pinto de Briga I |
| `400` | Pinto de Briga I em `399` | Galo de Quintal II |
| `500` | Galo de Quintal II em `499` | Galo de Quintal I |
| `600` | Galo de Quintal I em `599` | Galo de Rinha II |
| `700` | Galo de Rinha II em `699` | Galo de Rinha I |
| `800` | Galo de Rinha I em `799` | Galo Lendário II |
| `900` | Galo Lendário II em `899` | Galo Lendário I |

Testes adicionais:

- Rating `0` pertence a Ovo Quebrado II;
- perda que ultrapassaria zero termina em zero e registra limitação pelo piso;
- vitória possui variação nominal inclusiva entre `+10` e `+20`, testando `+10`, `+20` e valores fora da faixa;
- derrota possui variação nominal inclusiva entre `-20` e `-5`, testando `-20`, `-5` e valores fora da faixa;
- com Rating anterior `3` e variação nominal `-5`, a variação aplicada é `-3`, o novo Rating é `0` e o ledger preserva nominal e aplicada para auditoria;
- soma de lançamentos-base e ajustes de uma Série perdida permanece menor ou igual a zero;
- queda de `100` para `99`, e de cada limite seguinte para um ponto abaixo, recalcula automaticamente o elo inferior;
- não existe proteção implícita contra queda;
- lançamento-base por partida e ajuste de série alteram o ledger em entradas separadas e reproduzíveis.

## Gates por feature

- **023:** Seasons, competição e rodada obrigatórias em Série oficial, Série operacional mínima, resultados, picks, Fearless e Evento separado com todas as Séries internas em draft padrão.
- **024:** capitão titular, cinco titulares, três reservas, máximo de oito, vínculo único, janelas e qualificação global histórica por duas Séries diárias não consumíveis.
- **025:** participantes e modelo detalhado de partida, importação, consolidação, isolamento oficial e golden fixtures, sem duplicar a série mínima da 023.
- **026:** votação única por Série, turnos, autovoto, candidatos por lado, consulta administrativa auditada, invalidação por `votoId`, desempate e co-vencedores.
- **027:** Score `0..100`, todos os limites de elo, piso, queda, Rating por partida, ajustes de série separados, compensação e versão do algoritmo.

Uma feature não avança se seu gate possuir teste crítico ausente ou se houver divergência de i18n.

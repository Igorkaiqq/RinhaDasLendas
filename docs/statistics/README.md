# Estatísticas e fontes de dados de partidas

## Objetivo

Este conjunto de documentos registra o que o RinhaDasLendas possui hoje, quais fontes de dados de League of Legends podem alimentar partidas e estatísticas, quais campos foram observados na prática e quais restrições técnicas e de política precisam orientar decisões futuras.

A documentação é descritiva. Ela não aprova a implementação da feature 018, não define um contrato de importação e não substitui as fases de especificação, esclarecimento, plano e tarefas exigidas pelo projeto.

## Hierarquia normativa

A Constituição e a governança geral do projeto continuam soberanas. Dentro de uma feature, a especificação aprovada e os ADRs aceitos prevalecem sobre estes documentos temáticos. Os documentos em `docs/statistics/` explicam o contexto, registram evidências e apresentam propostas; não aprovam decisões por si próprios. Descrições de endpoints, eventos ou outros contratos neste diretório também não redefinem regras de domínio e somente se tornam normativas quando ratificadas pelos artefatos competentes.

## Estado documental

- Data da pesquisa e revisão: **2026-07-27**.
- Base analisada: worktree `feature/023-fundacao-competitiva-sazonal`.
- Feature relacionada: `018-importacao-partidas-lcu`, encontrada como rascunho fora deste worktree e ainda não integrada.
- Evidência prática: leitura local sanitizada da partida personalizada `3266170515` pelo League Client API (LCU).
- Dados deliberadamente omitidos: tokens, senha do `lockfile`, cabeçalhos de autorização, PUUIDs, Riot IDs individuais e outros dados pessoais sem necessidade documental.

## Como ler

### Diagnóstico e fontes

1. [Estado atual](CURRENT_STATE.md): inventário factual de backend, frontend, Discord, jogadores, times, drafts, partidas e estatísticas.
2. [Fontes de dados](DATA_SOURCES.md): comparação entre Riot API, Match V5, Tournament API, LCU, Live Client Data, Data Dragon, ROFL, OCR e entrada manual.
3. [Matriz de disponibilidade](FIELD_AVAILABILITY_MATRIX.md): origem e confiabilidade de cada grupo de métricas, incluindo os limites observados na partida de validação.
4. [Conformidade Riot e LCU](RIOT_LCU_COMPLIANCE.md): suporte oficial versus comunitário, registro do produto, monetização, custom matches, consentimento e segurança do `lockfile`.

### Arquitetura e domínio

5. [Arquitetura](ARCHITECTURE.md): fronteiras, dependências e fatos versus projeções.
6. [Modelo de domínio](DOMAIN_MODEL.md): conceitos existentes, extensões e propostas.
7. [Seasons e competições](SEASONS_AND_COMPETITIONS.md): calendário, formatos, Fearless e elegibilidade.
8. [Elencos e transferências](ROSTERS_AND_TRANSFERS.md): times oficiais, escalações, janelas e histórico.
9. [Fluxo de importação](IMPORT_PIPELINE.md): coleta, validação, revisão, confirmação e reprocessamento.
10. [Dados brutos](RAW_DATA_STORAGE.md): redaction, integridade, retenção e exceção auditável.

### Métricas e reconhecimento

11. [Catálogo de métricas](METRICS_CATALOG.md): fórmulas, confiabilidade e exclusões.
12. [Rinha Score v1](PERFORMANCE_SCORE_V1.md): nota 0 a 100, pesos, confiança e versionamento.
13. [Rinha Rating](RINHA_RATING.md): ledger, elos, pontos, queda e soft reset.
14. [Votação MVP/SVP](VOTING_MVP_SVP.md): elegibilidade, turnos, moderação e desempate.

### Produto, contratos e entrega

15. [Wireframes textuais](TEXTUAL_WIREFRAMES.md): telas, navegação, filtros e responsividade.
16. [Endpoints](API_ENDPOINTS.md): recursos REST e contratos de consulta/comando.
17. [Eventos de domínio](DOMAIN_EVENTS.md): fatos publicados e consumidores.
18. [Autorização](AUTHORIZATION.md): roles, capabilities, limites e testes negativos.
19. [Estratégia de testes](TEST_STRATEGY.md): pirâmide, fixtures, propriedades e gates.
20. [Roadmap](IMPLEMENTATION_ROADMAP.md): sequência incremental de features.
21. [Backlog](BACKLOG.md): histórias e critérios de aceite.
22. [Decisões pendentes](OPEN_DECISIONS.md): aprovações humanas ainda necessárias.
23. [Perguntas para a comunidade](COMMUNITY_QUESTIONS.md): roteiro de consulta a jogadores e organização.

Os itens 1 a 20 cobrem os **20 entregáveis principais** deste conjunto documental. O índice contém 23 documentos porque Backlog, Decisões pendentes e Perguntas para a comunidade são artefatos complementares: eles organizam trabalho futuro, registram escolhas ainda não ratificadas e apoiam consulta aos participantes, mas não ampliam a contagem dos entregáveis principais nem possuem autoridade normativa própria.

## Vocabulário de confiabilidade

| Classificação | Significado neste conjunto de documentos |
|---|---|
| **Confirmado** | O campo foi fornecido diretamente por uma fonte oficial documentada ou foi observado diretamente na validação empírica indicada. Para LCU, “Confirmado” prova apenas a observação naquela partida e naquele cliente; não significa suporte oficial nem estabilidade futura. |
| **Derivável** | O valor pode ser calculado de dados confirmados por uma fórmula explícita e reproduzível. A precisão depende da granularidade e da integridade da fonte. |
| **Experimental** | O campo, interpretação ou método foi observado, inferido ou documentado pela comunidade, mas pode variar, conter inconsistências ou não ter garantia oficial. Não deve alimentar decisão oficial sem validação. |
| **Indisponível** | A fonte não entrega o campo necessário, o repositório não o implementa ou não há evidência suficiente para afirmar disponibilidade. Ausência não equivale a zero. |

## Princípios aplicáveis

- O backend deve continuar sendo a fonte de verdade para autorização, confirmação, vínculos e agregações.
- Integrações externas são adaptadores e não podem bloquear o fluxo manual.
- Estatísticas históricas devem ser reconstruíveis a partir de partidas confirmadas, e não de uma projeção opaca.
- Dados de uma fonte externa devem preservar origem, momento de coleta e grau de confiança.
- Um valor numérico `0` é um fato possível; campo ausente é falta de informação. Os dois estados não podem compartilhar a mesma representação sem um indicador de presença.
- Rotas automáticas e assistências de objetivos do experimento LCU não são confiáveis para estatística oficial.
- Nenhuma coleta deve expor informação durante a partida que produza vantagem competitiva.

## Fontes consultadas

Consulta realizada em **2026-07-27**:

- [Riot Developer Portal: documentação de League of Legends](https://developer.riotgames.com/docs/lol)
- [Riot Developer Portal: referência de APIs, incluindo Match V5 e Tournament V5](https://developer.riotgames.com/apis)
- [Riot Developer Portal: políticas gerais](https://developer.riotgames.com/policies/general)
- [Riot Developer Portal: políticas específicas por jogo](https://developer.riotgames.com/policies/game-specific)
- [Data Dragon: versões disponíveis](https://ddragon.leagueoflegends.com/api/versions.json)
- [Amostra oficial da Live Client Data API](https://static.developer.riotgames.com/docs/lol/liveclientdata_sample.json)
- [Certificado raiz publicado pela Riot para APIs locais](https://static.developer.riotgames.com/docs/lol/riotgames.pem)
- [Hextechdocs: introdução comunitária à LCU API](https://hextechdocs.dev/getting-started-with-the-lcu-api/)
- Constituição, arquitetura, specs e código do próprio repositório, com destaque para `.specify/memory/constitution.md`, `docs/architecture/`, `specs/002-mvp-rinha-das-lendas.md`, `specs/003-cadastro-jogadores-rotas/`, `specs/006-cadastro-time/`, `specs/017-robustecer-drafts-discord-jogadores/`, `specs/018-importacao-partidas-lcu/spec.md`, `specs/021-redesenho-fluxo-draft/` e `specs/022-arquivar-drafts/`.

## Limites desta pesquisa

- Não foi consultado nem armazenado payload bruto da partida.
- Não foi feita chamada autenticada à Riot API, Match V5 ou Tournament API.
- Não foi testado parsing de arquivo ROFL nem OCR.
- Não foi estabelecido schema de arquivo, endpoint HTTP, modelo relacional ou regra definitiva de vínculo.
- As políticas da Riot podem mudar; devem ser verificadas novamente antes de disponibilizar ou monetizar qualquer integração.

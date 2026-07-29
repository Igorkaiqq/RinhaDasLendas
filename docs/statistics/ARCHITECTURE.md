# Arquitetura da Fundação Competitiva e Estatística

## Objetivo

Este documento define a direção arquitetural para competições, partidas, ingestão e análises estatísticas. Ele orienta especificações futuras; não aprova automaticamente entidades, tabelas, endpoints ou eventos.

## Decisão arquitetural

A fundação será construída como **monólito modular**, preservando as camadas `Api`, `Application`, `Domain`, `Infrastructure` e `Tests`. Não haverá microsserviços no MVP.

O monólito terá quatro fronteiras funcionais:

| Fronteira | Responsabilidade | Não deve decidir |
|---|---|---|
| Competição | Seasons, competições, rodadas, Times oficiais, vínculos, janelas publicadas pela organização e elegibilidade organizacional | Resultado de Partida, importação técnica ou fórmula estatística |
| Partidas | Agendamento, Séries oficiais e amistosas, Partidas, lados, escalações, picks, bans, Fearless, resultado e votação | Composição histórica de Time oficial ou cálculo proprietário de Score e Rating |
| Ingestão | Recepção manual ou externa, redação, validação, normalização, reconciliação e rastreabilidade da origem | Regras competitivas ou cálculo de ranking |
| Analytics | Métricas, Rinha Score, confiança, Rating, elos, rankings e projeções reconstruíveis | Alteração de fatos de partida ou vínculo de elenco |

As fronteiras são módulos lógicos no mesmo processo e banco. Cada uma controla suas regras e seus dados normalizados. A comunicação deve ocorrer por casos de uso explícitos, identificadores estáveis e fatos publicados após confirmação, sem acesso oportunista às estruturas internas de outro módulo.

## Fatos e projeções

### Fatos imutáveis

Resultados confirmados, participações, escalações efetivamente usadas, escolhas, bans, estatísticas importadas, votos válidos e lançamentos de Rating são fatos históricos. Depois de confirmados, não devem ser reescritos silenciosamente.

Uma correção deve:

- preservar o valor anterior e sua origem;
- registrar autoria, instante e motivo;
- produzir uma nova versão ou fato compensatório;
- invalidar ou substituir explicitamente a versão anterior;
- provocar a reconstrução das projeções afetadas.

Imutabilidade não significa aceitar erro conhecido. Significa corrigir com rastreabilidade, sem apagar a história.

### Projeções reconstruíveis

Score, Rating atual, elo, ranking, agregados por jogador, season, rota, campeão ou time são projeções. Elas podem ser descartadas e recalculadas a partir de fatos válidos e das versões de algoritmo correspondentes.

Toda projeção deve informar, conforme aplicável:

- versão do algoritmo;
- instante da última reconstrução;
- recorte de Season e competição;
- qualidade ou confiança dos dados;
- fato mais recente considerado;
- estado de reconstrução ou falha.

## Fluxo de dados

1. Competição e Partidas confirmam contexto, participantes e resultado.
2. Ingestão recebe dados manuais ou externos e mantém a procedência.
3. Ingestão valida identidades, normaliza campos e reconcilia duplicidades.
4. Partidas aceita os fatos normalizados compatíveis com suas invariantes.
5. Analytics calcula projeções versionadas sem alterar os fatos de origem.
6. Correções geram nova versão ou compensação e enfileiram reconstrução determinística.

Falha de integração externa não pode bloquear o fluxo manual. Discord, Riot e outros provedores são adaptadores, não fontes exclusivas das regras centrais.

## Persistência

Dados normalizados devem usar modelagem relacional no PostgreSQL, com UUID, chaves estrangeiras explícitas, `snake_case`, constraints e migrations. Regras relevantes devem ser protegidas pelo domínio e, quando possível, pelo banco.

Payload bruto é uma exceção auditável, não o modelo principal. Seu uso obedece a [Armazenamento de Dados Brutos](./RAW_DATA_STORAGE.md).

## Consistência e reconstrução

Operações dentro de uma fronteira devem ser transacionais. Comunicação derivada entre fronteiras pode ser posterior à confirmação, desde que seja idempotente e recuperável. Uma falha em Analytics não pode desfazer uma partida confirmada; deve deixar a projeção marcada como desatualizada até nova tentativa.

Reconstruções devem aceitar um recorte explícito, como jogador, série, Season ou versão de algoritmo. O resultado precisa ser equivalente quando os mesmos fatos e a mesma versão forem processados novamente.

## Integração com o domínio existente

- `DraftMontagem` continua sendo o fluxo oficial de presença e montagem de times.
- `DraftMontagemTime` representa um time temporário daquela montagem e não se torna um Time oficial por semelhança de nome ou composição.
- `Time` existente é a raiz reutilizável mais próxima de um Time oficial, mas as regras competitivas deste conjunto de documentos exigem especificação e migração próprias antes de alterar seu significado.
- O cadastro de `Jogador` continua sendo a identidade interna usada para vínculos e participações.
- Agendas de presença continuam em `America/Sao_Paulo`; confrontos oficiais e amistosos agendados não dependem de uma presença para existir.
- `Série oficial` é toda Série com validade competitiva oficial e exige vínculo com Competição e rodada.
- `DiariaTemporaria` é um subtipo de Série oficial originado em `DraftMontagem`; usa equipes temporárias, pertence ao circuito diário e alimenta todas as projeções oficiais elegíveis.
- `ConfrontoOficial` é um subtipo de Série oficial reservado ao confronto entre Times oficiais.
- Competição pode ser opcional ou ausente somente em contexto amistoso, conforme o contrato do amistoso.
- Fearless é configuração de qualquer Série direta de dois lados, oficial ou amistosa, desde que a Série não pertença a Evento de quatro times.
- Série amistosa entre Times oficiais continua amistosa e não se torna `ConfrontoOficial` por causa dos lados ou do uso de Fearless.
- `Partida` é a unidade individual com resultado e estatísticas; `Série` agrupa Partidas e deriva delas seu placar e resultado.
- Participar de duas Séries oficiais do subtipo `DiariaTemporaria` concede qualificação global, histórica, permanente e não consumível, sem vínculo com Time ou janela.
- Eventos de quatro times usam draft padrão em todos os seus confrontos e não permitem reativar Fearless em nenhuma fase interna.
- Amistosos, agendados, agrupados ou isolados, alimentam somente histórico e agregados amistosos separados, mesmo quando uma Série amistosa usa Fearless.
- O Rating principal recebe lançamentos por partida oficial; ajustes de série são lançamentos posteriores e distintos.

## Classificação das decisões

### Decisões aceitas nesta fundação

- monólito modular com as quatro fronteiras descritas;
- fatos imutáveis e projeções reconstruíveis;
- persistência relacional para dados normalizados;
- alternativa manual para integrações externas;
- separação entre times temporários de montagem e Times oficiais;
- faixas de Rating por partida, elos, piso zero e queda automática;
- soft reset sempre versionado, com fórmula e parâmetros sujeitos a calibração;
- exclusão integral de amistosos das projeções e recordes oficiais.

### Propostas que exigem especificação

- nomes de agregados, entidades, tabelas, eventos e endpoints;
- mecanismo técnico de fila ou reconstrução;
- política operacional de retenção de payload bruto;
- fórmulas e parâmetros internos propostos para Score, força, impacto, soft reset, KDA e ajustes de série;
- permissões detalhadas além das regras explicitamente registradas.

As fórmulas pendentes permanecem sob governança de [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md) e não podem ser tratadas como aprovadas por aparecerem nestes documentos.

## Alternativas rejeitadas

### Microsserviços por fronteira

Rejeitados no MVP por custo operacional e ausência de necessidade de escala ou isolamento de implantação.

### Atualizar agregados estatísticos como fonte da verdade

Rejeitado porque impede auditoria e torna correções dependentes do estado acumulado. Projeções devem ser derivadas dos fatos.

### Armazenar toda resposta externa como domínio em JSON

Rejeitado porque enfraquece integridade, consulta e evolução. JSON bruto é permitido somente como exceção redigida e auditável.

# Quickstart: Validação da Fundação Competitiva Sazonal

## Objetivo

Validar a feature 023 de ponta a ponta depois da implementação, incluindo domínio, API, banco, frontend, autorização, concorrência, i18n e responsividade. Os contratos esperados estão em `contracts/` e o modelo em `data-model.md`.

## Pré-requisitos

- branch `feature/023-fundacao-competitiva-sazonal`;
- Dev Container ativo ou Docker Desktop com os containers existentes;
- PostgreSQL saudável;
- dependências do frontend instaladas;
- contas de teste para SuperAdmin, Presidente, VicePresidente, Admin, Moderador, Capitão e Jogador;
- nenhum token, senha ou header sensível registrado em fixture, log ou evidência.

## Verificação automatizada

### Backend dentro do Dev Container

```bash
dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
```

### Backend a partir do host

Primeiro tente o Compose existente:

```bash
docker compose -f .devcontainer/docker-compose.yml exec -T app dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
docker compose -f .devcontainer/docker-compose.yml exec -T app dotnet build /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
```

Se o comando Linux não estiver disponível, reutilize o container do Docker Desktop conforme `AGENTS.md`:

```bash
docker.exe start rinhadaslendas_devcontainer-app-1
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
```

### Frontend

```bash
npm test --prefix FrontEnd
npm run lint:check --prefix FrontEnd
npm run build --prefix FrontEnd
npm audit --prefix FrontEnd -- --audit-level=moderate
```

## Migration

Após a migration da feature existir, verifique divergências e gere um script revisável:

```bash
dotnet ef migrations has-pending-model-changes \
  --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure \
  --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api

dotnet ef migrations script --idempotent \
  --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure \
  --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api \
  --output /tmp/feature-023.sql
```

Em bancos descartáveis, validar:

- aplicação sobre schema vazio e schema atual;
- nomes `snake_case`, UUIDs, FKs e políticas de exclusão;
- unicidade de `(ano, ordem_no_ano)` e rodada por ordem;
- no máximo uma Season ativa;
- Seasons sem sobreposição;
- uma Partida vinculada a no máximo uma Série;
- downgrade sem afetar dados fora da feature.

## Execução local

No Dev Container:

```bash
dotnet run --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj
```

No host, em outro terminal:

```bash
npm run dev --prefix FrontEnd -- --host 0.0.0.0
```

Swagger deve expor o subconjunto documentado em `contracts/competitive-foundation.openapi.yaml`.

## Cenário 1: calendário vazio e ativação concorrente

1. Consultar listagem sazonal sem `temporadaIds` e sem `todas` antes de ativar uma Season.
2. Confirmar `items=[]`, `calendarioConfigurado=false` e `temporadaAtual=null`.
3. Como Presidente, criar duas Seasons planejadas consecutivas e não sobrepostas.
4. Obter a mesma ETag do calendário em dois clientes.
5. Ativar Seasons diferentes simultaneamente usando o mesmo `If-Match`.
6. Confirmar uma resposta de sucesso, um `409`, uma única Season ativa e incremento único da versão.
7. Confirmar que SuperAdmin também pode gerir Seasons e que VicePresidente, Admin e inferiores recebem `403`.

## Cenário 2: seleção sazonal e histórico

1. Com uma Season ativa e uma encerrada, consultar sem filtro e confirmar apenas a ativa em `seasonsIncluidas`.
2. Consultar com dois `temporadaIds` repetidos e confirmar ambos os recortes identificados.
3. Consultar com `todas=true` e confirmar o histórico completo.
4. Combinar `todas=true` e `temporadaIds`; confirmar `400` localizado.
5. Trocar a Season ativa e consultar uma Série antiga por ID; confirmar que seu vínculo original permanece.
6. Confirmar que rotas aninhadas em `/temporadas/{id}` rejeitam seleção sazonal adicional.

## Cenário 3: competição, rodada e regras

1. Criar uma competição marcada como circuito diário na Season ativa.
2. Tentar criar uma segunda competição de circuito diário na mesma Season; confirmar conflito.
3. Criar e reordenar rodadas, confirmando ordem única e `409` com ETag antiga.
4. Publicar uma regra MD3 Fearless e depois uma regra MD5 padrão.
5. Confirmar que a primeira versão permanece imutável e consultável.
6. Publicar uma regra geral na Season e confirmar que ela atende amistoso sem competição, mas não Série oficial.
7. Tentar usar competição, rodada ou regra de outra Season em uma Série oficial; confirmar rejeição.

## Cenário 4: Série diária Fearless

1. Finalizar um `DraftMontagem` com dois lados, dez jogadores e dois capitães.
2. Criar uma `DiariaTemporaria` MD3 Fearless com data local, competição do circuito diário e rodada.
3. Confirmar snapshots dos lados/capitães e vínculo histórico com o Draft.
4. Arquivar o Draft e confirmar que a Série continua válida sem restaurá-lo.
5. Iniciar a Série e adicionar a primeira Partida.
6. Registrar cinco picks distintos por lado e confirmar um vencedor.
7. Adicionar a segunda Partida e tentar repetir qualquer campeão; confirmar `409` localizado.
8. Registrar picks válidos, confirmar o mesmo lado vencedor e verificar conclusão em `2-0`.
9. Tentar adicionar ou confirmar Partida excedente; confirmar rejeição.

## Cenário 5: remake e reconstrução Fearless

1. Em outra Série Fearless, registrar picks na primeira Partida e marcá-la como remake com `PreservarPicks`.
2. Confirmar que os dez campeões ficam bloqueados na próxima Partida.
3. Repetir em outra Série com `DesconsiderarPicks` e confirmar que os campeões ficam disponíveis.
4. Corrigir picks de uma Partida anterior para criar conflito com uma posterior já confirmada.
5. Confirmar `revisaoNecessaria=true`, `conflitoFearless=true` na Partida afetada e bloqueio de novas confirmações.
6. Corrigir ou anular a Partida afetada e confirmar reconstrução, remoção do conflito e histórico antes/depois com justificativa.

## Cenário 6: Times oficiais, amistoso e Evento

1. Agendar `ConfrontoOficial` entre dois Times ativos sem lista de presença.
2. Confirmar Season, competição, rodada, snapshots e elegibilidade oficial.
3. Agendar `Amistoso` entre os mesmos Times com Fearless.
4. Confirmar histórico separado e inelegibilidade explícita para Score, Rating, MVP/SVP, rankings, recordes e qualificação.
5. Criar Evento com quatro Times distintos.
6. Tentar habilitar Fearless no Evento ou em uma Série associada; confirmar rejeição.
7. Confirmar que todas as Séries do Evento usam draft padrão e não compartilham bloqueios.

## Cenário 7: idempotência, concorrência e auditoria

1. Repetir uma mutação com a mesma `Idempotency-Key` e conteúdo.
2. Confirmar mesma resposta, header `Idempotency-Replayed: true` e ausência de duplicação de fato/auditoria.
3. Reutilizar a chave com conteúdo diferente; confirmar `409` e nenhuma alteração.
4. Atualizar um recurso com ETag obsoleta; confirmar `409` sem sobrescrita silenciosa.
5. Corrigir resultado com justificativa e consultar auditoria como ator autorizado.
6. Confirmar ator autenticado, instante, capacidade, valor anterior/posterior e justificativa.
7. Confirmar que ator enviado no corpo é ignorado ou rejeitado e nunca substitui a identidade autenticada.
8. Confirmar que usuário sem `CanViewCompetitiveAudit` recebe `403` e não vê metadados sensíveis.
9. Consultar auditoria de Season, competição, regra, Evento, Série e Partida pelo filtro de recurso e confirmar que nenhum usa Season ativa como filtro implícito.

## Cenário 8: interface, responsividade e acessibilidade

Validar em Chromium autenticado:

- desktop com largura de 1280 px ou maior;
- mobile com 320 px;
- navegação por teclado;
- foco visível;
- preferência de movimento reduzido;
- PT-BR e EN-US.

Confirmar:

- cadastro de Season, competição, rodada e Série MD3 em até cinco minutos para ator já autenticado;
- seleção da Season atual, duas Seasons e todas em no máximo três ações e uma confirmação;
- natureza, formato, draft e estado visíveis sem depender apenas de cor;
- tabelas convertidas em cards no mobile sem perder placar, lados, Season ou estado;
- loading com skeleton, vazios distintos, retry, proibido e conflito de versão;
- controles de 44 px e dialogs com foco contido/restaurado;
- nenhum texto de enum, `messageCode` ou fallback técnico exibido diretamente.

## Auditoria de internacionalização

Antes de concluir a implementação, registrar evidência de que:

- não há texto novo hardcoded no frontend;
- não há mensagem nova hardcoded no backend;
- `pt.json` e `en.json` possuem as mesmas chaves;
- `Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx` estão sincronizados;
- português foi revisado quanto à acentuação;
- placeholders, botões, títulos, badges, tooltips, toasts, confirmações e vazios foram revisados;
- validações frontend e backend usam i18n/resources;
- todos os arquivos novos respeitam o padrão.

Qualquer item negativo bloqueia a conclusão da implementação.

## Cobertura automatizada obrigatória para `tasks.md`

- matriz completa das cinco capabilities para os sete papéis, incluindo ausência de capability no VicePresidente e todas as condições de SuperAdmin, Admin e Moderador;
- duas confirmações concorrentes em Partidas diferentes da mesma Série;
- correção concorrente com confirmação de outra Partida;
- correção de Série concluída: `2-0 -> 1-1` com anulação explícita, inversão de vencedor e anulação da Partida decisiva;
- limites inicial/final da Season e igualdade entre `dataLocal` e data de `agendadaPara` em São Paulo;
- catálogo de Seasons sem filtro implícito e demais coleções com seleção padrão;
- auditoria recuperável para todos os tipos de recurso e matriz negativa de acesso;
- paridade estrutural `pt.json`/`en.json` e paridade de chaves dos resources backend;
- envelopes localizados PT-BR/EN-US para validação, conflito, proibição e inexistência;
- componentes sem texto hardcoded e estados mobile/desktop essenciais.

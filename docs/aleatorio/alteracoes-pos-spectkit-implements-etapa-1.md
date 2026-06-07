# Alteracoes Pos Spec Kit

## Resumo

Apos a implementacao da spec `003-cadastro-jogadores-rotas`, o projeto recebeu a base funcional de cadastro e gestao de jogadores, persistencia em PostgreSQL, endpoints REST, validacoes de dominio/aplicacao, testes automatizados, collection HTTP para testes manuais, ajustes de experiencia no frontend e uma sidebar fixa de navegacao.

Tambem foram feitos ajustes posteriores para obrigatoriedade de Discord, Elo e Divisao, remocao do campo Nome Real do formulario, tratamento de Elo e Divisao como enums no backend e no frontend, migrations de saneamento de dados legados e feedbacks visuais ao usuario.

## Alteracoes Realizadas

### Cadastro de Jogadores

- Campo de Elo obrigatorio.
- Campo de Discord obrigatorio.
- Remocao do campo Nome Real do formulario de cadastro.
- Substituicao por Nome de Exibicao.
- Ajuste do cadastro para usar Nome/Nickname do jogo ou comunidade.
- Elo alterado para dropdown.
- Divisao alterada para dropdown.
- Validacoes para impedir cadastro incompleto.
- Feedbacks visuais adicionados para sucesso, erro e acoes de filtro.
- Persistencia de jogadores e preferencias de rota em PostgreSQL.
- Atualizacao de preferencias de rota sem conflito de indices durante troca de prioridades.

### Elo e Divisao

- Implementado dropdown de Elo com:
  - Ferro
  - Bronze
  - Prata
  - Ouro
  - Platina
  - Esmeralda
  - Diamante
  - Mestre
  - Grao-Mestre
  - Desafiante

- Implementado dropdown de Divisao para elos de Ferro ate Diamante:
  - IV
  - III
  - II
  - I

- Elos Mestre, Grao-Mestre e Desafiante nao exigem divisao.
- `Elo` e `Divisao` foram implementados como enums no dominio backend.
- `Elo` e `Divisao` foram implementados como enums TypeScript no frontend.
- A API recebe `elo` e `divisao` como campos separados.
- A resposta da API retorna `elo` e `divisao` separados para exibicao consistente.
- Foram adicionadas migrations para separar dados antigos como `Ouro II` e normalizar dados legados como `Esmeralda 4`.

### Sidebar

- Criada sidebar lateral fixa para navegacao.
- Sidebar com modo recolhido exibindo apenas icones.
- Sidebar com modo expandido exibindo icones e textos.
- Adicionado botao para expandir/recolher.
- Adicionadas opcoes no menu:
  - Home
  - Jogadores
  - Partidas
  - Draft
  - Times
  - Relatorios
  - Estatisticas
  - Configuracoes

### Home

- Criada tela inicial Home.
- Adicionado titulo do sistema: Rinha das Lendas.
- Adicionado resumo do sistema.
- Adicionado destaque para Discord do grupo.
- Adicionados cards de acesso rapido para Jogadores, Draft e Partidas.

### Telas Futuras

- Telas ainda nao implementadas exibem mensagem temporaria:
  - "Tela em desenvolvimento."

### API e Backend

- Adicionados controllers REST para jogadores.
- Adicionados commands, queries e handlers usando MediatR.
- Adicionados DTOs de criacao, atualizacao, resposta e paginacao.
- Adicionada validacao com FluentValidation.
- Adicionado middleware de excecao para respostas de erro consistentes.
- Adicionado DbContext, repository e migrations EF Core.
- Adicionada execucao automatica de migrations ao subir a API fora do ambiente de testes.
- Mantida compatibilidade com as connection strings `RinhaDasLendas` e `DefaultConnection`.

### Testes e Documentacao HTTP

- Criados testes de unidade para criacao, inativacao e preferencias de jogadores.
- Criado teste de integracao com Testcontainers/PostgreSQL para fluxo real da API.
- Criado relatorio de cobertura de endpoints em `docs/api/RELATORIO_TESTES_HTTP.md`.
- Criada collection HTTP em `docs/api/rinha-das-lendas.http`.

## Arquivos Alterados

### Backend

- `BackEnd/src/RinhaDasLendas.Api/Program.cs`: configuracao de controllers, Swagger, CORS, middleware de erro e migrations automaticas.
- `BackEnd/src/RinhaDasLendas.Api/Controllers/JogadoresController.cs`: endpoints REST de jogadores.
- `BackEnd/src/RinhaDasLendas.Api/Filters/ApiExceptionMiddleware.cs`: tratamento padronizado de erros.
- `BackEnd/src/RinhaDasLendas.Application/Commands/Jogadores/*`: commands de criacao, atualizacao, preferencias e inativacao.
- `BackEnd/src/RinhaDasLendas.Application/Queries/Jogadores/*`: queries de listagem e busca por ID.
- `BackEnd/src/RinhaDasLendas.Application/Handlers/Jogadores/*`: handlers de casos de uso.
- `BackEnd/src/RinhaDasLendas.Application/Dtos/*`: contratos internos e externos da aplicacao.
- `BackEnd/src/RinhaDasLendas.Application/Validators/JogadorValidator.cs`: validacoes de jogador, Elo, Divisao e preferencias de rota.
- `BackEnd/src/RinhaDasLendas.Domain/Entities/*`: entidades `Jogador` e `PreferenciaRota`.
- `BackEnd/src/RinhaDasLendas.Domain/Enums/*`: enums de rota, status, Elo e Divisao.
- `BackEnd/src/RinhaDasLendas.Domain/Repositories/IJogadorRepository.cs`: contrato de persistencia.
- `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`: mapeamento EF Core.
- `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/JogadorRepository.cs`: implementacao do repository.
- `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/*`: migrations de criacao, ajuste de indices e normalizacao de Elo/Divisao.
- `BackEnd/Directory.Packages.props`: versoes centralizadas de pacotes novos.

### Frontend

- `FrontEnd/src/App.vue`: layout global com sidebar e feedback para telas futuras.
- `FrontEnd/src/router/index.ts`: rotas Home e Jogadores.
- `FrontEnd/src/views/HomeView.vue`: tela inicial do sistema.
- `FrontEnd/src/views/PlayersView.vue`: formulario, listagem, validacoes, feedbacks e integracao com API.
- `FrontEnd/src/services/players.ts`: cliente HTTP, tipos, enums e normalizacao de payloads.
- `FrontEnd/src/components/PlayerStatusBadge.vue`: badge de status do jogador.
- `FrontEnd/src/components/RoutePreferenceEditor.vue`: editor de preferencias de rota.
- `FrontEnd/src/components/RoutePreferencesPanel.vue`: resumo visual das preferencias.
- `FrontEnd/src/styles/main.css`: tokens, layout, cards, sidebar, formulario, listagem e responsividade.
- `FrontEnd/package.json` e `FrontEnd/package-lock.json`: scripts e dependencias instaladas.

### Documentacao e Specs

- `docs/api/RELATORIO_TESTES_HTTP.md`: relatorio dos testes HTTP realizados.
- `docs/api/rinha-das-lendas.http`: collection HTTP para repetir chamadas manuais.
- `specs/003-cadastro-jogadores-rotas/tasks.md`: tarefas marcadas como concluidas.
- `specs/003-cadastro-jogadores-rotas/quickstart.md`: validacoes e fluxo manual atualizados.
- `specs/003-cadastro-jogadores-rotas/regras-jogadores.md`: regras complementares do cadastro.
- `docs/alteracoes-pos-spec-kit.md`: este documento de revisao final.

## Validacoes

### Cadastro e Dados Obrigatorios

- Nome de Exibicao obrigatorio.
- Discord obrigatorio.
- Elo obrigatorio.
- Divisao obrigatoria quando o Elo exigir divisao.
- Divisao proibida para Mestre, Grao-Mestre e Desafiante.
- Preferencias de rotas devem conter exatamente cinco rotas.
- Prioridades devem ser unicas e estar entre 1 e 5.
- Apenas uma rota pode ser marcada como "nao jogo nem lascando".
- URLs de OP.GG e Deeplol devem pertencer aos dominios esperados quando informadas.
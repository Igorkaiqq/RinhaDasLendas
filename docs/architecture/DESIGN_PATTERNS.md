# Design Patterns Guidelines

## Objetivo

Este documento define quais Design Patterns devem ser utilizados no projeto RinhaDasLendas, quando utilizá-los e quando evitá-los.

O objetivo não é usar padrões por moda ou excesso de engenharia.

Cada padrão deve resolver um problema real.

---

# Princípios Gerais

Antes de aplicar qualquer Design Pattern, verificar:

1. Existe um problema real?
2. O padrão reduz acoplamento?
3. O padrão facilita testes?
4. O padrão melhora manutenção?
5. O padrão não está tornando algo simples mais complexo?

Se a resposta for "não", evitar o uso do padrão.

---

# Padrões Permitidos

## Criacionais

Padrões responsáveis pela criação de objetos.

### Factory Method

### Utilizar

Quando existir mais de uma implementação para uma mesma abstração.

Exemplo:

```text
DiscordAuthenticationProvider
GoogleAuthenticationProvider
LocalAuthenticationProvider
```

### Evitar

Para objetos simples que podem ser instanciados diretamente.

---

### Builder

### Utilizar

Objetos complexos com muitos campos opcionais.

Exemplo:

```text
PartidaBuilder
SerieBuilder
RelatorioPartidaBuilder
```

### Evitar

Objetos pequenos.

---

### Abstract Factory

### Utilizar

Quando existir famílias completas de objetos.

Exemplo futuro:

```text
RiotProvider
DiscordProvider
SteamProvider
```

### Evitar

No MVP.

---

### Singleton

### Utilizar

Apenas para serviços realmente únicos.

Exemplos:

```text
ClockProvider
ApplicationSettings
```

### Evitar

Serviços de domínio.

Evitar Singletons globais.

---

# Estruturais

Padrões responsáveis pela composição entre objetos.

### Adapter

### Utilizar

Integrações externas.

Exemplos:

```text
Discord API
Riot API
OP.GG
Deeplol
```

Cada integração deve possuir seu Adapter.

---

### Facade

### Utilizar

Quando uma funcionalidade exigir múltiplos serviços.

Exemplo:

```text
DraftFacade
```

Responsável por:

* validar jogadores;
* validar rotas;
* gerar times;
* gerar draft.

---

### Decorator

### Utilizar

Adicionar comportamento sem alterar a implementação principal.

Exemplo:

```text
LoggingDecorator
CachingDecorator
```

---

### Proxy

### Utilizar

Controle de acesso.

Exemplo:

```text
CachedRiotApiProxy
```

---

# Comportamentais

Padrões responsáveis pela comunicação e comportamento dos objetos.

### Strategy

### Obrigatório Sempre Que Existirem Algoritmos Variáveis

Exemplos:

```text
RandomDraftStrategy
BalancedDraftStrategy
CaptainDraftStrategy
```

ou

```text
RandomTeamGenerationStrategy
EloBalancedTeamGenerationStrategy
```

O padrão Strategy é um dos mais importantes do projeto.

---

### Observer

### Utilizar

Eventos do sistema.

Exemplos:

```text
JogadorCadastrado
PartidaFinalizada
SerieFinalizada
```

Pode ser implementado usando MediatR.

O padrão Observer é naturalmente compatível com eventos de domínio.

---

### Command

### Utilizar

Operações de escrita.

Exemplo:

```text
CriarJogadorCommand
AtualizarJogadorCommand
InativarJogadorCommand
```

---

### Chain Of Responsibility

### Utilizar

Validações encadeadas.

Exemplo:

```text
ValidarQuantidadeJogadores
ValidarRotas
ValidarElo
ValidarRestricoes
```

---

### State

### Utilizar

Fluxos que mudam de comportamento conforme estado.

Exemplo:

```text
PartidaAberta
PartidaEmAndamento
PartidaFinalizada
```

---

# Padrões Arquiteturais Obrigatórios

## Clean Architecture

Separação em:

```text
Api
Application
Domain
Infrastructure
```

---

## Repository

Obrigatório para persistência.

Exemplo:

```text
IJogadorRepository
```

---

## Dependency Injection

Obrigatório.

Nenhum serviço deve instanciar dependências diretamente.

---

## CQRS

Utilizar para separação entre:

```text
Commands
Queries
```

Não é necessário criar microsserviços.

Apenas separar leitura e escrita na aplicação.

---

# Anti-Patterns Proibidos

## God Class

Classe com muitas responsabilidades.

---

## Service Locator

Utilizar Dependency Injection.

Nunca buscar dependências manualmente.

---

## Anemic Domain Model

Regras de negócio devem existir no domínio.

Não deixar toda lógica apenas em Services.

---

## Controllers Gordos

Controllers apenas recebem requisições e retornam respostas.

Não devem conter regras de negócio.

---

# Prioridade de Uso

Para este projeto, os padrões mais importantes são:

1. Repository
2. Dependency Injection
3. CQRS
4. Strategy
5. Command
6. Observer
7. Adapter
8. Facade
9. Builder

Todos os demais devem ser utilizados apenas quando houver necessidade real.

---

# Regra Final

Código simples é melhor que Design Pattern.

Design Pattern é uma ferramenta.

Nunca aplicar um padrão apenas porque ele existe.
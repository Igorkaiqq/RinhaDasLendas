# DDD Guidelines

## Objetivo

Este documento define as regras de Domain-Driven Design utilizadas no projeto RinhaDasLendas.

O objetivo é manter o domínio desacoplado da infraestrutura, facilitar testes e permitir evolução do sistema sem acoplamento excessivo.

---

# Camadas

O sistema deve seguir a seguinte estrutura:

```text
Api
Application
Domain
Infrastructure
Tests
```

---

# Domain

A camada Domain é o núcleo do sistema.

Ela contém:

* Entidades
* Value Objects
* Enums
* Regras de negócio
* Eventos de domínio
* Exceções de domínio

A camada Domain NÃO pode conhecer:

* Entity Framework
* PostgreSQL
* HTTP
* Controllers
* DTOs
* APIs externas

---

# Application

A camada Application coordena casos de uso.

Responsabilidades:

* Executar comandos
* Executar consultas
* Chamar serviços de domínio
* Chamar repositórios

Não deve conter regras de negócio complexas.

---

# Infrastructure

Responsável por:

* Banco de dados
* Integrações externas
* Repositórios
* Serviços de terceiros

Exemplos:

* Riot API
* Discord API
* PostgreSQL
* Cache

---

# Api

Responsável apenas por:

* Receber requisições
* Chamar casos de uso
* Retornar respostas

Controllers não devem conter regras de negócio.

---

# Entidades

Entidades devem possuir:

* Identidade
* Comportamento
* Validação de invariantes

Exemplo:

```csharp
Jogador.Inativar();
Jogador.AlterarNome();
Jogador.AtualizarPreferencias();
```

Evitar:

```csharp
jogador.Nome = nome;
jogador.Status = status;
```

---

# Value Objects

Utilizar quando não existir identidade própria.

Exemplos:

```text
RiotId
DiscordId
UrlPerfil
```

Características:

* Imutáveis
* Comparados por valor

---

# Repositórios

Interfaces devem ficar no Domain ou Application.

Implementações devem ficar na Infrastructure.

Exemplo:

```csharp
IJogadorRepository
```

---

# CQRS

Separar comandos e consultas.

Exemplo:

```text
Commands
Queries
```

Não misturar leitura e escrita no mesmo handler.

---

# Eventos de Domínio

Utilizar para desacoplar comportamentos.

Exemplo:

```text
JogadorCriado
PartidaFinalizada
SerieFinalizada
```

Preferencialmente utilizando MediatR.

---

# Regra Final

O domínio deve ser capaz de existir sem banco, sem API e sem frontend.

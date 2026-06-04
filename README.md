# Rinha das Lendas

Estrutura inicial para um sistema interno de organizacao das partidas da
comunidade. O repositorio esta separado em backend .NET, frontend Vue e
documentacao inicial para evolucao orientada por specs.

## Pre-requisitos

- Docker Desktop em execucao.
- VS Code com a extensao Dev Containers.

O host nao precisa ter .NET ou Node.js instalados. O Dev Container inclui:

- .NET SDK 10;
- Node.js 24 LTS e npm;
- Git, ferramentas basicas de build e cliente PostgreSQL;
- `dotnet-ef`;
- Specify CLI oficial para uso com GitHub Spec Kit.

## Abrir no Dev Container

1. Abra esta pasta no VS Code.
2. Execute `Dev Containers: Reopen in Container`.
3. Aguarde o build do container e a inicializacao do PostgreSQL.

Para subir apenas os containers pelo terminal do host:

```bash
docker compose -f .devcontainer/docker-compose.yml up -d --build
```

## Backend

Dentro do Dev Container:

```bash
make backend-restore
make backend-build
make backend-test
make backend-run
```

A API fica disponivel em:

- `http://localhost:5000/health`
- `http://localhost:5000/swagger`

## Frontend

Dentro do Dev Container:

```bash
make frontend-install
make frontend-run
```

O frontend fica disponivel em `http://localhost:5173`.

## Banco de dados

O PostgreSQL sobe junto com o Dev Container:

```text
host: postgres
port: 5432
database: rinha_das_lendas
user: postgres
password: postgres
```

Para acessar pelo terminal do Dev Container:

```bash
psql -h postgres -U postgres -d rinha_das_lendas
```

Use `postgres` como senha quando solicitado.

## Spec Kit

A pasta `specs/` contem notas iniciais curtas para orientar o refinamento do
produto. Quando quiser ativar o workflow completo do Spec Kit no repositorio,
execute dentro do Dev Container:

```bash
make speckit-init
```

O comando inicializa a integracao do Codex em modo skills e adiciona os
artefatos oficiais do Spec Kit ao repositorio atual. Revise as alteracoes antes
de commitar.

## Proximos passos

1. Abrir e revisar `specs/002-mvp-rinha-das-lendas.md`.
2. Inicializar o workflow completo com `make speckit-init`.
3. Criar a constituicao do projeto pelo Spec Kit.
4. Refinar o MVP antes de implementar entidades, autenticacao ou integracoes.
5. Adicionar o primeiro `DbContext` e a primeira migration somente quando o
   modelo inicial estiver acordado.


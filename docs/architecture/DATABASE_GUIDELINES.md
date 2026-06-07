# Database Guidelines

## Objetivo

Definir padrões de modelagem e persistência para PostgreSQL.

---

# Banco Oficial

Utilizar PostgreSQL.

---

# Chaves Primárias

Utilizar UUID.

Exemplo:

```sql
id UUID PRIMARY KEY
```

---

# Nomenclatura

Tabelas:

```sql
jogadores
partidas
series
preferencias_rotas
```

Colunas:

```sql
nome_exibicao
riot_id
discord_id
data_cadastro
```

Utilizar snake_case.

---

# Auditoria

Todas as tabelas principais devem possuir:

```sql
data_cadastro
data_atualizacao
```

Opcionalmente:

```sql
usuario_cadastro
usuario_atualizacao
```

---

# Exclusão

Preferir exclusão lógica.

Exemplo:

```sql
status
```

ou

```sql
ativo
```

Evitar delete físico.

---

# Foreign Keys

Sempre declarar explicitamente.

Exemplo:

```sql
jogador_id UUID NOT NULL
```

---

# Índices

Criar índices para:

* Foreign Keys
* Campos de busca frequente
* Campos únicos

---

# Constraints

Utilizar constraints sempre que possível.

Exemplo:

```sql
CHECK (prioridade >= 1 AND prioridade <= 5)
```

---

# JSON

Evitar armazenar regras de negócio em JSON.

Preferir modelagem relacional.

Utilizar JSON apenas quando:

* Configuração dinâmica
* Payload externo
* Logs

---

# Migrations

Toda alteração estrutural deve ser realizada via migration.

Nunca alterar banco manualmente.

---

# Entity Framework

Utilizar Fluent API.

Evitar DataAnnotations complexas.

Exemplo:

```csharp
IEntityTypeConfiguration<T>
```

---

# Performance

Evitar:

* SELECT *
* Includes desnecessários
* N+1 queries

Preferir:

* Projeções
* Paginação
* Queries específicas

---

# Integridade

O banco deve proteger os dados mesmo quando a aplicação falhar.

Regras importantes devem existir:

* no domínio;
* na aplicação;
* quando possível, no banco.

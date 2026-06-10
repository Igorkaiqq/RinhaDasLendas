# Commit Messages

Todos os commits devem ser escritos em português brasileiro e seguir o formato:

```text
tipo: descrição curta em português
```

## Tipos permitidos

| Tipo | Uso |
|------|-----|
| `docs` | Documentação, specs, planos, tasks e guias |
| `feat` | Nova funcionalidade de produto |
| `fix` | Correção de bug |
| `refactor` | Reorganização sem mudança de comportamento |
| `test` | Testes automatizados ou ajustes de testes |
| `chore` | Configuração, tooling ou manutenção |

## Bons exemplos

```text
docs: atualizar padrões de branch e commits
docs: gerar tasks da feature de mensagens
feat: adicionar provedor de mensagens localizadas
fix: corrigir fallback de mensagem desconhecida
refactor: centralizar constantes de rotas
test: adicionar testes do catálogo de mensagens
chore: ajustar configuração do eslint
```

## Evitar

```text
feat: implement message catalog
fix: update route validation
docs: update architecture docs
```

## Recomendações

- Use verbo no infinitivo ou substantivo claro: `adicionar`, `corrigir`, `atualizar`, `gerar`.
- Não misture temas distantes no mesmo commit.
- Commits de Spec Kit devem indicar a fase quando fizer sentido: `docs: concluir plano da feature de i18n`.
- Antes de commitar, verifique `git status --short` para evitar incluir mudanças acidentais.

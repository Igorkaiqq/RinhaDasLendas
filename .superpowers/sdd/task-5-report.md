# Relatório da Unidade 5: Autoria e Auditoria Administrativa

## Resultado

- Adicionado `DraftMontagemActor` com factories invariantes para `User` e `System`.
- Preservados construtores e fluxos existentes baseados em `Guid` para compatibilidade.
- DTO administrativo agora expõe `responsavelTipo` e `responsavelUsuarioId` anulável.
- Migration EF `20260731012844_AddDraftMontagemSystemActor` gerada no container.
- Migration revisada com backfill `User`, FK anulável preservada e check constraint para combinações válidas.
- `Down` inicia com guarda SQL explícita: recusa downgrade quando existem ações `System`, antes de qualquer DDL e sem criar GUID fictício.
- O downgrade de schema conclui normalmente quando ainda não existe ação `System`.
- Integração real PostgreSQL/EF/API comprova materialização e JSON de `User` e `System` no endpoint administrativo.
- Frontend apresenta `Sistema`/`System` por i18n e não renderiza ID vazio ou fictício.
- Nenhum arquivo de tasks ou plano foi alterado.

## Evidência TDD

- RED backend: compilação falhou pelos contratos `DraftMontagemActor`, `ResponsavelTipo` e nullability ausentes.
- RED frontend: 2 falhas esperadas, pela chave i18n ausente e pela autoria sistêmica renderizada como responsável vazio.
- RED da correção: o rollback com `System` retornou `23502` incidental em vez da guarda operacional `P0001`.
- GREEN focado backend: 6/6 testes, incluindo os dois caminhos de rollback e EF/API real.
- GREEN focado frontend: 196/196 testes.

## Verificação Completa

- Backend tests Release: 806/806.
- Backend build Release: sucesso, 0 warnings e 0 errors.
- Frontend tests: 556/556.
- Frontend lint: sucesso.
- Frontend build: sucesso.
- EF pending model changes: nenhum.
- `git diff --check`: sucesso.

O build frontend manteve avisos informativos preexistentes de anotação PURE em dependências e chunk acima de 500 kB; não houve erro.

## Auditoria de internacionalização

- Textos hardcoded no frontend encontrados: **Sim, auditado; nenhum novo texto visível hardcoded**.
- Mensagens hardcoded no backend encontradas: **Sim, auditado; nenhuma nova mensagem voltada ao usuário**.
- `pt.json` e `en.json` sincronizados: **Sim**.
- Resources backend atualizados: **Sim, não aplicável; a unidade não introduz mensagens backend**.
- Acentuação em português revisada: **Sim** (`Sistema`).
- Placeholders, botões, títulos, badges, toasts e mensagens vazias revisados: **Sim; não alterados**.
- Validações frontend/backend usam i18n/resources: **Sim; nenhuma nova validação textual foi introduzida**.
- Novos arquivos respeitam o padrão: **Sim**.

## Política de rollback

- Rollback de código pode manter o schema aditivo e forward-compatible desta unidade.
- Downgrade de schema só é permitido antes da persistência da primeira ação com autoria `System`.
- Após existir qualquer ação `System`, a guarda da migration interrompe o `Down` com mensagem operacional explícita e a recuperação deve seguir por roll-forward.
- A política formal será reconciliada na documentação da feature após aprovação, sem alterar tasks ou plans nesta correção.

## Riscos e preocupações

- Não há preocupação bloqueante na unidade; operadores devem respeitar a política de rollback acima.
- O frontend continua com o aviso não bloqueante de bundle principal acima de 500 kB, fora do escopo desta unidade.

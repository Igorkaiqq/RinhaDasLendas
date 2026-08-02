# Relatório da Unidade 7

## Status

Implementado o worker isolado de encerramento automático de presença da feature 029, mantendo a regra de encerrar com ao menos dez confirmações e cancelar abaixo desse limite.

## Entregas

- `DraftMontagemPresenceClosureCandidate` contém somente `Id`.
- O scan EF usa projeção sem tracking e sem `Include`, selecionando somente identificadores.
- O scope do scan termina antes do primeiro comando; cada draft usa um scope de comando novo e descartado antes do item seguinte.
- `EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler` recarrega o agregado e revalida status e prazo antes de agir.
- Drafts com dez ou mais presenças são encerrados; drafts com menos de dez são cancelados com uma ação administrativa atribuída a `DraftMontagemActor.System()`.
- A ordem dos efeitos é persistência, tentativa de publicação realtime pós-commit e métrica de sucesso.
- Conflitos de persistência propagam pelo handler sem publicação nem métrica de sucesso; o worker observa o erro por ID e continua nos itens seguintes.
- Exceções genéricas e cancelamentos não originados pelo host são observados por item e não interrompem o lote; somente cancelamento solicitado pelo host propaga.
- Logs do worker são limitados ao ID do draft e ao nome do tipo da exceção, sem mensagem, payload ou objeto de exceção.
- `Microsoft.EntityFrameworkCore.Database.Command` usa `Warning` na configuração de produção e permanece `Information` em desenvolvimento.

## TDD

- RED confirmado: o filtro falhou na compilação pela ausência de `DraftMontagemPresenceClosureCandidate` e `EncerrarPresencaDraftMontagemAutomaticamenteCommand`.
- GREEN focado: 11 testes aprovados, 0 falhas e 0 ignorados.
- Cobertura focada: contrato e SQL ID-only, scopes e descarte, falha genérica segura, conflito, cancelamentos, continuação, recarga/revalidação, autoria sistêmica, ordem dos efeitos e ausência de publicação após conflito.

## Verificação

- Testes focados Release: 11 aprovados, 0 falhas, 0 ignorados.
- Build Release da solução: aprovado com 0 warnings e 0 erros.
- `git diff --check`: aprovado.
- `tasks.md` e planos não foram alterados.

## Auditoria de internacionalização

- Sim: nenhum texto visível hardcoded foi adicionado ao frontend.
- Sim: nenhuma mensagem de API hardcoded foi adicionada ao backend; os novos textos são exclusivamente logs técnicos estruturados.
- Sim: `pt.json` e `en.json` permanecem sincronizados, pois não foram alterados.
- Sim: resources backend permanecem atualizados, pois nenhuma mensagem de usuário foi criada.
- Sim: a acentuação em português foi revisada neste relatório; não houve novo texto de produto.
- Sim: placeholders, botões, títulos, badges, toasts e estados vazios não foram afetados.
- Sim: validações frontend/backend não receberam mensagens novas e continuam usando i18n/resources.
- Sim: todos os novos arquivos respeitam o padrão de internacionalização.

## Preocupações

- A validação executada foi o gate solicitado de testes focados mais build; a suíte backend completa não foi executada nesta unidade.

# Relatório da Unidade 6

## Status

Implementados candidatos mínimos e processamento isolado do timer de turno da feature 029, preservando o intervalo de 1 segundo e as regras de timeout da feature 028.

## Entregas

- `DraftMontagemRealtimeCandidate` contém somente `Id`.
- O scan usa projeção EF sem tracking e sem `Include`, selecionando somente o identificador dos candidatos.
- O scope do scan termina após materializar os IDs; cada candidato é enviado em um scope de comando independente.
- `ProcessarTurnoDraftMontagemExpiradoCommandHandler` recarrega o agregado e revalida status, modo e expiração antes de agir.
- Timeout continua registrado somente no histórico de escolhas, sem ação administrativa adicional.
- Duração máxima cancela com exatamente uma ação administrativa atribuída a `DraftMontagemActor.System()`.
- Persistência ocorre antes da métrica e da tentativa de publicação realtime pós-commit.
- Exceções genéricas e cancelamentos não originados pelo host são observados por item e não interrompem os candidatos seguintes.
- Somente o cancelamento solicitado pelo host é propagado pelo ciclo de processamento.

## Correções dos findings de testes

- Adicionada sequência explícita que prova `save`, depois `publish`, depois métrica de sucesso.
- Adicionado conflito de concorrência em `SaveChangesAsync`: a mesma exceção propaga pelo handler, sem publisher e sem métrica de sucesso.
- Adicionada cobertura do worker observando o conflito com o ID do item e processando o candidato seguinte.
- Adicionado probe scoped descartável que prova o descarte do scope de scan antes do primeiro envio.
- O mesmo probe prova dois scopes de comando distintos, descarte entre itens e ausência de dependência scoped viva após o lote.
- Nenhum código de produção foi alterado na correção; os findings eram lacunas de evidência sobre comportamento já correto.

## TDD

- RED confirmado: o focused build falhou pela ausência de `DraftMontagemRealtimeCandidate` e `ProcessarTurnoDraftMontagemExpiradoCommand`.
- GREEN focado original: 8 testes aprovados, 0 falhas e 0 ignorados.
- Os testes suplementares dos findings iniciaram verdes porque exercitam comportamento já implementado; nenhuma alteração de produção foi necessária.
- GREEN focado após findings: 11 testes aprovados, 0 falhas e 0 ignorados.
- Cobertura focada: formato do candidato, SQL somente ID, ordem save/publish, conflito sem efeitos pós-commit, observação por item, ciclo de scopes, cancelamento do host, recarga/revalidação, timeout sem auditoria e cancelamento `System` único.

## Verificação

- Testes focados Release: 11 aprovados, 0 falhas, 0 ignorados.
- Build Release da solução: aprovado com 0 warnings e 0 erros.
- `git diff --check`: aprovado.
- `tasks.md` e planos não foram alterados.

## Auditoria de internacionalização

- Sim: nenhum texto visível hardcoded foi adicionado ao frontend.
- Sim: nenhuma mensagem de API hardcoded foi adicionada ao backend; o único texto novo é log técnico estruturado por ID.
- Sim: `pt.json` e `en.json` permanecem sincronizados, pois não foram alterados.
- Sim: resources backend permanecem atualizados, pois nenhuma mensagem de usuário foi criada.
- Sim: a acentuação em português foi revisada neste relatório; não houve novo texto de produto.
- Sim: placeholders, botões, títulos, badges, toasts e estados vazios não foram afetados.
- Sim: validações frontend/backend não receberam mensagens novas e continuam usando os mecanismos existentes de i18n/resources.
- Sim: todos os novos arquivos respeitam o padrão de internacionalização.

## Preocupações

- A validação executada foi o gate solicitado de testes focados mais build; a suíte backend completa não foi executada nesta unidade.

# Specs and Planning

O projeto usa Specification Driven Development com Spec Kit. A sequência obrigatória é:

1. Constitution
2. Specify
3. Plan
4. Tasks
5. Implement

Nenhuma implementação deve começar antes das tasks estarem geradas e aprovadas.

## Constituição

A Constituição em `.specify/memory/constitution.md` define os princípios obrigatórios. Qualquer feature deve responder se:

- Resolve um problema real do grupo.
- Funciona sem integração externa obrigatória.
- Mantém regras críticas testáveis.
- Respeita separação backend, frontend e domínio.
- Ajuda o MVP sem sobreengenharia.

## Specify

A especificação deve registrar histórias de usuário, prioridades, critérios de aceitação, requisitos funcionais, edge cases e assumptions. Itens com `[NEEDS CLARIFICATION]` devem ser resolvidos antes do plano.

## Plan

O plano transforma a spec em decisões técnicas, estrutura de arquivos, fases, dependências e validações. Ele deve apontar para os documentos de arquitetura em `docs/architecture/` e para design em `docs/design/` quando houver frontend.

## Tasks

As tasks devem ser pequenas, independentes e organizadas por história. Cada task deve indicar área afetada, como `[Docs]`, `[Backend]` ou `[Frontend]`, e conter caminho de arquivo.

## Implement

A implementação deve seguir a ordem das phases e marcar cada task concluída com `[X]` em `tasks.md`. Testes descritos na história devem ser criados antes da implementação correspondente.

## Validação mínima por tipo de mudança

| Tipo | Validação |
|------|-----------|
| Docs | Revisão de links, caminhos e aderência ao AGENTS.md |
| Backend | `dotnet build` e `dotnet test` quando houver código |
| Frontend | `npm run lint` e `npm run build` quando houver código |
| Mensagens | Código existe em `docs/messages/message-catalog.md` antes de uso |

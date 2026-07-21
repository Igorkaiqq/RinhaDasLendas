# Relatorio da Task 13

## Status

Concluida. T105 e T106 foram marcadas como finalizadas.

## Implementacao

- Separada a projecao publica de drafts, sem auditoria, motivos, executor/alvo, IDs operacionais do Discord, IDs de claim, IDs de publicacao ou Discord user IDs.
- Adicionada a projecao administrativa completa e o endpoint protegido `GET /api/v1/draft-montagens/{id}/administracao` com `CanManageDrafts`.
- Adicionada a projecao operacional minima do bot para listagem ativa, confirmacao/cancelamento de presenca e conclusao/falha de publicacao.
- Mantida a projecao publica no SignalR, sem reutilizar o DTO administrativo.
- Publicacoes publicas serializam somente `tipo` e `status`.
- Separadas queries e handlers CQRS para leitura administrativa e operacional.
- Removidos IDs Discord da listagem publica.
- Modelados service e types administrativos no frontend.
- Ajustada `DraftsView` para usar somente o endpoint administrativo quando autorizada, usar somente o endpoint publico para jogador comum e fazer fallback publico em `403`, ocultando acoes administrativas.
- Preservados status de publicacao e republicacao para administradores.
- Recarregada a projecao administrativa apos realtime e mutacoes, com descarte de respostas obsoletas e fallback publico unico em `403`.
- Adicionada defesa arquitetural para as 17 superficies publicas detalhadas e testes de serializacao dos DTOs publico e realtime.
- Reduzido o contrato operacional do bot aos campos comprovadamente consumidos por polling, embeds e edicao da mensagem de presenca.
- Testados os shapes do bot em listagem, confirmacao/cancelamento de presenca e conclusao/falha de publicacao.
- Mantidos os paths e payloads consumidos pelo bot.

## TDD

- RED frontend: 3 testes falharam pela ausencia de service e fluxo administrativo; 23 passaram.
- GREEN frontend focado: 26 testes passaram.
- GREEN backend focado: 27 testes passaram cobrindo jogador, administrador, bot e serializacao.
- RED da revisao: testes falharam para timestamps publicos, campos extras do bot e perda da projecao administrativa.
- GREEN da revisao: 48 testes backend focados e 15 testes da view passaram; o contrato minimo do bot foi validado por testes e build TypeScript.

## Verificacoes

- Backend completo: 231 testes aprovados.
- Backend build Release: aprovado.
- Frontend completo: 80 testes aprovados.
- Frontend build: aprovado.
- Bot completo: 47 testes aprovados.
- Bot build: aprovado.
- EF `has-pending-model-changes`: nenhuma alteracao pendente.
- `git diff --check`: aprovado.

## Auditoria de internacionalizacao

- Textos hardcoded novos no frontend: Nao encontrados.
- Mensagens hardcoded novas no backend: Nao encontradas.
- `pt.json` e `en.json` sincronizados: Sim, validado pela suite frontend.
- Recursos backend atualizados: Sim; nenhuma mensagem nova exigiu chave adicional.
- Acentuacao em portugues revisada: Sim; nenhum texto visivel novo foi adicionado.
- Placeholders, botoes, titulos, badges, toasts e estados vazios revisados: Sim; nenhum texto novo foi adicionado.
- Validacoes frontend/backend usam i18n/resource: Sim; nenhuma validacao nova foi adicionada.
- Novos arquivos respeitam o padrao: Sim.

## Concerns

- Esta task valida apenas os acessos jogador/administrador/bot necessarios aos contratos alterados. A matriz completa de autenticacao, roles, esquemas, `401` e `403` permanece deliberadamente na Task 15.
- O restore do backend continua reportando a vulnerabilidade conhecida `NU1903` em `Microsoft.OpenApi` 2.4.1; nao foi introduzida por esta task.
- O build frontend continua reportando o aviso existente de chunk principal acima de 500 kB.

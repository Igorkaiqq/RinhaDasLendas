# Relatório da Task 5

## Status

Concluída, incluindo os três findings da revisão.

## Entregas

- Adicionado `PATCH /api/v1/draft-montagens/{id}/modo` com o command existente e resposta pública.
- Adicionadas as policies `CanManageDraftCycle` e `CanCreateDraftPresenceOrManageCycle`.
- Restringidas escolha de modo, capitães, ordem, início, layout, substituição, sorteio e finalização a `Admin` e `SuperAdmin`.
- Preservadas as policies de presença, `ClosePresence`, cancelamento e pick.
- Permitida criação de presença pelo bot e rejeitada criação direta com jogadores no handler por `MV110`.
- Vinculada a permissão de bot ao scheme interno real, impedindo que um JWT com scope forjado seja tratado como bot.
- Adicionados `Modo` anulável e `CicloVersao` aos contratos público, administrativo e resumido.
- Mantido `CapitaesElegiveisIds` somente na projeção administrativa, sem exposição no DTO público.
- Restringida a projeção administrativa com `CanManageDraftCycle`: Moderador recebe 403; Admin e SuperAdmin alcançam o handler.
- Tornado o bypass de autenticação do ambiente `Testing` opt-in por `TestHost:EnableAuthenticationBypass`, com default `false` e falha de startup sem habilitação explícita.
- Habilitado o bypass somente nas factories de teste que usam `Testing`; `IntegrationTesting` continua exercitando autenticação JWT/bot real.
- Completado o contrato Swagger do endpoint de modo com 401, 403 e 409, além de 200, 400 e 404.
- Adicionadas e verificadas mensagens `MV107` a `MV110` em PT-BR e EN-US nos três resources.

## Evidências TDD

- RED inicial: falha de compilação por endpoint, identidade de bot e contratos ausentes.
- GREEN focado: 135 testes aprovados para contratos, handlers, segurança e cobertura de endpoint.
- RED de revisão: JWT de Jogador com scope de bot recebeu `201 Created` indevidamente.
- GREEN de revisão: 11 testes de autenticação/autorização aprovados após vincular scope ao scheme interno.
- RED dos findings finais: Moderador alcançou a projeção administrativa, `Testing` iniciou sem opt-in e o inventário expôs somente 200/400/404.
- GREEN dos findings finais: 5 testes focados aprovados para policy, startup e Swagger.
- Segurança focada: 87 testes aprovados, incluindo autenticação real em `IntegrationTesting`.
- Backend completo: 662 testes aprovados, 0 falhas, 0 ignorados.
- Build Release: aprovado com 0 warnings e 0 erros.

## Matriz Validada

| Identidade | Operações do ciclo | Create presença | Create direto |
|---|---:|---:|---:|
| Anônimo | 401 | 401 | 401 |
| Jogador | 403 | 403 | 403 |
| Moderador | 403 | 403 | 403 |
| Bot | 403 | 201 | 400 / MV110 |
| Admin | 200 no fluxo válido | 201 | 201 |
| SuperAdmin | 200 no fluxo válido | 201 | 201 |

## Auditoria de Internacionalização

- Textos hardcoded no frontend encontrados: Não; frontend não foi alterado.
- Mensagens hardcoded novas no backend encontradas: Não.
- `pt.json` e `en.json` sincronizados: Sim; não foram alterados.
- Resources backend atualizados e sincronizados: Sim, incluindo `ME045` em PT-BR e EN-US.
- Acentuação do português revisada: Sim.
- Placeholders, botões, títulos, badges, toasts e estados vazios revisados: Sim; não houve alteração frontend.
- Validações frontend/backend usam i18n/resource: Sim; `MV110` usa `MessageCodes` e resources.
- Novos arquivos respeitam o padrão: Sim.

## Preocupações

Nenhuma preocupação bloqueante. O bypass não está presente em appsettings, segredo ou variável de produção; ele só é considerado no ambiente `Testing` e exige opt-in explícito do test host.

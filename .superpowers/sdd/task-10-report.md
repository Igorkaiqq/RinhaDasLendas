# Task 10 Report

## Status

- T097 e T098 concluídas.
- Autorização runtime de comandos mutáveis permanece antes da consulta de configuração.
- Comandos e botão de status read-only não consultam `botEnabled`.
- Claim persistido, polling e fronteira de resultado desconhecido foram preservados.

## RED

Comando:

```bash
npm test -- src/modules/drafts/draftInteractions.spec.ts
```

Resultado inicial: 36 testes aprovados e 5 falhos. As falhas comprovaram:

- código único para ausência de qualquer permissão;
- `permissionsFor=null` permitindo envio;
- resposta genérica quando `botEnabled=false`;
- confirmação consultando vínculo e alcançando mutação antes da guarda;
- ausência das mensagens específicas em pt-BR/en-US.

## GREEN

Comando focado após a implementação:

```bash
npm test -- src/modules/drafts/draftInteractions.spec.ts
```

Resultado: 41 testes aprovados e 0 falhos. O script do projeto inclui todas as specs do bot e acrescenta o caminho informado.

Build intermediário:

```bash
npm run build
```

Resultado: TypeScript compilado sem erros.

## Follow-up de revisão

### RED

Após os findings Medium/Low, a execução focada registrou 43 testes aprovados e 2 falhos. As falhas esperadas comprovaram que:

- resolução indeterminada de permissões ainda era reportada como ausência de `ViewChannel`;
- não existiam código nem mensagem localizada próprios para esse estado.

Os novos testes de falha de configuração, argumentos reais e CTA positivo já passaram no RED, confirmando lacunas de cobertura/fixture sem exigir alteração adicional de produção nesses fluxos.

### GREEN

- `permissionsFor=null`, `client.user=null` e ausência de `permissionsFor` agora geram `DiscordChannelPermissionsUnknownError` antes de qualquer envio.
- A mensagem específica `indeterminateChannelPermissions` foi adicionada em pt-BR e en-US.
- Falhas de rede e `MV079` ao obter configuração foram testadas nas cinco mutações de comando e nos dois botões, sempre com zero mutações e zero consultas de vínculo.
- Fixtures usam `DraftOptionNames.DraftId` (`draft_id`) e validam ID, motivo, capitães, modo e ordem enviados às APIs.
- O caminho positivo com cargo envia a mensagem principal e o CTA com `allowedMentions.roles` exato; times finais continuam com um único envio.
- Os testes de claim e resultado desconhecido permanecem cobrindo a fronteira anterior/depois do primeiro `send`.
- T097 e T098 permanecem marcadas como concluídas.

## Implementação

- Guarda única obtém configuração habilitada para create/cancel/close/captains/order e para confirmar/cancelar presença.
- Confirmação rejeita integração desabilitada antes de `getDiscordLink`.
- CTA exige `MentionEveryone` somente com `DRAFT_NOTIFY_ROLE_ID` configurado.
- Publicação de times finais exige view/send/embed, nunca mention.
- `permissionsFor=null` falha de forma segura antes do envio.
- Erros internos distintos: `DiscordChannelViewPermissionError`, `DiscordChannelSendPermissionError`, `DiscordChannelEmbedPermissionError` e `DiscordChannelMentionPermissionError`.
- Validação de canal/permissão permanece antes do primeiro `send` e pode registrar `Falha`.
- Depois que `send` começa, rejeição de envio ou falha de conclusão mantém resultado desconhecido e não registra `Falha`.

## Auditoria de internacionalização

- Textos hardcoded novos no frontend: Não encontrados.
- Mensagens hardcoded novas no backend: Não encontradas.
- `pt.json` e `en.json`: Não alterados; sincronização fora do escopo preservada.
- Recursos backend: Não alterados; nenhuma mensagem backend foi adicionada.
- Mensagens do bot pt-BR/en-US: Chaves novas em paridade.
- Acentuação pt-BR: Revisada.
- Placeholders, botões, títulos, badges, toasts e estados vazios: Não alterados.
- Validações frontend/backend com i18n/resources: Não alteradas.
- Novos arquivos: Este relatório não contém texto exibido pela aplicação.

## Verificação final

- `npm test`: 45 testes aprovados, 0 falhos.
- `npm run build`: concluído sem erros.
- Paridade recursiva pt-BR/en-US: 137 chaves em cada locale, nenhuma ausente.
- `git diff --check`: concluído sem erros.

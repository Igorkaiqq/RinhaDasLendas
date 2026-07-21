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

- `npm test`: 41 testes aprovados, 0 falhos.
- `npm run build`: concluído sem erros.
- Paridade recursiva pt-BR/en-US: 136 chaves em cada locale, nenhuma ausente.
- `git diff --check`: concluído sem erros.

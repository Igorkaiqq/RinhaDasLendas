# I18N Guidelines

A internacionalização começa com `pt-BR` como locale padrão e `en-US` como segundo locale suportado.

## Princípios

- Texto visível ao usuário deve ser planejado como chave de tradução ou código de mensagem.
- Backend é fonte de verdade para mensagens de API e validação de regra de negócio.
- Frontend é responsável por exibir labels, navegação, botões e feedback visual com chaves localizadas.
- O sistema deve ter fallback claro quando uma chave ou código não existir.

## Backend

- Códigos ficam em `MessageCodes.cs`.
- Textos ficam em `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/*.resx`.
- `IMessageProvider` resolve código e cultura.
- Não hardcode mensagens em controllers, handlers ou validators quando houver código no catálogo.

## Frontend

Estrutura planejada:

```text
FrontEnd/src/i18n/
├── index.ts
└── locales/
    ├── pt-BR.json
    └── en-US.json
```

Use nomes de chave por domínio:

```json
{
  "navigation": {
    "players": "Jogadores"
  },
  "messages": {
    "MSIS001": "Operação realizada com sucesso"
  }
}
```

## Fallback

- Locale padrão: `pt-BR`.
- Se `en-US` não tiver uma chave, usar `pt-BR`.
- Se um código de mensagem não existir, exibir fallback legível como `[MV999]` e registrar warning técnico quando houver infraestrutura para isso.

## Checklist para novo texto

1. O texto é feedback de operação ou erro? Registre em `docs/messages/message-catalog.md`.
2. O texto é label, menu, botão ou título? Registre no arquivo de locale frontend.
3. O texto aparece no backend? Use `IMessageProvider`.
4. O texto aparece no frontend? Use serviço de mensagens ou i18n.
5. Adicione validação ou teste quando o texto fizer parte de fluxo crítico.

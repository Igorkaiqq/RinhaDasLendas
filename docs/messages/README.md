# Messages

Este diretório é a fonte de verdade para mensagens visíveis ao usuário no RinhaDasLendas.

## Documentos

| Documento | Uso |
|-----------|-----|
| [message-catalog.md](message-catalog.md) | Lista completa de códigos, textos, categorias e contexto |
| [message-codes.md](message-codes.md) | Referência rápida, ranges e processo de manutenção |

## Formato do código

```text
PREFIXO + número de 3 dígitos
```

Exemplos: `MI001`, `MSIS001`, `MV001`, `ME001`, `MC001`, `MA001`.

## Categorias

| Prefixo | Categoria | Uso |
|---------|-----------|-----|
| `MI` | Info | Carregamento e estado neutro |
| `MSIS` | Success | Operações concluídas |
| `ME` | Error | Falhas inesperadas e erros de sistema |
| `MV` | Validation | Validação de entrada |
| `MC` | Confirmation | Confirmações de ação |
| `MA` | Alert | Avisos e atenção |

## Regras de manutenção

- Todo código usado em backend ou frontend deve existir primeiro em `message-catalog.md`.
- Códigos publicados são imutáveis: altere texto ou marque deprecado, mas não reutilize o código para outro significado.
- Toda mensagem deve ter texto `pt-BR`, texto `en-US`, categoria, contexto e severidade.
- Novos códigos usam o próximo número livre dentro da categoria.
- Mensagens técnicas sensíveis não devem ser expostas ao usuário final.

## Implementação planejada

- Backend: códigos em `MessageCodes.cs`, textos em `.resx`, acesso por `IMessageProvider`.
- Frontend: códigos tipados em `FrontEnd/src/constants/messageCode.ts`, textos em i18n JSON e acesso por `messageService.ts`.

# Message Codes

Referência rápida para códigos de mensagens do RinhaDasLendas.

## Ranges oficiais

| Prefixo | Categoria | Range ativo | Uso |
|---------|-----------|-------------|-----|
| `MI` | Info | `MI001` a `MI009` | Estados neutros, carregamento e ausência de dados |
| `MSIS` | Success | `MSIS001` a `MSIS009` | Operações concluídas com sucesso |
| `ME` | Error | `ME001` a `ME020` | Erros inesperados, falhas de sistema e recursos ausentes |
| `MV` | Validation | `MV001` a `MV019` | Validações de entrada e regras de formulário |
| `MC` | Confirmation | `MC001` a `MC009` | Confirmações antes de ações relevantes |
| `MA` | Alert | `MA001` a `MA009` | Avisos e estados que exigem atenção |

## Códigos principais

| Código | Significado |
|--------|-------------|
| `MSIS001` | Operação realizada com sucesso |
| `MSIS002` | Jogador criado com sucesso |
| `MV001` | Campo obrigatório |
| `MV008` | Prioridades de rota devem ser únicas |
| `ME001` | Ocorreu um erro inesperado |
| `ME003` | Jogador não encontrado |
| `MC002` | Confirmar inativação de jogador |
| `MA001` | Existem alterações não salvas |

## Como registrar novo código

1. Escolha a categoria correta.
2. Use o próximo número livre do range.
3. Adicione entrada em `docs/messages/message-catalog.md` com `pt-BR`, `en-US`, contexto e severidade.
4. Adicione constante no backend ou frontend somente na fase de implementação correspondente.
5. Nunca reutilize código antigo para outro significado.

## Imutabilidade

Depois que um código aparece em release, ele passa a ser estável. O texto pode ser ajustado para clareza, mas o significado funcional do código não deve mudar.

## Depreciação

Quando uma mensagem deixar de ser usada:

- Mantenha a entrada no catálogo.
- Marque como deprecada em uma coluna futura ou nota de manutenção.
- Crie um novo código se o significado novo for diferente.
- Não remova o código enquanto clientes antigos puderem recebê-lo.

## Backend e frontend

- Backend referencia códigos por constantes e resolve texto via `.resx`.
- Frontend referencia códigos por enum/constante e resolve texto via i18n ou serviço de mensagens.
- O catálogo em Markdown é a referência para ambos.

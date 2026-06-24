# I18N Guidelines

A internacionalização começa com `pt` como locale padrão e `en` como segundo locale suportado.

## Regra obrigatória

Toda mensagem, label, botão, título, placeholder, tooltip, erro, confirmação, status, badge, texto vazio e qualquer outro texto visível ao usuário deve ser internacionalizado.

É proibido deixar texto hardcoded no front-end ou no back-end.

Toda nova chave ou mensagem deve existir em português e inglês. O português deve conter acentuação correta.

## Princípios

- Texto visível ao usuário deve ser planejado como chave de tradução ou código de mensagem.
- Backend é fonte de verdade para mensagens de API e validação de regra de negócio.
- Frontend é responsável por exibir labels, navegação, botões e feedback visual com chaves localizadas.
- O sistema deve ter fallback claro quando uma chave ou código não existir.

## Backend

- Códigos ficam em `MessageCodes.cs`.
- Textos ficam em `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/*.resx`.
- `IMessageProvider` resolve código e cultura.
- Não use mensagens hardcoded em exceptions, validators, handlers, endpoints, middlewares, responses ou domain notifications.
- Toda mensagem retornada pela API deve vir de resource `.resx` ou estrutura equivalente de localização.
- Toda chave nova deve existir em português e inglês.

Exemplo proibido:

```csharp
"Jogador não encontrado"
"Nome é obrigatório"
"Erro ao criar jogador"
```

Exemplo obrigatório:

```csharp
_localizer["PlayerNotFound"]
_localizer["PlayerNameRequired"]
_localizer["PlayerCreateError"]
```

## Frontend

Estrutura obrigatória de locales:

```text
FrontEnd/src/i18n/
├── index.ts
└── locales/
    ├── pt.json
    └── en.json
```

Toda tela, componente, modal, toast, tabela, formulário e validação visual deve usar chaves i18n. Toda nova chave criada em `pt.json` deve ter equivalente em `en.json`.

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

Exemplo proibido:

```ts
"Salvar"
"Cancelar"
"Jogador criado com sucesso"
"Erro ao carregar jogadores"
```

Exemplo obrigatório:

```ts
t('players.actions.save')
t('common.actions.cancel')
t('players.messages.createdSuccess')
t('players.errors.loadFailed')
```

## Fallback

- Locale padrão: `pt`.
- Se `en` não tiver uma chave, usar `pt`.
- Se um código de mensagem não existir, exibir fallback legível como `[MV999]` e registrar warning técnico quando houver infraestrutura para isso.

## Checklist para novo texto

1. O texto é feedback de operação ou erro? Registre em `docs/messages/message-catalog.md`.
2. O texto é label, menu, botão ou título? Registre no arquivo de locale frontend.
3. O texto aparece no backend? Use `IMessageProvider`.
4. O texto aparece no frontend? Use serviço de mensagens ou i18n.
5. Adicione validação ou teste quando o texto fizer parte de fluxo crítico.

## Auditoria obrigatória antes de finalizar

Antes de finalizar qualquer implementação, investigue e informe:

1. Se há textos hardcoded no front-end.
2. Se há textos hardcoded no back-end.
3. Se toda chave do `pt.json` existe no `en.json`.
4. Se toda mensagem do back-end tem resource correspondente.
5. Se há textos em português sem acento.
6. Se placeholders, botões, títulos, badges, toasts e mensagens vazias foram revisados.
7. Se validações do front-end e back-end usam i18n/resource.
8. Se arquivos novos respeitam o padrão.

Comandos sugeridos:

```bash
grep -RIn --exclude-dir=node_modules --exclude-dir=dist --exclude="*.json" -E '"[A-Za-zÀ-ÿ ]{3,}"|'\''[A-Za-zÀ-ÿ ]{3,}'\''' FrontEnd/src
grep -RIn --exclude-dir=bin --exclude-dir=obj -E '"[A-Za-zÀ-ÿ ]{3,}"' BackEnd/src
grep -RIn -E "Usuario|Configuracao|Exclusao|Acao|Informacao|Confirmacao|Operacao|Criacao|Atualizacao|Nao|Esta|Voce|Pagina|Obrigatorio|Autenticacao|Autorizacao" FrontEnd/src BackEnd/src
```

Toda resposta final de implementação deve conter:

```md
## Auditoria de internacionalização

- Front-end sem textos hardcoded: Sim/Não
- Back-end sem mensagens hardcoded: Sim/Não
- pt/en sincronizados: Sim/Não
- Resources do back-end atualizados: Sim/Não
- Revisão de acentuação feita: Sim/Não
- Pendências encontradas:
  - ...
```

Se algum item estiver como “Não”, a tarefa não pode ser considerada concluída.

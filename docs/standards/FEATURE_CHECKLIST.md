# Feature Development Checklist

Use este checklist em toda feature para garantir que mensagens, traduções, constantes e validações sejam atualizadas antes da revisão.

## Antes de implementar

- [ ] Confirmar que a branch segue `feature/NNN-slug` e não está em `main`.
- [ ] Confirmar que a sequência Constitution, Specify, Plan, Tasks e Implement foi seguida.
- [ ] Ler os documentos relevantes em `docs/architecture/` e `docs/design/` quando a feature tocar backend ou frontend.
- [ ] Garantir que as tasks aprovadas dizem claramente se alteram Docs, Backend ou Frontend.

## Mensagens

- [ ] Registrar todo novo feedback de operação, erro, alerta, confirmação ou validação em `docs/messages/message-catalog.md` antes de usar no código.
- [ ] Usar o próximo código livre da categoria correta (`MI`, `MSIS`, `ME`, `MV`, `MC`, `MA`).
- [ ] Preencher texto em português, texto em inglês, categoria, severidade e contexto.
- [ ] Atualizar `docs/messages/message-codes.md` quando a inclusão mudar a referência rápida ou política de uso.
- [ ] Adicionar o código em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs` e/ou `FrontEnd/src/constants/messageCode.ts` quando a feature usar o código em runtime.
- [ ] Adicionar textos backend nos arquivos `.resx` correspondentes quando a mensagem vier da API.

## Traduções frontend

- [ ] Adicionar todo texto visível de interface em `FrontEnd/src/i18n/locales/pt.json`.
- [ ] Adicionar a mesma chave em `FrontEnd/src/i18n/locales/en.json`.
- [ ] Usar nomes de chave por domínio, como `navigation.players` ou `playerForm.errors.nameRequired`.
- [ ] Não usar labels, placeholders, botões, títulos, badges, toasts ou mensagens vazias hardcoded em componentes Vue.
- [ ] Validar troca de locale quando a feature alterar telas existentes.

## Constantes, enums e tipos

- [ ] Centralizar valores fechados antes de reutilizá-los em mais de um arquivo.
- [ ] Usar `BackEnd/src/RinhaDasLendas.Domain/Constants/` para constantes de domínio sem dependência de infraestrutura.
- [ ] Usar `BackEnd/src/RinhaDasLendas.Domain/Enums/` para conjuntos fechados de domínio.
- [ ] Usar `FrontEnd/src/constants/` para rotas, status, roles, códigos e outros valores estáveis do frontend.
- [ ] Usar `FrontEnd/src/types/` para union types compartilhados.
- [ ] Substituir magic strings nos arquivos tocados pela feature quando existir constante equivalente.

## Validação antes do review

- [ ] Rodar `dotnet build BackEnd/RinhaDasLendas.sln` quando a feature tocar backend.
- [ ] Rodar `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj` quando a feature tocar regras, validators ou serviços backend.
- [ ] Rodar `npm run lint --prefix FrontEnd` quando a feature tocar frontend.
- [ ] Rodar `npm run test --prefix FrontEnd` quando a feature adicionar ou alterar comportamento testado no frontend.
- [ ] Rodar `npm run build --prefix FrontEnd` quando a feature tocar frontend.
- [ ] Verificar que todo código de mensagem usado em `BackEnd/src` ou `FrontEnd/src` existe em `docs/messages/message-catalog.md`.
- [ ] Confirmar que novos docs estão linkados em `docs/standards/README.md` ou no índice mais próximo.
- [ ] Auditar textos hardcoded no front-end.
- [ ] Auditar mensagens hardcoded no back-end.
- [ ] Confirmar que toda chave de `pt.json` existe em `en.json`.
- [ ] Confirmar que toda mensagem do back-end tem resource correspondente em português e inglês.
- [ ] Revisar acentuação de textos em português.
- [ ] Confirmar que validações do front-end e back-end usam i18n/resource.

## Antes do commit

- [ ] Revisar `git diff --check`.
- [ ] Revisar `git status --short` para separar mudanças não relacionadas.
- [ ] Usar mensagem de commit semântica em português brasileiro.

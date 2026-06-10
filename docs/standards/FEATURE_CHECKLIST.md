# Feature Development Checklist

Use este checklist em toda feature para garantir que mensagens, traducoes, constantes e validacoes sejam atualizadas antes da revisao.

## Antes de implementar

- [ ] Confirmar que a branch segue `feature/NNN-slug` e nao esta em `main`.
- [ ] Confirmar que a sequencia Constitution, Specify, Plan, Tasks e Implement foi seguida.
- [ ] Ler os documentos relevantes em `docs/architecture/` e `docs/design/` quando a feature tocar backend ou frontend.
- [ ] Garantir que as tasks aprovadas dizem claramente se alteram Docs, Backend ou Frontend.

## Mensagens

- [ ] Registrar todo novo feedback de operacao, erro, alerta, confirmacao ou validacao em `docs/messages/message-catalog.md` antes de usar no codigo.
- [ ] Usar o proximo codigo livre da categoria correta (`MI`, `MSIS`, `ME`, `MV`, `MC`, `MA`).
- [ ] Preencher texto `pt-BR`, texto `en-US`, categoria, severidade e contexto.
- [ ] Atualizar `docs/messages/message-codes.md` quando a inclusao mudar a referencia rapida ou politica de uso.
- [ ] Adicionar o codigo em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs` e/ou `FrontEnd/src/constants/messageCode.ts` quando a feature usar o codigo em runtime.
- [ ] Adicionar textos backend nos arquivos `.resx` correspondentes quando a mensagem vier da API.

## Traducoes frontend

- [ ] Adicionar todo texto visivel de interface em `FrontEnd/src/i18n/locales/pt-BR.json`.
- [ ] Adicionar a mesma chave em `FrontEnd/src/i18n/locales/en-US.json`.
- [ ] Usar nomes de chave por dominio, como `navigation.players` ou `playerForm.errors.nameRequired`.
- [ ] Evitar labels, placeholders, botoes e titulos hardcoded em componentes Vue.
- [ ] Validar troca de locale quando a feature alterar telas existentes.

## Constantes, enums e tipos

- [ ] Centralizar valores fechados antes de reutiliza-los em mais de um arquivo.
- [ ] Usar `BackEnd/src/RinhaDasLendas.Domain/Constants/` para constantes de dominio sem dependencia de infraestrutura.
- [ ] Usar `BackEnd/src/RinhaDasLendas.Domain/Enums/` para conjuntos fechados de dominio.
- [ ] Usar `FrontEnd/src/constants/` para rotas, status, roles, codigos e outros valores estaveis do frontend.
- [ ] Usar `FrontEnd/src/types/` para union types compartilhados.
- [ ] Substituir magic strings nos arquivos tocados pela feature quando existir constante equivalente.

## Validacao antes do review

- [ ] Rodar `dotnet build BackEnd/RinhaDasLendas.sln` quando a feature tocar backend.
- [ ] Rodar `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj` quando a feature tocar regras, validators ou servicos backend.
- [ ] Rodar `npm run lint --prefix FrontEnd` quando a feature tocar frontend.
- [ ] Rodar `npm run test --prefix FrontEnd` quando a feature adicionar ou alterar comportamento testado no frontend.
- [ ] Rodar `npm run build --prefix FrontEnd` quando a feature tocar frontend.
- [ ] Verificar que todo codigo de mensagem usado em `BackEnd/src` ou `FrontEnd/src` existe em `docs/messages/message-catalog.md`.
- [ ] Confirmar que novos docs estao linkados em `docs/standards/README.md` ou no indice mais proximo.

## Antes do commit

- [ ] Revisar `git diff --check`.
- [ ] Revisar `git status --short` para separar mudancas nao relacionadas.
- [ ] Usar mensagem de commit semantica em portugues brasileiro.

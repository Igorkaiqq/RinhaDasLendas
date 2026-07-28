# Quickstart: Validar Reabertura de Presença

## Pré-requisitos

- Devcontainer `rinhadaslendas_devcontainer-app-1` ativo.
- Frontend com dependências instaladas.
- Usuários de teste com e sem `CanManageDrafts`.

## Verificação automatizada

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

```bash
cd FrontEnd
npm run lint:check
npm test
npm run build
```

Resultado esperado: todos os comandos terminam com código zero.

## Cenário 1: corrigir encerramento acidental

1. Abrir um draft com 19 confirmações.
2. Encerrar presença e confirmar `3 times`, `4 reservas` e exigência de `3 capitães`.
3. Acionar `Reabrir presença` e confirmar no diálogo.
4. Verificar as 19 confirmações preservadas e os controles de presença novamente disponíveis.
5. Aguardar mais de 30 segundos e confirmar que a presença continua aberta.
6. Adicionar ou confirmar o vigésimo jogador.
7. Encerrar novamente e confirmar `4 times`, `0 reservas` e exigência de `4 capitães`.

## Cenário 2: prosseguir com 19

1. Encerrar presença com 19 jogadores e times de cinco.
2. Selecionar um capitão e confirmar que o botão e a linha ficam destacados enquanto `aria-pressed=true`.
3. Desmarcar o mesmo capitão e confirmar que ambos os destaques desaparecem enquanto `aria-pressed=false`.
4. Selecionar três capitães e observar `3 / 3 capitães`.
5. Definir capitães e ordem.
6. Iniciar o modo escolhido.
7. Confirmar que quatro jogadores permanecem como reservas.

## Cenário 3: autorização e estados inválidos

1. Como jogador comum, confirmar que a ação não aparece e que chamada direta recebe negação.
2. Como Moderador+, confirmar que a ação aparece somente em `Presença encerrada`.
3. Após definir capitães, confirmar que a ação desaparece e chamada direta é recusada sem alteração.
4. Confirmar o mesmo para draft cancelado, finalizado ou arquivado.

## Cenário 4: internacionalização e acessibilidade

1. Repetir abertura e confirmação do diálogo em português e inglês.
2. Confirmar equivalência de título, descrição, botões, contagem, sucesso e erro.
3. Operar por teclado em desktop e em viewport mobile, verificando foco inicial, retorno de foco e ausência de overflow horizontal.
4. Em desktop e mobile, confirmar que hover e foco não ocultam o estado selecionado e que cada controle mantém área mínima de toque.

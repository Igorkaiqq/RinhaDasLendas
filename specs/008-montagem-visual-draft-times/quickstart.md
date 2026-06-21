# Quickstart: Montagem Visual de Draft e Times

## Prerequisites

- Dev Container em execucao.
- PostgreSQL disponivel.
- Backend executando migrations.
- Frontend apontando para API local.
- Pelo menos 20 jogadores ativos cadastrados com preferencias de rota.

## Validation Scenario 1: Criar montagem com 15 jogadores

1. Abrir a tela de drafts.
2. Iniciar `Nova montagem visual`.
3. Selecionar 15 jogadores ativos.
4. Informar tamanho da equipe como 5.
5. Validar resumo antes de criar:
   - 3 times.
   - 3 capitaes obrigatorios.
   - 0 reservas.
6. Selecionar ou sortear capitaes.
7. Criar montagem.
8. Confirmar que board abre com 3 times e area de jogadores livres.

## Validation Scenario 2: Criar montagem com reservas

1. Selecionar 18 jogadores ativos.
2. Informar tamanho da equipe como 5.
3. Validar resumo:
   - 3 times.
   - 3 capitaes.
   - 3 reservas.
4. Criar montagem.
5. Confirmar que a area `Reservas` aparece com 3 jogadores.

## Validation Scenario 3: Drag and drop sem duplicidade

1. Em uma montagem aberta, arrastar jogador livre para Time 1.
2. Confirmar que jogador desaparece dos livres.
3. Arrastar o mesmo jogador para Time 2.
4. Confirmar que jogador aparece apenas no Time 2.
5. Tentar arrastar jogador para time completo.
6. Confirmar bloqueio com mensagem clara.

## Validation Scenario 4: Clique abre detalhes e drag nao abre

1. Clicar em um card sem mover o mouse.
2. Confirmar abertura do drawer/modal com dados completos.
3. Fechar drawer/modal.
4. Arrastar o mesmo card para outro local.
5. Confirmar que drawer/modal nao abre.

## Validation Scenario 5: Salvar, recarregar e finalizar

1. Montar times manualmente.
2. Selecionar rotas contextuais para alguns jogadores.
3. Salvar layout.
4. Recarregar a pagina.
5. Confirmar que times, reservas, livres, capitaes e rotas foram preservados.
6. Finalizar montagem.
7. Confirmar que board fica read-only.

## Validation Scenario 6: Exportar imagem

1. Abrir montagem com times e reservas.
2. Acionar exportacao.
3. Confirmar que imagem baixada inclui nome da montagem, times, capitaes, jogadores, reservas e data.
4. Confirmar que botoes e filtros nao aparecem na imagem.

## Suggested Commands

Backend:

```bash
cd BackEnd
dotnet test
```

Frontend:

```bash
cd FrontEnd
npm test
npm run build
```

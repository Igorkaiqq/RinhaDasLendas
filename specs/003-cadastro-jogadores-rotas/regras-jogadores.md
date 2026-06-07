# Regras de Jogadores e Preferencias de Rotas

## Jogador

- Todo jogador nasce com status `Ativo`.
- Inativar jogador nao remove o registro fisicamente.
- Dados basicos podem ser atualizados sem alterar preferencias de rota.
- Controllers nao alteram entidades diretamente; comandos MediatR coordenam os casos de uso.

## Preferencias de Rotas

- O fluxo normal exige exatamente cinco preferencias.
- As rotas validas sao `Top`, `Jungle`, `Mid`, `Adc` e `Support`.
- Cada rota deve aparecer uma unica vez.
- As prioridades devem usar os valores de 1 a 5 sem repeticao.
- No maximo uma rota pode ser marcada como `naoJogoNemLascando`.

## Persistencia

- `jogadores` e `preferencias_rotas` usam UUID como chave primaria.
- Colunas e tabelas seguem snake_case.
- O banco protege unicidade de rota/prioridade por jogador e limita prioridade entre 1 e 5.

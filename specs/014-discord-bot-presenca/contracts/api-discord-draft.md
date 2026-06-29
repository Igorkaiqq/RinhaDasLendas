# API Contract: Discord Bot e Presença no DraftMontagem

## Discord Configurações

### GET /api/v1/discord/configuracoes

Retorna configuração da guild padrão ou informada.

### PUT /api/v1/discord/configuracoes

Atualiza configuração Discord. Restrito a Admin e SuperAdmin.

```json
{
  "guildId": "123456789012345678",
  "presenceChannelId": "123456789012345678",
  "newsChannelId": "123456789012345678",
  "adminChannelId": "123456789012345678",
  "draftChannelId": "123456789012345678",
  "matchResultChannelId": "123456789012345678",
  "botEnabled": true
}
```

## Vínculo Discord

### GET /api/v1/usuarios/discord/{discordUserId}/vinculo

Retorna vínculo interno para uso do bot. Requer token interno do bot.

```json
{
  "vinculado": true,
  "usuarioId": "00000000-0000-0000-0000-000000000000",
  "jogadorId": "00000000-0000-0000-0000-000000000000",
  "nomeExibicao": "Igor",
  "roles": ["Jogador"]
}
```

## DraftMontagem Presença

### POST /api/v1/draft-montagens

Cria DraftMontagem com presença aberta. Capitães não são obrigatórios.

### GET /api/v1/draft-montagens/ativos

Lista drafts relevantes para sincronização do bot.

### POST /api/v1/draft-montagens/{id}/presencas/confirmar

Confirma presença via Web ou Discord.

```json
{
  "discordUserId": "123456789012345678",
  "origem": "Discord"
}
```

### POST /api/v1/draft-montagens/{id}/presencas/cancelar

Cancela presença própria enquanto presença está aberta. Admin/Moderador podem remover após encerramento quando permitido por regra.

### POST /api/v1/draft-montagens/{id}/encerrar-presenca

Encerra presença manualmente ou registra continuidade manual com menos de 10 confirmados.

```json
{
  "continuarComMenosDez": false,
  "tamanhoEquipe": 5
}
```

### POST /api/v1/draft-montagens/{id}/capitaes

Define capitães após presença encerrada.

```json
{
  "capitaesIds": ["00000000-0000-0000-0000-000000000000"]
}
```

### POST /api/v1/draft-montagens/{id}/ordem-escolha

Define ordem manual ou sorteada.

```json
{
  "modo": "Sorteado",
  "capitaesIds": []
}
```

## Finalização

### PATCH /api/v1/draft-montagens/{id}/finalizar

Finaliza DraftMontagem e registra estado para publicação no Discord.

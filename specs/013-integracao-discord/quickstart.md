# Quickstart: Integração Discord

## Variáveis

Configure no ambiente do backend:

```text
DISCORD_CLIENT_ID=
DISCORD_CLIENT_SECRET=
DISCORD_REDIRECT_URI=
DISCORD_SCOPES=identify email
FRONTEND_AUTH_SUCCESS_URL=http://localhost:5173/configuracoes?discord=success
FRONTEND_AUTH_ERROR_URL=http://localhost:5173/configuracoes?discord=error
```

## Fluxo Manual

1. Inicie backend e frontend.
2. Entre com usuário e senha.
3. Acesse `/configuracoes`.
4. Clique em vincular Discord.
5. Autorize no Discord.
6. Confirme que o status aparece como vinculado.
7. Saia da aplicação.
8. Use “Entrar com Discord” na tela de login.

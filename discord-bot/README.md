# Discord Bot

Bot independente do Rinha das Lendas para lista de presença e sincronização com DraftMontagem.

## Regras

- Não acessa banco diretamente.
- Toda regra de negócio passa pela API do backend.
- Usa token interno limitado via `RINHA_API_INTERNAL_TOKEN`.

## Comandos

- `/draft-criar`
- `/draft-status`
- `/draft-encerrar-presenca`
- `/draft-definir-capitaes`
- `/draft-definir-ordem-escolha`

## Desenvolvimento

```bash
npm install
cp .env.example .env
npm run dev
```

## Build

```bash
npm run build
npm start
```

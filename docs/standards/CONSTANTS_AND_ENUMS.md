# Constants and Enums

Constantes, enums e tipos devem remover duplicação real, evitar magic strings e tornar regras fechadas mais fáceis de revisar.

## Quando usar enum

Use enum quando o domínio tem conjunto fechado e conhecido:

- Status de jogador.
- Rotas do League of Legends.
- Categoria de mensagem.
- Elo ou divisão quando modelados no domínio.

## Quando usar constante

Use constante para valores estáveis usados em múltiplos pontos, mas que não representam necessariamente entidade de domínio:

- Rotas do frontend.
- Códigos de mensagem.
- Chaves de storage local.
- Limites técnicos ou nomes de configuração.

## Quando usar type union no frontend

Use union type quando TypeScript precisa restringir strings sem gerar enum runtime:

```typescript
export type LocaleCode = 'pt-BR' | 'en-US'
```

## Backend

- Códigos de mensagem ficam em `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`.
- Categorias de mensagem ficam em `BackEnd/src/RinhaDasLendas.Domain/Enums/MessageCategory.cs`.
- Textos localizados não devem ficar em constantes C#; devem ir para resource files `.resx`.
- Domain não deve depender de EF, HTTP, DTOs ou infraestrutura.

## Frontend

- Rotas ficam em `FrontEnd/src/constants/appRoutes.ts`.
- Códigos e categorias de mensagem ficam em `FrontEnd/src/constants/`.
- Tipos compartilhados ficam em `FrontEnd/src/types/`.
- Componentes devem importar constantes em vez de repetir strings.

## Nomes

- C# usa PascalCase para constantes públicas: `OperationSuccess`.
- TypeScript usa nomes descritivos em UPPER_SNAKE_CASE para enum members quando fizer sentido: `OPERATION_SUCCESS`.
- Códigos externos permanecem no formato do catálogo: `MSIS001`, `MV001`, `ME001`.

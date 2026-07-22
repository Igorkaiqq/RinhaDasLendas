# Histórico de Atualizações

Este padrão define como publicar mudanças visíveis no histórico oficial da arena sem transformar commits técnicos em conteúdo editorial.

## Fluxo editorial

1. Decidir se a mudança é visível ou relevante ao usuário.
2. Incrementar `AAAA.MM.N` dentro do mês da publicação.
3. Adicionar metadados e somente chaves i18n ao registro.
4. Escrever título curto e benefício prático em português e inglês.
5. Classificar cada detalhe em uma categoria e a release em áreas afetadas.
6. Usar link interno apenas quando houver uma ação útil e rota existente.
7. Executar testes de contrato, i18n, interface e build.
8. Commitar a entrada junto da mudança; agrupar alterações internas pequenas para evitar ruído.

## Exemplo

O registro em `FrontEnd/src/constants/systemUpdates.ts` contém somente estrutura e chaves de tradução:

```ts
{
  id: 'draft-access-improvement',
  version: '2026.08.1',
  publishedAt: '2026-08-05',
  titleKey: 'updates.releases.draftAccess.title',
  summaryKey: 'updates.releases.draftAccess.summary',
  categories: ['improvement'],
  areas: ['drafts', 'discord'],
  featured: true,
  details: [
    {
      id: 'discord-draft-link',
      category: 'improvement',
      titleKey: 'updates.releases.draftAccess.details.discordLink.title',
      descriptionKey: 'updates.releases.draftAccess.details.discordLink.description',
      link: '/drafts',
    },
  ],
}
```

As chaves do exemplo devem existir com estrutura equivalente em `pt.json` e `en.json`. O título identifica a mudança; a descrição explica o benefício prático para quem usa a plataforma.

## Conteúdo proibido

Não publicar:

- segredos, tokens ou credenciais;
- IDs operacionais ou dados pessoais;
- payloads, endpoints ou detalhes internos sensíveis;
- mensagens de commit;
- jargão de infraestrutura sem explicar o benefício ao usuário.

Mudanças exclusivamente internas devem ser agrupadas quando uma entrada isolada gerar ruído editorial.

## Verificação

Execute antes do commit:

```bash
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint --prefix FrontEnd
```

Confirme também que os links usam rotas registradas, as categorias e áreas pertencem aos tipos fechados, e português e inglês mantêm a mesma estrutura.

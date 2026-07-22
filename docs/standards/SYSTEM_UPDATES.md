# Histórico de Atualizações

Este padrão define como publicar mudanças visíveis no histórico oficial da arena sem transformar commits técnicos em conteúdo editorial.

## Fluxo editorial

1. Decidir se a mudança é visível ou relevante ao usuário.
2. Incrementar `AAAA.MM.N` dentro do mês da publicação.
3. Adicionar a nova release no topo do registro, mantendo a ordem cronológica decrescente.
4. Escrever título curto e benefício prático em português e inglês.
5. Classificar cada detalhe em uma categoria e a release em áreas afetadas.
6. Usar link interno apenas quando houver uma ação útil e rota existente.
7. Ao destacar a nova release com `featured: true`, alterar a release anteriormente destacada para `featured: false`.
8. Executar testes de contrato, i18n, interface e build.
9. Commitar a entrada junto da mudança; agrupar alterações internas pequenas para evitar ruído.

## Exemplo

O registro em `FrontEnd/src/constants/systemUpdates.ts` contém somente estrutura e chaves de tradução:

```ts
{
  id: 'draft-access-improvement',
  version: '2026.08.1',
  publishedAt: '2026-08-05',
  titleKey: 'updates.releases.2026_08_1.title',
  summaryKey: 'updates.releases.2026_08_1.summary',
  categories: ['improvement'],
  areas: ['drafts', 'discord'],
  featured: true,
  details: [
    {
      id: 'discord-draft-link',
      category: 'improvement',
      titleKey: 'updates.releases.2026_08_1.details.discord-draft-link.title',
      descriptionKey: 'updates.releases.2026_08_1.details.discord-draft-link.description',
      link: '/drafts',
    },
  ],
}
```

As chaves são normalizadas a partir da versão, substituindo pontos por sublinhados. As chaves do exemplo devem existir com estrutura equivalente em `pt.json` e `en.json`. O título identifica a mudança; a descrição explica o benefício prático para quem usa a plataforma.

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

Confirme também que há exatamente uma release destacada, que ela ocupa o topo do registro, que as releases permanecem em ordem cronológica decrescente, que os links usam rotas registradas, que as categorias e áreas pertencem aos tipos fechados e que português e inglês mantêm a mesma estrutura.

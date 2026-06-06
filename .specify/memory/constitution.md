<!--
Sync Impact Report
Version change: none → 1.0.0
Modified principles: initial constitution
Added sections: Arquitetura e Qualidade; Regras de Domínio e Evolução
Removed sections: none
Templates requiring updates: none identified (.specify/templates/plan-template.md, .specify/templates/spec-template.md, .specify/templates/tasks-template.md)
Follow-up TODOs: none
-->

# RinhaDasLendas Constitution

## Core Principles

### MVP Primeiro
A primeira versão MUST funcionar com o menor conjunto de funcionalidades possível.
A primeira versão MUST funcionar mesmo sem integração completa com Discord ou Riot API.
Quando uma integração externa for complexa, instável ou exigir aprovação, deve existir alternativa manual.
O produto MUST suportar entrada manual para nick do LoL, link de OP.GG ou Deeplol, resultado de partida, picks, bans e estatísticas se a API não estiver disponível.
Rationale: Começar simples evita bloqueios de implementação e garante entrega de valor real.

### Uso Interno
O sistema é inicialmente voltado para uso privado do grupo.
Não deve ser tratado como aplicação pública de larga escala no início.
Deve manter estrutura limpa e extensível para permitir crescimento futuro sem adicionar complexidade desnecessária.
Rationale: Escopo controlado preserva agilidade e reduz riscos de sobreengenharia.

### Simplicidade de Uso
A interface deve ser objetiva e fácil de entender.
O usuário MUST conseguir cadastrar jogador, informar contas, definir preferência de rotas, participar de fila/votação, visualizar times, estatísticas e histórico sem precisar entender regras técnicas do sistema.
A linguagem pode ser descontraída, mas ações importantes MUST ser apresentadas de forma clara.
Rationale: A adoção depende da experiência prática e da clareza do fluxo para o grupo.

### Regras de Jogo Claras
A formação de times, rotas e draft MUST ser explícita e configurável sempre que possível.
Quando o sistema sortear ou sugerir times, MUST mostrar os critérios usados.
O produto MUST evitar ocultar decisões importantes ou aplicar regras sem transparência.
Rationale: Decisões claras aumentam confiança e permitem ajustes sem frustração.

### Integrações Não Devem Travar o Produto
Discord e Riot API são desejáveis, mas não devem impedir o funcionamento básico.
O sistema MUST permitir modo manual, modo parcialmente integrado e modo totalmente integrado no futuro.
Nenhuma regra central MUST depender exclusivamente de uma integração externa na primeira versão.
Rationale: Integrações são adaptadores, não pré-requisitos para o MVP.

## Arquitetura e Qualidade
O backend MUST ser desenvolvido em .NET e seguir separação clara de responsabilidades entre API, Application, Domain, Infrastructure e Tests.
Regras de negócio MUST ficar fora dos controllers; controllers MUST apenas receber requisições, validar entrada básica, chamar casos de uso e retornar respostas.
O frontend MUST ser desenvolvido em Vue, responsivo, simples e consumir o backend via API HTTP.
Regras de negócio importantes MUST estar no backend/domain e não apenas no frontend.
O banco preferencial é PostgreSQL; entidades MUST ter identificadores únicos estáveis e o modelo MUST permitir evolução para novas regras de draft, partidas, estatísticas e integrações.
O ambiente MUST ser executável em Dev Container para facilitar desenvolvimento sem dependências manuais no host.
O código MUST ser limpo, pequeno e direto; evitar métodos grandes, classes com responsabilidade excessiva e duplicação desnecessária.
Toda entrada importante MUST ser validada; mensagens de erro para usuário final MUST ser claras e não técnicas.
Logs técnicos MUST existir para ajudar investigação.
Regras de negócio críticas MUST ter testes automatizados, com prioridade em cadastro de jogador, preferências de rota, regras de fila, sorteio de times, draft, validação de partidas e cálculo de estatísticas.
Rationale: Disciplina técnica garante entrega confiável e mantém a base de código evolutiva.

## Regras de Domínio e Evolução
Um jogador MUST poder possuir nome de exibição, conta do Discord, conta do League of Legends, região/servidor, link OP.GG, link Deeplol, elo informado manualmente e status ativo/inativo.
Cada jogador MUST poder ranquear as cinco rotas (Top, Jungle, Mid, ADC, Support) com prioridades de 1 a 5.
O jogador MAY indicar no máximo uma rota como “não jogo nem lascando”.
O sistema MUST impedir preferências de rota inconsistentes.
A fila/votação MUST permitir organizar lista de interessados para jogar, com identificação de confirmados, reservas e ausentes, e quantidade padrão de 10 vagas.
O draft MUST suportar capitães definidos manualmente ou sorteados e registrar a ordem de escolha dos jogadores.
O sistema MUST evitar jogador duplicado na mesma partida.
Cada partida MUST conter dois times, até cinco jogadores por time e, sempre que possível, cada jogador MUST ocupar uma rota.
Uma partida MUST permitir data, jogadores, times, rotas, campeões pickados, campeões banidos, resultado, estatísticas e observações.
O sistema MUST suportar séries MD3 e MD5, agrupar múltiplas partidas e calcular o vencedor da série.
O sistema MUST permitir visualizar estatísticas por jogador, incluindo partidas jogadas, vitórias, derrotas, win rate, KDA, média de kills, mortes, assistências, farm médio, gold médio, campeões mais jogados e rotas mais jogadas.
No MVP, estatísticas podem ser preenchidas manualmente.
A integração com Discord e Riot API é desejável, mas não obrigatória no MVP; cada fase de integração MUST ter alternativa manual.
Quando a API não fornecer informação, o sistema MUST permitir entrada manual.
O sistema MUST proteger dados dos usuários; senhas, tokens e secrets NEVER devem ser versionados; variáveis sensíveis MUST ficar em arquivos de ambiente ou secrets locais.
O sistema MUST ter autenticação antes de permitir alterações importantes.
Permissões futuras podem incluir administrador, capitão, jogador comum e espectador.
A interface deve ser descontraída, mas ações importantes MUST ser claras para o usuário.
O projeto MUST começar simples demais; não implementar microsserviços no MVP, não depender exclusivamente da Riot API ou do Discord, não criar automações antes de existir fluxo manual funcional e não misturar regra de negócio diretamente em controller ou componente visual.
Toda nova feature MUST responder se resolve um problema real do grupo, funciona sem integração externa, tem regra crítica testada, é compreendida pelo usuário, respeita a separação backend/frontend/domínio e ajuda o MVP.
Se essa resposta não for clara, a feature MUST ser refinada antes de implementação.
Rationale: Regras de domínio explícitas protegem o MVP de complexidade prematura e mantém a evolução alinhada aos objetivos do grupo.

## Governance
Esta Constituição define os princípios obrigatórios de produto, arquitetura, qualidade e domínio.
Emendas MUST ser documentadas, ratificadas pela equipe e acompanhadas de plano de mitigação quando alterarem princípios ou escopo de MVP.
Cada PR/feature MUST verificar conformidade com esta Constituição antes de aprovação.
Mudanças que alterem princípios ou escopo de MVP MUST incluir justificativa de valor e plano de implementação.
Revisões de compliance MUST ocorrer em cada ciclo de entrega e antes de integração de novas integrações externas.
A Constituição é a referência para priorização, escopo e arquitetura; decisões divergentes MUST ser aprovadas explicitamente.

**Version**: 1.0.0 | **Ratified**: 2026-06-06 | **Last Amended**: 2026-06-06


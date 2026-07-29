# Conformidade Riot, LCU, privacidade e segurança

## Escopo

Este documento resume as políticas e orientações técnicas consultadas em **2026-07-27**. Não é parecer jurídico e não substitui aprovação da Riot Games. Como as políticas mudam, a verificação deve ser repetida antes de lançar, ampliar acesso ou monetizar o produto.

## Suporte oficial versus comunitário

| Tecnologia | Situação | Consequência para o RinhaDasLendas |
|---|---|---|
| Riot API e Match V5 | Serviços oficiais documentados | Usar apenas com produto registrado, credencial adequada, rate limits e políticas aplicáveis. |
| Tournament API | Serviço oficial documentado | Adequado quando partidas são criadas por tournament code; exige configuração e acesso próprios. |
| Data Dragon | Fonte oficial de dados e ativos estáticos | Pode traduzir IDs e fornecer imagens; não comprova fatos de partida. |
| Game Client e Live Client Data API | APIs locais documentadas pela Riot | Uso deve permanecer local, seguro e compatível com integridade competitiva. Não substitui histórico oficial. |
| League Client API (LCU) | A Riot reconhece a interface local, mas declara ausência de suporte oficial para terceiros, garantia de documentação, disponibilidade ou comunicação de mudanças | Todo endpoint LCU deve ser isolado em adaptador experimental, monitorado por compatibilidade e acompanhado de fallback manual. |
| Catálogos de endpoints LCU | Documentação comunitária | Úteis para pesquisa, nunca fonte de garantia. Confirmar empiricamente e limitar cada endpoint por versão. |
| ROFL e parsers | Replay oficial, parsing estatístico normalmente comunitário e sem contrato público estável | Não usar como fonte primária nem prometer compatibilidade entre patches. |
| OCR | Técnica externa | Exigir revisão humana e marcar origem experimental. |

O endpoint de histórico usado na partida `3266170515` é evidência experimental. A riqueza do payload não altera seu status não suportado.

## Registro do produto

As políticas gerais consultadas afirmam que:

- todos os produtos devem ser registrados e auditados pela Riot pelo Developer Portal;
- novos recursos e mudanças no produto devem passar pela página do produto;
- produtos devem preferir serviços suportados para ingestão;
- quem utiliza a League Client API deve ler a documentação correspondente e registrar o produto;
- a documentação específica de League of Legends também solicita informar à Riot quais endpoints LCU são usados e de que forma.

Consequência: uso interno, ausência de chave oficial ou gratuidade não eliminam a obrigação indicada pela política. Antes de disponibilizar o coletor LCU, o RinhaDasLendas deve registrar o produto e descrever com precisão o fluxo pós-partida, os endpoints locais, a sanitização e o fallback manual.

## Monetização

Segundo as políticas consultadas, monetização exige, entre outros pontos:

- produto registrado com estado `Approved` ou `Acknowledged`;
- uma camada gratuita para jogadores, que pode conter publicidade;
- conteúdo transformativo quando houver cobrança;
- ausência de apostas ou jogos de azar;
- cobrança que não seja abusiva ou injusta;
- formas admissíveis listadas pela Riot, como assinatura, doação, financiamento coletivo e taxas de inscrição de torneio, observadas as demais regras.

Para torneios, as políticas gerais também registram exigências próprias, incluindo pelo menos 70% das taxas de inscrição destinados à premiação, condições justas e transparentes, mínimo de 20 participantes e proibição de apostas. Essas regras não devem ser convertidas em funcionalidades sem nova revisão de contexto e jurisdição.

Estado atual: esta documentação não afirma que o RinhaDasLendas esteja registrado, aprovado, reconhecido ou autorizado a monetizar. A confirmação deve vir do Developer Portal, não do repositório.

## Custom matches e opt-in

A política de League of Legends consultada declara que produtos não podem exibir publicamente o histórico de um jogador na fila de partidas personalizadas sem que o jogador opte especificamente por compartilhar esse histórico. Sem opt-in, a política aponta que os dados custom do jogador só podem ser disponibilizados ao próprio jogador usando RSO.

### Política mínima recomendada para o RinhaDasLendas

- Partida customizada deve ser privada por padrão e acessível apenas a membros autenticados com necessidade legítima.
- Publicação pública deve permanecer desativada até existir opt-in específico, verificável, revogável e associado ao jogador e ao escopo de compartilhamento.
- Consentimento genérico de participar do Discord, do draft ou da partida não deve ser tratado automaticamente como opt-in para histórico público.
- A confirmação de quem importou a partida não substitui o consentimento dos demais participantes.
- Participante sem vínculo interno ou sem opt-in não deve ter perfil público criado implicitamente.
- Revogação deve interromper exposição futura sem falsificar os fatos internos necessários para auditoria, observadas as regras de retenção que ainda precisam ser definidas.
- O produto deve informar finalidade, campos publicados, público destinatário, duração e forma de revogação.

O uso interno autenticado reduz exposição, mas não deve ser descrito como autorização automática da Riot. Antes de qualquer modo público, é necessária revisão de política e, quando aplicável, RSO e orientação da Riot.

## Integridade competitiva

As políticas proíbem vantagem injusta e informação específica da sessão que não seria conhecida pelo jogador. Para este projeto:

- coleta principal deve ocorrer após o término;
- não criar overlay, recomendação ou alerta durante a partida com dados ocultos;
- não expor cooldown inimigo, visão não disponível ou análise que substitua decisões do jogador;
- Live Client Data, se estudada, deve ter caso de uso explícito, opt-in e revisão de integridade;
- estatísticas históricas não devem virar um sistema alternativo de MMR/ELO proibido pela política.

## Segurança do `lockfile`

### Natureza do risco

O `lockfile` local do League Client contém informações de processo, porta, credencial temporária e protocolo usadas para autenticar no serviço local. A documentação comunitária descreve uma linha delimitada com esses componentes e autenticação HTTP Basic. A senha e a porta mudam com o ciclo do cliente.

Mesmo sendo local e temporária, a credencial é segredo. Quem a obtém durante a sessão pode chamar serviços do cliente com a autoridade disponível naquele processo.

### Controles obrigatórios para qualquer experimento futuro

- Ler o `lockfile` somente no computador do usuário e sob ação explícita.
- Nunca enviar o arquivo, seu conteúdo, a senha, a porta ou o cabeçalho de autorização ao backend.
- Nunca persistir a credencial em arquivo de exportação, configuração, banco, telemetria, relatório de erro, crash dump, histórico de terminal ou log.
- Manter credencial apenas em memória pelo menor tempo necessário e descartá-la ao fechar, reiniciar ou trocar a sessão do cliente.
- Revalidar processo e endpoint a cada sessão; não reutilizar porta ou senha anterior.
- Restringir o destino a loopback. Não aceitar host fornecido por arquivo, argumento remoto, redirecionamento ou resposta externa.
- Bloquear redirecionamentos para fora de loopback e validar o destino final de cada chamada.
- Não expor uma ponte HTTP local aberta a navegadores, rede doméstica ou origem arbitrária.
- Tratar permissões insuficientes ou arquivo em troca como falha segura, sem tentar copiar diretórios inteiros.
- Aplicar allowlist de endpoints e métodos estritamente necessários.
- Aplicar timeout, cancelamento, retry limitado e backoff; não realizar polling agressivo.
- Sanitizar erros antes de exibi-los ou registrá-los.

### TLS local

A Riot documenta certificado autoassinado nas APIs locais e publica um certificado raiz. Qualquer exceção de validação deve ser limitada ao cliente HTTP dedicado, ao host de loopback esperado e ao certificado/contexto aprovado. É proibido desabilitar validação TLS globalmente ou aceitar certificado inválido para hosts remotos.

### Exportação segura

Se a feature 018 for planejada e aprovada no futuro, o arquivo exportado deve conter somente campos de uma allowlist de negócio. Não deve conter:

- senha ou conteúdo do `lockfile`;
- cabeçalho `Authorization`;
- token, JWT ou credencial de chat;
- PUUID ou identificador pessoal sem necessidade aprovada;
- URL assinada;
- endereço, porta ou detalhes da sessão local;
- payload bruto por conveniência;
- caminho local de instalação ou nome de usuário do sistema operacional.

## Dados pessoais e minimização

- Riot ID deve ser coletado somente quando necessário ao vínculo e protegido conforme o público do produto.
- PUUID não deve ser exibido nem usado como rótulo de usuário.
- Participantes externos podem permanecer como snapshots não vinculados; isso é mais seguro do que criar associação incerta.
- Logs devem usar identificadores internos ou correlation IDs e evitar nomes, mensagens do Discord e payload de partida.
- Fixtures no repositório devem ser sintéticas ou irreversivelmente sanitizadas.
- A partida empírica deve continuar documentada apenas pelo Game ID e totais técnicos necessários, sem nomes ou identificadores dos participantes.

## Confiabilidade e transparência

Uma interface futura deve informar:

- origem do dado: Match V5, Tournament API, LCU, Live Client, OCR ou manual;
- momento da coleta;
- se o campo é direto, derivado, revisado ou rejeitado;
- quando um campo está ausente, e não mostrar `0` por padrão;
- aviso de experimental para rota automática, assistência de objetivo e mapeamento ainda não validado;
- possibilidade de correção auditada sem apagar o valor original.

Consulte a [matriz de disponibilidade](FIELD_AVAILABILITY_MATRIX.md) para a classificação atual.

## Checklist antes de qualquer implementação

| Verificação | Estado em 2026-07-27 |
|---|---|
| Produto registrado no Developer Portal | Não comprovado no repositório; verificar externamente. |
| Uso de LCU descrito à Riot | Não comprovado; necessário antes da disponibilização. |
| Política de opt-in de custom match aprovada | Não implementada. |
| RSO disponível para acesso individual quando necessário | Não implementado. |
| Contrato de exportação aprovado | Não; feature 018 é rascunho. |
| Threat model e retenção aprovados | Parcialmente levantados na spec 018, não aprovados. |
| Lockfile restrito ao processo local | Não há coletor implementado. |
| Fallback manual funcional | Exigido, mas ainda não implementado para partidas. |
| Distinção entre zero e ausente | Documentada como requisito, não implementada. |
| Métricas experimentais excluídas de agregados oficiais | Política documental definida aqui; não há agregador implementado. |

## Fontes consultadas

Consulta realizada em **2026-07-27**:

- [Políticas gerais da Riot Games para desenvolvedores](https://developer.riotgames.com/policies/general)
- [Políticas específicas por jogo](https://developer.riotgames.com/policies/game-specific)
- [Documentação de League of Legends: política, Match V5, Tournament API, League Client API, Game Client API, Live Client Data e Data Dragon](https://developer.riotgames.com/docs/lol)
- [Referência oficial de APIs](https://developer.riotgames.com/apis)
- [Registro de produto no Developer Portal](https://developer.riotgames.com/app-type)
- [Termos da API](https://developer.riotgames.com/terms)
- [Certificado raiz para APIs locais](https://static.developer.riotgames.com/docs/lol/riotgames.pem)
- [Guia comunitário de conexão ao LCU](https://hextechdocs.dev/getting-started-with-the-lcu-api/)

## Conclusão

O caminho tecnicamente mais rico sem chave, observado via LCU, também é o de maior instabilidade e sensibilidade local. Ele só é aceitável como integração experimental, registrada, explícita, pós-partida, minimizada e substituível pelo fluxo manual. Para operação oficial de competição, Tournament API e Match V5 devem ser reavaliadas como fontes preferenciais. Nenhuma fonte dispensa consentimento, autorização do backend, auditoria e distinção rigorosa entre zero, ausência e dado não confiável.

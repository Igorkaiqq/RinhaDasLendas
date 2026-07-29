# Decisões pendentes do domínio competitivo

Este arquivo contém somente decisões genuinamente pendentes. Nenhum documento ou implementação deve adotar valor padrão oculto para estes itens.

## 1. Fórmula exata do soft reset

Definir a transformação entre rating final de uma temporada e rating inicial da seguinte, incluindo centro de regressão, intensidade, arredondamento e tratamento de estreantes.

Evidência necessária: simulação sobre distribuições históricas e impacto por faixa.

## 2. Bônus e reembolso

Definir quais eventos concedem bônus, quando existe reembolso de variação e como evitar exploração, duplicidade ou compensação excessiva.

Evidência necessária: cenários de ausência, substituição, erro operacional, partida invalidada e correção.

## 3. Thresholds das faixas S-F

Definir limites, ordem, estabilidade e eventual cálculo relativo ou absoluto das faixas de classificação de S a F.

Evidência necessária: distribuição simulada por temporada, função e tamanho de comunidade.

## 4. Amostra mínima

Definir quantidade e tipo de partidas oficiais necessárias para ranking, comparação, faixa e demais indicadores publicados.

Evidência necessária: análise de variância e risco de exposição enganosa com amostras pequenas.

## 5. Retenção de dados brutos

Definir prazo, finalidade, acesso, anonimização e descarte de payloads brutos de coleta após normalização e auditoria.

Evidência necessária: necessidade operacional, custo, privacidade, segurança e capacidade de reprocessamento.

## 6. Poderes finos do VicePresidente

Definir se e em quais condições o VicePresidente recebe `CanManageSeasons`, incluindo suplência do Presidente, limites de duração, justificativa, dupla aprovação e auditoria. SuperAdmin e Presidente já possuem a capacidade; Admin e papéis inferiores permanecem negados.

Evidência necessária: fluxo real de governança, ausência temporária e resposta a conflito de interesse.

## 7. Exceções de transferências

Definir se existem transferências fora de janela, emergenciais, temporárias ou retroativas, quem aprova e quais limites preservam integridade competitiva.

Evidência necessária: cenários reais de indisponibilidade, abandono, substituição e erro cadastral.

## 8. Política de backfill

Definir quais dados históricos podem ser importados, desde quando, com qual nível de confiança e se recalculam estatísticas, votação ou rating.

Evidência necessária: qualidade das fontes históricas, custo de revisão e impacto sobre classificações já publicadas.

## 9. Uso da Tournament API

Definir se e quando integrar a Tournament API, finalidade, limites, fallback manual e responsabilidade operacional.

Evidência necessária: disponibilidade, termos, credenciais, estabilidade e aderência ao fluxo interno.

## 10. Identidade visual da área competitiva

Definir se a área precisa de extensão visual própria dentro do design system atual, sem criar paleta, tipografia ou tokens paralelos antes de aprovação.

Evidência necessária: protótipos com jogadores e organizadores, acessibilidade e consistência com a arena de draft.

## 11. Mudança de voto enquanto a janela estiver aberta

Definir se o eleitor pode alterar sua escolha de MVP/SVP em uma votação de série antes do encerramento do turno, quantas vezes, qual registro de auditoria permanece e qual resposta idempotente é esperada.

Evidência necessária: preferência da comunidade, risco de coerção, simplicidade de uso e tratamento de envio acidental.

## 12. Definição de KDA

Definir fórmula, precisão, tratamento de zero mortes, agregação entre Partidas e apresentação do KDA sem confundir ausência de dado com valor zero.

Evidência necessária: casos-limite por Partida e Série, consistência com fontes disponíveis e legibilidade para jogadores.

## 13. Cálculo de força do confronto

Definir como Ratings anteriores à Partida compõem a força dos lados, como tratar reservas, estreantes, Ratings ausentes e diferença entre força da Partida e contexto da Série.

Evidência necessária: simulações de confrontos equilibrados e desequilibrados, resistência a manipulação e comportamento com dados incompletos.

## 14. Cálculo de impacto

Definir como Score, confiança, função e contexto da Partida alteram o impacto individual, incluindo neutralidade quando o Score não for confiável.

Evidência necessária: distribuição por função, cenários extremos, prevenção de inversão indevida entre vitória e derrota e análise de vieses.

## 15. Parâmetros internos do Rating

Definir componentes, limites intermediários, arredondamento, precisão, ordem de aplicação e versionamento dos parâmetros internos usados para transformar resultado, força e impacto em variação nominal e aplicada.

Evidência necessária: golden fixtures, propriedades matemáticas, reconstrução do ledger e validação das faixas nominais publicadas.

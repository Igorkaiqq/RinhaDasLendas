# ADR-002: Tratar fontes externas como adaptadores e preservar payload bruto redigido

## Status

Aceito

## Data

2026-07-27

## Contexto

A Rinha pode receber dados por Tournament API, Match V5, LCU, Live Client Data, ROFL, OCR ou entrada manual. Essas fontes possuem contratos, retenção e confiabilidade diferentes. A validação empírica de uma partida customizada comprovou dados úteis no LCU, mas também mostrou rota incorreta e assistências de objetivo inconsistentes.

O projeto precisa auditar importações sem acoplar o domínio aos contratos externos nem armazenar credenciais locais.

## Decisão

Cada fonte será encapsulada por adaptador. O domínio recebe um contrato canônico e uma matriz de presença/confiança, nunca o contrato externo diretamente.

O payload bruto poderá ser preservado como artefato imutável de auditoria, após allowlist ou redaction. Ele terá hash, origem, versão do schema, versão do importador e política de retenção. Dados normalizados e consultáveis permanecem relacionais.

Tokens, senha, porta e headers do `lockfile`, chaves de API, cookies e outros segredos nunca são enviados ou persistidos como payload de partida.

## Alternativas consideradas

### Persistir somente o modelo normalizado

- Vantagem: menor volume e modelo simples.
- Desvantagem: perde evidência para corrigir importadores e comparar versões.
- Rejeição: insuficiente para uma integração LCU volátil.

### Persistir payload integral sem redaction

- Vantagem: máxima fidelidade aparente.
- Desvantagem: aumenta risco de segredo, dado pessoal desnecessário e acoplamento.
- Rejeição: incompatível com minimização e segurança.

### Usar LCU como fonte de verdade

- Vantagem: automação rápida para partidas customizadas.
- Desvantagem: serviço não suportado, transitório e empiricamente inconsistente em alguns campos.
- Rejeição: integrações são adaptadores; confirmação humana e fluxo manual permanecem obrigatórios.

## Consequências

- Toda importação registra origem, confiança e campos ausentes.
- Mudanças de patch exigem fixtures redigidas e testes de contrato.
- Payload bruto é exceção auditável à preferência relacional, não modelo de consulta.
- Retenção exata precisa de aprovação antes da implementação.
- O Collector futuro opera com escopo mínimo e não possui poder de confirmação.

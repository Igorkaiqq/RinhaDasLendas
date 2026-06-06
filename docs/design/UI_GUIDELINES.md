# UI Guidelines

## Dashboard

O Dashboard deve mostrar:

### Cards Principais

* Jogadores ativos
* Partidas jogadas
* Séries realizadas
* Win Rate geral

---

### Atividade Recente

Lista das últimas partidas.

---

### Próxima Rinha

Área destacada para próxima partida.

---

# Cards

## Jogador

Deve conter:

* avatar
* nome
* elo
* rota principal
* win rate

---

## Partida

Deve conter:

* times
* resultado
* data
* duração

---

## Série

Deve conter:

* placar
* vencedor
* partidas

---

# Tabelas

Utilizar tabelas para:

* rankings
* histórico
* estatísticas

Evitar tabelas para edição.

---

# Formulários

Utilizar layout vertical.

Máximo:

```text
2 colunas
```

em desktop.

---

# Botões

## Primário

Cor:

```yaml
primary
```

Ações principais.

---

## Secundário

Cor:

```yaml
surface-2
```

Ações secundárias.

---

## Danger

Cor:

```yaml
danger
```

Exclusões e ações destrutivas.

---

# Feedback

## Loading

Utilizar Skeleton.

Evitar spinner sempre que possível.

---

## Empty State

Toda listagem deve possuir:

* mensagem amigável;
* CTA principal.

---

# Draft

Tela principal do sistema.

Deve destacar:

* capitães;
* ordem de picks;
* jogadores disponíveis;
* posições preferidas.

---

# Estatísticas

Priorizar:

* gráficos simples;
* rankings;
* comparações.

Evitar dashboards poluídos.

---

# Responsividade

## Desktop

Experiência principal.

---

## Tablet

Cards em duas colunas.

---

## Mobile

Cards em uma coluna.

Menu lateral colapsado.

---

# Regra Final

Toda tela deve responder:

1. O que aconteceu?
2. O que posso fazer?
3. Qual é a próxima ação?

Se isso não estiver claro, a interface precisa ser simplificada.

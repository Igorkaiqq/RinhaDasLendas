# Data Model: Reabertura de Presença do Draft

## DraftMontagem

Nenhum campo novo é necessário.

| Campo existente | Antes | Depois da reabertura |
|---|---|---|
| `Status` | `PresencaEncerrada` | `PresencaAberta` |
| `TamanhoEquipe` | valor configurado | preservado |
| `QuantidadeTimes` | estrutura do encerramento anterior | `0` |
| `QuantidadeReservas` | estrutura do encerramento anterior | `0` |
| `PresencaContinuadaManualmente` | valor do encerramento anterior | `false` |
| `HorarioEncerramentoPresenca` | instante original, possivelmente vencido | `null` |
| `Presencas` | confirmações e cancelamentos existentes | preservados integralmente |
| `VersaoEstado` | versão anterior | incrementada pela mutação |

## DraftMontagemAcaoAdministrativa

Reutiliza a entidade existente com:

- `Tipo`: `ReaberturaPresenca`;
- `ResponsavelUsuarioId`: identidade autenticada;
- `Motivo`: `null`, pois a confirmação explícita é suficiente;
- `JogadorId`: `null`;
- `CriadaEm`: instante gerado pela entidade existente.

## Invariantes

- O draft não pode estar arquivado.
- O status deve ser exatamente `PresencaEncerrada`.
- Presenças não podem ser removidas, restauradas, reordenadas ou duplicadas pela reabertura.
- O prazo deve ser removido na mesma persistência que altera o status.
- O próximo encerramento é a única operação que volta a preencher times, reservas e continuação excepcional.

## State Transitions

```text
PresencaAberta
  └─ encerrar → PresencaEncerrada
                    ├─ reabrir → PresencaAberta
                    └─ definir capitães → CapitaesDefinidos
```

Estados `CapitaesDefinidos`, `OrdemDefinida`, `Aberta`, `EmAndamento`, `Finalizada`, `Cancelada` e drafts arquivados não aceitam reabertura.

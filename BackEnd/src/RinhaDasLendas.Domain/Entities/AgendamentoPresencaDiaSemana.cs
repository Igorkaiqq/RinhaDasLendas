using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class AgendamentoPresencaDiaSemana
{
    private AgendamentoPresencaDiaSemana()
    {
    }

    internal AgendamentoPresencaDiaSemana(Guid agendamentoPresencaId, DiaSemanaIso diaSemana)
    {
        AgendamentoPresencaId = agendamentoPresencaId;
        DiaSemana = diaSemana;
    }

    public Guid AgendamentoPresencaId { get; private set; }
    public DiaSemanaIso DiaSemana { get; private set; }
}

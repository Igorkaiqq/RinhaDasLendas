using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class CalendarioCompetitivo
{
    private CalendarioCompetitivo()
    {
    }

    public CalendarioCompetitivo(Guid id, Guid atualizadoPorUsuarioId, DateTimeOffset atualizadoEm)
    {
        if (id == Guid.Empty || atualizadoPorUsuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Id = id;
        AtualizadoPorUsuarioId = atualizadoPorUsuarioId;
        AtualizadoEm = atualizadoEm.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid? SeasonAtivaId { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }
    public Guid AtualizadoPorUsuarioId { get; private set; }
}

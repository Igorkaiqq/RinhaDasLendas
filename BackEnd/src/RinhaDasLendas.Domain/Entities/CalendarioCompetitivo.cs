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

    public void AtivarSeason(
        Season candidata,
        Season? anterior,
        long expectedVersion,
        Guid usuarioId,
        DateTimeOffset ativadaEm)
    {
        ValidarVersaoEAtor(expectedVersion, usuarioId);
        if (candidata is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (candidata.Id == SeasonAtivaId
            || SeasonAtivaId is null && anterior is not null
            || SeasonAtivaId is not null && (anterior is null || anterior.Id != SeasonAtivaId))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        candidata.ValidarAtivacao(usuarioId);
        anterior?.ValidarEncerramento(usuarioId, ativadaEm);

        anterior?.Encerrar(usuarioId, ativadaEm);
        candidata.Ativar(usuarioId, ativadaEm);

        SeasonAtivaId = candidata.Id;
        RegistrarAtualizacao(usuarioId, ativadaEm);
    }

    public void EncerrarSeason(
        Season season,
        long expectedVersion,
        Guid usuarioId,
        DateTimeOffset encerradaEm)
    {
        ValidarVersaoEAtor(expectedVersion, usuarioId);
        if (season is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (SeasonAtivaId != season.Id)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        season.ValidarEncerramento(usuarioId, encerradaEm);
        season.Encerrar(usuarioId, encerradaEm);

        SeasonAtivaId = null;
        RegistrarAtualizacao(usuarioId, encerradaEm);
    }

    private void ValidarVersaoEAtor(long expectedVersion, Guid usuarioId)
    {
        if (expectedVersion != Versao)
        {
            throw new DomainException(MessageCodes.CompetitiveCalendarVersionStale);
        }

        if (usuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private void RegistrarAtualizacao(Guid usuarioId, DateTimeOffset atualizadoEm)
    {
        AtualizadoEm = atualizadoEm.ToUniversalTime();
        AtualizadoPorUsuarioId = usuarioId;
        Versao++;
    }
}

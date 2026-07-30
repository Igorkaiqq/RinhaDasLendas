using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Events.Competitive;

public interface ICompetitiveDomainEvent
{
    Guid EventoDominioId { get; }
    Guid AgregadoId { get; }
    long VersaoAgregado { get; }
    DateTimeOffset OcorridoEm { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
    Guid AtorUsuarioId { get; }
}

public abstract record CompetitiveDomainEvent : ICompetitiveDomainEvent
{
    protected CompetitiveDomainEvent(
        Guid eventoDominioId,
        Guid agregadoId,
        long versaoAgregado,
        DateTimeOffset ocorridoEm,
        Guid correlationId,
        Guid? causationId,
        Guid atorUsuarioId)
    {
        EventoDominioId = eventoDominioId;
        AgregadoId = agregadoId;
        VersaoAgregado = versaoAgregado;
        OcorridoEm = ocorridoEm.ToUniversalTime();
        CorrelationId = correlationId;
        CausationId = causationId;
        AtorUsuarioId = atorUsuarioId;
    }

    public Guid EventoDominioId { get; }
    public Guid AgregadoId { get; }
    public long VersaoAgregado { get; }
    public DateTimeOffset OcorridoEm { get; }
    public Guid CorrelationId { get; }
    public Guid? CausationId { get; }
    public Guid AtorUsuarioId { get; }
}

public sealed record TemporadaCriada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    int Ano,
    int OrdemNoAno)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record TemporadaAtivada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid? TemporadaAnteriorId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record TemporadaEncerrada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record CompeticaoCriada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid SeasonId,
    string Codigo)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record RodadaCriada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid RodadaId,
    int Ordem)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record RodadasReordenadas(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record RegrasCompeticaoPublicadas(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid VersaoRegrasId,
    int Numero,
    SerieFormato Formato,
    ModoDraft ModoDraft)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record EventoCriado(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid SeasonId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record SerieCriada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid SeasonId,
    Guid? EventoId,
    SerieTipo Tipo)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record SerieAdicionadaAoEvento(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid SerieId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record SerieIniciada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record PartidaAdicionada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid PartidaId,
    int Ordem)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record PicksPartidaRegistrados(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid PartidaId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record PartidaConfirmada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid PartidaId,
    Guid LadoVencedorId,
    MotivoTerminoPartida MotivoTermino)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record PartidaMarcadaComoRemake(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid PartidaId,
    DecisaoPicksRemake DecisaoPicks)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record ResultadoSerieConfirmado(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    Guid LadoVencedorId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record SerieCancelada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record SerieAnulada(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

public sealed record FatoCompetitivoCorrigido(
    Guid EventoDominioId,
    Guid AgregadoId,
    long VersaoAgregado,
    DateTimeOffset OcorridoEm,
    Guid CorrelationId,
    Guid? CausationId,
    Guid AtorUsuarioId,
    RecursoCompetitivoTipo RecursoTipo,
    Guid RecursoId)
    : CompetitiveDomainEvent(EventoDominioId, AgregadoId, VersaoAgregado, OcorridoEm, CorrelationId, CausationId, AtorUsuarioId);

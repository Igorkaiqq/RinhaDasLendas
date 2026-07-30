using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class EventoCompetitivo
{
    private readonly List<EventoTime> _times = [];

    private EventoCompetitivo()
    {
    }

    public EventoCompetitivo(
        Guid seasonId,
        string nome,
        Guid criadoPorUsuarioId,
        DateTimeOffset criadoEm,
        IReadOnlyCollection<EventoTimeSnapshot> times)
    {
        if (seasonId == Guid.Empty || criadoPorUsuarioId == Guid.Empty || criadoEm == default)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > 120)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (times is null
            || times.Count != 4
            || times.Any(time => time is null || time.TimeId == Guid.Empty)
            || times.Select(time => time.TimeId).Distinct().Count() != 4
            || !times.Select(time => time.Ordem).Order().SequenceEqual([1, 2, 3, 4]))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var criadoEmUtc = criadoEm.ToUniversalTime();

        Id = Guid.NewGuid();
        SeasonId = seasonId;
        Nome = nomeNormalizado;
        ModoDraft = ModoDraft.Padrao;
        CriadoEm = criadoEmUtc;
        AtualizadoEm = criadoEmUtc;
        CriadoPorUsuarioId = criadoPorUsuarioId;
        AtualizadoPorUsuarioId = criadoPorUsuarioId;

        foreach (var snapshot in times.OrderBy(time => time.Ordem))
        {
            _times.Add(new EventoTime(Id, snapshot));
        }
    }

    public Guid Id { get; private set; }
    public Guid SeasonId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public ModoDraft ModoDraft { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }
    public Guid CriadoPorUsuarioId { get; private set; }
    public Guid AtualizadoPorUsuarioId { get; private set; }
    public IReadOnlyCollection<EventoTime> Times => _times.AsReadOnly();
}

using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Domain.Entities;

public sealed class RegistroAuditoriaCompetitiva
{
    private static readonly HashSet<string> CapacidadesCompetitivas =
    [
        AuthPermissions.CanManageSeasons,
        AuthPermissions.CanManageCompetitions,
        AuthPermissions.CanManageMatches,
        AuthPermissions.CanFinalizeMatches,
        AuthPermissions.CanViewCompetitiveAudit
    ];

    private static readonly HashSet<AcaoAuditoriaCompetitiva> AcoesExigemJustificativa =
    [
        AcaoAuditoriaCompetitiva.SerieAnulada,
        AcaoAuditoriaCompetitiva.PartidaAnulada,
        AcaoAuditoriaCompetitiva.PartidaMarcadaComoRemake,
        AcaoAuditoriaCompetitiva.PartidaCorrigida,
        AcaoAuditoriaCompetitiva.FatoCompetitivoCorrigido
    ];

    private RegistroAuditoriaCompetitiva()
    {
    }

    public RegistroAuditoriaCompetitiva(
        RecursoCompetitivoTipo recursoTipo,
        Guid recursoId,
        AcaoAuditoriaCompetitiva acao,
        Guid atorUsuarioId,
        string capacidade,
        string? justificativa,
        SnapshotAuditoriaRedigido? valorAnterior,
        SnapshotAuditoriaRedigido? valorPosterior,
        Guid correlationId,
        DateTimeOffset ocorridoEm)
    {
        if (!Enum.IsDefined(recursoTipo)
            || !Enum.IsDefined(acao)
            || recursoId == Guid.Empty
            || atorUsuarioId == Guid.Empty
            || correlationId == Guid.Empty
            || ocorridoEm == default)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(capacidade))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var capacidadeNormalizada = capacidade.Trim();
        var justificativaNormalizada = justificativa?.Trim();
        if (AcoesExigemJustificativa.Contains(acao)
            && string.IsNullOrWhiteSpace(justificativaNormalizada))
        {
            throw new DomainException(MessageCodes.CorrectionJustificationRequired);
        }

        if (justificativaNormalizada?.Length > 500)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (!CapacidadesCompetitivas.Contains(capacidadeNormalizada))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Id = Guid.NewGuid();
        RecursoTipo = recursoTipo;
        RecursoId = recursoId;
        Acao = acao;
        AtorUsuarioId = atorUsuarioId;
        Capacidade = capacidadeNormalizada;
        Justificativa = justificativaNormalizada;
        ValorAnterior = valorAnterior;
        ValorPosterior = valorPosterior;
        CorrelationId = correlationId;
        OcorridoEm = ocorridoEm.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public RecursoCompetitivoTipo RecursoTipo { get; private set; }
    public Guid RecursoId { get; private set; }
    public AcaoAuditoriaCompetitiva Acao { get; private set; }
    public Guid AtorUsuarioId { get; private set; }
    public string Capacidade { get; private set; } = string.Empty;
    public string? Justificativa { get; private set; }
    public SnapshotAuditoriaRedigido? ValorAnterior { get; private set; }
    public SnapshotAuditoriaRedigido? ValorPosterior { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
}

using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class OcorrenciaAgendamentoPresenca
{
    private OcorrenciaAgendamentoPresenca()
    {
    }

    private OcorrenciaAgendamentoPresenca(
        Guid agendaId,
        DateOnly dataLocal,
        DateTimeOffset publicacao,
        DateTimeOffset encerramento,
        OcorrenciaAgendamentoPresencaStatus status,
        string? codigo,
        DateTimeOffset agora)
    {
        if (encerramento <= publicacao)
        {
            throw new DomainException(MessageCodes.PresenceScheduleTimeRangeInvalid);
        }

        Id = Guid.NewGuid();
        AgendamentoPresencaId = agendaId;
        DataLocal = dataLocal;
        PublicacaoPrevistaEm = publicacao;
        EncerramentoPrevistoEm = encerramento;
        Status = status;
        CodigoFalha = status == OcorrenciaAgendamentoPresencaStatus.Bloqueada
            ? ValidarCodigo(codigo, MessageCodes.PresenceScheduleDiscordUnavailable)
            : null;
        UltimaTentativaEm = agora;
        CriadaEm = agora;
        AtualizadaEm = agora;
    }

    public Guid Id { get; private set; }
    public Guid AgendamentoPresencaId { get; private set; }
    public DateOnly DataLocal { get; private set; }
    public DateTimeOffset PublicacaoPrevistaEm { get; private set; }
    public DateTimeOffset EncerramentoPrevistoEm { get; private set; }
    public OcorrenciaAgendamentoPresencaStatus Status { get; private set; }
    public Guid? DraftMontagemId { get; private set; }
    public string? CodigoFalha { get; private set; }
    public Guid? ClaimId { get; private set; }
    public DateTimeOffset? ClaimExpiresAt { get; private set; }
    public DateTimeOffset? UltimaTentativaEm { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }

    public static OcorrenciaAgendamentoPresenca Processando(
        Guid agendaId,
        DateOnly dataLocal,
        DateTimeOffset publicacao,
        DateTimeOffset encerramento,
        DateTimeOffset agora)
    {
        return new OcorrenciaAgendamentoPresenca(
            agendaId,
            dataLocal,
            publicacao,
            encerramento,
            OcorrenciaAgendamentoPresencaStatus.Processando,
            null,
            agora);
    }

    public static OcorrenciaAgendamentoPresenca Bloqueada(
        Guid agendaId,
        DateOnly dataLocal,
        DateTimeOffset publicacao,
        DateTimeOffset encerramento,
        string codigo,
        DateTimeOffset agora)
    {
        return new OcorrenciaAgendamentoPresenca(
            agendaId,
            dataLocal,
            publicacao,
            encerramento,
            OcorrenciaAgendamentoPresencaStatus.Bloqueada,
            codigo,
            agora);
    }

    public void IniciarProcessamento(DateTimeOffset agora)
    {
        ExigirStatus(OcorrenciaAgendamentoPresencaStatus.Bloqueada);
        Status = OcorrenciaAgendamentoPresencaStatus.Processando;
        CodigoFalha = null;
        UltimaTentativaEm = agora;
        Touch(agora);
    }

    public void MarcarCriada(Guid draftId, DateTimeOffset agora)
    {
        ExigirStatus(OcorrenciaAgendamentoPresencaStatus.Processando);
        if (draftId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }

        Status = OcorrenciaAgendamentoPresencaStatus.Criada;
        DraftMontagemId = draftId;
        CodigoFalha = null;
        LimparClaim();
        Touch(agora);
    }

    public void MarcarPerdida(string codigo, DateTimeOffset agora)
    {
        ExigirStatus(OcorrenciaAgendamentoPresencaStatus.Bloqueada);
        var codigoValidado = ValidarCodigo(codigo, MessageCodes.PresenceScheduleWindowExpired);
        Status = OcorrenciaAgendamentoPresencaStatus.Perdida;
        CodigoFalha = codigoValidado;
        LimparClaim();
        Touch(agora);
    }

    public void MarcarFalha(string codigo, DateTimeOffset agora)
    {
        ExigirStatus(OcorrenciaAgendamentoPresencaStatus.Processando);
        var codigoValidado = ValidarCodigo(codigo, MessageCodes.PresenceScheduleTimeZoneInvalid);
        Status = OcorrenciaAgendamentoPresencaStatus.Falha;
        CodigoFalha = codigoValidado;
        LimparClaim();
        Touch(agora);
    }

    private void ExigirStatus(OcorrenciaAgendamentoPresencaStatus esperado)
    {
        if (Status != esperado)
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }
    }

    private static string ValidarCodigo(string? codigo, string codigoPermitido)
    {
        var codigoNormalizado = codigo?.Trim();
        if (string.IsNullOrEmpty(codigoNormalizado)
            || codigoNormalizado.Length > 16
            || !string.Equals(codigoNormalizado, codigoPermitido, StringComparison.Ordinal))
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }

        return codigoNormalizado;
    }

    private void LimparClaim()
    {
        ClaimId = null;
        ClaimExpiresAt = null;
    }

    private void Touch(DateTimeOffset agora)
    {
        AtualizadaEm = agora;
    }
}

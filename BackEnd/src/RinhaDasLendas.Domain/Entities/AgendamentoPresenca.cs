using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Domain.Entities;

public sealed class AgendamentoPresenca
{
    private static readonly string[] CamposCriacao =
    [
        nameof(Nome),
        nameof(Observacao),
        "DiasSemana",
        nameof(HorarioPublicacaoLocal),
        nameof(HorarioEncerramentoLocal),
        nameof(Status)
    ];

    private readonly List<AgendamentoPresencaDiaSemana> _diasSemana = [];
    private readonly List<OcorrenciaAgendamentoPresenca> _ocorrencias = [];
    private readonly List<HistoricoAgendamentoPresenca> _historicos = [];

    private AgendamentoPresenca()
    {
    }

    public AgendamentoPresenca(
        string nome,
        string? observacao,
        TimeOnly publicacao,
        TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias,
        DateOnly ultimaDataAvaliada,
        Guid responsavelId,
        DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        ValidarEAplicarConfiguracao(nome, observacao, publicacao, encerramento, dias);
        Status = AgendamentoPresencaStatus.Ativo;
        AtivadoEm = agora;
        UltimaDataAvaliada = ultimaDataAvaliada;
        CriadoPorUsuarioId = responsavelId;
        CriadoEm = agora;
        AtualizadoEm = agora;
        RegistrarHistorico(AgendamentoPresencaAcao.Criado, responsavelId, agora, CamposCriacao);
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Observacao { get; private set; }
    public TimeOnly HorarioPublicacaoLocal { get; private set; }
    public TimeOnly HorarioEncerramentoLocal { get; private set; }
    public AgendamentoPresencaStatus Status { get; private set; }
    public DateTimeOffset AtivadoEm { get; private set; }
    public DateTimeOffset? PausadoEm { get; private set; }
    public DateTimeOffset? ArquivadoEm { get; private set; }
    public DateOnly UltimaDataAvaliada { get; private set; }
    public Guid CriadoPorUsuarioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }
    public IReadOnlyCollection<AgendamentoPresencaDiaSemana> DiasSemana => _diasSemana.AsReadOnly();
    public IReadOnlyCollection<OcorrenciaAgendamentoPresenca> Ocorrencias => _ocorrencias.AsReadOnly();
    public IReadOnlyCollection<HistoricoAgendamentoPresenca> Historicos => _historicos.AsReadOnly();

    public void Editar(
        string nome,
        string? observacao,
        TimeOnly publicacao,
        TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias,
        Guid responsavelId,
        DateTimeOffset agora)
    {
        ExigirNaoArquivada();
        var nomeNormalizado = NormalizarNome(nome);
        var observacaoNormalizada = NormalizarObservacao(observacao);
        ValidarConfiguracao(nomeNormalizado, observacaoNormalizada, publicacao, encerramento, dias);
        var diasOrdenados = dias.Order().ToArray();
        var camposAlterados = new List<string>();

        AdicionarSeAlterado(camposAlterados, nameof(Nome), Nome != nomeNormalizado);
        AdicionarSeAlterado(camposAlterados, nameof(Observacao), Observacao != observacaoNormalizada);
        AdicionarSeAlterado(camposAlterados, nameof(HorarioPublicacaoLocal), HorarioPublicacaoLocal != publicacao);
        AdicionarSeAlterado(camposAlterados, nameof(HorarioEncerramentoLocal), HorarioEncerramentoLocal != encerramento);
        AdicionarSeAlterado(camposAlterados, "DiasSemana", !_diasSemana.Select(item => item.DiaSemana).SequenceEqual(diasOrdenados));

        Nome = nomeNormalizado;
        Observacao = observacaoNormalizada;
        HorarioPublicacaoLocal = publicacao;
        HorarioEncerramentoLocal = encerramento;
        SubstituirDias(diasOrdenados);
        Touch(agora);
        RegistrarHistorico(AgendamentoPresencaAcao.Editado, responsavelId, agora, camposAlterados);
    }

    public void Pausar(Guid responsavelId, DateTimeOffset agora)
    {
        ExigirNaoArquivada();
        var alterouStatus = Status == AgendamentoPresencaStatus.Ativo;
        if (alterouStatus)
        {
            Status = AgendamentoPresencaStatus.Pausado;
            PausadoEm = agora;
        }

        Touch(agora);
        RegistrarHistorico(AgendamentoPresencaAcao.Pausado, responsavelId, agora, alterouStatus ? [nameof(Status)] : []);
    }

    public void Reativar(Guid responsavelId, DateTimeOffset agora)
    {
        ExigirNaoArquivada();
        var alterouStatus = Status == AgendamentoPresencaStatus.Pausado;
        if (alterouStatus)
        {
            Status = AgendamentoPresencaStatus.Ativo;
            AtivadoEm = agora;
            PausadoEm = null;
        }

        Touch(agora);
        RegistrarHistorico(AgendamentoPresencaAcao.Reativado, responsavelId, agora, alterouStatus ? [nameof(Status)] : []);
    }

    public void Arquivar(Guid responsavelId, DateTimeOffset agora)
    {
        ExigirNaoArquivada();
        Status = AgendamentoPresencaStatus.Arquivado;
        ArquivadoEm = agora;
        Touch(agora);
        RegistrarHistorico(AgendamentoPresencaAcao.Arquivado, responsavelId, agora, [nameof(Status)]);
    }

    public void MarcarDataAvaliada(DateOnly data, DateTimeOffset agora)
    {
        if (data <= UltimaDataAvaliada)
        {
            return;
        }

        UltimaDataAvaliada = data;
        Touch(agora);
    }

    public bool OcorreEm(DateOnly data)
    {
        var dia = data.DayOfWeek == DayOfWeek.Sunday ? DiaSemanaIso.Domingo : (DiaSemanaIso)(int)data.DayOfWeek;
        return _diasSemana.Any(item => item.DiaSemana == dia);
    }

    public void AdicionarOcorrencia(OcorrenciaAgendamentoPresenca ocorrencia)
    {
        if (ocorrencia.AgendamentoPresencaId != Id
            || _ocorrencias.Any(item => item.DataLocal == ocorrencia.DataLocal))
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }

        _ocorrencias.Add(ocorrencia);
    }

    private void ValidarEAplicarConfiguracao(
        string nome,
        string? observacao,
        TimeOnly publicacao,
        TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias)
    {
        var nomeNormalizado = NormalizarNome(nome);
        var observacaoNormalizada = NormalizarObservacao(observacao);
        ValidarConfiguracao(nomeNormalizado, observacaoNormalizada, publicacao, encerramento, dias);
        Nome = nomeNormalizado;
        Observacao = observacaoNormalizada;
        HorarioPublicacaoLocal = publicacao;
        HorarioEncerramentoLocal = encerramento;
        SubstituirDias(dias.Order());
    }

    private static void ValidarConfiguracao(
        string nome,
        string? observacao,
        TimeOnly publicacao,
        TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias)
    {
        if (!AgendamentoPresencaRules.HasValidNameLength(nome))
        {
            throw new DomainException(MessageCodes.PresenceScheduleNameLengthInvalid);
        }

        if (!AgendamentoPresencaRules.HasValidObservationLength(observacao))
        {
            throw new DomainException(MessageCodes.PresenceScheduleObservationTooLong);
        }

        if (!AgendamentoPresencaRules.HasValidDays(dias))
        {
            throw new DomainException(MessageCodes.PresenceScheduleDayRequired);
        }

        if (!AgendamentoPresencaRules.HasUniqueDays(dias))
        {
            throw new DomainException(MessageCodes.PresenceScheduleDayDuplicated);
        }

        if (!AgendamentoPresencaRules.HasValidTimeRange(publicacao, encerramento))
        {
            throw new DomainException(MessageCodes.PresenceScheduleTimeRangeInvalid);
        }
    }

    private static string NormalizarNome(string nome)
    {
        var normalized = AgendamentoPresencaRules.NormalizeName(nome);
        if (normalized.Length == 0)
        {
            throw new DomainException(MessageCodes.PresenceScheduleNameRequired);
        }

        return normalized;
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        return AgendamentoPresencaRules.NormalizeObservation(observacao);
    }

    private void SubstituirDias(IEnumerable<DiaSemanaIso> dias)
    {
        _diasSemana.Clear();
        _diasSemana.AddRange(dias.Select(dia => new AgendamentoPresencaDiaSemana(Id, dia)));
    }

    private void ExigirNaoArquivada()
    {
        if (Status == AgendamentoPresencaStatus.Arquivado)
        {
            throw new DomainException(MessageCodes.PresenceScheduleArchived);
        }
    }

    private void RegistrarHistorico(
        AgendamentoPresencaAcao acao,
        Guid responsavelId,
        DateTimeOffset agora,
        IEnumerable<string> camposAlterados)
    {
        _historicos.Add(new HistoricoAgendamentoPresenca(Id, acao, responsavelId, agora, camposAlterados));
    }

    private void Touch(DateTimeOffset agora)
    {
        AtualizadoEm = agora;
    }

    private static void AdicionarSeAlterado(ICollection<string> campos, string campo, bool alterado)
    {
        if (alterado)
        {
            campos.Add(campo);
        }
    }
}

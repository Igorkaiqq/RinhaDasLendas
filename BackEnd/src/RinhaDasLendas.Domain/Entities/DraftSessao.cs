using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftSessao
{
    public const int MinimoTamanhoTime = 1;
    public const int MaximoTamanhoTime = 5;

    private readonly List<DraftParticipante> _participantes = [];
    private readonly List<DraftEscolha> _escolhas = [];

    private DraftSessao()
    {
    }

    public DraftSessao(
        string nome,
        string? observacoes,
        int tamanhoTime,
        Guid capitaoTimeAId,
        Guid capitaoTimeBId,
        DraftCriterioSelecao criterioCapitaes,
        DraftTime primeiroTime,
        DraftCriterioSelecao criterioPrimeiroPick,
        IEnumerable<Guid> jogadoresIds)
    {
        Id = Guid.NewGuid();
        Status = DraftStatus.Aberto;
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;
        AtualizarDados(nome, observacoes, tamanhoTime);
        ConfigurarParticipantes(capitaoTimeAId, capitaoTimeBId, criterioCapitaes, primeiroTime, criterioPrimeiroPick, jogadoresIds);
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Observacoes { get; private set; }
    public DraftStatus Status { get; private set; }
    public int TamanhoTime { get; private set; }
    public Guid CapitaoTimeAId { get; private set; }
    public Guid CapitaoTimeBId { get; private set; }
    public DraftCriterioSelecao CriterioCapitaes { get; private set; }
    public DraftTime? ProximoTime { get; private set; }
    public DraftCriterioSelecao CriterioPrimeiroPick { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public IReadOnlyCollection<DraftParticipante> Participantes => _participantes;
    public IReadOnlyCollection<DraftEscolha> Escolhas => _escolhas;

    public void RegistrarPick(Guid jogadorId)
    {
        if (Status != DraftStatus.Aberto)
        {
            throw new DomainException("Draft nao esta aberto para escolhas.");
        }

        if (ProximoTime is null)
        {
            throw new DomainException("Draft nao possui proximo time definido.");
        }

        var participante = _participantes.FirstOrDefault(item => item.JogadorId == jogadorId);
        if (participante is null)
        {
            throw new DomainException("Jogador nao participa deste draft.");
        }

        if (!participante.Disponivel)
        {
            throw new DomainException("Jogador ja foi escolhido neste draft.");
        }

        if (QuantidadeNoTime(ProximoTime.Value) >= TamanhoTime)
        {
            throw new DomainException("Time atual ja atingiu o limite de jogadores.");
        }

        var timeEscolha = ProximoTime.Value;
        participante.AtribuirTime(timeEscolha);
        _escolhas.Add(new DraftEscolha(_escolhas.Count + 1, timeEscolha, CapitaoDoTime(timeEscolha), jogadorId));
        AvancarProximoTime();
        Touch();
    }

    public void Cancelar(string? motivo)
    {
        if (Status != DraftStatus.Aberto)
        {
            throw new DomainException("Apenas drafts abertos podem ser cancelados.");
        }

        Status = DraftStatus.Cancelado;
        ProximoTime = null;
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Touch();
    }

    private void AtualizarDados(string nome, string? observacoes, int tamanhoTime)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do draft e obrigatorio.");
        }

        if (nome.Trim().Length > 120)
        {
            throw new DomainException("Nome do draft deve ter no maximo 120 caracteres.");
        }

        if (tamanhoTime is < MinimoTamanhoTime or > MaximoTamanhoTime)
        {
            throw new DomainException("Tamanho do time deve estar entre 1 e 5.");
        }

        Nome = nome.Trim();
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        TamanhoTime = tamanhoTime;
        Touch();
    }

    private void ConfigurarParticipantes(
        Guid capitaoTimeAId,
        Guid capitaoTimeBId,
        DraftCriterioSelecao criterioCapitaes,
        DraftTime primeiroTime,
        DraftCriterioSelecao criterioPrimeiroPick,
        IEnumerable<Guid> jogadoresIds)
    {
        var jogadores = jogadoresIds.Where(id => id != Guid.Empty).ToList();
        if (jogadores.Distinct().Count() != jogadores.Count)
        {
            throw new DomainException("O mesmo jogador nao pode aparecer mais de uma vez no draft.");
        }

        if (capitaoTimeAId == Guid.Empty || capitaoTimeBId == Guid.Empty || capitaoTimeAId == capitaoTimeBId)
        {
            throw new DomainException("Informe dois capitaes distintos para o draft.");
        }

        if (!jogadores.Contains(capitaoTimeAId) || !jogadores.Contains(capitaoTimeBId))
        {
            throw new DomainException("Capitaes devem fazer parte dos jogadores do draft.");
        }

        if (jogadores.Count < 2)
        {
            throw new DomainException("Informe pelo menos dois jogadores para o draft.");
        }

        CapitaoTimeAId = capitaoTimeAId;
        CapitaoTimeBId = capitaoTimeBId;
        CriterioCapitaes = criterioCapitaes;
        ProximoTime = primeiroTime;
        CriterioPrimeiroPick = criterioPrimeiroPick;

        foreach (var jogadorId in jogadores)
        {
            var time = jogadorId == capitaoTimeAId ? DraftTime.TimeA : jogadorId == capitaoTimeBId ? DraftTime.TimeB : (DraftTime?)null;
            _participantes.Add(new DraftParticipante(jogadorId, time, time is not null));
        }

        NormalizarProximoTime();
    }

    private void AvancarProximoTime()
    {
        if (QuantidadeNoTime(DraftTime.TimeA) >= TamanhoTime && QuantidadeNoTime(DraftTime.TimeB) >= TamanhoTime)
        {
            Status = DraftStatus.Concluido;
            ProximoTime = null;
            return;
        }

        if (!_participantes.Any(participante => participante.Disponivel))
        {
            Status = DraftStatus.Concluido;
            ProximoTime = null;
            return;
        }

        var candidato = ProximoTime == DraftTime.TimeA ? DraftTime.TimeB : DraftTime.TimeA;
        if (QuantidadeNoTime(candidato) < TamanhoTime)
        {
            ProximoTime = candidato;
            return;
        }

        ProximoTime = candidato == DraftTime.TimeA ? DraftTime.TimeB : DraftTime.TimeA;
    }

    private void NormalizarProximoTime()
    {
        if (QuantidadeNoTime(DraftTime.TimeA) >= TamanhoTime && QuantidadeNoTime(DraftTime.TimeB) >= TamanhoTime)
        {
            Status = DraftStatus.Concluido;
            ProximoTime = null;
            return;
        }

        if (ProximoTime is not null && QuantidadeNoTime(ProximoTime.Value) < TamanhoTime)
        {
            return;
        }

        ProximoTime = QuantidadeNoTime(DraftTime.TimeA) < TamanhoTime ? DraftTime.TimeA : DraftTime.TimeB;
    }

    private int QuantidadeNoTime(DraftTime time)
    {
        return _participantes.Count(participante => participante.Time == time);
    }

    private Guid CapitaoDoTime(DraftTime time)
    {
        return time == DraftTime.TimeA ? CapitaoTimeAId : CapitaoTimeBId;
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}

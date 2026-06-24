using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagem
{
    public const int MinimoTamanhoEquipe = 1;
    public const int MaximoTamanhoEquipe = 5;
    public const int DuracaoTurnoPadraoSegundos = 30;

    private static readonly string[] CoresPadrao = ["blue", "red", "yellow", "green", "purple", "cyan", "orange", "pink"];
    private readonly List<DraftMontagemTime> _times = [];
    private readonly List<DraftMontagemParticipante> _participantes = [];
    private readonly List<DraftMontagemEscolha> _escolhas = [];
    private readonly List<DraftMontagemSubstituicao> _substituicoes = [];

    private DraftMontagem()
    {
    }

    public DraftMontagem(
        string nome,
        string? observacoes,
        int tamanhoEquipe,
        DraftMontagemCriterioCapitaes criterioCapitaes,
        IReadOnlyCollection<Guid> jogadoresIds,
        IReadOnlyCollection<Guid> capitaesIds)
    {
        Id = Guid.NewGuid();
        Status = DraftMontagemStatus.Aberta;
        Modo = DraftMontagemModo.Manual;
        DuracaoTurnoSegundos = DuracaoTurnoPadraoSegundos;
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;
        AtualizarDados(nome, observacoes, tamanhoEquipe);
        ConfigurarInicial(criterioCapitaes, jogadoresIds, capitaesIds);
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Observacoes { get; private set; }
    public DraftMontagemStatus Status { get; private set; }
    public DraftMontagemModo Modo { get; private set; }
    public int TamanhoEquipe { get; private set; }
    public int QuantidadeTimes { get; private set; }
    public int QuantidadeReservas { get; private set; }
    public DraftMontagemCriterioCapitaes CriterioCapitaes { get; private set; }
    public Guid? TurnoAtualTimeId { get; private set; }
    public Guid? TurnoAtualCapitaoId { get; private set; }
    public int? TurnoSequencia { get; private set; }
    public DateTimeOffset? TurnoIniciadoEm { get; private set; }
    public DateTimeOffset? TurnoExpiraEm { get; private set; }
    public int DuracaoTurnoSegundos { get; private set; }
    public long VersaoEstado { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public IReadOnlyCollection<DraftMontagemTime> Times => _times;
    public IReadOnlyCollection<DraftMontagemParticipante> Participantes => _participantes;
    public IReadOnlyCollection<DraftMontagemEscolha> Escolhas => _escolhas;
    public IReadOnlyCollection<DraftMontagemSubstituicao> Substituicoes => _substituicoes;

    public static (int QuantidadeTimes, int QuantidadeReservas) CalcularEstrutura(int totalJogadores, int tamanhoEquipe)
    {
        if (tamanhoEquipe is < MinimoTamanhoEquipe or > MaximoTamanhoEquipe)
        {
            throw new DomainException(MessageCodes.TeamSizeRange);
        }

        return (totalJogadores / tamanhoEquipe, totalJogadores % tamanhoEquipe);
    }

    public void SalvarLayout(IReadOnlyCollection<DraftMontagemLayoutTime> times, IReadOnlyCollection<DraftMontagemLayoutParticipante> livres, IReadOnlyCollection<DraftMontagemLayoutParticipante> reservas)
    {
        EnsureAberta();
        if (Modo == DraftMontagemModo.TempoReal)
        {
            throw new DomainException(MessageCodes.DraftMontagemRealtimeLayoutLocked);
        }

        if (times.Count != QuantidadeTimes)
        {
            throw new DomainException(MessageCodes.InconsistentDataFound);
        }

        var atribuicoes = new HashSet<Guid>();
        foreach (var timeLayout in times)
        {
            var time = _times.FirstOrDefault(item => item.Id == timeLayout.TimeId) ?? throw new DomainException(MessageCodes.TeamNotFound);
            if (timeLayout.Jogadores.Count > TamanhoEquipe)
            {
                throw new DomainException(MessageCodes.TeamPlayerLimitReached);
            }

            if (timeLayout.CapitaoId is null || !timeLayout.Jogadores.Any(item => item.JogadorId == timeLayout.CapitaoId))
            {
                throw new DomainException(MessageCodes.TeamCaptainMustBeMember);
            }

            time.Atualizar(timeLayout.Nome, timeLayout.CapitaoId);
            foreach (var item in timeLayout.Jogadores)
            {
                AtribuirParticipante(item, atribuicoes, participante => participante.AtribuirTime(time.Id, item.Ordem, item.JogadorId == timeLayout.CapitaoId, item.RotaContextual));
            }
        }

        foreach (var item in livres)
        {
            AtribuirParticipante(item, atribuicoes, participante => participante.AtribuirLivre(item.Ordem, item.RotaContextual));
        }

        foreach (var item in reservas)
        {
            AtribuirParticipante(item, atribuicoes, participante => participante.AtribuirReserva(item.Ordem, item.RotaContextual));
        }

        if (atribuicoes.Count != _participantes.Count)
        {
            throw new DomainException(MessageCodes.InconsistentDataFound);
        }

        Touch();
    }

    public void IniciarTempoReal(DateTimeOffset agora)
    {
        EnsureAberta();
        if (Modo == DraftMontagemModo.TempoReal)
        {
            throw new DomainException(MessageCodes.DraftMontagemAlreadyRealtime);
        }

        foreach (var time in _times)
        {
            var membros = MembrosDoTime(time.Id).ToList();
            if (time.CapitaoId is null || membros.All(participante => participante.JogadorId != time.CapitaoId) || membros.Count(participante => participante.Capitao) != 1)
            {
                throw new DomainException(MessageCodes.TeamCaptainMustBeMember);
            }
        }

        Modo = DraftMontagemModo.TempoReal;
        DuracaoTurnoSegundos = DuracaoTurnoPadraoSegundos;
        IniciarProximoTurno(agora, 1);
        Touch();
    }

    public void RegistrarPickTempoReal(Guid capitaoId, Guid jogadorId, DateTimeOffset agora)
    {
        EnsureTurnoAtivo(agora);
        if (TurnoAtualCapitaoId != capitaoId)
        {
            throw new DomainException(MessageCodes.DraftMontagemNotCaptainTurn);
        }

        var time = _times.FirstOrDefault(item => item.Id == TurnoAtualTimeId) ?? throw new DomainException(MessageCodes.TeamNotFound);
        if (QuantidadeNoTime(time.Id) >= TamanhoEquipe)
        {
            throw new DomainException(MessageCodes.TeamPlayerLimitReached);
        }

        var participante = _participantes.FirstOrDefault(item => item.JogadorId == jogadorId) ?? throw new DomainException(MessageCodes.PlayerNotFound);
        if (participante.Estado == DraftMontagemParticipanteEstado.Reserva)
        {
            throw new DomainException(MessageCodes.DraftMontagemReserveCannotBePicked);
        }

        if (participante.Estado != DraftMontagemParticipanteEstado.Livre)
        {
            throw new DomainException(MessageCodes.DraftPlayerAlreadyPicked);
        }

        participante.AtribuirTime(time.Id, QuantidadeNoTime(time.Id) + 1, false, participante.RotaContextual);
        _escolhas.Add(new DraftMontagemEscolha(TurnoSequencia!.Value, time.Id, capitaoId, jogadorId, DraftMontagemEscolhaTipo.Escolha, agora));
        IniciarProximoTurno(agora, TurnoSequencia.Value + 1);
        Touch();
    }

    public bool AvancarTurnoPorTimeout(DateTimeOffset agora)
    {
        if (Status != DraftMontagemStatus.Aberta || Modo != DraftMontagemModo.TempoReal || TurnoExpiraEm is null || TurnoExpiraEm > agora)
        {
            return false;
        }

        if (TurnoAtualTimeId is null || TurnoAtualCapitaoId is null || TurnoSequencia is null)
        {
            return false;
        }

        _escolhas.Add(new DraftMontagemEscolha(TurnoSequencia.Value, TurnoAtualTimeId.Value, TurnoAtualCapitaoId.Value, null, DraftMontagemEscolhaTipo.Timeout, agora));
        IniciarProximoTurno(agora, TurnoSequencia.Value + 1);
        Touch();
        return true;
    }

    public void SubstituirPorReserva(Guid timeId, Guid jogadorSaiuId, Guid reservaEntrouId, string? motivo, Guid responsavelUsuarioId, DateTimeOffset agora)
    {
        if (Status == DraftMontagemStatus.Cancelada)
        {
            throw new DomainException(MessageCodes.DraftClosed);
        }

        var time = _times.FirstOrDefault(item => item.Id == timeId) ?? throw new DomainException(MessageCodes.TeamNotFound);
        var jogadorSaiu = _participantes.FirstOrDefault(item => item.JogadorId == jogadorSaiuId && item.TimeId == time.Id && item.Estado == DraftMontagemParticipanteEstado.Time)
            ?? throw new DomainException(MessageCodes.DraftMontagemPlayerNotInTeam);
        var reserva = _participantes.FirstOrDefault(item => item.JogadorId == reservaEntrouId && item.Estado == DraftMontagemParticipanteEstado.Reserva)
            ?? throw new DomainException(MessageCodes.DraftMontagemReserveRequired);

        var ordem = jogadorSaiu.Ordem;
        var rota = jogadorSaiu.RotaContextual;
        var capitaoSaiu = jogadorSaiu.Capitao;
        jogadorSaiu.AtribuirReserva(reserva.Ordem, jogadorSaiu.RotaContextual);
        reserva.AtribuirTime(time.Id, ordem, capitaoSaiu, rota);
        if (capitaoSaiu)
        {
            time.Atualizar(time.Nome, reserva.JogadorId);
        }

        _substituicoes.Add(new DraftMontagemSubstituicao(time.Id, jogadorSaiuId, reservaEntrouId, motivo, responsavelUsuarioId, agora));
        Touch();
    }

    public void SortearCapitaes()
    {
        EnsureAberta();
        foreach (var participante in _participantes)
        {
            participante.DefinirCapitao(false);
        }

        foreach (var time in _times)
        {
            var candidatos = _participantes.Where(participante => participante.TimeId == time.Id).OrderBy(_ => Guid.NewGuid()).ToList();
            if (candidatos.Count == 0)
            {
                throw new DomainException(MessageCodes.TeamPlayersRequired);
            }

            var capitao = candidatos[0];
            time.Atualizar(time.Nome, capitao.JogadorId);
            capitao.DefinirCapitao(true);
        }

        CriterioCapitaes = DraftMontagemCriterioCapitaes.Sorteio;
        Touch();
    }

    public void Finalizar()
    {
        EnsureAberta();
        foreach (var time in _times)
        {
            var membros = _participantes.Where(participante => participante.TimeId == time.Id).ToList();
            if (membros.Count > TamanhoEquipe)
            {
                throw new DomainException(MessageCodes.TeamPlayerLimitReached);
            }

            if (time.CapitaoId is null || membros.Count(participante => participante.Capitao) != 1 || membros.All(participante => participante.JogadorId != time.CapitaoId))
            {
                throw new DomainException(MessageCodes.TeamCaptainMustBeMember);
            }
        }

        Status = DraftMontagemStatus.Finalizada;
        LimparTurno();
        Touch();
    }

    public void Cancelar(string? motivo)
    {
        EnsureAberta();
        Status = DraftMontagemStatus.Cancelada;
        LimparTurno();
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Touch();
    }

    private void AtualizarDados(string nome, string? observacoes, int tamanhoEquipe)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.DraftNameRequired);
        }

        if (tamanhoEquipe is < MinimoTamanhoEquipe or > MaximoTamanhoEquipe)
        {
            throw new DomainException(MessageCodes.TeamSizeRange);
        }

        Nome = nome.Trim();
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        TamanhoEquipe = tamanhoEquipe;
        Touch();
    }

    private void ConfigurarInicial(DraftMontagemCriterioCapitaes criterioCapitaes, IReadOnlyCollection<Guid> jogadoresIds, IReadOnlyCollection<Guid> capitaesIds)
    {
        var jogadores = jogadoresIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (jogadores.Count != jogadoresIds.Count)
        {
            throw new DomainException(MessageCodes.DraftPlayerAlreadyPicked);
        }

        var (quantidadeTimes, quantidadeReservas) = CalcularEstrutura(jogadores.Count, TamanhoEquipe);
        if (quantidadeTimes < 1)
        {
            throw new DomainException(MessageCodes.DraftMontagemInsufficientPlayers);
        }

        if (capitaesIds.Count != quantidadeTimes || capitaesIds.Distinct().Count() != capitaesIds.Count || capitaesIds.Any(id => !jogadores.Contains(id)))
        {
            throw new DomainException(MessageCodes.DraftMontagemCaptainsRequired);
        }

        QuantidadeTimes = quantidadeTimes;
        QuantidadeReservas = quantidadeReservas;
        CriterioCapitaes = criterioCapitaes;

        for (var index = 0; index < quantidadeTimes; index++)
        {
            var time = new DraftMontagemTime($"Time {index + 1}", index + 1, CoresPadrao[index % CoresPadrao.Length]);
            time.Atualizar(time.Nome, capitaesIds.ElementAt(index));
            _times.Add(time);
        }

        var reservas = jogadores.TakeLast(quantidadeReservas).ToHashSet();
        var ordemLivre = 1;
        var ordemReserva = 1;
        foreach (var jogadorId in jogadores)
        {
            var participante = new DraftMontagemParticipante(jogadorId, reservas.Contains(jogadorId) ? DraftMontagemParticipanteEstado.Reserva : DraftMontagemParticipanteEstado.Livre, reservas.Contains(jogadorId) ? ordemReserva++ : ordemLivre++);
            var timeCapitao = _times.FirstOrDefault(time => time.CapitaoId == jogadorId);
            if (timeCapitao is not null)
            {
                participante.AtribuirTime(timeCapitao.Id, 0, true, null);
            }
            _participantes.Add(participante);
        }
    }

    private void AtribuirParticipante(DraftMontagemLayoutParticipante item, HashSet<Guid> atribuicoes, Action<DraftMontagemParticipante> atribuir)
    {
        if (!atribuicoes.Add(item.JogadorId))
        {
            throw new DomainException(MessageCodes.DraftPlayerAlreadyPicked);
        }

        var participante = _participantes.FirstOrDefault(current => current.JogadorId == item.JogadorId) ?? throw new DomainException(MessageCodes.PlayerNotFound);
        atribuir(participante);
    }

    private void EnsureAberta()
    {
        if (Status != DraftMontagemStatus.Aberta)
        {
            throw new DomainException(MessageCodes.DraftClosed);
        }
    }

    private void EnsureTurnoAtivo(DateTimeOffset agora)
    {
        EnsureAberta();
        if (Modo != DraftMontagemModo.TempoReal || TurnoAtualTimeId is null || TurnoAtualCapitaoId is null || TurnoSequencia is null)
        {
            throw new DomainException(MessageCodes.DraftMontagemRealtimeNotStarted);
        }

        if (TurnoExpiraEm <= agora)
        {
            throw new DomainException(MessageCodes.DraftMontagemTurnExpired);
        }
    }

    private void IniciarProximoTurno(DateTimeOffset agora, int sequencia)
    {
        var proximoTime = ProximoTimeElegivel();
        if (proximoTime is null || !_participantes.Any(participante => participante.Estado == DraftMontagemParticipanteEstado.Livre))
        {
            Status = DraftMontagemStatus.Finalizada;
            LimparTurno();
            return;
        }

        TurnoAtualTimeId = proximoTime.Id;
        TurnoAtualCapitaoId = proximoTime.CapitaoId;
        TurnoSequencia = sequencia;
        TurnoIniciadoEm = agora;
        TurnoExpiraEm = agora.AddSeconds(DuracaoTurnoSegundos);
    }

    private DraftMontagemTime? ProximoTimeElegivel()
    {
        var ordenados = _times.OrderBy(time => time.Ordem).ToList();
        if (ordenados.Count == 0)
        {
            return null;
        }

        var startIndex = 0;
        if (TurnoAtualTimeId is not null)
        {
            var currentIndex = ordenados.FindIndex(time => time.Id == TurnoAtualTimeId);
            startIndex = currentIndex < 0 ? 0 : currentIndex + 1;
        }

        for (var offset = 0; offset < ordenados.Count; offset++)
        {
            var time = ordenados[(startIndex + offset) % ordenados.Count];
            if (time.CapitaoId is not null && QuantidadeNoTime(time.Id) < TamanhoEquipe)
            {
                return time;
            }
        }

        return null;
    }

    private IEnumerable<DraftMontagemParticipante> MembrosDoTime(Guid timeId)
    {
        return _participantes.Where(participante => participante.TimeId == timeId && participante.Estado == DraftMontagemParticipanteEstado.Time);
    }

    private int QuantidadeNoTime(Guid timeId)
    {
        return MembrosDoTime(timeId).Count();
    }

    private void LimparTurno()
    {
        TurnoAtualTimeId = null;
        TurnoAtualCapitaoId = null;
        TurnoSequencia = null;
        TurnoIniciadoEm = null;
        TurnoExpiraEm = null;
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
        VersaoEstado++;
    }
}

public sealed record DraftMontagemLayoutTime(Guid TimeId, string Nome, Guid? CapitaoId, IReadOnlyCollection<DraftMontagemLayoutParticipante> Jogadores);

public sealed record DraftMontagemLayoutParticipante(Guid JogadorId, int Ordem, Rota? RotaContextual);

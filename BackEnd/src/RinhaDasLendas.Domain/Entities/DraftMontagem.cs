using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagem
{
    public const int MinimoTamanhoEquipe = 1;
    public const int MaximoTamanhoEquipe = 5;

    private static readonly string[] CoresPadrao = ["blue", "red", "yellow", "green", "purple", "cyan", "orange", "pink"];
    private readonly List<DraftMontagemTime> _times = [];
    private readonly List<DraftMontagemParticipante> _participantes = [];

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
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;
        AtualizarDados(nome, observacoes, tamanhoEquipe);
        ConfigurarInicial(criterioCapitaes, jogadoresIds, capitaesIds);
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? Observacoes { get; private set; }
    public DraftMontagemStatus Status { get; private set; }
    public int TamanhoEquipe { get; private set; }
    public int QuantidadeTimes { get; private set; }
    public int QuantidadeReservas { get; private set; }
    public DraftMontagemCriterioCapitaes CriterioCapitaes { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public IReadOnlyCollection<DraftMontagemTime> Times => _times;
    public IReadOnlyCollection<DraftMontagemParticipante> Participantes => _participantes;

    public static (int QuantidadeTimes, int QuantidadeReservas) CalcularEstrutura(int totalJogadores, int tamanhoEquipe)
    {
        if (tamanhoEquipe is < MinimoTamanhoEquipe or > MaximoTamanhoEquipe)
        {
            throw new DomainException("Tamanho da equipe deve estar entre 1 e 5.");
        }

        return (totalJogadores / tamanhoEquipe, totalJogadores % tamanhoEquipe);
    }

    public void SalvarLayout(IReadOnlyCollection<DraftMontagemLayoutTime> times, IReadOnlyCollection<DraftMontagemLayoutParticipante> livres, IReadOnlyCollection<DraftMontagemLayoutParticipante> reservas)
    {
        EnsureAberta();
        if (times.Count != QuantidadeTimes)
        {
            throw new DomainException("Quantidade de times invalida para a montagem.");
        }

        var atribuicoes = new HashSet<Guid>();
        foreach (var timeLayout in times)
        {
            var time = _times.FirstOrDefault(item => item.Id == timeLayout.TimeId) ?? throw new DomainException("Time da montagem nao encontrado.");
            if (timeLayout.Jogadores.Count > TamanhoEquipe)
            {
                throw new DomainException("Time excede o tamanho configurado.");
            }

            if (timeLayout.CapitaoId is null || !timeLayout.Jogadores.Any(item => item.JogadorId == timeLayout.CapitaoId))
            {
                throw new DomainException("Cada time deve possuir um capitao dentro do proprio time.");
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
            throw new DomainException("Todos os participantes devem aparecer exatamente uma vez na montagem.");
        }

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
                throw new DomainException("Todos os times devem possuir jogadores para sortear capitaes.");
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
                throw new DomainException("Time excede o tamanho configurado.");
            }

            if (time.CapitaoId is null || membros.Count(participante => participante.Capitao) != 1 || membros.All(participante => participante.JogadorId != time.CapitaoId))
            {
                throw new DomainException("Todos os times devem possuir exatamente um capitao.");
            }
        }

        Status = DraftMontagemStatus.Finalizada;
        Touch();
    }

    public void Cancelar(string? motivo)
    {
        EnsureAberta();
        Status = DraftMontagemStatus.Cancelada;
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Touch();
    }

    private void AtualizarDados(string nome, string? observacoes, int tamanhoEquipe)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da montagem e obrigatorio.");
        }

        if (tamanhoEquipe is < MinimoTamanhoEquipe or > MaximoTamanhoEquipe)
        {
            throw new DomainException("Tamanho da equipe deve estar entre 1 e 5.");
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
            throw new DomainException("O mesmo jogador nao pode aparecer mais de uma vez na montagem.");
        }

        var (quantidadeTimes, quantidadeReservas) = CalcularEstrutura(jogadores.Count, TamanhoEquipe);
        if (quantidadeTimes < 1)
        {
            throw new DomainException("Jogadores insuficientes para formar um time completo.");
        }

        if (capitaesIds.Count != quantidadeTimes || capitaesIds.Distinct().Count() != capitaesIds.Count || capitaesIds.Any(id => !jogadores.Contains(id)))
        {
            throw new DomainException("Informe um capitao valido e distinto para cada time.");
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
            throw new DomainException("Jogador duplicado na montagem.");
        }

        var participante = _participantes.FirstOrDefault(current => current.JogadorId == item.JogadorId) ?? throw new DomainException("Participante da montagem nao encontrado.");
        atribuir(participante);
    }

    private void EnsureAberta()
    {
        if (Status != DraftMontagemStatus.Aberta)
        {
            throw new DomainException("A montagem nao esta aberta para alteracoes.");
        }
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}

public sealed record DraftMontagemLayoutTime(Guid TimeId, string Nome, Guid? CapitaoId, IReadOnlyCollection<DraftMontagemLayoutParticipante> Jogadores);

public sealed record DraftMontagemLayoutParticipante(Guid JogadorId, int Ordem, Rota? RotaContextual);

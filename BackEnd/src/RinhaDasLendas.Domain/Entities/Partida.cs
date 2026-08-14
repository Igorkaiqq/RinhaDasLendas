using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Partida
{
    private readonly List<PickPartida> _picks = [];

    private Partida()
    {
    }

    internal Partida(Guid serieId, int ordem, DateTimeOffset criadaEm)
    {
        if (serieId == Guid.Empty || ordem <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var criadaEmUtc = criadaEm.ToUniversalTime();

        Id = Guid.NewGuid();
        SerieId = serieId;
        Ordem = ordem;
        Estado = PartidaEstado.Rascunho;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
    }

    public Guid Id { get; private set; }
    public Guid SerieId { get; private set; }
    public int Ordem { get; private set; }
    public PartidaEstado Estado { get; private set; }
    public Guid? LadoVencedorId { get; private set; }
    public MotivoTerminoPartida? MotivoTermino { get; private set; }
    public DecisaoPicksRemake? DecisaoPicksRemake { get; private set; }
    public bool ConflitoFearless { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }
    public DateTimeOffset? ConfirmadaEm { get; private set; }
    public IReadOnlyCollection<PickPartida> Picks => _picks.AsReadOnly();

    internal void RegistrarPicks(
        IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> picks,
        DateTimeOffset atualizadoEm)
    {
        if (Estado != PartidaEstado.Rascunho)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        var proximaVersao = _picks.Count == 0 ? 1 : _picks.Max(pick => pick.VersaoFato) + 1;
        var novosPicks = picks
            .Select(pick => PickPartida.CriarNovaVersao(
                this,
                pick.LadoSerieId,
                pick.ChampionId,
                pick.Ordem,
                proximaVersao,
                atualizadoEm))
            .ToArray();

        foreach (var pick in _picks.Where(pick => pick.Valido))
        {
            pick.Invalidar();
        }

        _picks.AddRange(novosPicks);
        Atualizar(atualizadoEm);
    }

    internal void CorrigirPicks(
        IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> picks,
        DateTimeOffset atualizadoEm)
    {
        if (Estado is PartidaEstado.Rascunho or PartidaEstado.Anulada
            || !_picks.Any(pick => pick.Valido))
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        var proximaVersao = _picks.Max(pick => pick.VersaoFato) + 1;
        var novosPicks = picks
            .Select(pick => PickPartida.CriarNovaVersao(
                this,
                pick.LadoSerieId,
                pick.ChampionId,
                pick.Ordem,
                proximaVersao,
                atualizadoEm))
            .ToArray();

        foreach (var pick in _picks.Where(pick => pick.Valido))
        {
            pick.Invalidar();
        }

        _picks.AddRange(novosPicks);
        Atualizar(atualizadoEm);
    }

    internal void Confirmar(
        Guid ladoVencedorId,
        MotivoTerminoPartida motivoTermino,
        DateTimeOffset confirmadoEm)
    {
        if (Estado != PartidaEstado.Rascunho || !Enum.IsDefined(motivoTermino))
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = PartidaEstado.Confirmada;
        LadoVencedorId = ladoVencedorId;
        MotivoTermino = motivoTermino;
        DecisaoPicksRemake = null;
        ConfirmadaEm = confirmadoEm.ToUniversalTime();
        Atualizar(confirmadoEm);
    }

    internal void MarcarComoRemake(DecisaoPicksRemake decisao, DateTimeOffset atualizadoEm)
    {
        if (Estado != PartidaEstado.Rascunho || !Enum.IsDefined(decisao))
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = PartidaEstado.Remake;
        LadoVencedorId = null;
        MotivoTermino = null;
        DecisaoPicksRemake = decisao;
        ConfirmadaEm = null;
        Atualizar(atualizadoEm);
    }

    internal void CorrigirResultado(
        Guid ladoVencedorId,
        MotivoTerminoPartida motivoTermino,
        DateTimeOffset atualizadoEm)
    {
        if (Estado != PartidaEstado.Confirmada || !Enum.IsDefined(motivoTermino))
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        LadoVencedorId = ladoVencedorId;
        MotivoTermino = motivoTermino;
        Atualizar(atualizadoEm);
    }

    internal void Anular(DateTimeOffset atualizadoEm)
    {
        if (Estado == PartidaEstado.Anulada)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = PartidaEstado.Anulada;
        LadoVencedorId = null;
        MotivoTermino = null;
        DecisaoPicksRemake = null;
        ConfirmadaEm = null;
        ConflitoFearless = false;
        Atualizar(atualizadoEm);
    }

    internal void DefinirConflitoFearless(
        bool conflito,
        DateTimeOffset atualizadoEm,
        bool incrementarVersao)
    {
        if (ConflitoFearless == conflito)
        {
            return;
        }

        ConflitoFearless = conflito;
        if (incrementarVersao)
        {
            Atualizar(atualizadoEm);
        }
    }

    private void Atualizar(DateTimeOffset atualizadoEm)
    {
        Versao++;
        AtualizadaEm = atualizadoEm.ToUniversalTime();
    }
}

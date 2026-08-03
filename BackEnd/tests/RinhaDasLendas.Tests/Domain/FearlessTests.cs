using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using Xunit;

namespace RinhaDasLendas.Tests.Domain;

public sealed class FearlessTests
{
    private static readonly DateTimeOffset Agora = new(2026, 8, 10, 22, 0, 0, TimeSpan.Zero);
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void Deve_rejeitar_composicoes_que_nao_sejam_exatamente_cinco_picks_validos_por_lado()
    {
        var cenarios = new Func<Serie, (Guid LadoSerieId, int ChampionId, int Ordem)[]>[]
        {
            serie => CriarPicksDistribuidos(serie, 1, 5, 4),
            serie => CriarPicksDistribuidos(serie, 20, 5, 6),
            serie => CriarPicksDistribuidos(serie, 40, 6, 4),
            serie => SubstituirPick(
                CriarPicksDistribuidos(serie, 60, 5, 5),
                4,
                CriarPick(serie, 1, 64, 4)),
            serie => SubstituirPick(
                CriarPicksDistribuidos(serie, 80, 5, 5),
                9,
                CriarPick(serie, 2, 80, 5)),
            serie => SubstituirPick(
                CriarPicksDistribuidos(serie, 100, 5, 5),
                9,
                (Guid.NewGuid(), 109, 5)),
            serie => SubstituirPick(
                CriarPicksDistribuidos(serie, 110, 5, 5),
                9,
                (Guid.Empty, 119, 5)),
            serie => Enumerable.Range(0, 5)
                .Select(indice => CriarPick(serie, 1, 130 + indice, indice + 1))
                .ToArray(),
        };

        foreach (var criarPicksInvalidos in cenarios)
        {
            var serie = CriarSerieFearless();
            var partida = serie.AdicionarPartida(UsuarioId, Agora.AddMinutes(1));
            var antes = SerieSnapshot.Capturar(serie);

            var act = () => serie.RegistrarPicks(
                partida.Id,
                criarPicksInvalidos(serie),
                UsuarioId,
                Agora.AddMinutes(2));

            act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
            SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
        }
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 1)]
    public void Deve_bloquear_reuso_bilateral_de_campeao_usado_pelo_lado_oposto(
        int ladoQueTentaReusar,
        int championIdBloqueado)
    {
        var serie = CriarSerieFearless();
        ConfirmarPartida(serie, 1, 1);
        var segundaPartida = serie.AdicionarPartida(UsuarioId, Agora.AddHours(1));
        var picks = CriarPicks(serie, 11, 10);
        var indice = ladoQueTentaReusar == 1 ? 0 : 5;
        picks[indice] = CriarPick(serie, ladoQueTentaReusar, championIdBloqueado, 1);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.RegistrarPicks(
            segundaPartida.Id,
            picks,
            UsuarioId,
            Agora.AddHours(1).AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ChampionBlockedByFearless);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
        serie.ObterBloqueiosFearless(segundaPartida.Ordem).Should().BeEquivalentTo(Enumerable.Range(1, 10));
    }

    [Fact]
    public void Deve_considerar_apenas_picks_validos_da_versao_corrente()
    {
        var serie = CriarSerieFearless();
        var partida = ConfirmarPartida(serie, 1, 1);
        var picksCorrigidos = CriarPicks(serie, 21, 10);

        serie.CorrigirPicks(partida.Id, picksCorrigidos, UsuarioId, Agora.AddHours(1));

        partida.Picks.Should().HaveCount(20);
        partida.Picks.Where(pick => pick.VersaoFato == 1)
            .Should().HaveCount(10).And.OnlyContain(pick => !pick.Valido);
        partida.Picks.Where(pick => pick.VersaoFato == 2)
            .Should().HaveCount(10).And.OnlyContain(pick => pick.Valido);
        serie.ObterBloqueiosFearless(2).Should().BeEquivalentTo(Enumerable.Range(21, 10));
    }

    [Fact]
    public void Deve_ignorar_picks_de_rascunho_ate_a_confirmacao_da_partida()
    {
        var serie = CriarSerieFearless();
        var partida = serie.AdicionarPartida(UsuarioId, Agora.AddMinutes(1));
        serie.RegistrarPicks(partida.Id, CriarPicks(serie, 1, 10), UsuarioId, Agora.AddMinutes(2));

        serie.ObterBloqueiosFearless(2).Should().BeEmpty();

        serie.ConfirmarPartida(
            partida.Id,
            serie.Lados.Single(lado => lado.Ordem == 1).Id,
            MotivoTerminoPartida.Normal,
            UsuarioId,
            Agora.AddMinutes(3));

        serie.ObterBloqueiosFearless(2).Should().BeEquivalentTo(Enumerable.Range(1, 10));
    }

    [Theory]
    [InlineData(DecisaoPicksRemake.PreservarPicks, true)]
    [InlineData(DecisaoPicksRemake.DesconsiderarPicks, false)]
    public void Deve_aplicar_a_decisao_do_remake_aos_bloqueios_fearless(
        DecisaoPicksRemake decisao,
        bool devePreservar)
    {
        var serie = CriarSerieFearless();
        var remake = serie.AdicionarPartida(UsuarioId, Agora.AddMinutes(1));
        serie.RegistrarPicks(remake.Id, CriarPicks(serie, 1, 10), UsuarioId, Agora.AddMinutes(2));

        serie.MarcarPartidaComoRemake(remake.Id, decisao, UsuarioId, Agora.AddMinutes(3));

        remake.Estado.Should().Be(PartidaEstado.Remake);
        remake.LadoVencedorId.Should().BeNull();
        remake.MotivoTermino.Should().BeNull();
        remake.ConfirmadaEm.Should().BeNull();
        remake.DecisaoPicksRemake.Should().Be(decisao);
        serie.Resultado.VitoriasLadoUm.Should().Be(0);
        serie.Resultado.VitoriasLadoDois.Should().Be(0);
        serie.Resultado.QuantidadePartidasValidas.Should().Be(0);
        serie.Resultado.LadoVencedorId.Should().BeNull();
        serie.LadoVencedorId.Should().BeNull();
        serie.Estado.Should().Be(SerieEstado.EmAndamento);
        var bloqueios = serie.ObterBloqueiosFearless(2);
        if (devePreservar)
        {
            bloqueios.Should().BeEquivalentTo(Enumerable.Range(1, 10));
        }
        else
        {
            bloqueios.Should().BeEmpty();
        }
    }

    [Fact]
    public void Deve_excluir_partida_anulada_do_placar_e_dos_bloqueios()
    {
        var serie = CriarSerieFearless();
        var partida = ConfirmarPartida(serie, 1, 1);

        serie.AnularPartida(partida.Id, UsuarioId, Agora.AddHours(1));

        partida.Estado.Should().Be(PartidaEstado.Anulada);
        partida.LadoVencedorId.Should().BeNull();
        partida.MotivoTermino.Should().BeNull();
        serie.Resultado.VitoriasLadoUm.Should().Be(0);
        serie.Resultado.VitoriasLadoDois.Should().Be(0);
        serie.Resultado.QuantidadePartidasValidas.Should().Be(0);
        serie.Resultado.LadoVencedorId.Should().BeNull();
        serie.ObterBloqueiosFearless(2).Should().BeEmpty();
    }

    [Fact]
    public void Deve_isolar_bloqueios_entre_series()
    {
        var primeiraSerie = CriarSerieFearless();
        var segundaSerie = CriarSerieFearless();
        ConfirmarPartida(primeiraSerie, 1, 1);
        var partidaSegundaSerie = segundaSerie.AdicionarPartida(UsuarioId, Agora.AddHours(1));

        var act = () => segundaSerie.RegistrarPicks(
            partidaSegundaSerie.Id,
            CriarPicks(segundaSerie, 1, 10),
            UsuarioId,
            Agora.AddHours(1).AddMinutes(1));

        act.Should().NotThrow();
        segundaSerie.ObterBloqueiosFearless(partidaSegundaSerie.Ordem).Should().BeEmpty();
    }

    [Fact]
    public void Deve_reconstruir_bloqueios_respeitando_a_ordem_competitiva_da_partida()
    {
        var serie = CriarSerieFearless();
        ConfirmarPartida(serie, 1, 1);
        ConfirmarPartida(serie, 11, 2);
        serie.AdicionarPartida(UsuarioId, Agora.AddHours(2));

        serie.ObterBloqueiosFearless(2).Should().BeEquivalentTo(Enumerable.Range(1, 10));
        serie.ObterBloqueiosFearless(3).Should().BeEquivalentTo(Enumerable.Range(1, 20));
    }

    [Fact]
    public void Deve_sinalizar_conflito_posterior_e_revisao_apos_correcao_de_pick_anterior()
    {
        var serie = CriarSerieFearless();
        var primeira = ConfirmarPartida(serie, 1, 1);
        var segunda = ConfirmarPartida(serie, 11, 2);
        var picksCorrigidos = CriarPicks(serie, 21, 10);
        picksCorrigidos[0] = CriarPick(serie, 1, 11, 1);

        serie.CorrigirPicks(primeira.Id, picksCorrigidos, UsuarioId, Agora.AddHours(2));

        segunda.ConflitoFearless.Should().BeTrue();
        serie.RevisaoNecessaria.Should().BeTrue();
        serie.ObterBloqueiosFearless(3)
            .Should().BeEquivalentTo(Enumerable.Range(11, 20).Except([21]));

        var terceira = serie.AdicionarPartida(UsuarioId, Agora.AddHours(3));
        serie.RegistrarPicks(
            terceira.Id,
            CriarPicks(serie, 31, 10),
            UsuarioId,
            Agora.AddHours(3).AddMinutes(1));
        var confirmar = () => serie.ConfirmarPartida(
            terceira.Id,
            serie.Lados.Single(lado => lado.Ordem == 1).Id,
            MotivoTerminoPartida.Normal,
            UsuarioId,
            Agora.AddHours(3).AddMinutes(2));
        var antes = SerieSnapshot.Capturar(serie);

        confirmar.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesReviewRequired);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_limpar_conflitos_apos_correcao_e_permitir_nova_confirmacao()
    {
        var serie = CriarSerieFearless();
        var primeira = ConfirmarPartida(serie, 1, 1);
        var segunda = ConfirmarPartida(serie, 11, 2);
        var primeiraCorrigida = CriarPicks(serie, 21, 10);
        primeiraCorrigida[0] = CriarPick(serie, 1, 11, 1);
        serie.CorrigirPicks(primeira.Id, primeiraCorrigida, UsuarioId, Agora.AddHours(2));
        segunda.ConflitoFearless.Should().BeTrue();
        serie.RevisaoNecessaria.Should().BeTrue();

        serie.CorrigirPicks(segunda.Id, CriarPicks(serie, 31, 10), UsuarioId, Agora.AddHours(3));

        serie.Partidas.Should().OnlyContain(partida => !partida.ConflitoFearless);
        serie.RevisaoNecessaria.Should().BeFalse();
        serie.ObterBloqueiosFearless(3).Should().BeEquivalentTo(new[] { 11 }.Concat(Enumerable.Range(22, 19)));
        var terceira = serie.AdicionarPartida(UsuarioId, Agora.AddHours(4));
        serie.RegistrarPicks(terceira.Id, CriarPicks(serie, 41, 10), UsuarioId, Agora.AddHours(4).AddMinutes(1));

        var act = () => serie.ConfirmarPartida(
            terceira.Id,
            serie.Lados.Single(lado => lado.Ordem == 1).Id,
            MotivoTerminoPartida.Normal,
            UsuarioId,
            Agora.AddHours(4).AddMinutes(2));

        act.Should().NotThrow();
        terceira.Estado.Should().Be(PartidaEstado.Confirmada);
        serie.Estado.Should().Be(SerieEstado.Concluida);
    }

    private static Partida ConfirmarPartida(Serie serie, int primeiroChampionId, int ladoVencedor)
    {
        var criadaEm = Agora.AddMinutes(serie.Partidas.Count * 10);
        var partida = serie.AdicionarPartida(UsuarioId, criadaEm);
        serie.RegistrarPicks(
            partida.Id,
            CriarPicks(serie, primeiroChampionId, 10),
            UsuarioId,
            criadaEm.AddMinutes(1));
        serie.ConfirmarPartida(
            partida.Id,
            serie.Lados.Single(lado => lado.Ordem == ladoVencedor).Id,
            MotivoTerminoPartida.Normal,
            UsuarioId,
            criadaEm.AddMinutes(2));
        return partida;
    }

    private static Serie CriarSerieFearless()
    {
        var lados = new[]
        {
            new LadoSerie(Guid.NewGuid(), 1, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Lado 1", "L1", null, null, []),
            new LadoSerie(Guid.NewGuid(), 2, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Lado 2", "L2", null, null, [])
        };

        var serie = new Serie(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            SerieTipo.ConfrontoOficial,
            SerieFormato.Md3,
            ModoDraft.Fearless,
            true,
            Agora,
            null,
            Guid.NewGuid(),
            Agora,
            lados);
        serie.Iniciar(UsuarioId, Agora);
        return serie;
    }

    private static (Guid LadoSerieId, int ChampionId, int Ordem)[] CriarPicks(
        Serie serie,
        int primeiroChampionId,
        int quantidade)
    {
        var lados = serie.Lados.OrderBy(lado => lado.Ordem).ToArray();
        return Enumerable.Range(0, quantidade)
            .Select(indice =>
            {
                var indiceNoLado = indice % 5;
                var lado = lados[indice / 5];
                return (lado.Id, primeiroChampionId + indice, indiceNoLado + 1);
            })
            .ToArray();
    }

    private static (Guid LadoSerieId, int ChampionId, int Ordem)[] CriarPicksDistribuidos(
        Serie serie,
        int primeiroChampionId,
        int quantidadeLadoUm,
        int quantidadeLadoDois)
    {
        var lados = serie.Lados.OrderBy(lado => lado.Ordem).ToArray();
        return Enumerable.Range(0, quantidadeLadoUm)
            .Select(indice => (lados[0].Id, primeiroChampionId + indice, indice + 1))
            .Concat(Enumerable.Range(0, quantidadeLadoDois)
                .Select(indice => (lados[1].Id, primeiroChampionId + quantidadeLadoUm + indice, indice + 1)))
            .ToArray();
    }

    private static (Guid LadoSerieId, int ChampionId, int Ordem)[] SubstituirPick(
        (Guid LadoSerieId, int ChampionId, int Ordem)[] picks,
        int indice,
        (Guid LadoSerieId, int ChampionId, int Ordem) novoPick)
    {
        picks[indice] = novoPick;
        return picks;
    }

    private static (Guid LadoSerieId, int ChampionId, int Ordem) CriarPick(
        Serie serie,
        int lado,
        int championId,
        int ordem) =>
        (serie.Lados.Single(candidate => candidate.Ordem == lado).Id, championId, ordem);

    private sealed record SerieSnapshot(
        Guid Id,
        Guid SeasonId,
        Guid? CompeticaoId,
        Guid? RodadaId,
        Guid VersaoRegrasId,
        Guid? EventoId,
        Guid? DraftMontagemId,
        SerieTipo Tipo,
        SerieFormato Formato,
        ModoDraft ModoDraft,
        bool FearlessHabilitado,
        SerieEstado Estado,
        DateTimeOffset AgendadaPara,
        DateOnly? DataLocal,
        Guid? LadoVencedorId,
        bool RevisaoNecessaria,
        long Versao,
        DateTimeOffset CriadaEm,
        DateTimeOffset AtualizadaEm,
        DateTimeOffset? ConcluidaEm,
        Guid CriadaPorUsuarioId,
        Guid AtualizadaPorUsuarioId,
        ResultadoSnapshot Resultado,
        IReadOnlyList<LadoSnapshot> Lados,
        IReadOnlyList<PartidaSnapshot> Partidas,
        IReadOnlyList<BloqueioSnapshot> Bloqueios)
    {
        public static SerieSnapshot Capturar(Serie serie) => new(
            serie.Id,
            serie.SeasonId,
            serie.CompeticaoId,
            serie.RodadaId,
            serie.VersaoRegrasId,
            serie.EventoId,
            serie.DraftMontagemId,
            serie.Tipo,
            serie.Formato,
            serie.ModoDraft,
            serie.FearlessHabilitado,
            serie.Estado,
            serie.AgendadaPara,
            serie.DataLocal,
            serie.LadoVencedorId,
            serie.RevisaoNecessaria,
            serie.Versao,
            serie.CriadaEm,
            serie.AtualizadaEm,
            serie.ConcluidaEm,
            serie.CriadaPorUsuarioId,
            serie.AtualizadaPorUsuarioId,
            ResultadoSnapshot.Capturar(serie),
            serie.Lados.OrderBy(lado => lado.Ordem).Select(LadoSnapshot.Capturar).ToArray(),
            serie.Partidas.OrderBy(partida => partida.Ordem).Select(PartidaSnapshot.Capturar).ToArray(),
            Enumerable.Range(1, serie.Partidas.Count + 1)
                .Select(ordem => new BloqueioSnapshot(
                    ordem,
                    serie.ObterBloqueiosFearless(ordem).Order().ToArray()))
                .ToArray());
    }

    private sealed record ResultadoSnapshot(
        int VitoriasLadoUm,
        int VitoriasLadoDois,
        int VitoriasNecessarias,
        int QuantidadePartidasValidas,
        Guid? LadoVencedorId)
    {
        public static ResultadoSnapshot Capturar(Serie serie) => new(
            serie.Resultado.VitoriasLadoUm,
            serie.Resultado.VitoriasLadoDois,
            serie.Resultado.VitoriasNecessarias,
            serie.Resultado.QuantidadePartidasValidas,
            serie.Resultado.LadoVencedorId);
    }

    private sealed record BloqueioSnapshot(int OrdemPartida, IReadOnlyList<int> ChampionIds);

    private sealed record LadoSnapshot(
        Guid Id,
        Guid SerieId,
        int Ordem,
        LadoSerieTipo Tipo,
        Guid OrigemId,
        string NomeSnapshot,
        string? TagSnapshot,
        Guid? CapitaoJogadorId,
        string? CapitaoNomeSnapshot,
        IReadOnlyList<ParticipanteSnapshot> Participantes)
    {
        public static LadoSnapshot Capturar(LadoSerie lado) => new(
            lado.Id,
            lado.SerieId,
            lado.Ordem,
            lado.Tipo,
            lado.OrigemId,
            lado.NomeSnapshot,
            lado.TagSnapshot,
            lado.CapitaoJogadorId,
            lado.CapitaoNomeSnapshot,
            lado.ParticipantesEsperados.OrderBy(participante => participante.Ordem)
                .Select(ParticipanteSnapshot.Capturar)
                .ToArray());
    }

    private sealed record ParticipanteSnapshot(
        Guid Id,
        Guid LadoSerieId,
        Guid JogadorId,
        string NomeSnapshot,
        int Ordem)
    {
        public static ParticipanteSnapshot Capturar(ParticipanteEsperadoSerie participante) => new(
            participante.Id,
            participante.LadoSerieId,
            participante.JogadorId,
            participante.NomeSnapshot,
            participante.Ordem);
    }

    private sealed record PartidaSnapshot(
        Guid Id,
        Guid SerieId,
        int Ordem,
        PartidaEstado Estado,
        Guid? LadoVencedorId,
        MotivoTerminoPartida? MotivoTermino,
        DecisaoPicksRemake? DecisaoPicksRemake,
        bool ConflitoFearless,
        long Versao,
        DateTimeOffset CriadaEm,
        DateTimeOffset AtualizadaEm,
        DateTimeOffset? ConfirmadaEm,
        IReadOnlyList<PickSnapshot> Picks)
    {
        public static PartidaSnapshot Capturar(Partida partida) => new(
            partida.Id,
            partida.SerieId,
            partida.Ordem,
            partida.Estado,
            partida.LadoVencedorId,
            partida.MotivoTermino,
            partida.DecisaoPicksRemake,
            partida.ConflitoFearless,
            partida.Versao,
            partida.CriadaEm,
            partida.AtualizadaEm,
            partida.ConfirmadaEm,
            partida.Picks.OrderBy(pick => pick.LadoSerieId).ThenBy(pick => pick.Ordem)
                .Select(PickSnapshot.Capturar)
                .ToArray());
    }

    private sealed record PickSnapshot(
        Guid Id,
        Guid PartidaId,
        Guid LadoSerieId,
        int ChampionId,
        int Ordem,
        int VersaoFato,
        bool Valido,
        DateTimeOffset RegistradoEm)
    {
        public static PickSnapshot Capturar(PickPartida pick) => new(
            pick.Id,
            pick.PartidaId,
            pick.LadoSerieId,
            pick.ChampionId,
            pick.Ordem,
            pick.VersaoFato,
            pick.Valido,
            pick.RegistradoEm);
    }
}

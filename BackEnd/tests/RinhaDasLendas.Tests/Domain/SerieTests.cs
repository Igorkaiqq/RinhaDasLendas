using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using Xunit;

namespace RinhaDasLendas.Tests.Domain;

public sealed class SerieTests
{
    private static readonly DateTimeOffset Agora =
        new(2026, 8, 10, 18, 0, 0, TimeSpan.FromHours(-3));

    public static TheoryData<SerieFormato, int[], int, int> SeriesDisputadas =>
        new()
        {
            { SerieFormato.Md3, [1, 2, 1], 2, 1 },
            { SerieFormato.Md5, [1, 2, 1, 2, 1], 3, 2 },
        };

    [Theory]
    [InlineData(SerieFormato.Md3, 2)]
    [InlineData(SerieFormato.Md5, 3)]
    public void Deve_criar_serie_agendada_com_formato_lados_e_snapshots_imutaveis(
        SerieFormato formato,
        int vitoriasNecessarias)
    {
        var templates = CriarTemplatesDosLados();

        var serie = CriarSerie(formato, templates);

        serie.Formato.Should().Be(formato);
        serie.Estado.Should().Be(SerieEstado.Agendada);
        serie.Partidas.Should().BeEmpty();
        serie.Resultado.VitoriasNecessarias.Should().Be(vitoriasNecessarias);
        serie.Resultado.VitoriasLadoUm.Should().Be(0);
        serie.Resultado.VitoriasLadoDois.Should().Be(0);
        serie.Resultado.QuantidadePartidasValidas.Should().Be(0);
        serie.Resultado.LadoVencedorId.Should().BeNull();
        serie.Lados.Should().HaveCount(2);
        serie.Lados.Select(lado => lado.Ordem).Should().Equal(1, 2);
        serie.Lados.Should().OnlyContain(lado => lado.SerieId == serie.Id);

        var primeiroLado = serie.Lados.Single(lado => lado.Ordem == 1);
        primeiroLado.Id.Should().NotBe(templates[0].Id);
        primeiroLado.OrigemId.Should().Be(templates[0].OrigemId);
        primeiroLado.NomeSnapshot.Should().Be("Aurora");
        primeiroLado.TagSnapshot.Should().Be("AUR");
        primeiroLado.CapitaoJogadorId.Should().Be(templates[0].CapitaoJogadorId);
        primeiroLado.CapitaoNomeSnapshot.Should().Be("Capita Aurora");
        primeiroLado.ParticipantesEsperados.Select(participante => new
        {
            participante.JogadorId,
            participante.NomeSnapshot,
            participante.Ordem,
        }).Should().BeEquivalentTo(
        [
            new { JogadorId = templates[0].ParticipantesEsperados.ElementAt(0).JogadorId, NomeSnapshot = "Aurora 1", Ordem = 1 },
            new { JogadorId = templates[0].ParticipantesEsperados.ElementAt(1).JogadorId, NomeSnapshot = "Aurora 2", Ordem = 2 },
        ], options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Deve_exigir_exatamente_dois_lados(int quantidade)
    {
        var lados = CriarTemplatesDosLados().Take(quantidade).ToList();
        if (quantidade == 3)
        {
            lados.Add(CriarLado(1, "Terceiro", "TER"));
        }

        var act = () => CriarSerie(SerieFormato.Md3, lados);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_copiar_entradas_e_expor_colecoes_somente_leitura()
    {
        var participantes = new List<ParticipanteEsperadoSerie>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Jogador original", 1),
        };
        var primeiroTemplate = new LadoSerie(
            Guid.NewGuid(),
            1,
            LadoSerieTipo.Temporario,
            Guid.NewGuid(),
            "Aurora",
            "AUR",
            Guid.NewGuid(),
            "Capita Aurora",
            participantes);
        participantes.Clear();
        var templates = new List<LadoSerie>
        {
            primeiroTemplate,
            CriarLado(2, "Eclipse", "ECL"),
        };

        var serie = CriarSerie(SerieFormato.Md3, templates);
        templates.Clear();
        serie.Iniciar(Guid.NewGuid(), Agora);
        var partida = serie.AdicionarPartida(Guid.NewGuid(), Agora.AddMinutes(1));

        serie.Lados.Should().HaveCount(2);
        serie.Lados.Single(lado => lado.Ordem == 1).ParticipantesEsperados.Should().ContainSingle()
            .Which.NomeSnapshot.Should().Be("Jogador original");
        var alterarLados = () => ((ICollection<LadoSerie>)serie.Lados).Clear();
        var alterarParticipantes = () => ((ICollection<ParticipanteEsperadoSerie>)serie.Lados
            .Single(lado => lado.Ordem == 1).ParticipantesEsperados).Clear();
        var alterarPartidas = () => ((ICollection<Partida>)serie.Partidas).Clear();
        var alterarPicks = () => ((ICollection<PickPartida>)partida.Picks).Clear();

        alterarLados.Should().Throw<NotSupportedException>();
        alterarParticipantes.Should().Throw<NotSupportedException>();
        alterarPartidas.Should().Throw<NotSupportedException>();
        alterarPicks.Should().Throw<NotSupportedException>();
        serie.Lados.Should().HaveCount(2);
        serie.Lados.Single(lado => lado.Ordem == 1).ParticipantesEsperados.Should().ContainSingle();
        serie.Partidas.Should().ContainSingle().Which.Should().BeSameAs(partida);
    }

    [Fact]
    public void Deve_rejeitar_adicao_de_partida_enquanto_agendada_sem_mutacao()
    {
        var serie = CriarSerie(SerieFormato.Md3);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.AdicionarPartida(Guid.NewGuid(), Agora.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesTransitionInvalid);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_transicionar_de_agendada_para_em_andamento_ao_criar_primeira_partida()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);

        serie.Iniciar(usuarioId, Agora);
        var partida = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(1));

        serie.Estado.Should().Be(SerieEstado.EmAndamento);
        serie.Versao.Should().Be(2);
        serie.AtualizadaPorUsuarioId.Should().Be(usuarioId);
        serie.AtualizadaEm.Should().Be(Agora.AddMinutes(1).ToUniversalTime());
        partida.SerieId.Should().Be(serie.Id);
        partida.Ordem.Should().Be(1);
        partida.Estado.Should().Be(PartidaEstado.Rascunho);
    }

    [Fact]
    public void Deve_derivar_placar_apenas_de_partidas_confirmadas_e_preservar_surrender()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var lados = serie.Lados.OrderBy(lado => lado.Ordem).ToArray();
        var primeira = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(1));
        var segunda = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(2));

        serie.ConfirmarPartida(
            primeira.Id,
            lados[0].Id,
            MotivoTerminoPartida.Surrender,
            usuarioId,
            Agora.AddMinutes(3));

        primeira.Estado.Should().Be(PartidaEstado.Confirmada);
        primeira.LadoVencedorId.Should().Be(lados[0].Id);
        primeira.MotivoTermino.Should().Be(MotivoTerminoPartida.Surrender);
        segunda.Estado.Should().Be(PartidaEstado.Rascunho);
        serie.Resultado.VitoriasLadoUm.Should().Be(1);
        serie.Resultado.VitoriasLadoDois.Should().Be(0);
        serie.Resultado.QuantidadePartidasValidas.Should().Be(1);
        serie.Resultado.LadoVencedorId.Should().BeNull();
        serie.LadoVencedorId.Should().BeNull();
        serie.Estado.Should().Be(SerieEstado.EmAndamento);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Deve_cancelar_serie_agendada_ou_em_andamento(bool iniciar)
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        if (iniciar)
        {
            serie.Iniciar(usuarioId, Agora);
        }

        serie.Cancelar(usuarioId, Agora.AddMinutes(1));

        serie.Estado.Should().Be(SerieEstado.Cancelada);
        serie.LadoVencedorId.Should().BeNull();
        serie.ConcluidaEm.Should().BeNull();
        serie.AtualizadaPorUsuarioId.Should().Be(usuarioId);
        serie.AtualizadaEm.Should().Be(Agora.AddMinutes(1).ToUniversalTime());
    }

    [Theory]
    [MemberData(nameof(SeriesDisputadas))]
    public void Deve_concluir_serie_disputada_no_limite_do_formato(
        SerieFormato formato,
        int[] vencedoresPorOrdem,
        int vitoriasLadoUm,
        int vitoriasLadoDois)
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(formato);
        serie.Iniciar(usuarioId, Agora);
        var lados = serie.Lados.OrderBy(lado => lado.Ordem).ToArray();

        for (var indice = 0; indice < vencedoresPorOrdem.Length; indice++)
        {
            var partida = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(indice * 2 + 1));
            serie.ConfirmarPartida(
                partida.Id,
                lados[vencedoresPorOrdem[indice] - 1].Id,
                MotivoTerminoPartida.Normal,
                usuarioId,
                Agora.AddMinutes(indice * 2 + 2));

            serie.Estado.Should().Be(
                indice == vencedoresPorOrdem.Length - 1
                    ? SerieEstado.Concluida
                    : SerieEstado.EmAndamento);
        }

        serie.Resultado.VitoriasLadoUm.Should().Be(vitoriasLadoUm);
        serie.Resultado.VitoriasLadoDois.Should().Be(vitoriasLadoDois);
        serie.Resultado.VitoriasNecessarias.Should().Be(formato == SerieFormato.Md3 ? 2 : 3);
        serie.Resultado.QuantidadePartidasValidas.Should().Be(vencedoresPorOrdem.Length);
        serie.Resultado.LadoVencedorId.Should().Be(lados[0].Id);
        serie.LadoVencedorId.Should().Be(lados[0].Id);
        serie.ConcluidaEm.Should().Be(Agora.AddMinutes(vencedoresPorOrdem.Length * 2).ToUniversalTime());
    }

    [Fact]
    public void Deve_calcular_segundo_lado_como_vencedor_da_serie()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var segundoLadoId = serie.Lados.Single(lado => lado.Ordem == 2).Id;

        for (var indice = 0; indice < 2; indice++)
        {
            var partida = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(indice * 2 + 1));
            serie.ConfirmarPartida(
                partida.Id,
                segundoLadoId,
                MotivoTerminoPartida.Normal,
                usuarioId,
                Agora.AddMinutes(indice * 2 + 2));
        }

        serie.Resultado.VitoriasLadoUm.Should().Be(0);
        serie.Resultado.VitoriasLadoDois.Should().Be(2);
        serie.Resultado.LadoVencedorId.Should().Be(segundoLadoId);
        serie.LadoVencedorId.Should().Be(segundoLadoId);
        serie.Estado.Should().Be(SerieEstado.Concluida);
    }

    [Fact]
    public void Deve_bloquear_confirmacao_de_partida_excedente_sem_mutacao_parcial()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var vencedorId = serie.Lados.Single(lado => lado.Ordem == 1).Id;
        var primeira = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(1));
        var segunda = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(2));
        var excedente = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(3));
        serie.ConfirmarPartida(primeira.Id, vencedorId, MotivoTerminoPartida.Normal, usuarioId, Agora.AddMinutes(4));
        serie.ConfirmarPartida(segunda.Id, vencedorId, MotivoTerminoPartida.Normal, usuarioId, Agora.AddMinutes(5));
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.ConfirmarPartida(
            excedente.Id,
            vencedorId,
            MotivoTerminoPartida.Surrender,
            usuarioId,
            Agora.AddMinutes(6));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesAlreadyDecided);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_bloquear_nova_partida_apos_conclusao_sem_mutacao_parcial()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerieConcluida(usuarioId);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.AdicionarPartida(usuarioId, Agora.AddMinutes(10));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesAlreadyDecided);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_bloquear_cancelamento_apos_conclusao_sem_mutacao_parcial()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerieConcluida(usuarioId);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.Cancelar(usuarioId, Agora.AddMinutes(10));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesTransitionInvalid);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_bloquear_reinicio_apos_conclusao_sem_mutacao_parcial()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerieConcluida(usuarioId);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.Iniciar(usuarioId, Agora.AddMinutes(10));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesTransitionInvalid);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_bloquear_reinicio_sem_mutacao_parcial()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var antes = SerieSnapshot.Capturar(serie);

        var act = () => serie.Iniciar(usuarioId, Agora.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesTransitionInvalid);
        SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_bloquear_todas_as_operacoes_normais_apos_cancelamento_sem_mutacao()
    {
        var usuarioId = Guid.NewGuid();
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var partida = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(1));
        serie.Cancelar(usuarioId, Agora.AddMinutes(2));
        Action[] operacoes =
        [
            () => serie.Iniciar(usuarioId, Agora.AddMinutes(3)),
            () => serie.AdicionarPartida(usuarioId, Agora.AddMinutes(3)),
            () => serie.ConfirmarPartida(
                partida.Id,
                serie.Lados.Single(lado => lado.Ordem == 1).Id,
                MotivoTerminoPartida.Normal,
                usuarioId,
                Agora.AddMinutes(3)),
            () => serie.Cancelar(usuarioId, Agora.AddMinutes(3)),
        ];

        foreach (var operacao in operacoes)
        {
            var antes = SerieSnapshot.Capturar(serie);

            operacao.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesTransitionInvalid);
            SerieSnapshot.Capturar(serie).Should().BeEquivalentTo(antes, options => options.WithStrictOrdering());
        }
    }

    private static Serie CriarSerie(
        SerieFormato formato,
        IReadOnlyCollection<LadoSerie>? lados = null)
    {
        return new Serie(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            eventoId: null,
            draftMontagemId: Guid.NewGuid(),
            SerieTipo.DiariaTemporaria,
            formato,
            ModoDraft.Padrao,
            fearlessHabilitado: false,
            Agora,
            new DateOnly(2026, 8, 10),
            Guid.NewGuid(),
            Agora.AddHours(-1),
            lados ?? CriarTemplatesDosLados());
    }

    private static Serie CriarSerieConcluida(Guid usuarioId)
    {
        var serie = CriarSerie(SerieFormato.Md3);
        serie.Iniciar(usuarioId, Agora);
        var vencedorId = serie.Lados.Single(lado => lado.Ordem == 1).Id;

        for (var indice = 1; indice <= 2; indice++)
        {
            var partida = serie.AdicionarPartida(usuarioId, Agora.AddMinutes(indice * 2 - 1));
            serie.ConfirmarPartida(
                partida.Id,
                vencedorId,
                MotivoTerminoPartida.Normal,
                usuarioId,
                Agora.AddMinutes(indice * 2));
        }

        return serie;
    }

    private static IReadOnlyList<LadoSerie> CriarTemplatesDosLados() =>
    [
        CriarLado(1, "Aurora", "AUR"),
        CriarLado(2, "Eclipse", "ECL"),
    ];

    private static LadoSerie CriarLado(int ordem, string nome, string tag)
    {
        var capitaoId = Guid.NewGuid();
        return new LadoSerie(
            Guid.NewGuid(),
            ordem,
            LadoSerieTipo.Temporario,
            Guid.NewGuid(),
            nome,
            tag,
            capitaoId,
            ordem == 1 ? "Capita Aurora" : "Capita Eclipse",
            [
                new ParticipanteEsperadoSerie(Guid.NewGuid(), capitaoId, $"{nome} 1", 1),
                new ParticipanteEsperadoSerie(Guid.NewGuid(), Guid.NewGuid(), $"{nome} 2", 2),
            ]);
    }

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
        IReadOnlyList<PartidaSnapshot> Partidas)
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
            serie.Partidas.OrderBy(partida => partida.Ordem).Select(PartidaSnapshot.Capturar).ToArray());
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

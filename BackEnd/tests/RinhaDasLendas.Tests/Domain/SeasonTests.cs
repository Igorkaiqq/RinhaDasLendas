using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Tests.Domain;

public sealed class SeasonTests
{
    private static readonly DateOnly Inicio = new(2026, 1, 1);
    private static readonly DateOnly FimExclusivo = new(2026, 5, 1);
    private static readonly DateTimeOffset CriadaEm = DateTimeOffset.Parse("2025-12-01T12:00:00Z");

    public static TheoryData<DateOnly, bool> DatasDoIntervaloMeioAberto =>
        new()
        {
            { Inicio.AddDays(-1), false },
            { Inicio, true },
            { Inicio.AddDays(1), true },
            { FimExclusivo.AddDays(-1), true },
            { FimExclusivo, false },
            { FimExclusivo.AddDays(1), false }
        };

    public static TheoryData<DateOnly, DateOnly> PeriodosSobrepostos =>
        new()
        {
            { Inicio, FimExclusivo },
            { Inicio.AddDays(-10), Inicio.AddDays(1) },
            { Inicio.AddDays(1), FimExclusivo.AddDays(10) },
            { Inicio.AddDays(1), FimExclusivo.AddDays(-1) },
            { Inicio.AddDays(-10), FimExclusivo.AddDays(10) }
        };

    [Fact]
    public void Deve_criar_nova_season_planejada()
    {
        var usuarioId = Guid.NewGuid();

        var season = CriarSeason(criadaPorUsuarioId: usuarioId);

        season.Estado.Should().Be(SeasonEstado.Planejada);
        season.Versao.Should().Be(0);
        season.AtivadaEm.Should().BeNull();
        season.EncerradaEm.Should().BeNull();
        season.CriadaPorUsuarioId.Should().Be(usuarioId);
        season.AtualizadaPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Deve_rejeitar_periodo_com_inicio_igual_ao_fim_exclusivo()
    {
        var act = () => CriarSeason(
            dataInicio: Inicio,
            dataFimExclusiva: Inicio);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodInvalid);
    }

    [Fact]
    public void Deve_rejeitar_periodo_com_fim_exclusivo_anterior_ao_inicio()
    {
        var act = () => CriarSeason(
            dataInicio: Inicio,
            dataFimExclusiva: Inicio.AddDays(-1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodInvalid);
    }

    [Theory]
    [MemberData(nameof(DatasDoIntervaloMeioAberto))]
    public void Deve_avaliar_data_no_periodo_meio_aberto(DateOnly data, bool pertenceAoPeriodo)
    {
        var season = CriarSeason();

        season.Contem(data).Should().Be(pertenceAoPeriodo);
    }

    [Fact]
    public void Deve_aceitar_seasons_consecutivas_na_fronteira_final_inicial()
    {
        var existente = CriarSeason();
        var anterior = CriarSeason(
            nome: "Season anterior",
            ano: 2025,
            dataInicio: Inicio.AddMonths(-4),
            dataFimExclusiva: Inicio);
        var seguinte = CriarSeason(
            nome: "Season seguinte",
            ordemNoAno: 2,
            dataInicio: FimExclusivo,
            dataFimExclusiva: FimExclusivo.AddMonths(4));

        var validarAnterior = () => SeasonRules.ValidarInclusao(anterior, [existente]);
        var validarSeguinte = () => SeasonRules.ValidarInclusao(seguinte, [existente]);

        validarAnterior.Should().NotThrow();
        validarSeguinte.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(PeriodosSobrepostos))]
    public void Deve_rejeitar_qualquer_sobreposicao_de_periodo(
        DateOnly dataInicio,
        DateOnly dataFimExclusiva)
    {
        var existente = CriarSeason();
        var candidata = CriarSeason(
            nome: "Season candidata",
            ordemNoAno: 2,
            dataInicio: dataInicio,
            dataFimExclusiva: dataFimExclusiva);

        var act = () => SeasonRules.ValidarInclusao(candidata, [existente]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodOverlap);
    }

    [Fact]
    public void Deve_rejeitar_ordem_repetida_no_mesmo_ano()
    {
        var existente = CriarSeason(
            dataInicio: new DateOnly(2026, 1, 1),
            dataFimExclusiva: new DateOnly(2026, 5, 1));
        var candidata = CriarSeason(
            nome: "Segunda Season 2026",
            dataInicio: new DateOnly(2026, 5, 1),
            dataFimExclusiva: new DateOnly(2026, 9, 1));

        var act = () => SeasonRules.ValidarInclusao(candidata, [existente]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_aceitar_ordens_distintas_no_mesmo_ano_em_ordem_cronologica()
    {
        var primeira = CriarSeason(
            dataInicio: new DateOnly(2026, 1, 1),
            dataFimExclusiva: new DateOnly(2026, 5, 1));
        var segunda = CriarSeason(
            nome: "Segunda Season 2026",
            ordemNoAno: 2,
            dataInicio: new DateOnly(2026, 5, 1),
            dataFimExclusiva: new DateOnly(2026, 9, 1));

        var act = () => SeasonRules.ValidarInclusao(segunda, [primeira]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_aceitar_a_mesma_ordem_em_anos_distintos()
    {
        var season2026 = CriarSeason();
        var season2027 = CriarSeason(
            nome: "Primeira Season 2027",
            ano: 2027,
            dataInicio: new DateOnly(2027, 1, 1),
            dataFimExclusiva: new DateOnly(2027, 5, 1));

        var act = () => SeasonRules.ValidarInclusao(season2027, [season2026]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_ativar_season_planejada()
    {
        var season = CriarSeason();
        var usuarioId = Guid.NewGuid();
        var ativadaEm = DateTimeOffset.Parse("2026-01-01T03:00:00Z");

        season.Ativar(usuarioId, ativadaEm);

        season.Estado.Should().Be(SeasonEstado.Ativa);
        season.AtivadaEm.Should().Be(ativadaEm);
        season.EncerradaEm.Should().BeNull();
        season.AtualizadaEm.Should().Be(ativadaEm);
        season.AtualizadaPorUsuarioId.Should().Be(usuarioId);
        season.Versao.Should().Be(1);
    }

    [Fact]
    public void Deve_encerrar_season_ativa_sem_apagar_a_ativacao()
    {
        var season = CriarSeason();
        var ativadaEm = DateTimeOffset.Parse("2026-01-01T03:00:00Z");
        var encerradaEm = DateTimeOffset.Parse("2026-05-01T03:00:00Z");
        season.Ativar(Guid.NewGuid(), ativadaEm);
        var usuarioEncerramentoId = Guid.NewGuid();

        season.Encerrar(usuarioEncerramentoId, encerradaEm);

        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.AtivadaEm.Should().Be(ativadaEm);
        season.EncerradaEm.Should().Be(encerradaEm);
        season.AtualizadaEm.Should().Be(encerradaEm);
        season.AtualizadaPorUsuarioId.Should().Be(usuarioEncerramentoId);
        season.Versao.Should().Be(2);
    }

    [Fact]
    public void Deve_normalizar_instantes_das_transicoes_para_utc()
    {
        var season = CriarSeason();
        var ativadaEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-3));
        var encerradaEm = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.FromHours(-3));

        season.Ativar(Guid.NewGuid(), ativadaEm);
        season.Encerrar(Guid.NewGuid(), encerradaEm);

        season.AtivadaEm.Should().Be(ativadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        season.EncerradaEm.Should().Be(encerradaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        season.AtualizadaEm.Should().Be(encerradaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
    }

    [Fact]
    public void Deve_rejeitar_encerramento_de_season_planejada()
    {
        var season = CriarSeason();

        var act = () => season.Encerrar(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Planejada);
        season.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_nova_ativacao_de_season_ativa()
    {
        var season = CriarSeason();
        season.Ativar(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        var act = () => season.Ativar(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Ativa);
        season.Versao.Should().Be(1);
    }

    [Fact]
    public void Deve_rejeitar_reativacao_de_season_encerrada()
    {
        var season = CriarSeasonEncerrada();

        var act = () => season.Ativar(Guid.NewGuid(), DateTimeOffset.Parse("2026-05-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.Versao.Should().Be(2);
    }

    [Fact]
    public void Deve_rejeitar_novo_encerramento_de_season_encerrada()
    {
        var season = CriarSeasonEncerrada();

        var act = () => season.Encerrar(Guid.NewGuid(), DateTimeOffset.Parse("2026-05-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.Versao.Should().Be(2);
    }

    [Fact]
    public void Deve_rejeitar_ator_vazio_nas_transicoes_sem_alterar_estado()
    {
        var planejada = CriarSeason();
        var ativa = CriarSeason();
        ativa.Ativar(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        var ativar = () => planejada.Ativar(Guid.Empty, DateTimeOffset.UtcNow);
        var encerrar = () => ativa.Encerrar(Guid.Empty, DateTimeOffset.UtcNow);

        ativar.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        encerrar.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        planejada.Estado.Should().Be(SeasonEstado.Planejada);
        ativa.Estado.Should().Be(SeasonEstado.Ativa);
    }

    [Fact]
    public void Deve_ativar_season_seguinte_encerrando_e_preservando_a_anterior_no_historico()
    {
        var usuarioId = Guid.NewGuid();
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), usuarioId, CriadaEm);
        var anterior = CriarSeason();
        var seguinte = CriarSeason(
            nome: "Segunda Season 2026",
            ordemNoAno: 2,
            dataInicio: FimExclusivo,
            dataFimExclusiva: new DateOnly(2026, 9, 1));
        var anteriorId = anterior.Id;
        var anteriorCriadaEm = anterior.CriadaEm;
        var anteriorCriadaPorUsuarioId = anterior.CriadaPorUsuarioId;
        var periodoAnterior = (anterior.DataInicio, anterior.DataFimExclusiva);
        var ativacaoAnterior = DateTimeOffset.Parse("2026-01-01T03:00:00Z");
        var ativacaoSeguinte = DateTimeOffset.Parse("2026-05-01T03:00:00Z");

        calendario.AtivarSeason(anterior, null, calendario.Versao, usuarioId, ativacaoAnterior);
        calendario.AtivarSeason(seguinte, anterior, calendario.Versao, usuarioId, ativacaoSeguinte);

        calendario.SeasonAtivaId.Should().Be(seguinte.Id);
        calendario.Versao.Should().Be(2);
        calendario.AtualizadoEm.Should().Be(ativacaoSeguinte);
        calendario.AtualizadoPorUsuarioId.Should().Be(usuarioId);
        anterior.Estado.Should().Be(SeasonEstado.Encerrada);
        anterior.Id.Should().Be(anteriorId);
        anterior.AtivadaEm.Should().Be(ativacaoAnterior);
        anterior.EncerradaEm.Should().Be(ativacaoSeguinte);
        anterior.CriadaEm.Should().Be(anteriorCriadaEm);
        anterior.CriadaPorUsuarioId.Should().Be(anteriorCriadaPorUsuarioId);
        (anterior.DataInicio, anterior.DataFimExclusiva).Should().Be(periodoAnterior);
        seguinte.Estado.Should().Be(SeasonEstado.Ativa);
        seguinte.AtivadaEm.Should().Be(ativacaoSeguinte);
    }

    private static Season CriarSeason(
        string nome = "Primeira Season 2026",
        int ano = 2026,
        int ordemNoAno = 1,
        DateOnly? dataInicio = null,
        DateOnly? dataFimExclusiva = null,
        Guid? criadaPorUsuarioId = null)
    {
        return new Season(
            nome,
            ano,
            ordemNoAno,
            dataInicio ?? Inicio,
            dataFimExclusiva ?? FimExclusivo,
            criadaPorUsuarioId ?? Guid.NewGuid(),
            CriadaEm);
    }

    private static Season CriarSeasonEncerrada()
    {
        var season = CriarSeason();
        season.Ativar(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
        season.Encerrar(Guid.NewGuid(), DateTimeOffset.Parse("2026-05-01T03:00:00Z"));
        return season;
    }

}

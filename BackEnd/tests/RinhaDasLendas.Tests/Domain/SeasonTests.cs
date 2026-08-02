using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;
using System.Reflection;

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
        var calendario = CriarCalendario(usuarioId);

        calendario.AtivarSeason(season, null, calendario.Versao, usuarioId, ativadaEm);

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
        var calendario = AtivarEmNovoCalendario(season, Guid.NewGuid(), ativadaEm);
        var usuarioEncerramentoId = Guid.NewGuid();

        calendario.EncerrarSeason(
            season,
            calendario.Versao,
            usuarioEncerramentoId,
            encerradaEm);

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
        var usuarioId = Guid.NewGuid();
        var calendario = AtivarEmNovoCalendario(season, usuarioId, ativadaEm);

        calendario.EncerrarSeason(season, calendario.Versao, usuarioId, encerradaEm);

        season.AtivadaEm.Should().Be(ativadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        season.EncerradaEm.Should().Be(encerradaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        season.AtualizadaEm.Should().Be(encerradaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
    }

    [Fact]
    public void Deve_rejeitar_encerramento_de_season_planejada()
    {
        var season = CriarSeason();
        var calendario = CriarCalendario(Guid.NewGuid());

        var act = () => calendario.EncerrarSeason(
            season,
            calendario.Versao,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Planejada);
        season.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_nova_ativacao_de_season_ativa()
    {
        var season = CriarSeason();
        var usuarioId = Guid.NewGuid();
        var calendario = AtivarEmNovoCalendario(
            season,
            usuarioId,
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        var act = () => calendario.AtivarSeason(
            season,
            season,
            calendario.Versao,
            usuarioId,
            DateTimeOffset.Parse("2026-01-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Ativa);
        season.Versao.Should().Be(1);
    }

    [Fact]
    public void Deve_rejeitar_reativacao_de_season_encerrada()
    {
        var (season, calendario) = CriarSeasonEncerrada();

        var act = () => calendario.AtivarSeason(
            season,
            null,
            calendario.Versao,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-05-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.Versao.Should().Be(2);
    }

    [Fact]
    public void Deve_rejeitar_novo_encerramento_de_season_encerrada()
    {
        var (season, calendario) = CriarSeasonEncerrada();

        var act = () => calendario.EncerrarSeason(
            season,
            calendario.Versao,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-05-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.Versao.Should().Be(2);
    }

    [Fact]
    public void Deve_rejeitar_ator_vazio_nas_transicoes_sem_alterar_estado()
    {
        var planejada = CriarSeason();
        var ativa = CriarSeason();
        var calendarioPlanejada = CriarCalendario(Guid.NewGuid());
        var calendarioAtiva = AtivarEmNovoCalendario(
            ativa,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        var ativar = () => calendarioPlanejada.AtivarSeason(
            planejada,
            null,
            calendarioPlanejada.Versao,
            Guid.Empty,
            DateTimeOffset.UtcNow);
        var encerrar = () => calendarioAtiva.EncerrarSeason(
            ativa,
            calendarioAtiva.Versao,
            Guid.Empty,
            DateTimeOffset.UtcNow);

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

    [Fact]
    public void Deve_rejeitar_versao_obsoleta_do_calendario_sem_alterar_seasons()
    {
        var usuarioId = Guid.NewGuid();
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), usuarioId, CriadaEm);
        var candidata = CriarSeason();

        var act = () => calendario.AtivarSeason(
            candidata,
            null,
            expectedVersion: 1,
            usuarioId,
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.CompetitiveCalendarVersionStale);
        calendario.SeasonAtivaId.Should().BeNull();
        calendario.Versao.Should().Be(0);
        candidata.Estado.Should().Be(SeasonEstado.Planejada);
        candidata.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_candidata_invalida_sem_encerrar_season_anterior()
    {
        var usuarioId = Guid.NewGuid();
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), usuarioId, CriadaEm);
        var anterior = CriarSeason();
        var candidata = CriarSeason(
            nome: "Season encerrada",
            ordemNoAno: 2,
            dataInicio: FimExclusivo,
            dataFimExclusiva: FimExclusivo.AddMonths(4));
        var ativacaoAnterior = DateTimeOffset.Parse("2026-01-01T03:00:00Z");
        calendario.AtivarSeason(anterior, null, calendario.Versao, usuarioId, ativacaoAnterior);
        var calendarioCandidata = AtivarEmNovoCalendario(
            candidata,
            usuarioId,
            DateTimeOffset.Parse("2026-05-01T03:00:00Z"));
        calendarioCandidata.EncerrarSeason(
            candidata,
            calendarioCandidata.Versao,
            usuarioId,
            DateTimeOffset.Parse("2026-05-02T03:00:00Z"));

        var act = () => calendario.AtivarSeason(
            candidata,
            anterior,
            calendario.Versao,
            usuarioId,
            DateTimeOffset.Parse("2026-05-03T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        calendario.SeasonAtivaId.Should().Be(anterior.Id);
        calendario.Versao.Should().Be(1);
        anterior.Estado.Should().Be(SeasonEstado.Ativa);
        anterior.EncerradaEm.Should().BeNull();
    }

    [Fact]
    public void Deve_prevalidar_encerramento_anterior_antes_de_ativar_candidata()
    {
        var usuarioId = Guid.NewGuid();
        var anterior = CriarSeason();
        var candidata = CriarSeason(
            nome: "Segunda Season 2026",
            ordemNoAno: 2,
            dataInicio: FimExclusivo,
            dataFimExclusiva: FimExclusivo.AddMonths(4));
        var ativadaEm = DateTimeOffset.Parse("2026-05-01T03:00:00Z");
        var calendario = AtivarEmNovoCalendario(anterior, usuarioId, ativadaEm);

        var act = () => calendario.AtivarSeason(
            candidata,
            anterior,
            calendario.Versao,
            usuarioId,
            ativadaEm.AddTicks(-1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        calendario.SeasonAtivaId.Should().Be(anterior.Id);
        calendario.Versao.Should().Be(1);
        anterior.Estado.Should().Be(SeasonEstado.Ativa);
        anterior.EncerradaEm.Should().BeNull();
        candidata.Estado.Should().Be(SeasonEstado.Planejada);
        candidata.AtivadaEm.Should().BeNull();
        candidata.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_expor_transicoes_somente_pelo_calendario()
    {
        var metodosPublicos = typeof(Season).GetMethods(BindingFlags.Instance | BindingFlags.Public);

        metodosPublicos.Select(method => method.Name).Should().NotContain(["Ativar", "Encerrar"]);
    }

    [Fact]
    public void Deve_encerrar_season_ativa_e_limpar_calendario_atomicamente()
    {
        var usuarioId = Guid.NewGuid();
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), usuarioId, CriadaEm);
        var season = CriarSeason();
        calendario.AtivarSeason(
            season,
            null,
            calendario.Versao,
            usuarioId,
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
        var encerradaEm = DateTimeOffset.Parse("2026-05-01T03:00:00Z");

        calendario.EncerrarSeason(season, calendario.Versao, usuarioId, encerradaEm);

        calendario.SeasonAtivaId.Should().BeNull();
        calendario.Versao.Should().Be(2);
        calendario.AtualizadoEm.Should().Be(encerradaEm);
        calendario.AtualizadoPorUsuarioId.Should().Be(usuarioId);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        season.EncerradaEm.Should().Be(encerradaEm);
    }

    [Fact]
    public void Deve_criar_selecao_sazonal_especifica_com_ids_distintos()
    {
        var primeiraId = Guid.NewGuid();
        var segundaId = Guid.NewGuid();

        var selecao = SelecaoSazonal.Especifica([segundaId, primeiraId, segundaId]);

        selecao.SeasonIds.Should().BeEquivalentTo([primeiraId, segundaId]);
        selecao.SeasonIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Deve_rejeitar_opcoes_sazonais_mutuamente_exclusivas()
    {
        var act = () => SelecaoSazonal.Criar([Guid.NewGuid()], todas: true);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonalFilterConflict);
    }

    [Fact]
    public void Deve_rejeitar_selecao_sazonal_especifica_sem_ids()
    {
        var act = () => SelecaoSazonal.Especifica([]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_comparar_selecao_sazonal_por_tipo_e_conjunto_de_ids()
    {
        var primeiraId = Guid.NewGuid();
        var segundaId = Guid.NewGuid();

        var primeira = SelecaoSazonal.Especifica([primeiraId, segundaId]);
        var equivalente = SelecaoSazonal.Especifica([segundaId, primeiraId, primeiraId]);

        primeira.Should().Be(equivalente);
        primeira.GetHashCode().Should().Be(equivalente.GetHashCode());
        primeira.Should().NotBe(SelecaoSazonal.Todas());
    }

    [Fact]
    public void Deve_atualizar_season_planejada_com_versao_e_auditoria()
    {
        var season = CriarSeason();
        var usuarioId = Guid.NewGuid();
        var atualizadaEm = DateTimeOffset.Parse("2025-12-02T12:00:00-03:00");
        var novoInicio = Inicio.AddDays(1);
        var novoFimExclusivo = FimExclusivo.AddDays(1);

        season.Atualizar(
            "  Season atualizada  ",
            2027,
            2,
            novoInicio,
            novoFimExclusivo,
            usuarioId,
            atualizadaEm);

        season.Nome.Should().Be("Season atualizada");
        season.Ano.Should().Be(2027);
        season.OrdemNoAno.Should().Be(2);
        season.DataInicio.Should().Be(novoInicio);
        season.DataFimExclusiva.Should().Be(novoFimExclusivo);
        season.AtualizadaEm.Should().Be(atualizadaEm.ToUniversalTime());
        season.AtualizadaPorUsuarioId.Should().Be(usuarioId);
        season.Versao.Should().Be(1);
    }

    [Fact]
    public void Deve_rejeitar_atualizacao_de_season_ativa_sem_alterar_dados()
    {
        var season = CriarSeason();
        _ = AtivarEmNovoCalendario(
            season,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));

        var act = () => season.Atualizar(
            "Outro nome",
            2027,
            2,
            Inicio.AddDays(1),
            FimExclusivo.AddDays(1),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-02T03:00:00Z"));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Nome.Should().Be("Primeira Season 2026");
        season.Ano.Should().Be(2026);
        season.OrdemNoAno.Should().Be(1);
        season.DataInicio.Should().Be(Inicio);
        season.DataFimExclusiva.Should().Be(FimExclusivo);
        season.Versao.Should().Be(1);
    }

    [Theory]
    [InlineData(2008, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, -1)]
    public void Deve_rejeitar_ano_ou_ordem_intrinsecamente_invalidos_na_atualizacao(
        int ano,
        int ordemNoAno)
    {
        var season = CriarSeason();

        var act = () => season.Atualizar(
            "Season inválida",
            ano,
            ordemNoAno,
            Inicio,
            FimExclusivo,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        season.Ano.Should().Be(2026);
        season.OrdemNoAno.Should().Be(1);
        season.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_periodo_intrinsecamente_invalido_na_atualizacao()
    {
        var season = CriarSeason();

        var act = () => season.Atualizar(
            "Season inválida",
            2026,
            1,
            Inicio,
            Inicio,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodInvalid);
        season.DataInicio.Should().Be(Inicio);
        season.DataFimExclusiva.Should().Be(FimExclusivo);
        season.Versao.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(SelecoesEspecificasInvalidas))]
    public void Deve_rejeitar_formato_invalido_de_selecao_especifica_com_erro_de_validacao(
        IReadOnlyCollection<Guid>? seasonIds)
    {
        var act = () => SelecaoSazonal.Especifica(seasonIds!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_entradas_nulas_com_codigo_localizavel()
    {
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), Guid.NewGuid(), CriadaEm);

        var inclusao = () => SeasonRules.ValidarInclusao(null!, []);
        var selecao = () => SelecaoSazonal.Especifica(null!);
        var ativacao = () => calendario.AtivarSeason(
            null!,
            null,
            calendario.Versao,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        inclusao.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        selecao.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
        ativacao.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
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

    public static TheoryData<IReadOnlyCollection<Guid>?> SelecoesEspecificasInvalidas =>
        new()
        {
            null,
            Array.Empty<Guid>(),
            new[] { Guid.Empty },
            new[] { Guid.NewGuid(), Guid.Empty }
        };

    private static CalendarioCompetitivo CriarCalendario(Guid usuarioId) =>
        new(Guid.NewGuid(), usuarioId, CriadaEm);

    private static CalendarioCompetitivo AtivarEmNovoCalendario(
        Season season,
        Guid usuarioId,
        DateTimeOffset ativadaEm)
    {
        var calendario = CriarCalendario(usuarioId);
        calendario.AtivarSeason(season, null, calendario.Versao, usuarioId, ativadaEm);
        return calendario;
    }

    private static (Season Season, CalendarioCompetitivo Calendario) CriarSeasonEncerrada()
    {
        var season = CriarSeason();
        var usuarioId = Guid.NewGuid();
        var calendario = AtivarEmNovoCalendario(
            season,
            usuarioId,
            DateTimeOffset.Parse("2026-01-01T03:00:00Z"));
        calendario.EncerrarSeason(
            season,
            calendario.Versao,
            usuarioId,
            DateTimeOffset.Parse("2026-05-01T03:00:00Z"));
        return (season, calendario);
    }

}

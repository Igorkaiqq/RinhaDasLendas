using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using System.Reflection;

namespace RinhaDasLendas.Tests.Domain;

public sealed class CompetitiveCalendarEntityTests
{
    public static TheoryData<Type> EntityTypes =>
    [
        typeof(CalendarioCompetitivo),
        typeof(Season),
        typeof(Competicao),
        typeof(Rodada),
        typeof(VersaoRegras)
    ];

    [Fact]
    public void Deve_criar_calendario_competitivo_sem_season_ativa()
    {
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var agora = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

        var calendario = new CalendarioCompetitivo(id, usuarioId, agora);

        calendario.Id.Should().Be(id);
        calendario.SeasonAtivaId.Should().BeNull();
        calendario.Versao.Should().Be(0);
        calendario.AtualizadoEm.Should().Be(agora);
        calendario.AtualizadoPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Deve_criar_season_planejada_com_periodo_e_auditoria()
    {
        var usuarioId = Guid.NewGuid();
        var agora = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

        var season = new Season(
            "  Temporada 2026  ",
            2026,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            usuarioId,
            agora);

        season.Id.Should().NotBeEmpty();
        season.Nome.Should().Be("Temporada 2026");
        season.Ano.Should().Be(2026);
        season.OrdemNoAno.Should().Be(1);
        season.DataInicio.Should().Be(new DateOnly(2026, 1, 1));
        season.DataFimExclusiva.Should().Be(new DateOnly(2027, 1, 1));
        season.Estado.Should().Be(SeasonEstado.Planejada);
        season.Versao.Should().Be(0);
        season.CriadaEm.Should().Be(agora);
        season.AtualizadaEm.Should().Be(agora);
        season.AtivadaEm.Should().BeNull();
        season.EncerradaEm.Should().BeNull();
        season.CriadaPorUsuarioId.Should().Be(usuarioId);
        season.AtualizadaPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Deve_criar_competicao_vinculada_a_season()
    {
        var seasonId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var agora = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

        var competicao = new Competicao(
            seasonId,
            "  Circuito Diário  ",
            "  CD26  ",
            true,
            usuarioId,
            agora);

        competicao.Id.Should().NotBeEmpty();
        competicao.SeasonId.Should().Be(seasonId);
        competicao.Nome.Should().Be("Circuito Diário");
        competicao.Codigo.Should().Be("CD26");
        competicao.CircuitoDiario.Should().BeTrue();
        competicao.Versao.Should().Be(0);
        competicao.CriadaEm.Should().Be(agora);
        competicao.AtualizadaEm.Should().Be(agora);
        competicao.CriadaPorUsuarioId.Should().Be(usuarioId);
        competicao.AtualizadaPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Deve_criar_rodada_ordenada_na_competicao()
    {
        var competicaoId = Guid.NewGuid();
        var agora = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

        var rodada = new Rodada(competicaoId, "  Semifinal  ", 2, agora);

        rodada.Id.Should().NotBeEmpty();
        rodada.CompeticaoId.Should().Be(competicaoId);
        rodada.Nome.Should().Be("Semifinal");
        rodada.Ordem.Should().Be(2);
        rodada.Versao.Should().Be(0);
        rodada.CriadaEm.Should().Be(agora);
        rodada.AtualizadaEm.Should().Be(agora);
    }

    [Fact]
    public void Deve_publicar_versao_de_regras_imutavel_no_escopo_da_competicao()
    {
        var seasonId = Guid.NewGuid();
        var competicaoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var agora = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

        var regras = new VersaoRegras(
            seasonId,
            competicaoId,
            3,
            SerieFormato.Md5,
            ModoDraft.Fearless,
            usuarioId,
            agora);

        regras.Id.Should().NotBeEmpty();
        regras.SeasonId.Should().Be(seasonId);
        regras.CompeticaoId.Should().Be(competicaoId);
        regras.Numero.Should().Be(3);
        regras.Formato.Should().Be(SerieFormato.Md5);
        regras.ModoDraft.Should().Be(ModoDraft.Fearless);
        regras.PublicadaPorUsuarioId.Should().Be(usuarioId);
        regras.PublicadaEm.Should().Be(agora);
        typeof(VersaoRegras).GetProperties()
            .Should().OnlyContain(property => property.SetMethod == null || property.SetMethod.IsPrivate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_season_sem_nome(string? nome)
    {
        var act = () => CriarSeason(nome!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonNameRequired);
    }

    [Fact]
    public void Deve_rejeitar_nome_de_season_acima_do_limite()
    {
        var act = () => CriarSeason(new string('S', 121));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.MaxLengthExceeded);
    }

    [Theory]
    [InlineData(2008)]
    [InlineData(10000)]
    public void Deve_rejeitar_ano_de_season_fora_do_intervalo(int ano)
    {
        var act = () => CriarSeason(ano: ano);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deve_rejeitar_ordem_de_season_nao_positiva(int ordem)
    {
        var act = () => CriarSeason(ordemNoAno: ordem);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_periodo_de_season_sem_fim_posterior_ao_inicio()
    {
        var inicio = new DateOnly(2026, 1, 1);

        var act = () => CriarSeason(dataInicio: inicio, dataFimExclusiva: inicio);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodInvalid);
    }

    [Fact]
    public void Deve_rejeitar_periodo_de_season_com_fim_anterior_ao_inicio()
    {
        var act = () => CriarSeason(
            dataInicio: new DateOnly(2026, 1, 2),
            dataFimExclusiva: new DateOnly(2026, 1, 1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeasonPeriodInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_competicao_sem_nome(string? nome)
    {
        var act = () => CriarCompeticao(nome: nome!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.FieldRequired);
    }

    [Fact]
    public void Deve_rejeitar_nome_de_competicao_acima_do_limite()
    {
        var act = () => CriarCompeticao(nome: new string('C', 121));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.MaxLengthExceeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_competicao_sem_codigo(string? codigo)
    {
        var act = () => CriarCompeticao(codigo: codigo!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.FieldRequired);
    }

    [Fact]
    public void Deve_rejeitar_codigo_de_competicao_acima_do_limite()
    {
        var act = () => CriarCompeticao(codigo: new string('C', 41));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.MaxLengthExceeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_rejeitar_rodada_sem_nome(string? nome)
    {
        var act = () => CriarRodada(nome: nome!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.FieldRequired);
    }

    [Fact]
    public void Deve_rejeitar_nome_de_rodada_acima_do_limite()
    {
        var act = () => CriarRodada(nome: new string('R', 81));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.MaxLengthExceeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deve_rejeitar_ordem_de_rodada_nao_positiva(int ordem)
    {
        var act = () => CriarRodada(ordem: ordem);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deve_rejeitar_numero_de_versao_de_regras_nao_positivo(int numero)
    {
        var act = () => CriarVersaoRegras(numero: numero);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_formato_de_serie_indefinido()
    {
        var act = () => CriarVersaoRegras(formato: (SerieFormato)999);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesMustBeBestOfThreeOrFive);
    }

    [Fact]
    public void Deve_rejeitar_modo_de_draft_indefinido()
    {
        var act = () => CriarVersaoRegras(modoDraft: (ModoDraft)999);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_ids_e_atores_vazios()
    {
        var agora = DateTimeOffset.UtcNow;

        var actions = new Action[]
        {
            () => new CalendarioCompetitivo(Guid.Empty, Guid.NewGuid(), agora),
            () => new CalendarioCompetitivo(Guid.NewGuid(), Guid.Empty, agora),
            () => CriarSeason(criadaPorUsuarioId: Guid.Empty),
            () => CriarCompeticao(seasonId: Guid.Empty),
            () => CriarCompeticao(criadaPorUsuarioId: Guid.Empty),
            () => CriarRodada(competicaoId: Guid.Empty),
            () => CriarVersaoRegras(seasonId: Guid.Empty),
            () => CriarVersaoRegras(competicaoId: Guid.Empty),
            () => CriarVersaoRegras(publicadaPorUsuarioId: Guid.Empty)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_normalizar_metadados_temporais_para_utc()
    {
        var instanteComOffset = new DateTimeOffset(2026, 7, 29, 9, 30, 0, TimeSpan.FromHours(-3));
        var esperado = instanteComOffset.ToUniversalTime();

        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), Guid.NewGuid(), instanteComOffset);
        var season = CriarSeason(criadaEm: instanteComOffset);
        var competicao = CriarCompeticao(criadaEm: instanteComOffset);
        var rodada = CriarRodada(criadaEm: instanteComOffset);
        var regras = CriarVersaoRegras(publicadaEm: instanteComOffset);

        calendario.AtualizadoEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        season.CriadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        season.AtualizadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        competicao.CriadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        competicao.AtualizadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        rodada.CriadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        rodada.AtualizadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
        regras.PublicadaEm.Should().Be(esperado).And.HaveOffset(TimeSpan.Zero);
    }

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Deve_manter_construtor_parametrless_privado_para_ef(Type entityType)
    {
        var constructor = entityType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
        constructor!.IsPrivate.Should().BeTrue();
    }

    private static Season CriarSeason(
        string nome = "Season",
        int ano = 2026,
        int ordemNoAno = 1,
        DateOnly? dataInicio = null,
        DateOnly? dataFimExclusiva = null,
        Guid? criadaPorUsuarioId = null,
        DateTimeOffset? criadaEm = null)
    {
        return new Season(
            nome,
            ano,
            ordemNoAno,
            dataInicio ?? new DateOnly(2026, 1, 1),
            dataFimExclusiva ?? new DateOnly(2027, 1, 1),
            criadaPorUsuarioId ?? Guid.NewGuid(),
            criadaEm ?? DateTimeOffset.UtcNow);
    }

    private static Competicao CriarCompeticao(
        Guid? seasonId = null,
        string nome = "Competicao",
        string codigo = "COMP",
        Guid? criadaPorUsuarioId = null,
        DateTimeOffset? criadaEm = null)
    {
        return new Competicao(
            seasonId ?? Guid.NewGuid(),
            nome,
            codigo,
            false,
            criadaPorUsuarioId ?? Guid.NewGuid(),
            criadaEm ?? DateTimeOffset.UtcNow);
    }

    private static Rodada CriarRodada(
        Guid? competicaoId = null,
        string nome = "Rodada",
        int ordem = 1,
        DateTimeOffset? criadaEm = null)
    {
        return new Rodada(
            competicaoId ?? Guid.NewGuid(),
            nome,
            ordem,
            criadaEm ?? DateTimeOffset.UtcNow);
    }

    private static VersaoRegras CriarVersaoRegras(
        Guid? seasonId = null,
        Guid? competicaoId = null,
        int numero = 1,
        SerieFormato formato = SerieFormato.Md3,
        ModoDraft modoDraft = ModoDraft.Padrao,
        Guid? publicadaPorUsuarioId = null,
        DateTimeOffset? publicadaEm = null)
    {
        return new VersaoRegras(
            seasonId ?? Guid.NewGuid(),
            competicaoId,
            numero,
            formato,
            modoDraft,
            publicadaPorUsuarioId ?? Guid.NewGuid(),
            publicadaEm ?? DateTimeOffset.UtcNow);
    }
}

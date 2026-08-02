using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class CompeticaoTests
{
    private static readonly DateTimeOffset CriadaEm = DateTimeOffset.Parse("2026-01-10T12:00:00Z");

    [Fact]
    public void Deve_criar_competicao_vinculada_exatamente_a_season_informada()
    {
        var seasonId = Guid.NewGuid();

        var competicao = CriarCompeticao(seasonId: seasonId);

        competicao.SeasonId.Should().Be(seasonId);
        competicao.Id.Should().NotBeEmpty();
        competicao.Versao.Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_codigo_de_competicao_duplicado_na_mesma_season()
    {
        var seasonId = Guid.NewGuid();
        var existente = CriarCompeticao(seasonId, "Copa Um", "COPA");
        var candidata = CriarCompeticao(seasonId, "Copa Dois", "COPA");

        var act = () => candidata.ValidarInclusao([existente]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_permitir_o_mesmo_codigo_de_competicao_em_seasons_distintas()
    {
        var existente = CriarCompeticao(Guid.NewGuid(), "Copa anterior", "COPA");
        var candidata = CriarCompeticao(Guid.NewGuid(), "Copa atual", "COPA");

        var act = () => candidata.ValidarInclusao([existente]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_rejeitar_segundo_circuito_diario_na_mesma_season()
    {
        var seasonId = Guid.NewGuid();
        var existente = CriarCompeticao(seasonId, "Circuito Diário", "CD", true);
        var candidata = CriarCompeticao(seasonId, "Outra diária", "OD", true);

        var act = () => candidata.ValidarInclusao([existente]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DailyCircuitCompetitionInvalid);
    }

    [Fact]
    public void Deve_permitir_circuitos_diarios_em_seasons_distintas()
    {
        var existente = CriarCompeticao(Guid.NewGuid(), "Circuito anterior", "CA", true);
        var candidata = CriarCompeticao(Guid.NewGuid(), "Circuito atual", "CAT", true);

        var act = () => candidata.ValidarInclusao([existente]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_permitir_varias_competicoes_na_season_quando_apenas_uma_e_circuito_diario()
    {
        var seasonId = Guid.NewGuid();
        var circuito = CriarCompeticao(seasonId, "Circuito Diário", "CD", true);
        var torneio = CriarCompeticao(seasonId, "Copa da Season", "CS");

        var act = () => torneio.ValidarInclusao([circuito]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_adicionar_rodadas_com_ordens_unicas_e_expo_las_ordenadas()
    {
        var competicao = CriarCompeticao();

        var semifinal = competicao.AdicionarRodada("Semifinal", 2, CriadaEm.AddDays(1));
        var classificatoria = competicao.AdicionarRodada("Classificatória", 1, CriadaEm);

        competicao.Rodadas.Should().ContainInOrder(classificatoria, semifinal);
        competicao.Rodadas.Should().OnlyContain(rodada => rodada.CompeticaoId == competicao.Id);
    }

    [Fact]
    public void Deve_rejeitar_ordem_de_rodada_duplicada_na_competicao()
    {
        var competicao = CriarCompeticao();
        competicao.AdicionarRodada("Primeira rodada", 1, CriadaEm);

        var act = () => competicao.AdicionarRodada("Outra primeira rodada", 1, CriadaEm);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_reordenar_todas_as_rodadas_atomicamente_sem_trocar_identidades()
    {
        var competicao = CriarCompeticao();
        var primeira = competicao.AdicionarRodada("Classificatória", 1, CriadaEm);
        var segunda = competicao.AdicionarRodada("Semifinal", 2, CriadaEm);
        var terceira = competicao.AdicionarRodada("Final", 3, CriadaEm);
        var versaoEsperada = competicao.Versao;
        var atualizadaEm = DateTimeOffset.Parse("2026-02-01T15:00:00-03:00");

        competicao.ReordenarRodadas(
            [terceira.Id, primeira.Id, segunda.Id],
            versaoEsperada,
            Guid.NewGuid(),
            atualizadaEm);

        competicao.Rodadas.Select(rodada => rodada.Id)
            .Should().Equal(terceira.Id, primeira.Id, segunda.Id);
        competicao.Rodadas.Select(rodada => rodada.Ordem).Should().Equal(1, 2, 3);
        competicao.Rodadas.Should().OnlyContain(rodada => rodada.AtualizadaEm == atualizadaEm.ToUniversalTime());
        competicao.Versao.Should().Be(versaoEsperada + 1);
    }

    [Fact]
    public void Deve_rejeitar_reordenacao_com_versao_obsoleta_sem_alterar_qualquer_rodada()
    {
        var competicao = CriarCompeticao();
        var primeira = competicao.AdicionarRodada("Semifinal", 1, CriadaEm);
        var segunda = competicao.AdicionarRodada("Final", 2, CriadaEm);
        var estadoOriginal = CapturarEstado(competicao);
        var versaoAtual = competicao.Versao;

        var act = () => competicao.ReordenarRodadas(
            [segunda.Id, primeira.Id],
            versaoAtual - 1,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.CompetitiveResourceVersionStale);
        CapturarEstado(competicao).Should().BeEquivalentTo(estadoOriginal, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_rejeitar_reordenacao_incompleta_duplicada_ou_com_rodada_desconhecida()
    {
        var criarOrdensInvalidas = new Func<Competicao, IReadOnlyCollection<Guid>>[]
        {
            _ => [],
            competicao => [competicao.Rodadas.First().Id],
            competicao => [competicao.Rodadas.First().Id, competicao.Rodadas.First().Id],
            competicao => [competicao.Rodadas.First().Id, Guid.NewGuid()]
        };

        foreach (var criarOrdemInvalida in criarOrdensInvalidas)
        {
            var competicao = CriarCompeticaoComDuasRodadas();
            var estadoOriginal = CapturarEstado(competicao);
            var ordemInvalida = criarOrdemInvalida(competicao);
            var act = () => competicao.ReordenarRodadas(
                ordemInvalida,
                competicao.Versao,
                Guid.NewGuid(),
                CriadaEm.AddDays(1));

            act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
            CapturarEstado(competicao).Should().BeEquivalentTo(estadoOriginal, options => options.WithStrictOrdering());
        }
    }

    [Fact]
    public void Deve_iniciar_publicacao_de_regras_da_competicao_na_versao_um()
    {
        var competicao = CriarCompeticao();

        var regras = competicao.PublicarRegras(
            SerieFormato.Md3,
            ModoDraft.Fearless,
            competicao.Versao,
            Guid.NewGuid(),
            CriadaEm);

        regras.Numero.Should().Be(1);
        regras.SeasonId.Should().Be(competicao.SeasonId);
        regras.CompeticaoId.Should().Be(competicao.Id);
    }

    [Fact]
    public void Deve_rejeitar_publicacao_concorrente_que_duplicaria_a_versao_de_regras()
    {
        var competicao = CriarCompeticao();
        var versaoAntesDaPrimeiraPublicacao = competicao.Versao;
        competicao.PublicarRegras(
            SerieFormato.Md3,
            ModoDraft.Fearless,
            versaoAntesDaPrimeiraPublicacao,
            Guid.NewGuid(),
            CriadaEm);
        var estadoDepoisDaPrimeiraPublicacao = CapturarEstado(competicao);

        var act = () => competicao.PublicarRegras(
            SerieFormato.Md5,
            ModoDraft.Padrao,
            versaoAntesDaPrimeiraPublicacao,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.CompetitiveResourceVersionStale);
        CapturarEstado(competicao).Should().BeEquivalentTo(
            estadoDepoisDaPrimeiraPublicacao,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deve_publicar_md3_fearless_e_nova_md5_padrao_sem_alterar_a_versao_anterior()
    {
        var competicao = CriarCompeticao();
        var md3 = competicao.PublicarRegras(
            SerieFormato.Md3,
            ModoDraft.Fearless,
            competicao.Versao,
            Guid.NewGuid(),
            CriadaEm);
        var valoresPublicados = CapturarEstado(md3);

        var md5 = competicao.PublicarRegras(
            SerieFormato.Md5,
            ModoDraft.Padrao,
            competicao.Versao,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        CapturarEstado(md3).Should().Be(valoresPublicados);
        md5.Id.Should().NotBe(md3.Id);
        md5.Numero.Should().Be(2);
    }

    [Fact]
    public void Deve_manter_numeracoes_independentes_publicando_regra_especifica_antes_da_geral()
    {
        var season = CriarSeason();
        var competicao = CriarCompeticao(season.Id);

        var especifica = competicao.PublicarRegras(
            SerieFormato.Md3,
            ModoDraft.Fearless,
            competicao.Versao,
            Guid.NewGuid(),
            CriadaEm);
        var geral = season.PublicarRegrasGerais(
            SerieFormato.Md5,
            ModoDraft.Padrao,
            season.Versao,
            Guid.NewGuid(),
            CriadaEm.AddDays(1));

        especifica.Numero.Should().Be(1);
        especifica.CompeticaoId.Should().Be(competicao.Id);
        geral.Numero.Should().Be(1);
        geral.CompeticaoId.Should().BeNull();
    }

    [Fact]
    public void Deve_rejeitar_regra_geral_para_serie_oficial()
    {
        var seasonId = Guid.NewGuid();
        var competicaoId = Guid.NewGuid();
        var regraGeral = CriarRegras(seasonId, null);

        var act = () => regraGeral.ValidarEscopoParaSerie(
            SerieTipo.ConfrontoOficial,
            seasonId,
            competicaoId);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_regra_de_outra_competicao_para_serie_oficial()
    {
        var seasonId = Guid.NewGuid();
        var competicaoId = Guid.NewGuid();
        var regraDeOutraCompeticao = CriarRegras(seasonId, Guid.NewGuid());

        var act = () => regraDeOutraCompeticao.ValidarEscopoParaSerie(
            SerieTipo.ConfrontoOficial,
            seasonId,
            competicaoId);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_aplicar_regra_geral_ao_amistoso_sem_competicao()
    {
        var seasonId = Guid.NewGuid();
        var regraGeral = CriarRegras(seasonId, null);

        var act = () => regraGeral.ValidarEscopoParaSerie(SerieTipo.Amistoso, seasonId, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_aplicar_regra_especifica_ao_amistoso_da_mesma_competicao()
    {
        var seasonId = Guid.NewGuid();
        var competicaoId = Guid.NewGuid();
        var regras = CriarRegras(seasonId, competicaoId);

        var act = () => regras.ValidarEscopoParaSerie(SerieTipo.Amistoso, seasonId, competicaoId);

        act.Should().NotThrow();
    }

    [Fact]
    public void Deve_rejeitar_regra_de_outra_competicao_em_amistoso_vinculado()
    {
        var seasonId = Guid.NewGuid();
        var competicaoAId = Guid.NewGuid();
        var competicaoBId = Guid.NewGuid();
        var regrasDaCompeticaoB = CriarRegras(seasonId, competicaoBId);

        var act = () => regrasDaCompeticaoB.ValidarEscopoParaSerie(
            SerieTipo.Amistoso,
            seasonId,
            competicaoAId);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_regra_geral_em_amistoso_com_competicao()
    {
        var seasonId = Guid.NewGuid();
        var regras = CriarRegras(seasonId, null);

        var act = () => regras.ValidarEscopoParaSerie(SerieTipo.Amistoso, seasonId, Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_regra_especifica_em_amistoso_sem_competicao()
    {
        var seasonId = Guid.NewGuid();
        var regras = CriarRegras(seasonId, Guid.NewGuid());

        var act = () => regras.ValidarEscopoParaSerie(SerieTipo.Amistoso, seasonId, null);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_regra_de_outra_season_em_qualquer_serie()
    {
        var regras = CriarRegras(Guid.NewGuid(), null);

        var act = () => regras.ValidarEscopoParaSerie(SerieTipo.Amistoso, Guid.NewGuid(), null);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.RulesVersionSeasonMismatch);
    }

    private static Season CriarSeason() =>
        new(
            "Primeira Season 2026",
            2026,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 5, 1),
            Guid.NewGuid(),
            CriadaEm);

    private static Competicao CriarCompeticao(
        Guid? seasonId = null,
        string nome = "Copa da Season",
        string codigo = "CS",
        bool circuitoDiario = false)
    {
        return new Competicao(
            seasonId ?? Guid.NewGuid(),
            nome,
            codigo,
            circuitoDiario,
            Guid.NewGuid(),
            CriadaEm);
    }

    private static Competicao CriarCompeticaoComDuasRodadas()
    {
        var competicao = CriarCompeticao();
        competicao.AdicionarRodada("Semifinal", 1, CriadaEm);
        competicao.AdicionarRodada("Final", 2, CriadaEm);
        return competicao;
    }

    private static CompeticaoSnapshot CapturarEstado(Competicao competicao) =>
        new(
            competicao.Id,
            competicao.SeasonId,
            competicao.Nome,
            competicao.Codigo,
            competicao.CircuitoDiario,
            competicao.Versao,
            competicao.CriadaEm,
            competicao.AtualizadaEm,
            competicao.CriadaPorUsuarioId,
            competicao.AtualizadaPorUsuarioId,
            competicao.Rodadas.Select(rodada => new RodadaSnapshot(
                rodada.Id,
                rodada.CompeticaoId,
                rodada.Nome,
                rodada.Ordem,
                rodada.Versao,
                rodada.CriadaEm,
                rodada.AtualizadaEm)).ToArray(),
            competicao.VersoesRegras.Select(CapturarEstado).ToArray());

    private static VersaoRegrasSnapshot CapturarEstado(VersaoRegras regras) =>
        new(
            regras.Id,
            regras.SeasonId,
            regras.CompeticaoId,
            regras.Numero,
            regras.Formato,
            regras.ModoDraft,
            regras.PublicadaEm,
            regras.PublicadaPorUsuarioId);

    private static VersaoRegras CriarRegras(Guid seasonId, Guid? competicaoId) =>
        new(
            seasonId,
            competicaoId,
            1,
            SerieFormato.Md3,
            ModoDraft.Padrao,
            Guid.NewGuid(),
            CriadaEm);

    private sealed record CompeticaoSnapshot(
        Guid Id,
        Guid SeasonId,
        string Nome,
        string Codigo,
        bool CircuitoDiario,
        long Versao,
        DateTimeOffset CriadaEm,
        DateTimeOffset AtualizadaEm,
        Guid CriadaPorUsuarioId,
        Guid AtualizadaPorUsuarioId,
        IReadOnlyCollection<RodadaSnapshot> Rodadas,
        IReadOnlyCollection<VersaoRegrasSnapshot> VersoesRegras);

    private sealed record RodadaSnapshot(
        Guid Id,
        Guid CompeticaoId,
        string Nome,
        int Ordem,
        long Versao,
        DateTimeOffset CriadaEm,
        DateTimeOffset AtualizadaEm);

    private sealed record VersaoRegrasSnapshot(
        Guid Id,
        Guid SeasonId,
        Guid? CompeticaoId,
        int Numero,
        SerieFormato Formato,
        ModoDraft ModoDraft,
        DateTimeOffset PublicadaEm,
        Guid PublicadaPorUsuarioId);
}

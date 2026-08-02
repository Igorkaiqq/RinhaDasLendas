using System.Reflection;
using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Tests.Domain;

public sealed class CompetitiveFoundationRecordEntityTests
{
    private const string Sha512 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static TheoryData<Type> EntityTypes =>
    [
        typeof(EventoCompetitivo),
        typeof(EventoTime),
        typeof(RegistroAuditoriaCompetitiva),
        typeof(OperacaoIdempotente)
    ];

    public static TheoryData<string> CompetitiveCapabilities =>
    [
        AuthPermissions.CanManageSeasons,
        AuthPermissions.CanManageCompetitions,
        AuthPermissions.CanManageMatches,
        AuthPermissions.CanFinalizeMatches,
        AuthPermissions.CanViewCompetitiveAudit
    ];

    private static readonly AcaoAuditoriaCompetitiva[] ExpectedAuditActions =
    [
        AcaoAuditoriaCompetitiva.CalendarioCompetitivoAtualizado,
        AcaoAuditoriaCompetitiva.TemporadaCriada,
        AcaoAuditoriaCompetitiva.TemporadaAtualizada,
        AcaoAuditoriaCompetitiva.TemporadaAtivada,
        AcaoAuditoriaCompetitiva.TemporadaEncerrada,
        AcaoAuditoriaCompetitiva.CompeticaoCriada,
        AcaoAuditoriaCompetitiva.CompeticaoAtualizada,
        AcaoAuditoriaCompetitiva.RodadaCriada,
        AcaoAuditoriaCompetitiva.RodadasReordenadas,
        AcaoAuditoriaCompetitiva.RegrasGeraisSeasonPublicadas,
        AcaoAuditoriaCompetitiva.RegrasCompeticaoPublicadas,
        AcaoAuditoriaCompetitiva.EventoCriado,
        AcaoAuditoriaCompetitiva.EventoAtualizado,
        AcaoAuditoriaCompetitiva.SerieAssociadaAoEvento,
        AcaoAuditoriaCompetitiva.SerieCriada,
        AcaoAuditoriaCompetitiva.SerieIniciada,
        AcaoAuditoriaCompetitiva.PartidaAdicionada,
        AcaoAuditoriaCompetitiva.PicksPartidaRegistrados,
        AcaoAuditoriaCompetitiva.PartidaConfirmada,
        AcaoAuditoriaCompetitiva.PartidaMarcadaComoRemake,
        AcaoAuditoriaCompetitiva.PartidaCorrigida,
        AcaoAuditoriaCompetitiva.PartidaAnulada,
        AcaoAuditoriaCompetitiva.ResultadoSerieConfirmado,
        AcaoAuditoriaCompetitiva.SerieCancelada,
        AcaoAuditoriaCompetitiva.SerieAnulada,
        AcaoAuditoriaCompetitiva.FatoCompetitivoCorrigido
    ];

    public static TheoryData<AcaoAuditoriaCompetitiva> ExceptionalAuditActions =>
    [
        AcaoAuditoriaCompetitiva.SerieAnulada,
        AcaoAuditoriaCompetitiva.PartidaAnulada,
        AcaoAuditoriaCompetitiva.PartidaMarcadaComoRemake,
        AcaoAuditoriaCompetitiva.PartidaCorrigida,
        AcaoAuditoriaCompetitiva.FatoCompetitivoCorrigido
    ];

    [Fact]
    public void Deve_criar_evento_com_quatro_snapshots_e_draft_padrao()
    {
        var seasonId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var instante = new DateTimeOffset(2026, 7, 29, 9, 0, 0, TimeSpan.FromHours(-3));
        var snapshots = CriarSnapshots();

        var evento = new EventoCompetitivo(seasonId, "  Copa das Lendas  ", usuarioId, instante, snapshots);

        evento.Id.Should().NotBeEmpty();
        evento.SeasonId.Should().Be(seasonId);
        evento.Nome.Should().Be("Copa das Lendas");
        evento.ModoDraft.Should().Be(ModoDraft.Padrao);
        evento.Versao.Should().Be(0);
        evento.CriadoEm.Should().Be(instante.ToUniversalTime());
        evento.AtualizadoEm.Should().Be(instante.ToUniversalTime());
        evento.CriadoPorUsuarioId.Should().Be(usuarioId);
        evento.AtualizadoPorUsuarioId.Should().Be(usuarioId);
        evento.Times.Should().HaveCount(4);
        evento.Times.Select(time => time.Ordem).Should().Equal(1, 2, 3, 4);
        evento.Times.Should().OnlyContain(time => time.EventoId == evento.Id);
    }

    [Fact]
    public void Deve_clonar_snapshots_com_identidades_novas_de_evento_time()
    {
        var snapshots = CriarSnapshots();

        var primeiroEvento = CriarEvento(snapshots);
        var segundoEvento = CriarEvento(snapshots);

        typeof(EventoTimeSnapshot).GetProperty("Id").Should().BeNull();
        primeiroEvento.Times.Select(time => time.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        segundoEvento.Times.Select(time => time.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        primeiroEvento.Times.Select(time => time.Id).Should().NotIntersectWith(segundoEvento.Times.Select(time => time.Id));
    }

    [Fact]
    public void Deve_isolar_colecao_do_evento_de_mutacoes_do_chamador()
    {
        var snapshots = CriarSnapshots().ToList();
        var evento = CriarEvento(snapshots);

        snapshots.Clear();
        var act = () => ((ICollection<EventoTime>)evento.Times).Clear();

        evento.Times.Should().HaveCount(4);
        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Deve_rejeitar_quantidade_diferente_de_quatro_snapshots(int quantidade)
    {
        var snapshots = CriarSnapshots().Take(quantidade).ToList();
        while (snapshots.Count < quantidade)
        {
            snapshots.Add(new EventoTimeSnapshot(Guid.NewGuid(), snapshots.Count + 1, "Time", "TAG"));
        }

        var act = () => CriarEvento(snapshots);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_times_duplicados_no_evento()
    {
        var snapshots = CriarSnapshots().ToArray();
        snapshots[3] = snapshots[3] with { TimeId = snapshots[0].TimeId };

        var act = () => CriarEvento(snapshots);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_time_sem_identidade_no_evento()
    {
        var snapshots = CriarSnapshots().ToArray();
        snapshots[0] = snapshots[0] with { TimeId = Guid.Empty };

        var act = () => CriarEvento(snapshots);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_snapshot_nulo_no_evento()
    {
        var snapshots = CriarSnapshots();
        snapshots[0] = null!;

        var act = () => CriarEvento(snapshots);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_ordens_que_nao_formem_intervalo_de_um_a_quatro()
    {
        var snapshots = CriarSnapshots().ToArray();
        snapshots[3] = snapshots[3] with { Ordem = 3 };

        var act = () => CriarEvento(snapshots);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_snapshot_com_identidade_textual_invalida()
    {
        var snapshots = CriarSnapshots().ToArray();
        var actions = new Action[]
        {
            () => CriarEvento(Substituir(snapshots, 0, snapshots[0] with { NomeSnapshot = " " })),
            () => CriarEvento(Substituir(snapshots, 0, snapshots[0] with { NomeSnapshot = new string('N', 101) })),
            () => CriarEvento(Substituir(snapshots, 0, snapshots[0] with { TagSnapshot = new string('T', 11) }))
        };

        actions.Should().AllSatisfy(action => action.Should().Throw<DomainException>());
    }

    [Fact]
    public void Deve_criar_snapshot_de_auditoria_somente_com_campos_competitivos_tipados()
    {
        var id = Guid.NewGuid();
        var dados = new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Picks] = new[] { 11, 22, 33 },
            [CampoSnapshotAuditoria.Versao] = 3L,
            [CampoSnapshotAuditoria.EstadoSerie] = SerieEstado.EmAndamento,
            [CampoSnapshotAuditoria.Id] = id,
            [CampoSnapshotAuditoria.DataInicio] = new DateOnly(2026, 1, 1)
        };

        var snapshot = SnapshotAuditoriaRedigido.Criar(dados);

        snapshot.Campos.Keys.Should().BeEquivalentTo(dados.Keys);
        snapshot.Campos[CampoSnapshotAuditoria.Id].Should().Be(id);
        snapshot.Campos[CampoSnapshotAuditoria.EstadoSerie].Should().Be(SerieEstado.EmAndamento);
        snapshot.Campos[CampoSnapshotAuditoria.Picks].Should().BeEquivalentTo(new[] { 11, 22, 33 });
        snapshot.ValorSerializado.Should().NotContain("password").And.NotContain("payload");
    }

    [Fact]
    public void Deve_serializar_snapshot_tipado_de_forma_deterministica()
    {
        var id = Guid.NewGuid();
        var primeiro = SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Versao] = 2L,
            [CampoSnapshotAuditoria.Id] = id
        });
        var segundo = SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = id,
            [CampoSnapshotAuditoria.Versao] = 2L
        });

        primeiro.ValorSerializado.Should().Be(segundo.ValorSerializado);
    }

    [Fact]
    public void Deve_isolar_listas_do_snapshot_de_mutacoes_do_chamador()
    {
        var picks = new List<int> { 1, 2, 3 };
        var snapshot = SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Picks] = picks
        });

        picks.Add(4);

        snapshot.Campos[CampoSnapshotAuditoria.Picks].Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Deve_rejeitar_chave_fora_da_allowlist_do_snapshot()
    {
        var act = () => SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [(CampoSnapshotAuditoria)999] = 1
        });

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_texto_payload_objeto_ou_lista_excessiva_no_snapshot()
    {
        var actions = new Action[]
        {
            () => CriarSnapshot(CampoSnapshotAuditoria.Resultado, "2-0"),
            () => CriarSnapshot(CampoSnapshotAuditoria.Resultado, new Dictionary<string, string> { ["password"] = "value" }),
            () => CriarSnapshot(CampoSnapshotAuditoria.Picks, Enumerable.Range(1, SnapshotAuditoriaRedigido.QuantidadeMaximaLista + 1).ToArray())
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_primitivos_invalidos_no_snapshot()
    {
        var actions = new Action[]
        {
            () => CriarSnapshot(CampoSnapshotAuditoria.Id, Guid.Empty),
            () => CriarSnapshot(CampoSnapshotAuditoria.Versao, -1L),
            () => CriarSnapshot(CampoSnapshotAuditoria.Ordem, 0),
            () => CriarSnapshot(CampoSnapshotAuditoria.EstadoSerie, (SerieEstado)999),
            () => CriarSnapshot(CampoSnapshotAuditoria.DataInicio, default(DateOnly)),
            () => CriarSnapshot(CampoSnapshotAuditoria.AgendadaPara, default(DateTimeOffset))
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_tipo_clr_ou_enum_de_outro_campo_do_snapshot()
    {
        var actions = new Action[]
        {
            () => CriarSnapshot(CampoSnapshotAuditoria.EstadoSeason, SerieEstado.EmAndamento),
            () => CriarSnapshot(CampoSnapshotAuditoria.EstadoSerie, PartidaEstado.Confirmada),
            () => CriarSnapshot(CampoSnapshotAuditoria.EstadoPartida, SeasonEstado.Ativa),
            () => CriarSnapshot(CampoSnapshotAuditoria.TipoSerie, LadoSerieTipo.TimeOficial),
            () => CriarSnapshot(CampoSnapshotAuditoria.TipoLado, SerieTipo.ConfrontoOficial),
            () => CriarSnapshot(CampoSnapshotAuditoria.FormatoSerie, ModoDraft.Padrao),
            () => CriarSnapshot(CampoSnapshotAuditoria.ModoDraft, SerieFormato.Md3),
            () => CriarSnapshot(CampoSnapshotAuditoria.DecisaoPicksRemake, MotivoTerminoPartida.Normal),
            () => CriarSnapshot(CampoSnapshotAuditoria.MotivoTerminoPartida, DecisaoPicksRemake.PreservarPicks),
            () => CriarSnapshot(CampoSnapshotAuditoria.Versao, 1),
            () => CriarSnapshot(CampoSnapshotAuditoria.Ordem, 1L),
            () => CriarSnapshot(CampoSnapshotAuditoria.Picks, new[] { Guid.NewGuid() }),
            () => CriarSnapshot(CampoSnapshotAuditoria.DataInicio, DateTimeOffset.UtcNow),
            () => CriarSnapshot(CampoSnapshotAuditoria.AgendadaPara, DateOnly.FromDateTime(DateTime.UtcNow))
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_expor_enums_fechados_e_zero_based_para_auditoria()
    {
        Enum.GetValues<AcaoAuditoriaCompetitiva>().Should().Equal(ExpectedAuditActions);
        ((int)ExpectedAuditActions[0]).Should().Be(0);
        Enum.GetValues<CampoSnapshotAuditoria>().Should().Equal(
            CampoSnapshotAuditoria.Id,
            CampoSnapshotAuditoria.SeasonId,
            CampoSnapshotAuditoria.CompeticaoId,
            CampoSnapshotAuditoria.RodadaId,
            CampoSnapshotAuditoria.VersaoRegrasId,
            CampoSnapshotAuditoria.EventoId,
            CampoSnapshotAuditoria.SerieId,
            CampoSnapshotAuditoria.PartidaId,
            CampoSnapshotAuditoria.LadoSerieId,
            CampoSnapshotAuditoria.TimeId,
            CampoSnapshotAuditoria.DraftMontagemId,
            CampoSnapshotAuditoria.EstadoSeason,
            CampoSnapshotAuditoria.EstadoSerie,
            CampoSnapshotAuditoria.EstadoPartida,
            CampoSnapshotAuditoria.Versao,
            CampoSnapshotAuditoria.TipoSerie,
            CampoSnapshotAuditoria.TipoLado,
            CampoSnapshotAuditoria.FormatoSerie,
            CampoSnapshotAuditoria.ModoDraft,
            CampoSnapshotAuditoria.Ordem,
            CampoSnapshotAuditoria.Resultado,
            CampoSnapshotAuditoria.Picks,
            CampoSnapshotAuditoria.DataInicio,
            CampoSnapshotAuditoria.DataFimExclusiva,
            CampoSnapshotAuditoria.AgendadaPara,
            CampoSnapshotAuditoria.DataLocal,
            CampoSnapshotAuditoria.LadoVencedorId,
            CampoSnapshotAuditoria.FearlessHabilitado,
            CampoSnapshotAuditoria.RevisaoNecessaria,
            CampoSnapshotAuditoria.DecisaoPicksRemake,
            CampoSnapshotAuditoria.MotivoTerminoPartida,
            CampoSnapshotAuditoria.Nome,
            CampoSnapshotAuditoria.Ano,
            CampoSnapshotAuditoria.Codigo,
            CampoSnapshotAuditoria.CircuitoDiario,
            CampoSnapshotAuditoria.Numero,
            CampoSnapshotAuditoria.RodadaIds);
        ((int)CampoSnapshotAuditoria.Id).Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(CompetitiveCapabilities))]
    public void Deve_aceitar_somente_capabilities_competitivas_conhecidas(string capacidade)
    {
        var registro = CriarRegistroAuditoria(capacidade: capacidade);

        registro.Capacidade.Should().Be(capacidade);
    }

    [Fact]
    public void Deve_criar_auditoria_com_tipo_fechado_e_valores_redigidos()
    {
        var anterior = CriarSnapshot(CampoSnapshotAuditoria.Resultado, new[] { 2, 0 });
        var posterior = CriarSnapshot(CampoSnapshotAuditoria.Resultado, new[] { 1, 1 });
        var instante = new DateTimeOffset(2026, 7, 29, 9, 0, 0, TimeSpan.FromHours(-3));

        var registro = CriarRegistroAuditoria(
            recursoTipo: RecursoCompetitivoTipo.Serie,
            acao: AcaoAuditoriaCompetitiva.FatoCompetitivoCorrigido,
            capacidade: AuthPermissions.CanFinalizeMatches,
            valorAnterior: anterior,
            valorPosterior: posterior,
            justificativa: "Correção técnica confirmada",
            ocorridoEm: instante);

        registro.RecursoTipo.Should().Be(RecursoCompetitivoTipo.Serie);
        registro.Acao.Should().Be(AcaoAuditoriaCompetitiva.FatoCompetitivoCorrigido);
        registro.ValorAnterior.Should().BeSameAs(anterior);
        registro.ValorPosterior.Should().BeSameAs(posterior);
        registro.OcorridoEm.Should().Be(instante.ToUniversalTime());
    }

    [Fact]
    public void Deve_rejeitar_acao_de_auditoria_fora_do_enum()
    {
        var act = () => CriarRegistroAuditoria(acao: (AcaoAuditoriaCompetitiva)999);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_tipo_capability_e_instante_invalidos_na_auditoria()
    {
        var actions = new Action[]
        {
            () => CriarRegistroAuditoria(recursoTipo: (RecursoCompetitivoTipo)999),
            () => CriarRegistroAuditoria(capacidade: AuthPermissions.CanManageUsers),
            () => CriarRegistroAuditoria(ocorridoEm: new DateTimeOffset())
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Theory]
    [MemberData(nameof(ExceptionalAuditActions))]
    public void Deve_exigir_justificativa_nas_acoes_excepcionais(AcaoAuditoriaCompetitiva acao)
    {
        var actions = new Action[]
        {
            () => CriarRegistroAuditoria(acao: acao, justificativa: null),
            () => CriarRegistroAuditoria(acao: acao, justificativa: " ")
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.CorrectionJustificationRequired));
    }

    [Theory]
    [MemberData(nameof(ExceptionalAuditActions))]
    public void Deve_aceitar_justificativa_limitada_nas_acoes_excepcionais(AcaoAuditoriaCompetitiva acao)
    {
        var registro = CriarRegistroAuditoria(acao: acao, justificativa: "  decisão técnica  ");

        registro.Justificativa.Should().Be("decisão técnica");
    }

    [Fact]
    public void Deve_permitir_acao_normal_sem_justificativa()
    {
        var registro = CriarRegistroAuditoria(
            acao: AcaoAuditoriaCompetitiva.SerieCriada,
            justificativa: null);

        registro.Justificativa.Should().BeNull();
    }

    [Fact]
    public void Deve_rejeitar_justificativa_acima_do_limite()
    {
        var act = () => CriarRegistroAuditoria(
            acao: AcaoAuditoriaCompetitiva.PartidaCorrigida,
            justificativa: new string('J', 501));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.MaxLengthExceeded);
    }

    [Fact]
    public void Deve_preservar_bytes_e_metadados_fechados_em_roundtrip_exato()
    {
        byte[] conteudo = [0, 1, 2, 127, 128, 255];
        IReadOnlyDictionary<MetadadoResultadoOperacao, object?> metadados =
            new Dictionary<MetadadoResultadoOperacao, object?>
            {
                [MetadadoResultadoOperacao.TipoConteudo] = "application/octet-stream",
                [MetadadoResultadoOperacao.Localizacao] = "/api/v1/series/abc?view=full",
                [MetadadoResultadoOperacao.VersaoRecurso] = "\"series-v3\"",
                [MetadadoResultadoOperacao.Repeticao] = false
            };
        var resultado = ResultadoOperacaoIdempotente.Criar(conteudo, metadados);

        var restaurado = ResultadoOperacaoIdempotente.Desserializar(resultado.Serializar());

        restaurado.Conteudo.ToArray().Should().Equal(conteudo);
        restaurado.Metadados.Should().BeEquivalentTo(metadados);
    }

    [Fact]
    public void Deve_isolar_bytes_e_metadados_armazenados_de_mutacoes_do_chamador()
    {
        byte[] conteudo = [1, 2, 3];
        var metadados = new Dictionary<MetadadoResultadoOperacao, object?>
        {
            [MetadadoResultadoOperacao.Localizacao] = "/series/1"
        };
        var resultado = ResultadoOperacaoIdempotente.Criar(conteudo, metadados);

        conteudo[0] = 9;
        metadados[MetadadoResultadoOperacao.Localizacao] = "/alterado";

        resultado.Conteudo.ToArray().Should().Equal(1, 2, 3);
        resultado.Metadados[MetadadoResultadoOperacao.Localizacao].Should().Be("/series/1");
    }

    [Fact]
    public void Deve_expor_metadados_de_resultado_fechados_e_zero_based()
    {
        Enum.GetValues<MetadadoResultadoOperacao>().Should().Equal(
            MetadadoResultadoOperacao.TipoConteudo,
            MetadadoResultadoOperacao.Localizacao,
            MetadadoResultadoOperacao.VersaoRecurso,
            MetadadoResultadoOperacao.Repeticao);
        ((int)MetadadoResultadoOperacao.TipoConteudo).Should().Be(0);
    }

    [Fact]
    public void Deve_rejeitar_metadado_desconhecido_tipo_incorreto_ou_conteudo_excessivo()
    {
        var actions = new Action[]
        {
            () => CriarResultadoIdempotente((MetadadoResultadoOperacao)999, "value"),
            () => CriarResultadoIdempotente(MetadadoResultadoOperacao.TipoConteudo, false),
            () => CriarResultadoIdempotente(MetadadoResultadoOperacao.Localizacao, true),
            () => CriarResultadoIdempotente(MetadadoResultadoOperacao.VersaoRecurso, 1L),
            () => CriarResultadoIdempotente(MetadadoResultadoOperacao.Repeticao, "false"),
            () => CriarResultadoIdempotente(MetadadoResultadoOperacao.Localizacao, "/safe\r\nvalue"),
            () => ResultadoOperacaoIdempotente.Criar(
                new byte[ResultadoOperacaoIdempotente.TamanhoMaximoConteudo + 1],
                new Dictionary<MetadadoResultadoOperacao, object?>())
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_converter_resultado_persistido_corrompido_em_erro_de_dominio()
    {
        var actions = new Action[]
        {
            () => ResultadoOperacaoIdempotente.Desserializar(
                "{\"ConteudoBase64\":\"\",\"Metadados\":{\"Repeticao\":\"false\"}}"),
            () => ResultadoOperacaoIdempotente.Desserializar(
                "{\"ConteudoBase64\":\"\"}")
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_canonicalizar_identidade_da_operacao_idempotente()
    {
        var resultado = CriarResultadoIdempotente();
        var operacao = CriarOperacaoIdempotente(
            metodo: " post ",
            rota: " /API/V1/Series/?page=2 ",
            chave: " chave opaca ",
            resultado: resultado);

        operacao.Metodo.Should().Be("POST");
        operacao.Rota.Should().Be("/api/v1/series");
        operacao.Chave.Should().Be(" chave opaca ");
        operacao.RequestHash.Should().Be(Sha512);
        operacao.RespostaMinima.Should().Be(resultado.Serializar());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/API/V1/SEASONS")]
    [InlineData("/api/v1/seasons/")]
    [InlineData("/api/v1/seasons?ativa=true")]
    public void Deve_produzir_rota_canonical_sem_query_ou_barra_final(string rota)
    {
        var operacao = CriarOperacaoIdempotente(rota: rota);

        operacao.Rota.Should().Be(rota == "/" ? "/" : "/api/v1/seasons");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Deve_rejeitar_hash_que_nao_seja_sha512_hexadecimal_minusculo(string hash)
    {
        var act = () => CriarOperacaoIdempotente(requestHash: hash);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_metodo_rota_chave_status_ou_tipo_invalidos()
    {
        var actions = new Action[]
        {
            () => CriarOperacaoIdempotente(metodo: "P0ST"),
            () => CriarOperacaoIdempotente(rota: "https://example.com/api/v1/series"),
            () => CriarOperacaoIdempotente(rota: " ?x=1 "),
            () => CriarOperacaoIdempotente(chave: " "),
            () => CriarOperacaoIdempotente(chave: new string('k', 201)),
            () => CriarOperacaoIdempotente(statusCode: 99),
            () => CriarOperacaoIdempotente(statusCode: 600),
            () => CriarOperacaoIdempotente(recursoTipo: (RecursoCompetitivoTipo)999)
        };

        actions.Should().AllSatisfy(action => action.Should().Throw<DomainException>());
    }

    [Fact]
    public void Deve_normalizar_instante_seguro_e_reter_por_noventa_dias()
    {
        var instante = new DateTimeOffset(2026, 7, 29, 9, 0, 0, TimeSpan.FromHours(-3));

        var operacao = CriarOperacaoIdempotente(criadaEm: instante);

        operacao.CriadaEm.Should().Be(instante.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        operacao.ExpiraEm.Should().Be(instante.ToUniversalTime().AddDays(90)).And.HaveOffset(TimeSpan.Zero);
    }

    [Fact]
    public void Deve_aceitar_instante_no_limite_seguro_de_retencao()
    {
        var instante = DateTimeOffset.MaxValue.AddDays(-90);

        var operacao = CriarOperacaoIdempotente(criadaEm: instante);

        operacao.ExpiraEm.Should().Be(DateTimeOffset.MaxValue);
    }

    [Fact]
    public void Deve_rejeitar_instante_default_ou_com_overflow_de_retencao()
    {
        var actions = new Action[]
        {
            () => CriarOperacaoIdempotente(criadaEm: new DateTimeOffset()),
            () => CriarOperacaoIdempotente(criadaEm: DateTimeOffset.MaxValue.AddDays(-89))
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Deve_manter_estruturas_append_only_e_construtor_privado_para_ef(Type entityType)
    {
        entityType.GetProperties()
            .Should().OnlyContain(property => property.SetMethod == null || property.SetMethod.IsPrivate);

        var constructor = entityType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
        constructor!.IsPrivate.Should().BeTrue();
    }

    private static EventoCompetitivo CriarEvento(IReadOnlyCollection<EventoTimeSnapshot>? snapshots = null) =>
        new(Guid.NewGuid(), "Evento", Guid.NewGuid(), DateTimeOffset.UtcNow, snapshots ?? CriarSnapshots());

    private static EventoTimeSnapshot[] CriarSnapshots() =>
    [
        new(Guid.NewGuid(), 1, "Time 1", "T1"),
        new(Guid.NewGuid(), 2, "Time 2", "T2"),
        new(Guid.NewGuid(), 3, "Time 3", "T3"),
        new(Guid.NewGuid(), 4, "Time 4", "T4")
    ];

    private static EventoTimeSnapshot[] Substituir(
        EventoTimeSnapshot[] snapshots,
        int indice,
        EventoTimeSnapshot substituto)
    {
        var copia = snapshots.ToArray();
        copia[indice] = substituto;
        return copia;
    }

    private static SnapshotAuditoriaRedigido CriarSnapshot(CampoSnapshotAuditoria campo, object? valor) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?> { [campo] = valor });

    private static RegistroAuditoriaCompetitiva CriarRegistroAuditoria(
        RecursoCompetitivoTipo recursoTipo = RecursoCompetitivoTipo.Serie,
        AcaoAuditoriaCompetitiva acao = AcaoAuditoriaCompetitiva.SerieCriada,
        string capacidade = AuthPermissions.CanManageMatches,
        SnapshotAuditoriaRedigido? valorAnterior = null,
        SnapshotAuditoriaRedigido? valorPosterior = null,
        string? justificativa = null,
        DateTimeOffset? ocorridoEm = null) =>
        new(
            recursoTipo,
            Guid.NewGuid(),
            acao,
            Guid.NewGuid(),
            capacidade,
            justificativa,
            valorAnterior,
            valorPosterior ?? CriarSnapshot(CampoSnapshotAuditoria.EstadoSerie, SerieEstado.Agendada),
            Guid.NewGuid(),
            ocorridoEm ?? DateTimeOffset.UtcNow);

    private static ResultadoOperacaoIdempotente CriarResultadoIdempotente() =>
        ResultadoOperacaoIdempotente.Criar(
            "{\"id\":\"resultado\"}"u8.ToArray(),
            new Dictionary<MetadadoResultadoOperacao, object?>
            {
                [MetadadoResultadoOperacao.TipoConteudo] = "application/json",
                [MetadadoResultadoOperacao.VersaoRecurso] = "\"v1\""
            });

    private static ResultadoOperacaoIdempotente CriarResultadoIdempotente(
        MetadadoResultadoOperacao metadado,
        object? valor) =>
        ResultadoOperacaoIdempotente.Criar(
            [],
            new Dictionary<MetadadoResultadoOperacao, object?> { [metadado] = valor });

    private static OperacaoIdempotente CriarOperacaoIdempotente(
        string metodo = "POST",
        string rota = "/api/v1/series",
        string chave = "retry-001",
        string requestHash = Sha512,
        int statusCode = 201,
        RecursoCompetitivoTipo recursoTipo = RecursoCompetitivoTipo.Serie,
        ResultadoOperacaoIdempotente? resultado = null,
        DateTimeOffset? criadaEm = null) =>
        new(
            Guid.NewGuid(),
            metodo,
            rota,
            chave,
            requestHash,
            statusCode,
            recursoTipo,
            Guid.NewGuid(),
            resultado ?? CriarResultadoIdempotente(),
            criadaEm ?? DateTimeOffset.UtcNow);
}

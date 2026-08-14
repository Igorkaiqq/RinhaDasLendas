using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.ValueObjects;
using System.Reflection;

namespace RinhaDasLendas.Tests.Domain;

public sealed class CompetitiveSeriesEntityTests
{
    public static TheoryData<Type> ChildEntityTypes =>
    [
        typeof(LadoSerie),
        typeof(ParticipanteEsperadoSerie),
        typeof(Partida),
        typeof(PickPartida)
    ];

    public static TheoryData<Type> EntityTypes =>
    [
        typeof(Serie),
        typeof(LadoSerie),
        typeof(ParticipanteEsperadoSerie),
        typeof(Partida),
        typeof(PickPartida)
    ];

    [Fact]
    public void Deve_criar_confronto_oficial_fearless_sem_evento()
    {
        var lados = CriarLados(LadoSerieTipo.TimeOficial);
        var agendadaPara = new DateTimeOffset(2026, 8, 10, 19, 0, 0, TimeSpan.FromHours(-3));
        var criadaEm = new DateTimeOffset(2026, 7, 29, 9, 0, 0, TimeSpan.FromHours(-3));

        var serie = CriarSerie(
            SerieTipo.ConfrontoOficial,
            lados,
            modoDraft: ModoDraft.Fearless,
            fearlessHabilitado: true,
            agendadaPara: agendadaPara,
            criadaEm: criadaEm);

        serie.Id.Should().NotBeEmpty();
        serie.Tipo.Should().Be(SerieTipo.ConfrontoOficial);
        serie.EventoId.Should().BeNull();
        serie.ModoDraft.Should().Be(ModoDraft.Fearless);
        serie.FearlessHabilitado.Should().BeTrue();
        serie.Estado.Should().Be(SerieEstado.Agendada);
        serie.AgendadaPara.Should().Be(agendadaPara.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        serie.CriadaEm.Should().Be(criadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        serie.AtualizadaEm.Should().Be(criadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        serie.Lados.Should().HaveCount(2).And.OnlyContain(lado => lado.SerieId == serie.Id);
        serie.Partidas.Should().BeEmpty();
    }

    [Fact]
    public void Deve_criar_serie_de_evento_apenas_com_draft_padrao()
    {
        var serie = CriarSerie(
            SerieTipo.ConfrontoOficial,
            CriarLados(LadoSerieTipo.TimeOficial),
            eventoId: Guid.NewGuid());

        serie.EventoId.Should().NotBeNull();
        serie.ModoDraft.Should().Be(ModoDraft.Padrao);
        serie.FearlessHabilitado.Should().BeFalse();
    }

    [Fact]
    public void Deve_criar_diaria_temporaria_com_draft_data_capitaes_e_participantes()
    {
        var dataLocal = new DateOnly(2026, 8, 10);
        var serie = CriarSerie(
            SerieTipo.DiariaTemporaria,
            CriarLados(LadoSerieTipo.Temporario, comCapitaes: true, comParticipantes: true),
            draftMontagemId: Guid.NewGuid(),
            dataLocal: dataLocal,
            agendadaPara: new DateTimeOffset(2026, 8, 10, 22, 0, 0, TimeSpan.Zero));

        serie.DraftMontagemId.Should().NotBeNull();
        serie.DataLocal.Should().Be(dataLocal);
        serie.Lados.Should().OnlyContain(lado =>
            lado.Tipo == LadoSerieTipo.Temporario
            && lado.CapitaoJogadorId.HasValue
            && lado.ParticipantesEsperados.Count > 0);
    }

    [Fact]
    public void Deve_criar_amistoso_com_competicao_e_rodada_ambas_presentes_ou_ausentes()
    {
        var comContexto = CriarSerie(
            SerieTipo.Amistoso,
            CriarLados(LadoSerieTipo.TimeOficial));
        var semContexto = CriarSerie(
            SerieTipo.Amistoso,
            CriarLados(LadoSerieTipo.Temporario),
            omitirCompeticao: true,
            omitirRodada: true);

        comContexto.CompeticaoId.Should().NotBeNull();
        comContexto.RodadaId.Should().NotBeNull();
        semContexto.CompeticaoId.Should().BeNull();
        semContexto.RodadaId.Should().BeNull();
    }

    [Fact]
    public void Deve_clonar_lados_e_participantes_recebidos()
    {
        var participantes = new List<ParticipanteEsperadoSerie>
        {
            CriarParticipante(Guid.NewGuid(), 1)
        };
        var primeiro = CriarLado(
            LadoSerieTipo.TimeOficial,
            1,
            Guid.NewGuid(),
            participantes: participantes);
        var segundo = CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid());
        var lados = new List<LadoSerie> { primeiro, segundo };

        var serie = CriarSerie(SerieTipo.ConfrontoOficial, lados);
        participantes.Clear();
        lados.Clear();

        serie.Lados.Should().HaveCount(2);
        serie.Lados.Should().NotContain(lado => ReferenceEquals(lado, primeiro) || ReferenceEquals(lado, segundo));
        serie.Lados.Single(lado => lado.Ordem == 1).ParticipantesEsperados.Should().ContainSingle();
        serie.Lados.Select(lado => lado.Id).Should().NotContain([primeiro.Id, segundo.Id]);
        serie.Lados.Single(lado => lado.Ordem == 1).ParticipantesEsperados.Single().Id
            .Should().NotBe(primeiro.ParticipantesEsperados.Single().Id);
    }

    [Fact]
    public void Deve_gerar_novas_identidades_ao_reutilizar_template_em_duas_series()
    {
        var jogadorId = Guid.NewGuid();
        var origemPrimeiroLado = Guid.NewGuid();
        var template = new[]
        {
            CriarLado(
                LadoSerieTipo.TimeOficial,
                1,
                origemPrimeiroLado,
                nome: "Lado original",
                participantes: [CriarParticipante(jogadorId, 1, "Jogador original")]),
            CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid())
        };

        var primeiraSerie = CriarSerie(SerieTipo.ConfrontoOficial, template);
        var segundaSerie = CriarSerie(SerieTipo.ConfrontoOficial, template);
        var primeiroLadoPrimeiraSerie = primeiraSerie.Lados.Single(lado => lado.Ordem == 1);
        var primeiroLadoSegundaSerie = segundaSerie.Lados.Single(lado => lado.Ordem == 1);
        var participantePrimeiraSerie = primeiroLadoPrimeiraSerie.ParticipantesEsperados.Single();
        var participanteSegundaSerie = primeiroLadoSegundaSerie.ParticipantesEsperados.Single();

        primeiroLadoPrimeiraSerie.Id.Should().NotBe(template[0].Id);
        primeiroLadoSegundaSerie.Id.Should().NotBe(template[0].Id);
        primeiroLadoPrimeiraSerie.Id.Should().NotBe(primeiroLadoSegundaSerie.Id);
        participantePrimeiraSerie.Id.Should().NotBe(template[0].ParticipantesEsperados.Single().Id);
        participanteSegundaSerie.Id.Should().NotBe(template[0].ParticipantesEsperados.Single().Id);
        participantePrimeiraSerie.Id.Should().NotBe(participanteSegundaSerie.Id);

        primeiroLadoPrimeiraSerie.OrigemId.Should().Be(origemPrimeiroLado);
        primeiroLadoSegundaSerie.OrigemId.Should().Be(origemPrimeiroLado);
        primeiroLadoPrimeiraSerie.NomeSnapshot.Should().Be("Lado original");
        primeiroLadoSegundaSerie.NomeSnapshot.Should().Be("Lado original");
        participantePrimeiraSerie.JogadorId.Should().Be(jogadorId);
        participanteSegundaSerie.JogadorId.Should().Be(jogadorId);
        participantePrimeiraSerie.NomeSnapshot.Should().Be("Jogador original");
        participanteSegundaSerie.NomeSnapshot.Should().Be("Jogador original");
    }

    [Theory]
    [MemberData(nameof(ChildEntityTypes))]
    public void Deve_impedir_construcao_publica_de_entidades_filhas(Type entityType)
    {
        entityType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Should().BeEmpty();
        entityType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(constructor => constructor.GetParameters().Length > 0)
            .Should().Contain(constructor => constructor.IsAssembly)
            .And.NotContain(constructor => constructor.IsFamily || constructor.IsFamilyOrAssembly);
    }

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Deve_manter_construtor_sem_parametros_privado_para_ef(Type entityType)
    {
        var constructor = entityType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        constructor.Should().NotBeNull();
        constructor!.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void Deve_expor_factory_publica_com_inputs_immutaveis_para_criacao_de_diaria()
    {
        var factory = typeof(Serie).GetMethod(
            nameof(Serie.CriarDiaria),
            BindingFlags.Public | BindingFlags.Static);

        factory.Should().NotBeNull();
        typeof(Serie).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
        typeof(LadoSerieInput).IsPublic.Should().BeTrue();
        typeof(ParticipanteEsperadoSerieInput).IsPublic.Should().BeTrue();
        typeof(LadoSerieInput).GetProperties().Should().OnlyContain(property => property.SetMethod == null);
        typeof(ParticipanteEsperadoSerieInput).GetProperties()
            .Should().OnlyContain(property => property.SetMethod == null);
        factory!.GetParameters().Should().Contain(parameter =>
            parameter.ParameterType == typeof(IReadOnlyCollection<LadoSerieInput>));
    }

    [Fact]
    public void Deve_preservar_season_id_como_identidade_historica_imutavel_sem_snapshot_de_exibicao()
    {
        var property = typeof(Serie).GetProperty(nameof(Serie.SeasonId));

        property!.SetMethod.Should().NotBeNull();
        property.SetMethod!.IsPrivate.Should().BeTrue();
        typeof(Serie).GetProperties()
            .Should().NotContain(candidate => candidate.Name.Contains("Season", StringComparison.Ordinal)
                && candidate.Name.Contains("Snapshot", StringComparison.Ordinal));
    }

    [Fact]
    public void Deve_rejeitar_ids_obrigatorios_vazios_na_serie()
    {
        var actions = new Action[]
        {
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), seasonId: Guid.Empty),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), versaoRegrasId: Guid.Empty),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), criadaPorUsuarioId: Guid.Empty),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), competicaoId: Guid.Empty),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), rodadaId: Guid.Empty),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), eventoId: Guid.Empty),
            () => CriarSerie(SerieTipo.DiariaTemporaria, CriarLados(LadoSerieTipo.Temporario, true, true), draftMontagemId: Guid.Empty)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_enums_indefinidos_na_serie()
    {
        var actions = new Action[]
        {
            () => CriarSerie((SerieTipo)999, CriarLados(LadoSerieTipo.TimeOficial)),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), modoDraft: (ModoDraft)999)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_formato_indefinido_com_codigo_especifico()
    {
        var act = () => CriarSerie(
            SerieTipo.ConfrontoOficial,
            CriarLados(LadoSerieTipo.TimeOficial),
            formato: (SerieFormato)999);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.SeriesMustBeBestOfThreeOrFive);
    }

    [Theory]
    [InlineData(ModoDraft.Padrao, true)]
    [InlineData(ModoDraft.Fearless, false)]
    public void Deve_rejeitar_incoerencia_entre_modo_e_flag_fearless(ModoDraft modo, bool habilitado)
    {
        var act = () => CriarSerie(
            SerieTipo.ConfrontoOficial,
            CriarLados(LadoSerieTipo.TimeOficial),
            modoDraft: modo,
            fearlessHabilitado: habilitado);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_fearless_em_serie_de_evento()
    {
        var act = () => CriarSerie(
            SerieTipo.ConfrontoOficial,
            CriarLados(LadoSerieTipo.TimeOficial),
            eventoId: Guid.NewGuid(),
            modoDraft: ModoDraft.Fearless,
            fearlessHabilitado: true);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.EventFearlessConflict);
    }

    [Fact]
    public void Deve_exigir_competicao_e_rodada_no_confronto_oficial()
    {
        var actions = new Action[]
        {
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), omitirCompeticao: true),
            () => CriarSerie(SerieTipo.ConfrontoOficial, CriarLados(LadoSerieTipo.TimeOficial), omitirRodada: true)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_exigir_lados_oficiais_no_confronto_oficial()
    {
        var act = () => CriarSerie(
            SerieTipo.ConfrontoOficial,
            CriarLados(LadoSerieTipo.Temporario, true, true));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_exigir_estrutura_completa_na_diaria_temporaria()
    {
        var ladosValidos = () => CriarLados(LadoSerieTipo.Temporario, true, true);
        var actions = new (Action Action, string Code)[]
        {
            (() => CriarSerie(SerieTipo.DiariaTemporaria, ladosValidos(), omitirCompeticao: true), MessageCodes.DailyCircuitCompetitionInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, ladosValidos(), omitirRodada: true), MessageCodes.DailyCircuitCompetitionInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, ladosValidos(), omitirDraft: true), MessageCodes.DailySeriesDraftInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, ladosValidos(), omitirDataLocal: true), MessageCodes.DailySeriesDateInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, CriarLados(LadoSerieTipo.TimeOficial)), MessageCodes.DailySeriesSidesInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, CriarLados(LadoSerieTipo.Temporario, false, true)), MessageCodes.DailySeriesCaptainsInvalid),
            (() => CriarSerie(SerieTipo.DiariaTemporaria, CriarLados(LadoSerieTipo.Temporario, true, false)), MessageCodes.DailySeriesSidesInvalid)
        };

        foreach (var scenario in actions)
        {
            scenario.Action.Should().Throw<DomainException>().WithMessage(scenario.Code);
        }
    }

    [Fact]
    public void Deve_exigir_data_local_da_diaria_correspondente_ao_agendamento()
    {
        var act = () => CriarSerie(
            SerieTipo.DiariaTemporaria,
            CriarLados(LadoSerieTipo.Temporario, true, true),
            dataLocal: new DateOnly(2026, 8, 11),
            agendadaPara: new DateTimeOffset(2026, 8, 10, 22, 0, 0, TimeSpan.Zero));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DailySeriesDateInvalid);
    }

    [Fact]
    public void Deve_exigir_competicao_e_rodada_ambas_presentes_ou_ausentes_no_amistoso()
    {
        var actions = new Action[]
        {
            () => CriarSerie(SerieTipo.Amistoso, CriarLados(LadoSerieTipo.TimeOficial), omitirCompeticao: true),
            () => CriarSerie(SerieTipo.Amistoso, CriarLados(LadoSerieTipo.TimeOficial), omitirRodada: true)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_lados_mistos_no_amistoso()
    {
        var lados = new[]
        {
            CriarLado(LadoSerieTipo.Temporario, 1, Guid.NewGuid(), Guid.NewGuid(), "Capitao 1"),
            CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid())
        };

        var act = () => CriarSerie(SerieTipo.Amistoso, lados);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_exigir_exatamente_dois_lados_com_ids_e_origens_distintos()
    {
        var primeiro = CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid());
        var segundo = CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid());
        var mesmaOrigem = CriarLado(LadoSerieTipo.TimeOficial, 2, primeiro.OrigemId);
        var actions = new Action[]
        {
            () => CriarSerie(SerieTipo.ConfrontoOficial, [primeiro]),
            () => CriarSerie(SerieTipo.ConfrontoOficial, [primeiro, segundo, CriarLado(LadoSerieTipo.TimeOficial, 3, Guid.NewGuid())]),
            () => CriarSerie(SerieTipo.ConfrontoOficial, [primeiro, primeiro]),
            () => CriarSerie(SerieTipo.ConfrontoOficial, [primeiro, mesmaOrigem])
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_rejeitar_participantes_esperados_duplicados_entre_lados()
    {
        var jogadorId = Guid.NewGuid();
        var lados = new[]
        {
            CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), participantes: [CriarParticipante(jogadorId, 1)]),
            CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid(), participantes: [CriarParticipante(jogadorId, 1)])
        };

        var act = () => CriarSerie(SerieTipo.ConfrontoOficial, lados);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_substituir_ids_de_template_duplicados_por_identidades_de_participantes_distintas()
    {
        var primeiroParticipante = CriarParticipante(Guid.NewGuid(), 1);
        var segundoParticipante = CriarParticipante(Guid.NewGuid(), 1);
        typeof(ParticipanteEsperadoSerie).GetProperty(nameof(ParticipanteEsperadoSerie.Id))!
            .SetValue(segundoParticipante, primeiroParticipante.Id);
        var lados = new[]
        {
            CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), participantes: [primeiroParticipante]),
            CriarLado(LadoSerieTipo.TimeOficial, 2, Guid.NewGuid(), participantes: [segundoParticipante])
        };

        var serie = CriarSerie(SerieTipo.ConfrontoOficial, lados);

        serie.Lados.SelectMany(lado => lado.ParticipantesEsperados)
            .Select(participante => participante.Id)
            .Should().OnlyHaveUniqueItems()
            .And.NotContain(primeiroParticipante.Id);
    }

    [Fact]
    public void Deve_rejeitar_capitao_repetido_entre_lados_da_diaria()
    {
        var capitaoId = Guid.NewGuid();
        var lados = new[]
        {
            CriarLado(
                LadoSerieTipo.Temporario,
                1,
                Guid.NewGuid(),
                capitaoId,
                "Capitao",
                participantes: [CriarParticipante(Guid.NewGuid(), 1)]),
            CriarLado(
                LadoSerieTipo.Temporario,
                2,
                Guid.NewGuid(),
                capitaoId,
                "Capitao",
                participantes: [CriarParticipante(Guid.NewGuid(), 1)])
        };

        var act = () => CriarSerie(SerieTipo.DiariaTemporaria, lados);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DailySeriesCaptainsInvalid);
    }

    [Fact]
    public void Deve_validar_primitivos_e_snapshots_do_lado()
    {
        var actions = new (Action Action, string Code)[]
        {
            (() => CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), serieId: Guid.Empty), MessageCodes.ValidationError),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.Empty), MessageCodes.ValidationError),
            (() => CriarLado((LadoSerieTipo)999, 1, Guid.NewGuid()), MessageCodes.ValidationError),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 0, Guid.NewGuid()), MessageCodes.ValidationError),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 3, Guid.NewGuid()), MessageCodes.ValidationError),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), nome: " "), MessageCodes.FieldRequired),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), nome: new string('L', 101)), MessageCodes.MaxLengthExceeded),
            (() => CriarLado(LadoSerieTipo.TimeOficial, 1, Guid.NewGuid(), tag: new string('T', 11)), MessageCodes.MaxLengthExceeded),
            (() => CriarLado(LadoSerieTipo.Temporario, 1, Guid.NewGuid(), Guid.NewGuid(), new string('C', 101)), MessageCodes.MaxLengthExceeded)
        };

        foreach (var scenario in actions)
        {
            scenario.Action.Should().Throw<DomainException>().WithMessage(scenario.Code);
        }
    }

    [Fact]
    public void Deve_normalizar_snapshots_do_lado()
    {
        var lado = CriarLado(
            LadoSerieTipo.Temporario,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Capitao  ",
            nome: "  Lado  ",
            tag: "  TAG  ");

        lado.NomeSnapshot.Should().Be("Lado");
        lado.TagSnapshot.Should().Be("TAG");
        lado.CapitaoNomeSnapshot.Should().Be("Capitao");
    }

    [Fact]
    public void Deve_normalizar_snapshots_opcionais_vazios_para_nulo()
    {
        var lado = CriarLado(
            LadoSerieTipo.TimeOficial,
            1,
            Guid.NewGuid(),
            nome: "Lado",
            tag: "  ");

        lado.TagSnapshot.Should().BeNull();
        lado.CapitaoNomeSnapshot.Should().BeNull();
    }

    [Fact]
    public void Deve_validar_primitivos_snapshot_e_duplicidade_de_participantes()
    {
        var jogadorId = Guid.NewGuid();
        var actions = new (Action Action, string Code)[]
        {
            (() => CriarParticipante(jogadorId, 1, ladoSerieId: Guid.Empty), MessageCodes.ValidationError),
            (() => CriarParticipante(Guid.Empty, 1), MessageCodes.ValidationError),
            (() => CriarParticipante(jogadorId, 0), MessageCodes.ValidationError),
            (() => CriarParticipante(jogadorId, 1, " "), MessageCodes.FieldRequired),
            (() => CriarParticipante(jogadorId, 1, new string('P', 101)), MessageCodes.MaxLengthExceeded),
            (() => CriarLado(
                LadoSerieTipo.TimeOficial,
                1,
                Guid.NewGuid(),
                participantes: [CriarParticipante(jogadorId, 1), CriarParticipante(jogadorId, 2)]), MessageCodes.ValidationError)
        };

        foreach (var scenario in actions)
        {
            scenario.Action.Should().Throw<DomainException>().WithMessage(scenario.Code);
        }
    }

    [Fact]
    public void Deve_criar_partida_interna_em_rascunho_e_normalizar_utc()
    {
        var criadaEm = new DateTimeOffset(2026, 7, 29, 9, 30, 0, TimeSpan.FromHours(-3));

        var partida = new Partida(Guid.NewGuid(), 2, criadaEm);

        partida.Id.Should().NotBeEmpty();
        partida.Ordem.Should().Be(2);
        partida.Estado.Should().Be(PartidaEstado.Rascunho);
        partida.CriadaEm.Should().Be(criadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        partida.AtualizadaEm.Should().Be(criadaEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
        partida.Picks.Should().BeEmpty();
    }

    [Fact]
    public void Deve_rejeitar_serie_vazia_e_ordem_nao_positiva_na_partida()
    {
        var actions = new Action[]
        {
            () => new Partida(Guid.Empty, 1, DateTimeOffset.UtcNow),
            () => new Partida(Guid.NewGuid(), 0, DateTimeOffset.UtcNow)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    [Fact]
    public void Deve_criar_pick_ordinario_com_primeira_versao_valida_e_utc()
    {
        var registradoEm = new DateTimeOffset(2026, 7, 29, 10, 0, 0, TimeSpan.FromHours(-3));

        var pick = new PickPartida(Guid.NewGuid(), Guid.NewGuid(), 266, 4, registradoEm);

        pick.Id.Should().NotBeEmpty();
        pick.ChampionId.Should().Be(266);
        pick.Ordem.Should().Be(4);
        pick.VersaoFato.Should().Be(1);
        pick.Valido.Should().BeTrue();
        pick.RegistradoEm.Should().Be(registradoEm.ToUniversalTime()).And.HaveOffset(TimeSpan.Zero);
    }

    [Fact]
    public void Deve_manter_estado_de_correcao_de_pick_inacessivel_ate_t041()
    {
        typeof(PickPartida).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(constructor => !constructor.IsPrivate)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.Name)
            .Should().NotContain(["versaoFato", "valido"]);

        typeof(PickPartida).GetProperty(nameof(PickPartida.VersaoFato))!.SetMethod!.IsPrivate.Should().BeTrue();
        typeof(PickPartida).GetProperty(nameof(PickPartida.Valido))!.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 6)]
    public void Deve_rejeitar_champion_e_ordem_invalidos_no_pick(int championId, int ordem)
    {
        var act = () => new PickPartida(Guid.NewGuid(), Guid.NewGuid(), championId, ordem, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError);
    }

    [Fact]
    public void Deve_rejeitar_ids_vazios_no_pick()
    {
        var actions = new Action[]
        {
            () => new PickPartida(Guid.Empty, Guid.NewGuid(), 1, 1, DateTimeOffset.UtcNow),
            () => new PickPartida(Guid.NewGuid(), Guid.Empty, 1, 1, DateTimeOffset.UtcNow)
        };

        actions.Should().AllSatisfy(action =>
            action.Should().Throw<DomainException>().WithMessage(MessageCodes.ValidationError));
    }

    private static Serie CriarSerie(
        SerieTipo tipo,
        IReadOnlyCollection<LadoSerie> lados,
        Guid? seasonId = null,
        Guid? competicaoId = null,
        Guid? rodadaId = null,
        Guid? versaoRegrasId = null,
        Guid? eventoId = null,
        Guid? draftMontagemId = null,
        SerieFormato formato = SerieFormato.Md3,
        ModoDraft modoDraft = ModoDraft.Padrao,
        bool fearlessHabilitado = false,
        DateTimeOffset? agendadaPara = null,
        DateOnly? dataLocal = null,
        Guid? criadaPorUsuarioId = null,
        DateTimeOffset? criadaEm = null,
        bool omitirCompeticao = false,
        bool omitirRodada = false,
        bool omitirDraft = false,
        bool omitirDataLocal = false)
    {
        var diaria = tipo == SerieTipo.DiariaTemporaria;
        var agendamento = agendadaPara ?? new DateTimeOffset(2026, 8, 10, 22, 0, 0, TimeSpan.Zero);

        return new Serie(
            seasonId ?? Guid.NewGuid(),
            omitirCompeticao ? null : competicaoId ?? Guid.NewGuid(),
            omitirRodada ? null : rodadaId ?? Guid.NewGuid(),
            versaoRegrasId ?? Guid.NewGuid(),
            eventoId,
            omitirDraft ? null : draftMontagemId ?? (diaria ? Guid.NewGuid() : null),
            tipo,
            formato,
            modoDraft,
            fearlessHabilitado,
            agendamento,
            omitirDataLocal ? null : dataLocal ?? (diaria ? DataLocalSaoPaulo(agendamento) : null),
            criadaPorUsuarioId ?? Guid.NewGuid(),
            criadaEm ?? DateTimeOffset.UtcNow,
            lados);
    }

    private static IReadOnlyCollection<LadoSerie> CriarLados(
        LadoSerieTipo tipo,
        bool comCapitaes = false,
        bool comParticipantes = false)
    {
        return
        [
            CriarLado(
                tipo,
                1,
                Guid.NewGuid(),
                comCapitaes ? Guid.NewGuid() : null,
                comCapitaes ? "Capitao 1" : null,
                participantes: comParticipantes ? [CriarParticipante(Guid.NewGuid(), 1)] : []),
            CriarLado(
                tipo,
                2,
                Guid.NewGuid(),
                comCapitaes ? Guid.NewGuid() : null,
                comCapitaes ? "Capitao 2" : null,
                participantes: comParticipantes ? [CriarParticipante(Guid.NewGuid(), 1)] : [])
        ];
    }

    private static LadoSerie CriarLado(
        LadoSerieTipo tipo,
        int ordem,
        Guid origemId,
        Guid? capitaoJogadorId = null,
        string? capitaoNomeSnapshot = null,
        string nome = "Lado",
        string? tag = "TAG",
        IReadOnlyCollection<ParticipanteEsperadoSerie>? participantes = null,
        Guid? serieId = null)
    {
        return new LadoSerie(
            serieId ?? Guid.NewGuid(),
            ordem,
            tipo,
            origemId,
            nome,
            tag,
            capitaoJogadorId,
            capitaoNomeSnapshot,
            participantes ?? []);
    }

    private static ParticipanteEsperadoSerie CriarParticipante(
        Guid jogadorId,
        int ordem,
        string nome = "Jogador",
        Guid? ladoSerieId = null)
    {
        return new ParticipanteEsperadoSerie(ladoSerieId ?? Guid.NewGuid(), jogadorId, nome, ordem);
    }

    private static DateOnly DataLocalSaoPaulo(DateTimeOffset instante)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, timeZone).DateTime);
    }
}

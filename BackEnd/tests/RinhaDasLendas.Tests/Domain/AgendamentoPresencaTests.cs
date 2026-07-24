using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class AgendamentoPresencaTests
{
    private static readonly Guid Responsavel = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Agora = new(2026, 7, 24, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly UltimaDataAvaliada = new(2026, 7, 23);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_exigir_nome(string? nome)
    {
        var act = () => Criar(nome: nome!);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleNameRequired);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Deve_exigir_nome_entre_tres_e_cem_caracteres(string nome)
    {
        var act = () => Criar(nome: nome);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleNameLengthInvalid);
    }

    [Fact]
    public void Deve_rejeitar_observacao_acima_de_quinhentos_caracteres()
    {
        var act = () => Criar(observacao: new string('a', 501));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleObservationTooLong);
    }

    [Fact]
    public void Deve_exigir_ao_menos_um_dia()
    {
        var act = () => Criar(dias: []);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleDayRequired);
    }

    [Fact]
    public void Deve_rejeitar_dias_duplicados()
    {
        var act = () => Criar(dias: [DiaSemanaIso.Sexta, DiaSemanaIso.Sexta]);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleDayDuplicated);
    }

    [Theory]
    [MemberData(nameof(JanelasInvalidas))]
    public void Deve_exigir_janela_posterior_com_precisao_de_minuto(TimeOnly publicacao, TimeOnly encerramento)
    {
        var act = () => Criar(publicacao: publicacao, encerramento: encerramento);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleTimeRangeInvalid);
    }

    public static TheoryData<TimeOnly, TimeOnly> JanelasInvalidas => new()
    {
        { new TimeOnly(20, 0), new TimeOnly(18, 0) },
        { new TimeOnly(18, 0), new TimeOnly(18, 0) },
        { new TimeOnly(18, 0, 1), new TimeOnly(20, 0) },
        { new TimeOnly(18, 0), new TimeOnly(20, 0, 1) },
        { new TimeOnly(18, 0).Add(TimeSpan.FromTicks(1)), new TimeOnly(20, 0) }
    };

    [Fact]
    public void Deve_normalizar_dados_e_ordenar_dias()
    {
        var agenda = Criar(
            nome: "  Rinha semanal  ",
            observacao: "  Sem atrasos  ",
            dias: [DiaSemanaIso.Domingo, DiaSemanaIso.Segunda]);

        agenda.Nome.Should().Be("Rinha semanal");
        agenda.Observacao.Should().Be("Sem atrasos");
        agenda.DiasSemana.Select(item => item.DiaSemana).Should().Equal(DiaSemanaIso.Segunda, DiaSemanaIso.Domingo);
    }

    [Fact]
    public void Deve_normalizar_observacao_em_branco_para_nulo()
    {
        Criar(observacao: "   ").Observacao.Should().BeNull();
    }

    [Fact]
    public void Deve_criar_ativa_com_marcador_autoria_e_instantes_explicitos()
    {
        var agenda = Criar();

        agenda.Id.Should().NotBeEmpty();
        agenda.Status.Should().Be(AgendamentoPresencaStatus.Ativo);
        agenda.AtivadoEm.Should().Be(Agora);
        agenda.PausadoEm.Should().BeNull();
        agenda.ArquivadoEm.Should().BeNull();
        agenda.UltimaDataAvaliada.Should().Be(UltimaDataAvaliada);
        agenda.CriadoPorUsuarioId.Should().Be(Responsavel);
        agenda.CriadoEm.Should().Be(Agora);
        agenda.AtualizadoEm.Should().Be(Agora);
    }

    [Theory]
    [InlineData(2026, 7, 20, true)]
    [InlineData(2026, 7, 21, false)]
    [InlineData(2026, 7, 24, true)]
    [InlineData(2026, 7, 26, false)]
    public void Deve_identificar_dias_iso_sem_depender_da_cultura(int ano, int mes, int dia, bool esperado)
    {
        var agenda = Criar(dias: [DiaSemanaIso.Segunda, DiaSemanaIso.Sexta]);

        agenda.OcorreEm(new DateOnly(ano, mes, dia)).Should().Be(esperado);
    }

    [Fact]
    public void Deve_editar_configuracao_sem_alterar_ocorrencias_existentes()
    {
        var agenda = Criar();
        var ocorrencia = OcorrenciaAgendamentoPresenca.Processando(
            agenda.Id,
            new DateOnly(2026, 7, 24),
            Agora.AddHours(3),
            Agora.AddHours(5),
            Agora);
        agenda.AdicionarOcorrencia(ocorrencia);

        agenda.Editar(
            "Nova rinha",
            "Nova observacao",
            new TimeOnly(19, 0),
            new TimeOnly(21, 0),
            [DiaSemanaIso.Sabado],
            Responsavel,
            Agora.AddMinutes(1));

        agenda.Nome.Should().Be("Nova rinha");
        agenda.Ocorrencias.Should().ContainSingle().Which.Should().BeSameAs(ocorrencia);
        ocorrencia.PublicacaoPrevistaEm.Should().Be(Agora.AddHours(3));
        ocorrencia.EncerramentoPrevistoEm.Should().Be(Agora.AddHours(5));
    }

    [Fact]
    public void Deve_marcar_data_avaliada_sem_retroceder()
    {
        var agenda = Criar();

        agenda.MarcarDataAvaliada(UltimaDataAvaliada.AddDays(-1), Agora.AddMinutes(1));

        agenda.UltimaDataAvaliada.Should().Be(UltimaDataAvaliada);
        agenda.AtualizadoEm.Should().Be(Agora);

        agenda.MarcarDataAvaliada(UltimaDataAvaliada.AddDays(1), Agora.AddMinutes(2));

        agenda.UltimaDataAvaliada.Should().Be(UltimaDataAvaliada.AddDays(1));
        agenda.AtualizadoEm.Should().Be(Agora.AddMinutes(2));
    }

    [Fact]
    public void Deve_manter_igualdade_da_ativacao_e_publicacao_elegivel()
    {
        var agenda = Criar(agora: Agora);

        (agenda.AtivadoEm > Agora).Should().BeFalse();
        agenda.AtivadoEm.Should().Be(Agora);
    }

    [Fact]
    public void Deve_auditar_criacao_apenas_com_nomes_estaveis_ordenados()
    {
        var agenda = Criar(nome: "Segredo", observacao: "Valor confidencial");

        var historico = agenda.Historicos.Should().ContainSingle().Subject;
        historico.Acao.Should().Be(AgendamentoPresencaAcao.Criado);
        historico.ResponsavelUsuarioId.Should().Be(Responsavel);
        historico.RegistradoEm.Should().Be(Agora);
        historico.CamposAlterados.Should().Be("DiasSemana,HorarioEncerramentoLocal,HorarioPublicacaoLocal,Nome,Observacao,Status");
        historico.CamposAlterados.Should().NotContain("Segredo").And.NotContain("confidencial");
    }

    [Fact]
    public void Deve_auditar_somente_campos_realmente_editados_sem_valores()
    {
        var agenda = Criar();

        agenda.Editar(
            "Outra rinha",
            null,
            agenda.HorarioPublicacaoLocal,
            agenda.HorarioEncerramentoLocal,
            agenda.DiasSemana.Select(item => item.DiaSemana).ToArray(),
            Responsavel,
            Agora.AddMinutes(1));

        var historico = agenda.Historicos.Last();
        historico.Acao.Should().Be(AgendamentoPresencaAcao.Editado);
        historico.CamposAlterados.Should().Be("Nome");
        historico.CamposAlterados.Should().NotContain("Outra rinha");
    }

    [Fact]
    public void Deve_pausar_e_reativar_repetidamente_sem_mudar_o_resultado_e_com_auditoria()
    {
        var agenda = Criar();
        var primeiraPausa = Agora.AddMinutes(1);
        var segundaPausa = Agora.AddMinutes(2);

        agenda.Pausar(Responsavel, primeiraPausa);
        agenda.Pausar(Responsavel, segundaPausa);

        agenda.Status.Should().Be(AgendamentoPresencaStatus.Pausado);
        agenda.PausadoEm.Should().Be(primeiraPausa);
        agenda.AtualizadoEm.Should().Be(segundaPausa);
        agenda.Historicos.Count(item => item.Acao == AgendamentoPresencaAcao.Pausado).Should().Be(2);
        agenda.Historicos.Where(item => item.Acao == AgendamentoPresencaAcao.Pausado)
            .Select(item => item.CamposAlterados)
            .Should().Equal("Status", string.Empty);

        var primeiraReativacao = Agora.AddMinutes(3);
        var segundaReativacao = Agora.AddMinutes(4);
        agenda.Reativar(Responsavel, primeiraReativacao);
        agenda.Reativar(Responsavel, segundaReativacao);

        agenda.Status.Should().Be(AgendamentoPresencaStatus.Ativo);
        agenda.AtivadoEm.Should().Be(primeiraReativacao);
        agenda.PausadoEm.Should().BeNull();
        agenda.AtualizadoEm.Should().Be(segundaReativacao);
        agenda.Historicos.Count(item => item.Acao == AgendamentoPresencaAcao.Reativado).Should().Be(2);
        agenda.Historicos.Where(item => item.Acao == AgendamentoPresencaAcao.Reativado)
            .Select(item => item.CamposAlterados)
            .Should().Equal("Status", string.Empty);
    }

    [Fact]
    public void Deve_arquivar_logicamente_e_impedir_novas_mutacoes()
    {
        var agenda = Criar();
        var arquivadoEm = Agora.AddMinutes(1);
        agenda.Arquivar(Responsavel, arquivadoEm);

        agenda.Status.Should().Be(AgendamentoPresencaStatus.Arquivado);
        agenda.ArquivadoEm.Should().Be(arquivadoEm);
        agenda.Historicos.Last().CamposAlterados.Should().Be("Status");

        Action[] acts =
        [
            () => agenda.Editar("Outra", null, new TimeOnly(18, 0), new TimeOnly(20, 0), [DiaSemanaIso.Sexta], Responsavel, Agora.AddMinutes(2)),
            () => agenda.Pausar(Responsavel, Agora.AddMinutes(2)),
            () => agenda.Reativar(Responsavel, Agora.AddMinutes(2)),
            () => agenda.Arquivar(Responsavel, Agora.AddMinutes(2))
        ];

        acts.Should().AllSatisfy(act => act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleArchived));
    }

    [Fact]
    public void Deve_criar_ocorrencia_processando()
    {
        var agendaId = Guid.NewGuid();
        var data = new DateOnly(2026, 7, 24);
        var publicacao = Agora.AddHours(3);
        var encerramento = Agora.AddHours(5);

        var ocorrencia = OcorrenciaAgendamentoPresenca.Processando(agendaId, data, publicacao, encerramento, Agora);

        ocorrencia.Id.Should().NotBeEmpty();
        ocorrencia.AgendamentoPresencaId.Should().Be(agendaId);
        ocorrencia.DataLocal.Should().Be(data);
        ocorrencia.PublicacaoPrevistaEm.Should().Be(publicacao);
        ocorrencia.EncerramentoPrevistoEm.Should().Be(encerramento);
        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        ocorrencia.UltimaTentativaEm.Should().Be(Agora);
        ocorrencia.CriadaEm.Should().Be(Agora);
        ocorrencia.AtualizadaEm.Should().Be(Agora);
        ocorrencia.CodigoFalha.Should().BeNull();
        ocorrencia.DraftMontagemId.Should().BeNull();
    }

    [Fact]
    public void Deve_criar_bloqueada_e_reiniciar_processamento()
    {
        var ocorrencia = CriarBloqueada();

        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Bloqueada);
        ocorrencia.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleDiscordUnavailable);

        ocorrencia.IniciarProcessamento(Agora.AddMinutes(1));

        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        ocorrencia.CodigoFalha.Should().BeNull();
        ocorrencia.UltimaTentativaEm.Should().Be(Agora.AddMinutes(1));
        ocorrencia.AtualizadaEm.Should().Be(Agora.AddMinutes(1));
    }

    [Fact]
    public void Deve_marcar_processando_como_criada()
    {
        var ocorrencia = CriarProcessando();
        var draftId = Guid.NewGuid();

        ocorrencia.MarcarCriada(draftId, Agora.AddMinutes(1));

        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
        ocorrencia.DraftMontagemId.Should().Be(draftId);
        ocorrencia.CodigoFalha.Should().BeNull();
        ocorrencia.AtualizadaEm.Should().Be(Agora.AddMinutes(1));
    }

    [Fact]
    public void Deve_marcar_bloqueada_como_perdida()
    {
        var ocorrencia = CriarBloqueada();

        ocorrencia.MarcarPerdida(MessageCodes.PresenceScheduleWindowExpired, Agora.AddMinutes(1));

        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Perdida);
        ocorrencia.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleWindowExpired);
        ocorrencia.DraftMontagemId.Should().BeNull();
    }

    [Fact]
    public void Deve_marcar_processando_como_falha()
    {
        var ocorrencia = CriarProcessando();

        ocorrencia.MarcarFalha(MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(1));

        ocorrencia.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Falha);
        ocorrencia.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleTimeZoneInvalid);
        ocorrencia.DraftMontagemId.Should().BeNull();
    }

    [Fact]
    public void Deve_rejeitar_janela_invalida_na_ocorrencia()
    {
        var act = () => OcorrenciaAgendamentoPresenca.Processando(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 24),
            Agora.AddHours(5),
            Agora.AddHours(3),
            Agora);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleTimeRangeInvalid);
    }

    [Fact]
    public void Deve_rejeitar_transicoes_fora_do_fluxo_e_estados_terminais()
    {
        var processando = CriarProcessando();
        var bloqueada = CriarBloqueada();
        var criada = CriarProcessando();
        criada.MarcarCriada(Guid.NewGuid(), Agora.AddMinutes(1));
        var perdida = CriarBloqueada();
        perdida.MarcarPerdida(MessageCodes.PresenceScheduleWindowExpired, Agora.AddMinutes(1));
        var falha = CriarProcessando();
        falha.MarcarFalha(MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(1));

        Action[] acts =
        [
            () => processando.IniciarProcessamento(Agora.AddMinutes(2)),
            () => processando.MarcarPerdida(MessageCodes.PresenceScheduleWindowExpired, Agora.AddMinutes(2)),
            () => bloqueada.MarcarCriada(Guid.NewGuid(), Agora.AddMinutes(2)),
            () => bloqueada.MarcarFalha(MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(2)),
            () => criada.MarcarFalha(MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(2)),
            () => perdida.IniciarProcessamento(Agora.AddMinutes(2)),
            () => falha.MarcarCriada(Guid.NewGuid(), Agora.AddMinutes(2))
        ];

        acts.Should().AllSatisfy(act => act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleOccurrenceConflict));
    }

    private static AgendamentoPresenca Criar(
        string nome = "Rinha",
        string? observacao = null,
        TimeOnly? publicacao = null,
        TimeOnly? encerramento = null,
        IReadOnlyCollection<DiaSemanaIso>? dias = null,
        DateOnly? ultimaDataAvaliada = null,
        DateTimeOffset? agora = null)
    {
        return new AgendamentoPresenca(
            nome,
            observacao,
            publicacao ?? new TimeOnly(18, 0),
            encerramento ?? new TimeOnly(20, 0),
            dias ?? [DiaSemanaIso.Sexta],
            ultimaDataAvaliada ?? UltimaDataAvaliada,
            Responsavel,
            agora ?? Agora);
    }

    private static OcorrenciaAgendamentoPresenca CriarProcessando()
    {
        return OcorrenciaAgendamentoPresenca.Processando(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 24),
            Agora.AddHours(3),
            Agora.AddHours(5),
            Agora);
    }

    private static OcorrenciaAgendamentoPresenca CriarBloqueada()
    {
        return OcorrenciaAgendamentoPresenca.Bloqueada(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 24),
            Agora.AddHours(3),
            Agora.AddHours(5),
            MessageCodes.PresenceScheduleDiscordUnavailable,
            Agora);
    }
}

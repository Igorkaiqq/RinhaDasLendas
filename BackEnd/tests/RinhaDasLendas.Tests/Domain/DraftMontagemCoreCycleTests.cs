using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class DraftMontagemCoreCycleTests
{
    [Fact]
    public void CriarPorPresencaDeveIniciarCicloV2SemModoNemTimes()
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 5);

        montagem.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
        montagem.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
        montagem.Modo.Should().BeNull();
        montagem.Times.Should().BeEmpty();
        montagem.Participantes.Should().BeEmpty();
    }

    [Fact]
    public void CriarManualDiretoDeveAbrirBoardSemCapitaesESepararReservas()
    {
        var jogadoresIds = Enumerable.Range(1, 12).Select(_ => Guid.NewGuid()).ToList();

        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);

        montagem.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.Modo.Should().Be(DraftMontagemModo.Manual);
        montagem.QuantidadeTimes.Should().Be(2);
        montagem.QuantidadeReservas.Should().Be(2);
        montagem.Times.Should().HaveCount(2).And.OnlyContain(time => time.CapitaoId == null);
        montagem.Participantes.Should().HaveCount(12);
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Livre).Should().Be(10);
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Reserva).Should().Be(2);
        montagem.Participantes.Should().OnlyContain(item => !item.Capitao && item.TimeId == null);
    }

    [Fact]
    public void SelecionarManualDeveAbrirBoardSemCapitaes()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(12);

        montagem.SelecionarModo(DraftMontagemModo.Manual, jogadoresIds.ToHashSet());

        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.Modo.Should().Be(DraftMontagemModo.Manual);
        montagem.Times.Should().HaveCount(2).And.OnlyContain(time => time.CapitaoId == null);
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Livre).Should().Be(10);
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Reserva).Should().Be(2);
    }

    [Fact]
    public void SelecionarTempoRealDevePrepararTitularesEReservasSemIniciarTurno()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(12);

        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());

        montagem.Status.Should().Be(DraftMontagemStatus.PresencaEncerrada);
        montagem.Modo.Should().Be(DraftMontagemModo.TempoReal);
        montagem.Times.Should().BeEmpty();
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Livre).Should().Be(10);
        montagem.Participantes.Count(item => item.Estado == DraftMontagemParticipanteEstado.Reserva).Should().Be(2);
        montagem.TurnoAtualTimeId.Should().BeNull();
        montagem.TurnoAtualCapitaoId.Should().BeNull();
        montagem.TurnoSequencia.Should().BeNull();
    }

    [Theory]
    [InlineData(DraftMontagemModo.Manual)]
    [InlineData(DraftMontagemModo.TempoReal)]
    public void RepetirMesmoModoDeveSerIdempotente(DraftMontagemModo modo)
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(12);
        var jogadoresAtivosIds = jogadoresIds.ToHashSet();
        montagem.SelecionarModo(modo, jogadoresAtivosIds);
        var versao = montagem.VersaoEstado;
        var timesIds = montagem.Times.Select(time => time.Id).ToList();
        var participantesIds = montagem.Participantes.Select(item => item.Id).ToList();

        montagem.SelecionarModo(modo, jogadoresAtivosIds);

        montagem.VersaoEstado.Should().Be(versao);
        montagem.Times.Select(time => time.Id).Should().Equal(timesIds);
        montagem.Participantes.Select(item => item.Id).Should().Equal(participantesIds);
    }

    [Fact]
    public void AlterarModoDepoisDaEscolhaDeveFalharSemMutacao()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        var jogadoresAtivosIds = jogadoresIds.ToHashSet();
        montagem.SelecionarModo(DraftMontagemModo.Manual, jogadoresAtivosIds);
        var versao = montagem.VersaoEstado;
        var timesIds = montagem.Times.Select(time => time.Id).ToList();

        var act = () => montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresAtivosIds);

        act.Should().Throw<DomainException>();
        montagem.Modo.Should().Be(DraftMontagemModo.Manual);
        montagem.VersaoEstado.Should().Be(versao);
        montagem.Times.Select(time => time.Id).Should().Equal(timesIds);
    }

    [Fact]
    public void DefinirCapitaesV2DeveRejeitarReservaMesmoQuandoGlobalmenteElegivel()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(12);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var reservaId = jogadoresIds.Last();
        var capitaesIds = new[] { jogadoresIds.First(), reservaId };

        var act = () => montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeStarter);
    }

    [Fact]
    public void DefinirCapitaesV2DeveRejeitarTitularSemElegibilidadeGlobal()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();

        var act = () => montagem.DefinirCapitaes(capitaesIds, new[] { capitaesIds[0] }.ToHashSet());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeEligible);
    }

    [Fact]
    public void DefinirCapitaesV2DeveManterElegivelNaoDesignadoComoJogadorComum()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        var capitaoNaoDesignado = jogadoresIds[2];

        montagem.DefinirCapitaes(capitaesIds, jogadoresIds.Take(3).ToHashSet());

        montagem.Participantes.Single(participante => participante.JogadorId == capitaoNaoDesignado).Capitao.Should().BeFalse();
        montagem.Participantes.Single(participante => participante.JogadorId == capitaoNaoDesignado).Estado.Should().Be(DraftMontagemParticipanteEstado.Livre);
    }

    [Fact]
    public void OrdemV2DeveAguardarInicioExplicitoECriarUmUnicoPrimeiroTurno()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        var elegiveisIds = capitaesIds.ToHashSet();
        montagem.DefinirCapitaes(capitaesIds, elegiveisIds);

        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);

        montagem.Status.Should().Be(DraftMontagemStatus.OrdemDefinida);
        montagem.TurnoSequencia.Should().BeNull();
        montagem.TurnoAtualCapitaoId.Should().BeNull();

        var agora = DateTimeOffset.UtcNow;
        montagem.IniciarTempoReal(agora, elegiveisIds);

        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.Modo.Should().Be(DraftMontagemModo.TempoReal);
        montagem.TurnoSequencia.Should().Be(1);
        montagem.TurnoAtualCapitaoId.Should().Be(capitaesIds[0]);
        montagem.Escolhas.Should().BeEmpty();

        var act = () => montagem.IniciarTempoReal(agora.AddSeconds(1), elegiveisIds);

        act.Should().Throw<DomainException>();
        montagem.TurnoSequencia.Should().Be(1);
        montagem.Escolhas.Should().BeEmpty();
    }

    [Fact]
    public void IniciarV2DeveRevalidarElegibilidadeDosCapitaesDesignados()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);

        var act = () => montagem.IniciarTempoReal(DateTimeOffset.UtcNow, new[] { capitaesIds[0] }.ToHashSet());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeEligible);
        montagem.Status.Should().Be(DraftMontagemStatus.OrdemDefinida);
        montagem.TurnoSequencia.Should().BeNull();
    }

    [Fact]
    public void PickV2DeveRevalidarElegibilidadeDoCapitaoDoTurno()
    {
        var (montagem, jogadoresIds) = CriarPresencaEncerrada(10);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        var agora = DateTimeOffset.UtcNow;
        montagem.IniciarTempoReal(agora, capitaesIds.ToHashSet());

        var act = () => montagem.RegistrarPickTempoReal(capitaesIds[0], jogadoresIds[2], agora.AddSeconds(1), new[] { capitaesIds[1] }.ToHashSet());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeEligible);
        montagem.Participantes.Single(participante => participante.JogadorId == jogadoresIds[2]).Estado.Should().Be(DraftMontagemParticipanteEstado.Livre);
    }

    [Fact]
    public void ManualV2DeveAceitarLayoutCompletoSemCapitaesEFinalizar()
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);

        montagem.SalvarLayout(CriarLayoutCompleto(montagem, jogadoresIds), [], []);
        montagem.Finalizar();

        montagem.Status.Should().Be(DraftMontagemStatus.Finalizada);
        montagem.Times.Should().OnlyContain(time => time.CapitaoId == null);
        montagem.Participantes.Should().OnlyContain(participante => !participante.Capitao);
    }

    [Fact]
    public void ManualV2DeveRejeitarFinalizacaoComTimeIncompletoETitularLivre()
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);
        var times = montagem.Times.OrderBy(time => time.Ordem).ToList();
        var livre = jogadoresIds[^1];
        montagem.SalvarLayout(
        [
            new DraftMontagemLayoutTime(times[0].Id, times[0].Nome, null, CriarParticipantesLayout(jogadoresIds.Take(5))),
            new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, null, CriarParticipantesLayout(jogadoresIds.Skip(5).Take(4))),
        ],
        [new DraftMontagemLayoutParticipante(livre, 1, null)],
        []);

        var act = montagem.Finalizar;

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.IncompleteDraft);
        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManualV2DevePreservarClassificacaoVigenteDeReservasAoSalvarLayout(bool reservaNoTime)
    {
        var jogadoresIds = Enumerable.Range(1, 12).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);
        var times = montagem.Times.OrderBy(time => time.Ordem).ToList();
        var jogadoresTimeDois = jogadoresIds.Skip(5).Take(5).ToList();
        IReadOnlyCollection<DraftMontagemLayoutParticipante> livres = [];
        var reservas = jogadoresIds.Skip(10).Select((id, index) => new DraftMontagemLayoutParticipante(id, index + 1, null)).ToList();
        if (reservaNoTime)
        {
            jogadoresTimeDois[^1] = jogadoresIds[10];
            livres = [new DraftMontagemLayoutParticipante(jogadoresIds[9], 1, null)];
            reservas = [new DraftMontagemLayoutParticipante(jogadoresIds[11], 1, null)];
        }
        else
        {
            jogadoresTimeDois.RemoveAt(jogadoresTimeDois.Count - 1);
            reservas.Add(new DraftMontagemLayoutParticipante(jogadoresIds[9], 3, null));
        }

        var act = () => montagem.SalvarLayout(
        [
            new DraftMontagemLayoutTime(times[0].Id, times[0].Nome, null, CriarParticipantesLayout(jogadoresIds.Take(5))),
            new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, null, CriarParticipantesLayout(jogadoresTimeDois)),
        ],
        livres,
        reservas);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.InconsistentDataFound);
    }

    [Fact]
    public void ManualV2DeveRejeitarCapitaoNoLayoutSemPersistirAutoridade()
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);
        var times = montagem.Times.OrderBy(time => time.Ordem).ToList();

        var act = () => montagem.SalvarLayout(
        [
            new DraftMontagemLayoutTime(times[0].Id, times[0].Nome, jogadoresIds[0], CriarParticipantesLayout(jogadoresIds.Take(5))),
            new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, null, CriarParticipantesLayout(jogadoresIds.Skip(5).Take(5))),
        ],
        [],
        []);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.InconsistentDataFound);
        montagem.Times.Should().OnlyContain(time => time.CapitaoId == null);
        montagem.Participantes.Should().OnlyContain(participante => !participante.Capitao);
    }

    [Fact]
    public void SalvarLayoutComErroTardioDevePreservarEstadoCompletoEVersao()
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);
        var times = montagem.Times.OrderBy(time => time.Ordem).ToList();
        var versao = montagem.VersaoEstado;
        var timesAntes = montagem.Times.Select(time => new { time.Id, time.Nome, time.Ordem, time.CapitaoId }).ToList();
        var participantesAntes = montagem.Participantes.Select(participante => new
        {
            participante.JogadorId,
            participante.TimeId,
            participante.Estado,
            participante.Capitao,
            participante.Ordem,
            participante.RotaContextual,
            participante.DataAtualizacao,
        }).ToList();
        var segundoTime = jogadoresIds.Skip(5).Take(4).Append(jogadoresIds[0]);

        var act = () => montagem.SalvarLayout(
        [
            new DraftMontagemLayoutTime(times[0].Id, "Nome alterado", null, CriarParticipantesLayout(jogadoresIds.Take(5))),
            new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, null, CriarParticipantesLayout(segundoTime)),
        ],
        [],
        []);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftPlayerAlreadyPicked);
        montagem.VersaoEstado.Should().Be(versao);
        montagem.Times.Select(time => new { time.Id, time.Nome, time.Ordem, time.CapitaoId }).Should().BeEquivalentTo(timesAntes);
        montagem.Participantes.Select(participante => new
        {
            participante.JogadorId,
            participante.TimeId,
            participante.Estado,
            participante.Capitao,
            participante.Ordem,
            participante.RotaContextual,
            participante.DataAtualizacao,
        }).Should().BeEquivalentTo(participantesAntes);
    }

    [Fact]
    public void TempoRealV2NaoDeveAceitarFinalizacaoManual()
    {
        var (montagem, _) = CriarTempoRealIniciado(4, 2);

        var act = montagem.Finalizar;

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftClosed);
        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
    }

    [Fact]
    public void TempoRealV2DeveFinalizarSomenteAposUltimoPickCompletarTodosOsTimes()
    {
        var (montagem, jogadoresIds) = CriarTempoRealIniciado(4, 2);
        var capitaesIds = jogadoresIds.Take(2).ToHashSet();
        var agora = DateTimeOffset.UtcNow;

        montagem.RegistrarPickTempoReal(jogadoresIds[0], jogadoresIds[2], agora, capitaesIds);
        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);

        montagem.RegistrarPickTempoReal(jogadoresIds[1], jogadoresIds[3], agora.AddSeconds(1), capitaesIds);

        montagem.Status.Should().Be(DraftMontagemStatus.Finalizada);
        montagem.Times.Should().OnlyContain(time => montagem.Participantes.Count(participante => participante.TimeId == time.Id) == 2);
        montagem.TurnoAtualCapitaoId.Should().BeNull();
    }

    [Fact]
    public void TempoRealV2DevePermanecerAbertoAposTimeoutEnquantoHaVagas()
    {
        var (montagem, _) = CriarTempoRealIniciado(4, 2);

        montagem.AvancarTurnoPorTimeout(montagem.TurnoExpiraEm!.Value).Should().BeTrue();

        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.TurnoAtualCapitaoId.Should().NotBeNull();
    }

    [Fact]
    public void TempoRealV2DevePermanecerAbertoQuandoCapitaoDoTurnoNaoForMaisElegivel()
    {
        var (montagem, jogadoresIds) = CriarTempoRealIniciado(4, 2);

        var act = () => montagem.RegistrarPickTempoReal(
            montagem.TurnoAtualCapitaoId!.Value,
            jogadoresIds[2],
            DateTimeOffset.UtcNow,
            new HashSet<Guid>());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeEligible);
        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.TurnoAtualCapitaoId.Should().NotBeNull();
    }

    [Fact]
    public void SubstituicaoV2DeveExigirNovoCapitaoExplicitoSemHerdarAutoridade()
    {
        var (montagem, jogadoresIds) = CriarTempoRealIniciado(5, 2);
        var time = montagem.Times.Single(item => item.CapitaoId == montagem.TurnoAtualCapitaoId);
        var capitaoSaiuId = time.CapitaoId!.Value;
        var reservaEntrouId = jogadoresIds[^1];

        var act = () => montagem.SubstituirPorReserva(
            time.Id,
            capitaoSaiuId,
            reservaEntrouId,
            null,
            jogadoresIds.ToHashSet(),
            null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainsRequired);
        time.CapitaoId.Should().Be(capitaoSaiuId);
        montagem.Participantes.Single(item => item.JogadorId == reservaEntrouId).Capitao.Should().BeFalse();
    }

    [Fact]
    public void SubstituicaoV2DeCapitaoDoTurnoDeveAtualizarAutoridadeParaNovoCapitaoElegivel()
    {
        var (montagem, jogadoresIds) = CriarTempoRealIniciado(5, 2);
        var time = montagem.Times.Single(item => item.CapitaoId == montagem.TurnoAtualCapitaoId);
        var capitaoSaiuId = time.CapitaoId!.Value;
        var reservaEntrouId = jogadoresIds[^1];

        montagem.SubstituirPorReserva(
            time.Id,
            capitaoSaiuId,
            reservaEntrouId,
            reservaEntrouId,
            new HashSet<Guid> { reservaEntrouId },
            null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        time.CapitaoId.Should().Be(reservaEntrouId);
        montagem.TurnoAtualCapitaoId.Should().Be(reservaEntrouId);
        montagem.Participantes.Single(item => item.JogadorId == capitaoSaiuId).Capitao.Should().BeFalse();
        montagem.Participantes.Single(item => item.JogadorId == reservaEntrouId).Capitao.Should().BeTrue();
    }

    [Fact]
    public void SubstituicaoV2DeJogadorComumDeveRejeitarNovoCapitaoInformado()
    {
        var (montagem, jogadoresIds) = CriarTempoRealIniciado(5, 2);
        var capitaesIds = jogadoresIds.Take(2).ToHashSet();
        var agora = DateTimeOffset.UtcNow;
        montagem.RegistrarPickTempoReal(jogadoresIds[0], jogadoresIds[2], agora, capitaesIds);
        var time = montagem.Times.Single(item => item.Id == montagem.Participantes.Single(participante => participante.JogadorId == jogadoresIds[2]).TimeId);
        var reservaEntrouId = jogadoresIds[^1];
        var versao = montagem.VersaoEstado;

        var act = () => montagem.SubstituirPorReserva(
            time.Id,
            jogadoresIds[2],
            reservaEntrouId,
            Guid.NewGuid(),
            new HashSet<Guid>(),
            null,
            Guid.NewGuid(),
            agora.AddSeconds(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemNewCaptainNotAllowed);
        montagem.VersaoEstado.Should().Be(versao);
        montagem.Participantes.Single(item => item.JogadorId == jogadoresIds[2]).Estado.Should().Be(DraftMontagemParticipanteEstado.Time);
        montagem.Participantes.Single(item => item.JogadorId == reservaEntrouId).Estado.Should().Be(DraftMontagemParticipanteEstado.Reserva);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EstadoTerminalDeveBloquearLayoutSorteioCapitaesPickESubstituicao(bool cancelada)
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadoresIds);
        if (cancelada)
        {
            montagem.Cancelar(null);
        }
        else
        {
            montagem.SalvarLayout(CriarLayoutCompleto(montagem, jogadoresIds), [], []);
            montagem.Finalizar();
        }

        Action[] mutacoes =
        [
            () => montagem.SalvarLayout([], [], []),
            montagem.SortearCapitaes,
            () => montagem.DefinirCapitaes([]),
            () => montagem.RegistrarPickTempoReal(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow),
            () => montagem.SubstituirPorReserva(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                new HashSet<Guid>(),
                null,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow),
        ];

        foreach (var mutacao in mutacoes)
        {
            mutacao.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftClosed);
        }
    }

    [Fact]
    public void DraftLegadoDeveIniciarSemRevalidarCapitaesRetroativamente()
    {
        var jogadoresIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = new DraftMontagem(
            "Rinha",
            null,
            5,
            DraftMontagemCriterioCapitaes.Manual,
            jogadoresIds,
            jogadoresIds.Take(2).ToList());
        typeof(DraftMontagem)
            .GetProperty(nameof(DraftMontagem.CicloVersao))!
            .SetValue(montagem, DraftMontagemCicloVersao.Legado);

        montagem.IniciarTempoReal(DateTimeOffset.UtcNow, new HashSet<Guid>());

        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.Modo.Should().Be(DraftMontagemModo.TempoReal);
        montagem.TurnoSequencia.Should().Be(1);
    }

    private static (DraftMontagem Montagem, IReadOnlyList<Guid> JogadoresIds) CriarPresencaEncerrada(int quantidade)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 5);
        var jogadoresIds = Enumerable.Range(1, quantidade).Select(_ => Guid.NewGuid()).ToList();
        foreach (var jogadorId in jogadoresIds)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogadorId, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(false, 5);
        return (montagem, jogadoresIds);
    }

    private static IReadOnlyCollection<DraftMontagemLayoutTime> CriarLayoutCompleto(
        DraftMontagem montagem,
        IReadOnlyList<Guid> jogadoresIds)
    {
        return montagem.Times
            .OrderBy(time => time.Ordem)
            .Select((time, index) => new DraftMontagemLayoutTime(
                time.Id,
                time.Nome,
                null,
                CriarParticipantesLayout(jogadoresIds.Skip(index * montagem.TamanhoEquipe).Take(montagem.TamanhoEquipe))))
            .ToList();
    }

    private static (DraftMontagem Montagem, IReadOnlyList<Guid> JogadoresIds) CriarTempoRealIniciado(
        int quantidade,
        int tamanhoEquipe)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, tamanhoEquipe);
        var jogadoresIds = Enumerable.Range(1, quantidade).Select(_ => Guid.NewGuid()).ToList();
        foreach (var jogadorId in jogadoresIds)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogadorId, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(quantidade < 10, tamanhoEquipe);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(montagem.QuantidadeTimes).ToList();
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        montagem.IniciarTempoReal(DateTimeOffset.UtcNow, capitaesIds.ToHashSet());
        return (montagem, jogadoresIds);
    }

    private static IReadOnlyCollection<DraftMontagemLayoutParticipante> CriarParticipantesLayout(IEnumerable<Guid> jogadoresIds)
    {
        return jogadoresIds
            .Select((jogadorId, index) => new DraftMontagemLayoutParticipante(jogadorId, index + 1, null))
            .ToList();
    }
}

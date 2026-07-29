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
}

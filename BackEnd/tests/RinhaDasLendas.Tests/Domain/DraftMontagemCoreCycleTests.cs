using FluentAssertions;
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

    private static (DraftMontagem Montagem, IReadOnlyCollection<Guid> JogadoresIds) CriarPresencaEncerrada(int quantidade)
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

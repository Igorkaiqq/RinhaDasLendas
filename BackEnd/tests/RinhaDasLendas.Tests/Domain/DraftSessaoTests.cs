using FluentAssertions;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class DraftSessaoTests
{
    [Fact]
    public void Deve_criar_draft_aberto_com_capitaes()
    {
        var jogadores = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();

        var draft = new DraftSessao("Rinha", null, 2, jogadores[0], jogadores[1], DraftCriterioSelecao.Manual, DraftTime.TimeA, DraftCriterioSelecao.Manual, jogadores);

        draft.Status.Should().Be(DraftStatus.Aberto);
        draft.Participantes.Should().HaveCount(4);
        draft.Participantes.Count(participante => participante.Capitao).Should().Be(2);
        draft.ProximoTime.Should().Be(DraftTime.TimeA);
    }

    [Fact]
    public void Deve_registrar_pick_e_alternar_proximo_time()
    {
        var jogadores = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = new DraftSessao("Rinha", null, 2, jogadores[0], jogadores[1], DraftCriterioSelecao.Manual, DraftTime.TimeA, DraftCriterioSelecao.Manual, jogadores);

        draft.RegistrarPick(jogadores[2]);

        draft.Escolhas.Should().ContainSingle();
        draft.Participantes.Single(participante => participante.JogadorId == jogadores[2]).Time.Should().Be(DraftTime.TimeA);
        draft.ProximoTime.Should().Be(DraftTime.TimeB);
    }

    [Fact]
    public void Deve_impedir_pick_duplicado()
    {
        var jogadores = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = new DraftSessao("Rinha", null, 2, jogadores[0], jogadores[1], DraftCriterioSelecao.Manual, DraftTime.TimeA, DraftCriterioSelecao.Manual, jogadores);

        draft.RegistrarPick(jogadores[2]);
        var act = () => draft.RegistrarPick(jogadores[2]);

        act.Should().Throw<DomainException>().WithMessage("Jogador ja foi escolhido neste draft.");
    }

    [Fact]
    public void Deve_concluir_quando_times_estao_completos()
    {
        var jogadores = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = new DraftSessao("Rinha", null, 2, jogadores[0], jogadores[1], DraftCriterioSelecao.Manual, DraftTime.TimeA, DraftCriterioSelecao.Manual, jogadores);

        draft.RegistrarPick(jogadores[2]);
        draft.RegistrarPick(jogadores[3]);

        draft.Status.Should().Be(DraftStatus.Concluido);
        draft.ProximoTime.Should().BeNull();
    }
}

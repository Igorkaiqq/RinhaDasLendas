using FluentAssertions;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Drafts;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Handlers.Drafts;

public sealed class RegistrarPickDraftCommandHandlerTests
{
    [Fact]
    public async Task Deve_registrar_pick()
    {
        var repository = new InMemoryDraftRepository();
        var jogadores = Enumerable.Range(1, 4).Select(index => JogadorTestData.JogadorAtivo($"Jogador {index}")).ToList();
        jogadores.ForEach(repository.AddJogador);
        var draft = new DraftSessao("Rinha", null, 2, jogadores[0].Id, jogadores[1].Id, DraftCriterioSelecao.Manual, DraftTime.TimeA, DraftCriterioSelecao.Manual, jogadores.Select(jogador => jogador.Id));
        await repository.AddAsync(draft, CancellationToken.None);
        var handler = new RegistrarPickDraftCommandHandler(repository, new RegistrarPickDraftValidator());

        var response = await handler.Handle(new RegistrarPickDraftCommand(draft.Id, new RegistrarPickDraftRequestDto(jogadores[2].Id)), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Escolhas.Should().ContainSingle();
        response.ProximoTime.Should().Be("TimeB");
    }
}

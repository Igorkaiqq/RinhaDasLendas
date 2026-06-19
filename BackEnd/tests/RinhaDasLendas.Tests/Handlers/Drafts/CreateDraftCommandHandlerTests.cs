using FluentAssertions;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Drafts;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Handlers.Drafts;

public sealed class CreateDraftCommandHandlerTests
{
    [Fact]
    public async Task Deve_criar_draft_com_jogadores_ativos()
    {
        var repository = new InMemoryDraftRepository();
        var jogadores = Enumerable.Range(1, 4).Select(index => JogadorTestData.JogadorAtivo($"Jogador {index}")).ToList();
        jogadores.ForEach(repository.AddJogador);
        var request = new CreateDraftRequestDto("Rinha", null, 2, false, jogadores[0].Id, jogadores[1].Id, false, "TimeA", jogadores.Select(jogador => jogador.Id).ToList());
        var handler = new CreateDraftCommandHandler(repository, new CreateDraftValidator());

        var response = await handler.Handle(new CreateDraftCommand(request), CancellationToken.None);

        response.Status.Should().Be("Aberto");
        response.Disponiveis.Should().HaveCount(2);
        response.CapitaoTimeA.Id.Should().Be(jogadores[0].Id);
    }
}

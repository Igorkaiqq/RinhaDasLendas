using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class InativarTimeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldInactivateTeam_WhenItExists()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var created = await new CreateTimeCommandHandler(repository, new CreateTimeValidator())
            .Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);
        var handler = new InativarTimeCommandHandler(repository);

        var response = await handler.Handle(new InativarTimeCommand(created.Id), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Status.Should().Be("Inativo");
    }
}

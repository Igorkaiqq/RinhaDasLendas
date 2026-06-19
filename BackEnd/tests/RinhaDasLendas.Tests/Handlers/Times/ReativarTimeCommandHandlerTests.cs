using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class ReativarTimeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReactivateTeam_WhenItIsValid()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var created = await new CreateTimeCommandHandler(repository, new CreateTimeValidator())
            .Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);
        await new InativarTimeCommandHandler(repository).Handle(new InativarTimeCommand(created.Id), CancellationToken.None);
        var handler = new ReativarTimeCommandHandler(repository);

        var response = await handler.Handle(new ReativarTimeCommand(created.Id), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Status.Should().Be("Ativo");
    }
}

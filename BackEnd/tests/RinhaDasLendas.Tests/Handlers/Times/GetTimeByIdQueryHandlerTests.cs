using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Queries.Times;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class GetTimeByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTeam_WhenItExists()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var created = await new CreateTimeCommandHandler(repository, new CreateTimeValidator())
            .Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);
        var handler = new GetTimesQueryHandler(repository);

        var response = await handler.Handle(new GetTimeByIdQuery(created.Id), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Id.Should().Be(created.Id);
    }
}

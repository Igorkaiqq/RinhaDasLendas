using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Queries.Times;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class GetTimesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPaginatedFilteredTeams()
    {
        var repository = new InMemoryTimeRepository();
        var jogadorA = TimeHandlerTestData.CreateJogador("Faker da Firma");
        var jogadorB = TimeHandlerTestData.CreateJogador("Outro Jogador");
        repository.AddJogador(jogadorA);
        repository.AddJogador(jogadorB);
        var createHandler = new CreateTimeCommandHandler(repository, new CreateTimeValidator());
        await createHandler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogadorA.Id, nome: "Alpha", tag: "ALP")), CancellationToken.None);
        await createHandler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogadorB.Id, nome: "Beta", tag: "BET")), CancellationToken.None);
        var handler = new GetTimesQueryHandler(repository);

        var response = await handler.Handle(new GetTimesQuery("faker", "Ativo", 1, 1), CancellationToken.None);

        response.Page.Should().Be(1);
        response.PageSize.Should().Be(1);
        response.TotalItems.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.Items.Should().ContainSingle(time => time.Nome == "Alpha");
    }
}

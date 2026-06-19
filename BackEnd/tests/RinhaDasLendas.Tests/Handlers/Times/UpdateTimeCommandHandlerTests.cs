using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class UpdateTimeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateTeam_WhenRequestIsValid()
    {
        var repository = new InMemoryTimeRepository();
        var jogadorA = TimeHandlerTestData.CreateJogador("Jogador A");
        var jogadorB = TimeHandlerTestData.CreateJogador("Jogador B");
        repository.AddJogador(jogadorA);
        repository.AddJogador(jogadorB);
        var createHandler = new CreateTimeCommandHandler(repository, new CreateTimeValidator());
        var created = await createHandler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogadorA.Id)), CancellationToken.None);
        var handler = new UpdateTimeCommandHandler(repository, new UpdateTimeValidator());

        var response = await handler.Handle(new UpdateTimeCommand(created.Id, TimeHandlerTestData.UpdateRequest([jogadorA.Id, jogadorB.Id], jogadorB.Id)), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Nome.Should().Be("Os Sem Baron Atualizado");
        response.Membros.Should().HaveCount(2);
        response.Capitao.Should().NotBeNull();
        response.Capitao!.Id.Should().Be(jogadorB.Id);
    }

    [Fact]
    public async Task Handle_ShouldClearCaptain_WhenCaptainIsRemoved()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var createHandler = new CreateTimeCommandHandler(repository, new CreateTimeValidator());
        var created = await createHandler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);
        var handler = new UpdateTimeCommandHandler(repository, new UpdateTimeValidator());

        var response = await handler.Handle(new UpdateTimeCommand(created.Id, TimeHandlerTestData.UpdateRequest([jogador.Id], null)), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Capitao.Should().BeNull();
        response.Membros.Should().ContainSingle(membro => !membro.Capitao);
    }
}

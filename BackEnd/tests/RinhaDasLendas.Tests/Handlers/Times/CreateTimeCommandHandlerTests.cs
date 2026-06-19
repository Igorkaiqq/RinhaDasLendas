using FluentAssertions;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Handlers.Times;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Handlers.Times;

public sealed class CreateTimeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateTeam_WhenRequestIsValid()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var handler = new CreateTimeCommandHandler(repository, new CreateTimeValidator());

        var response = await handler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);

        response.Nome.Should().Be("Os Sem Baron");
        response.Status.Should().Be("Ativo");
        response.Membros.Should().ContainSingle(membro => membro.JogadorId == jogador.Id && membro.Capitao);
    }

    [Fact]
    public async Task Handle_ShouldRejectDuplicateActiveNameOrTag()
    {
        var repository = new InMemoryTimeRepository();
        var jogador = TimeHandlerTestData.CreateJogador();
        repository.AddJogador(jogador);
        var handler = new CreateTimeCommandHandler(repository, new CreateTimeValidator());
        await handler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id)), CancellationToken.None);

        var act = () => handler.Handle(new CreateTimeCommand(TimeHandlerTestData.CreateRequest(jogador.Id, nome: "os sem baron", tag: "outro")), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Ja existe um time ativo com este nome.");
    }
}

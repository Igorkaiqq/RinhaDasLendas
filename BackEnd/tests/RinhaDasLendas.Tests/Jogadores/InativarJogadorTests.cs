using FluentAssertions;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Queries.Jogadores;

namespace RinhaDasLendas.Tests.Jogadores;

public sealed class InativarJogadorTests
{
    [Fact]
    public async Task Handle_ShouldInactivatePlayer_WhenPlayerExists()
    {
        var repository = new InMemoryJogadorRepository();
        var jogador = JogadorTestData.JogadorAtivo();
        await repository.AddAsync(jogador, CancellationToken.None);
        var handler = new InativarJogadorCommandHandler(repository);

        var result = await handler.Handle(new InativarJogadorCommand(jogador.Id), CancellationToken.None);

        result.Should().BeTrue();
        jogador.Status.ToString().Should().Be("Inativo");
    }

    [Fact]
    public async Task Query_ShouldRespectActiveFiltering()
    {
        var repository = new InMemoryJogadorRepository();
        var ativo = JogadorTestData.JogadorAtivo("Ativo");
        var inativo = JogadorTestData.JogadorAtivo("Inativo");
        inativo.Inativar();
        await repository.AddAsync(ativo, CancellationToken.None);
        await repository.AddAsync(inativo, CancellationToken.None);
        var handler = new GetJogadoresQueryHandler(repository);

        var response = await handler.Handle(new GetJogadoresQuery(true, 1, 20), CancellationToken.None);

        response.Items.Should().ContainSingle();
        response.Items.Single().NomeExibicao.Should().Be("Ativo");
    }
}

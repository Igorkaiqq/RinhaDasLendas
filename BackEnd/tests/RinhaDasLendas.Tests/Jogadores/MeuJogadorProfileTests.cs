using FluentAssertions;
using FluentValidation;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Auth;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Jogadores;

public sealed class MeuJogadorProfileTests
{
    [Fact]
    public async Task Complete_ShouldCreateLinkedPlayer_WhenRequestIsValid()
    {
        var repository = new InMemoryJogadorRepository();
        var handler = new CompleteMeuJogadorProfileCommandHandler(repository, new MeuJogadorProfileRequestDtoValidator());
        var userId = Guid.NewGuid();

        var response = await handler.Handle(new CompleteMeuJogadorProfileCommand(userId, Request()), CancellationToken.None);

        response.Should().NotBeNull();
        response!.NomeExibicao.Should().Be("Faker da Firma");

        var linked = await repository.GetByUsuarioIdAsync(userId, CancellationToken.None);
        linked.Should().NotBeNull();
        linked!.UsuarioId.Should().Be(userId);
    }

    [Fact]
    public async Task Complete_ShouldReturnNull_WhenUserAlreadyHasLinkedPlayer()
    {
        var repository = new InMemoryJogadorRepository();
        var handler = new CompleteMeuJogadorProfileCommandHandler(repository, new MeuJogadorProfileRequestDtoValidator());
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        await repository.AddAsync(jogador, CancellationToken.None);

        var response = await handler.Handle(new CompleteMeuJogadorProfileCommand(userId, Request()), CancellationToken.None);

        response.Should().BeNull();
    }

    [Fact]
    public async Task Validator_ShouldRequireSelfServiceMandatoryLinksAndRiotId()
    {
        var validator = new MeuJogadorProfileRequestDtoValidator();
        var request = Request() with { RiotId = "", OpGgUrl = "", DeepLolUrl = "" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Informe o Riot ID.");
        result.Errors.Should().Contain(error => error.ErrorMessage == "Informe o link de OP.GG.");
        result.Errors.Should().Contain(error => error.ErrorMessage == "Informe o link de Deeplol.");
    }

    [Fact]
    public async Task Update_ShouldChangeLinkedPlayerData_WhenPlayerExists()
    {
        var repository = new InMemoryJogadorRepository();
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        await repository.AddAsync(jogador, CancellationToken.None);
        var handler = new UpdateMeuJogadorProfileCommandHandler(repository, new MeuJogadorProfileRequestDtoValidator());

        var response = await handler.Handle(new UpdateMeuJogadorProfileCommand(userId, Request("Novo Nome")), CancellationToken.None);

        response.Should().NotBeNull();
        response!.NomeExibicao.Should().Be("Novo Nome");
    }

    private static MeuJogadorProfileRequestDto Request(string nome = "Faker da Firma")
    {
        return new MeuJogadorProfileRequestDto(
            nome,
            "joao#1234",
            "Faker#BR1",
            "https://www.op.gg/summoners/br/Faker-BR1",
            "https://www.deeplol.gg/summoner/br/Faker-BR1",
            "Ouro",
            "II",
            JogadorTestData.PreferenciasValidas());
    }
}

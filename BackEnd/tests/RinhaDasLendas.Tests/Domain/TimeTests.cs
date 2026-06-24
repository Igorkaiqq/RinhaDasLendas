using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class TimeTests
{
    [Fact]
    public void Deve_criar_time_ativo_com_composicao_valida()
    {
        var jogadorId = Guid.NewGuid();

        var time = new Time("Os Sem Baron", "OSB", null, [jogadorId], jogadorId);

        time.Nome.Should().Be("Os Sem Baron");
        time.Tag.Should().Be("OSB");
        time.Membros.Should().ContainSingle();
        time.CapitaoId.Should().Be(jogadorId);
    }

    [Fact]
    public void Deve_rejeitar_time_sem_nome()
    {
        var act = () => new Time(" ", "OSB", null, [Guid.NewGuid()], null);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.TeamNameRequired);
    }

    [Fact]
    public void Deve_rejeitar_mais_de_cinco_jogadores()
    {
        var jogadoresIds = Enumerable.Range(1, 6).Select(_ => Guid.NewGuid()).ToList();

        var act = () => new Time("Os Sem Baron", "OSB", null, jogadoresIds, null);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.TeamPlayerLimitReached);
    }

    [Fact]
    public void Deve_rejeitar_jogador_duplicado()
    {
        var jogadorId = Guid.NewGuid();

        var act = () => new Time("Os Sem Baron", "OSB", null, [jogadorId, jogadorId], null);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PlayerAlreadyInTeam);
    }

    [Fact]
    public void Deve_rejeitar_capitao_fora_da_composicao()
    {
        var act = () => new Time("Os Sem Baron", "OSB", null, [Guid.NewGuid()], Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.TeamCaptainMustBeMember);
    }
}

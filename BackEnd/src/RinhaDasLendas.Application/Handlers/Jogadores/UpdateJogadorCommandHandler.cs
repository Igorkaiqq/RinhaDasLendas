using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class UpdateJogadorCommandHandler(
    IJogadorRepository jogadorRepository,
    IValidator<JogadorUpdateRequestDto> validator) : IRequestHandler<UpdateJogadorCommand, JogadorResponseDto?>
{
    public async Task<JogadorResponseDto?> Handle(UpdateJogadorCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var parsedElo = JogadorEloMapper.ToEnums(command.Request);

        var jogador = await jogadorRepository.GetByIdAsync(command.JogadorId, cancellationToken);
        if (jogador is null)
        {
            return null;
        }

        jogador.AtualizarDadosBasicos(
            command.Request.NomeExibicao,
            command.Request.NomeReal,
            command.Request.Discord,
            command.Request.RiotId,
            command.Request.OpGgUrl,
            command.Request.DeepLolUrl,
            parsedElo.Elo,
            parsedElo.Divisao);

        await jogadorRepository.SaveChangesAsync(cancellationToken);
        return JogadorResponseDto.FromEntity(jogador);
    }
}

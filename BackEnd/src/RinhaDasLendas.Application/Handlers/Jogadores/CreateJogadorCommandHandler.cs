using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class CreateJogadorCommandHandler(
    IJogadorRepository jogadorRepository,
    IValidator<JogadorCreateRequestDto> validator) : IRequestHandler<CreateJogadorCommand, JogadorResponseDto>
{
    public async Task<JogadorResponseDto> Handle(CreateJogadorCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var parsedElo = JogadorEloMapper.ToEnums(command.Request);

        var jogador = new Jogador(
            command.Request.NomeExibicao,
            command.Request.NomeReal,
            command.Request.Discord,
            command.Request.RiotId,
            command.Request.OpGgUrl,
            command.Request.DeepLolUrl,
            parsedElo.Elo,
            parsedElo.Divisao,
            JogadorHandlerHelpers.ToPreferencias(command.Request.Preferencias));

        await jogadorRepository.AddAsync(jogador, cancellationToken);
        await jogadorRepository.SaveChangesAsync(cancellationToken);

        return JogadorResponseDto.FromEntity(jogador);
    }
}

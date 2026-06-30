using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Queries.Auth;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class UpdateMeuJogadorProfileCommandHandler(
    IJogadorRepository jogadorRepository,
    IValidator<MeuJogadorProfileRequestDto> validator) : IRequestHandler<UpdateMeuJogadorProfileCommand, JogadorResponseDto?>
{
    public async Task<JogadorResponseDto?> Handle(UpdateMeuJogadorProfileCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);

        var jogador = await jogadorRepository.GetByUsuarioIdAsync(command.UserId, cancellationToken);
        if (jogador is null)
        {
            return null;
        }

        var parsedElo = JogadorEloMapper.ToEnums(command.Request);
        jogador.AtualizarDadosBasicos(
            command.Request.NomeExibicao,
            nomeReal: null,
            command.Request.Discord,
            command.Request.RiotId,
            command.Request.OpGgUrl,
            command.Request.DeepLolUrl,
            parsedElo.Elo,
            parsedElo.Divisao);
        jogador.AtualizarPreferencias(JogadorHandlerHelpers.ToPreferencias(command.Request.Preferencias));

        await jogadorRepository.SaveChangesAsync(cancellationToken);
        return JogadorResponseDto.FromEntity(jogador);
    }
}

using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Queries.Auth;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class CompleteMeuJogadorProfileCommandHandler(
    IJogadorRepository jogadorRepository,
    IValidator<MeuJogadorProfileRequestDto> validator) : IRequestHandler<CompleteMeuJogadorProfileCommand, JogadorResponseDto?>
{
    public async Task<JogadorResponseDto?> Handle(CompleteMeuJogadorProfileCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);

        if (await jogadorRepository.GetByUsuarioIdAsync(command.UserId, cancellationToken) is not null)
        {
            return null;
        }

        var parsedElo = JogadorEloMapper.ToEnums(command.Request);
        var jogador = new Jogador(
            command.Request.NomeExibicao,
            nomeReal: null,
            command.Request.Discord,
            command.Request.RiotId,
            command.Request.OpGgUrl,
            command.Request.DeepLolUrl,
            parsedElo.Elo,
            parsedElo.Divisao,
            JogadorHandlerHelpers.ToPreferencias(command.Request.Preferencias));
        jogador.VincularUsuario(command.UserId);

        await jogadorRepository.AddAsync(jogador, cancellationToken);
        await jogadorRepository.SaveChangesAsync(cancellationToken);

        return JogadorResponseDto.FromEntity(jogador);
    }
}

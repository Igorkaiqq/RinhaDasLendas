using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Queries.Auth;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class GetMeuJogadorProfileQueryHandler(IJogadorRepository jogadorRepository) : IRequestHandler<GetMeuJogadorProfileQuery, JogadorResponseDto?>
{
    public async Task<JogadorResponseDto?> Handle(GetMeuJogadorProfileQuery request, CancellationToken cancellationToken)
    {
        var jogador = await jogadorRepository.GetByUsuarioIdAsync(request.UserId, cancellationToken);
        return jogador is null ? null : JogadorResponseDto.FromEntity(jogador);
    }
}

using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record CompleteMeuJogadorProfileCommand(Guid UserId, MeuJogadorProfileRequestDto Request) : IRequest<JogadorResponseDto?>;

public sealed record UpdateMeuJogadorProfileCommand(Guid UserId, MeuJogadorProfileRequestDto Request) : IRequest<JogadorResponseDto?>;

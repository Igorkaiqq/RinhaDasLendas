using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record LoginCommand(LoginRequestDto Request, string? IpAddress, string? UserAgent) : IRequest<AuthResponseDto>;

using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record RefreshSessionCommand(string RefreshToken, string? IpAddress, string? UserAgent) : IRequest<AuthResponseDto>;

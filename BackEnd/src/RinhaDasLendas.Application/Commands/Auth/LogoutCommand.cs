using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record LogoutCommand(string? RefreshToken, Guid? UserId, string? IpAddress, string? UserAgent) : IRequest;

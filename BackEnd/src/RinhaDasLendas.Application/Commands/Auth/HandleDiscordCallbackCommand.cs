using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record HandleDiscordCallbackCommand(string? Code, string? State, string? IpAddress, string? UserAgent) : IRequest<DiscordCallbackResultDto>;

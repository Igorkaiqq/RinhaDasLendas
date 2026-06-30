using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class RefreshSessionCommandHandler(IAuthService authService) : IRequestHandler<RefreshSessionCommand, AuthResponseDto>
{
    public Task<AuthResponseDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        => authService.RefreshAsync(request.RefreshToken, request.IpAddress, request.UserAgent, cancellationToken);
}

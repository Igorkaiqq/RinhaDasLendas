using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class RegisterUserCommandHandler(IAuthService authService) : IRequestHandler<RegisterUserCommand, AuthenticatedUserDto>
{
    public Task<AuthenticatedUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request.Request, cancellationToken);
}

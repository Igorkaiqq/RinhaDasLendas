using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class UpdateOwnProfileCommandHandler(IAuthService authService) : IRequestHandler<UpdateOwnProfileCommand, AuthenticatedUserDto?>
{
    public Task<AuthenticatedUserDto?> Handle(UpdateOwnProfileCommand request, CancellationToken cancellationToken)
        => authService.UpdateOwnProfileAsync(request.UserId, request.Request, cancellationToken);
}

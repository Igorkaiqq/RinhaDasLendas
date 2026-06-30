using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequestDto Request) : IRequest;

using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record ForgotPasswordCommand(ForgotPasswordRequestDto Request) : IRequest<ForgotPasswordResponseDto>;

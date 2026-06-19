using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Times;

public sealed record ReativarTimeCommand(Guid TimeId) : IRequest<TimeResponseDto?>;

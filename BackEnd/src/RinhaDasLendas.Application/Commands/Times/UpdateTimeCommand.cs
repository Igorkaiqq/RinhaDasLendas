using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Times;

public sealed record UpdateTimeCommand(Guid TimeId, UpdateTimeRequestDto Request) : IRequest<TimeResponseDto?>;

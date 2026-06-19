using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Times;

public sealed record CreateTimeCommand(CreateTimeRequestDto Request) : IRequest<TimeResponseDto>;

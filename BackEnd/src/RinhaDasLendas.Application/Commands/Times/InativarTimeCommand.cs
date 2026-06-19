using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Times;

public sealed record InativarTimeCommand(Guid TimeId) : IRequest<TimeResponseDto?>;

using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Times;

public sealed record GetTimeByIdQuery(Guid TimeId) : IRequest<TimeResponseDto?>;

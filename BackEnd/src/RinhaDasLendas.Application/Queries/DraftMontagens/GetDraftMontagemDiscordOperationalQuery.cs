using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetDraftMontagemDiscordOperationalQuery(Guid Id) : IRequest<DraftMontagemDiscordOperationalDto?>;

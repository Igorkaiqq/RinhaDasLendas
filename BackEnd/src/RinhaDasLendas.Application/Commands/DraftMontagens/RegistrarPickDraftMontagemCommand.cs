using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RegistrarPickDraftMontagemCommand(Guid Id, RegistrarPickDraftMontagemRequestDto Request) : IRequest<DraftMontagemRealtimeStateDto?>;

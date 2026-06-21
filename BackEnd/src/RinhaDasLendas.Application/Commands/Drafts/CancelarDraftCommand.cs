using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Drafts;

public sealed record CancelarDraftCommand(Guid DraftId, CancelarDraftRequestDto Request) : IRequest<DraftResponseDto?>;

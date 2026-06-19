using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Drafts;

public sealed record RegistrarPickDraftCommand(Guid DraftId, RegistrarPickDraftRequestDto Request) : IRequest<DraftResponseDto?>;

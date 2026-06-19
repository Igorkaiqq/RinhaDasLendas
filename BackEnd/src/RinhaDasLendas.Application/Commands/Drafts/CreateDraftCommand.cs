using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Drafts;

public sealed record CreateDraftCommand(CreateDraftRequestDto Request) : IRequest<DraftResponseDto>;

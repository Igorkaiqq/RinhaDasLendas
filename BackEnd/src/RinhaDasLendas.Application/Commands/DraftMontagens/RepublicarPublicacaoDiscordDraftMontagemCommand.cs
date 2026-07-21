using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RepublicarPublicacaoDiscordDraftMontagemCommand(Guid Id, RepublicarPublicacaoDiscordDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

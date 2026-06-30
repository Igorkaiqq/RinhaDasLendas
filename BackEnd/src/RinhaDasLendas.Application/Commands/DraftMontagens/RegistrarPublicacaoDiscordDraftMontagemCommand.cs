using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RegistrarPublicacaoDiscordDraftMontagemCommand(Guid Id, RegistrarPublicacaoDiscordDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

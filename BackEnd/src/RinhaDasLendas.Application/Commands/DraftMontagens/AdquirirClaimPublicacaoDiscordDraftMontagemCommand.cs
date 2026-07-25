using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record AdquirirClaimPublicacaoDiscordDraftMontagemCommand(
    Guid Id,
    AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto Request) : IRequest<ClaimPublicacaoDiscordResponseDto?>;

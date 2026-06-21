using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Auth;

public sealed record GetMeuJogadorProfileQuery(Guid UserId) : IRequest<JogadorResponseDto?>;

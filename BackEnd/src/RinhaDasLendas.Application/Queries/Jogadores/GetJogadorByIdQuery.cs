using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Jogadores;

public sealed record GetJogadorByIdQuery(Guid JogadorId) : IRequest<JogadorResponseDto?>;

using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Jogadores;

public sealed record CreateJogadorCommand(JogadorCreateRequestDto Request) : IRequest<JogadorResponseDto>;

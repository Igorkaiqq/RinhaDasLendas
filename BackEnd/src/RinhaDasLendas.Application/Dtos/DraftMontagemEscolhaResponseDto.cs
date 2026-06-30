using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemEscolhaResponseDto(
    int Sequencia,
    Guid TimeId,
    Guid CapitaoId,
    Guid? JogadorId,
    string Tipo,
    string? JogadorNome,
    DateTimeOffset RegistradoEm)
{
    public static DraftMontagemEscolhaResponseDto FromEntity(DraftMontagemEscolha escolha)
    {
        return new DraftMontagemEscolhaResponseDto(
            escolha.Sequencia,
            escolha.TimeId,
            escolha.CapitaoId,
            escolha.JogadorId,
            escolha.Tipo.ToString(),
            escolha.Jogador?.NomeExibicao,
            escolha.RegistradoEm);
    }
}

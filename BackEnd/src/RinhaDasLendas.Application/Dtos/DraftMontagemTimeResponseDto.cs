using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemTimeResponseDto(
    Guid Id,
    string Nome,
    int Ordem,
    string Cor,
    Guid? CapitaoId,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Jogadores)
{
    public static DraftMontagemTimeResponseDto FromEntity(DraftMontagemTime time, IReadOnlyCollection<DraftMontagemParticipante> participantes)
    {
        return new DraftMontagemTimeResponseDto(
            time.Id,
            time.Nome,
            time.Ordem,
            time.Cor,
            time.CapitaoId,
            participantes.Where(participante => participante.TimeId == time.Id).OrderByDescending(participante => participante.Capitao).ThenBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList());
    }
}

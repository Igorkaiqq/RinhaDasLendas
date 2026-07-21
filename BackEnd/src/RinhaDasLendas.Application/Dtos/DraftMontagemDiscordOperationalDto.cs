using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemDiscordOperationalDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordPresenceMessageId,
    IReadOnlyCollection<DraftMontagemDiscordOperationalPublicacaoDto> PublicacoesDiscord,
    IReadOnlyCollection<DraftMontagemDiscordOperationalPresencaDto> Presencas,
    IReadOnlyCollection<DraftMontagemDiscordOperationalTimeDto> Times,
    IReadOnlyCollection<DraftMontagemDiscordOperationalParticipanteDto> Reservas)
{
    public static DraftMontagemDiscordOperationalDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        return new DraftMontagemDiscordOperationalDto(
            montagem.Id,
            montagem.Nome,
            montagem.Observacoes,
            montagem.Status.ToString(),
            montagem.TamanhoEquipe,
            montagem.QuantidadeTimes,
            montagem.QuantidadeReservas,
            montagem.HorarioEncerramentoPresenca,
            montagem.DiscordPresenceMessageId,
            montagem.PublicacoesDiscord.OrderBy(publicacao => publicacao.Tipo).Select(publicacao => new DraftMontagemDiscordOperationalPublicacaoDto(publicacao.Tipo.ToString(), publicacao.Status.ToString())).ToList(),
            montagem.Presencas.OrderBy(presenca => presenca.OrdemFinal ?? presenca.OrdemManual ?? presenca.OrdemConfirmacao).Select(presenca => new DraftMontagemDiscordOperationalPresencaDto(presenca.Id, presenca.Jogador?.NomeExibicao ?? string.Empty, presenca.Status.ToString(), presenca.OrigemConfirmacao.ToString())).ToList(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => new DraftMontagemDiscordOperationalTimeDto(
                time.Id,
                time.Nome,
                time.Cor,
                time.CapitaoId,
                participantes.Where(participante => participante.TimeId == time.Id).OrderBy(participante => participante.Ordem).Select(participante => new DraftMontagemDiscordOperationalParticipanteDto(participante.Jogador?.NomeExibicao ?? string.Empty, participante.Capitao)).ToList())).ToList(),
            participantes.Where(participante => participante.Estado == Domain.Enums.DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(participante => new DraftMontagemDiscordOperationalParticipanteDto(participante.Jogador?.NomeExibicao ?? string.Empty, participante.Capitao)).ToList());
    }
}

public sealed record DraftMontagemDiscordOperationalPublicacaoDto(string Tipo, string Status);
public sealed record DraftMontagemDiscordOperationalPresencaDto(Guid Id, string NomeExibicao, string Status, string OrigemConfirmacao);
public sealed record DraftMontagemDiscordOperationalTimeDto(Guid Id, string Nome, string Cor, Guid? CapitaoId, IReadOnlyCollection<DraftMontagemDiscordOperationalParticipanteDto> Jogadores);
public sealed record DraftMontagemDiscordOperationalParticipanteDto(string NomeExibicao, bool Capitao);

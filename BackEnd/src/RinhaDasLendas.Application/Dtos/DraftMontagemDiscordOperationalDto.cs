using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemDiscordOperationalDto(
    Guid Id,
    string Nome,
    string Status,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordPresenceMessageId,
    IReadOnlyCollection<DraftMontagemDiscordOperationalPublicacaoDto> PublicacoesDiscord,
    IReadOnlyCollection<DraftMontagemDiscordOperationalPresencaDto> Presencas,
    IReadOnlyCollection<DraftMontagemDiscordOperationalTimeDto> Times,
    IReadOnlyCollection<DraftMontagemDiscordOperationalReservaDto> Reservas,
    bool Arquivado,
    long VersaoEstado)
{
    public static DraftMontagemDiscordOperationalDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        return new DraftMontagemDiscordOperationalDto(
            montagem.Id,
            montagem.Nome,
            montagem.Status.ToString(),
            montagem.HorarioEncerramentoPresenca,
            montagem.DiscordPresenceMessageId,
            montagem.PublicacoesDiscord
                .Where(publicacao => !montagem.Arquivado || publicacao.Tipo == Domain.Enums.DraftMontagemPublicacaoDiscordTipo.Cancelamento)
                .OrderBy(publicacao => publicacao.Tipo)
                .Select(publicacao => new DraftMontagemDiscordOperationalPublicacaoDto(publicacao.Tipo.ToString(), publicacao.Status.ToString()))
                .ToList(),
            montagem.Presencas.OrderBy(presenca => presenca.OrdemFinal ?? presenca.OrdemManual ?? presenca.OrdemConfirmacao).Select(presenca => new DraftMontagemDiscordOperationalPresencaDto(presenca.Jogador?.NomeExibicao ?? string.Empty, presenca.Status.ToString())).ToList(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => new DraftMontagemDiscordOperationalTimeDto(
                time.Nome,
                participantes.Where(participante => participante.TimeId == time.Id).OrderBy(participante => participante.Ordem).Select(participante => new DraftMontagemDiscordOperationalParticipanteDto(participante.Jogador?.NomeExibicao ?? string.Empty, participante.Capitao)).ToList())).ToList(),
            participantes.Where(participante => participante.Estado == Domain.Enums.DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(participante => new DraftMontagemDiscordOperationalReservaDto(participante.Jogador?.NomeExibicao ?? string.Empty)).ToList(),
            montagem.Arquivado,
            montagem.VersaoEstado);
    }
}

public sealed record DraftMontagemDiscordOperationalPublicacaoDto(string Tipo, string Status);
public sealed record DraftMontagemDiscordOperationalPresencaDto(string NomeExibicao, string Status);
public sealed record DraftMontagemDiscordOperationalTimeDto(string Nome, IReadOnlyCollection<DraftMontagemDiscordOperationalParticipanteDto> Jogadores);
public sealed record DraftMontagemDiscordOperationalParticipanteDto(string NomeExibicao, bool Capitao);
public sealed record DraftMontagemDiscordOperationalReservaDto(string NomeExibicao);

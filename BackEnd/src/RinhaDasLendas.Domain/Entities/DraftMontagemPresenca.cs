using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemPresenca
{
    private DraftMontagemPresenca()
    {
    }

    public DraftMontagemPresenca(Guid usuarioId, Guid jogadorId, string? discordUserId, DraftMontagemPresencaOrigem origem, int ordemConfirmacao)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        JogadorId = jogadorId;
        DiscordUserId = string.IsNullOrWhiteSpace(discordUserId) ? null : discordUserId.Trim();
        OrigemConfirmacao = origem;
        Status = DraftMontagemPresencaStatus.Confirmada;
        ConfirmadoEm = DateTimeOffset.UtcNow;
        OrdemConfirmacao = ordemConfirmacao;
        DataCadastro = ConfirmadoEm;
        DataAtualizacao = ConfirmadoEm;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid JogadorId { get; private set; }
    public string? DiscordUserId { get; private set; }
    public DraftMontagemPresencaOrigem OrigemConfirmacao { get; private set; }
    public DraftMontagemPresencaStatus Status { get; private set; }
    public DateTimeOffset ConfirmadoEm { get; private set; }
    public DateTimeOffset? CanceladoEm { get; private set; }
    public int OrdemConfirmacao { get; private set; }
    public int? OrdemManual { get; private set; }
    public int? OrdemFinal { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public Jogador? Jogador { get; private set; }

    public bool Confirmada => Status == DraftMontagemPresencaStatus.Confirmada;

    public void Cancelar()
    {
        if (Status == DraftMontagemPresencaStatus.Cancelada)
        {
            return;
        }

        Status = DraftMontagemPresencaStatus.Cancelada;
        CanceladoEm = DateTimeOffset.UtcNow;
        Touch();
    }

    public void DefinirOrdemManual(int ordem)
    {
        OrdemManual = ordem;
        Touch();
    }

    public void DefinirOrdemFinal(int ordem)
    {
        OrdemFinal = ordem;
        Touch();
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}

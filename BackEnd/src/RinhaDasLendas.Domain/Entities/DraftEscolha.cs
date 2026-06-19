using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftEscolha
{
    private DraftEscolha()
    {
    }

    public DraftEscolha(int sequencia, DraftTime time, Guid capitaoId, Guid jogadorId)
    {
        Id = Guid.NewGuid();
        Sequencia = sequencia;
        Time = time;
        CapitaoId = capitaoId;
        JogadorId = jogadorId;
        DataEscolha = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DraftSessaoId { get; private set; }
    public int Sequencia { get; private set; }
    public DraftTime Time { get; private set; }
    public Guid CapitaoId { get; private set; }
    public Guid JogadorId { get; private set; }
    public DateTimeOffset DataEscolha { get; private set; }
    public Jogador? Capitao { get; private set; }
    public Jogador? Jogador { get; private set; }
}

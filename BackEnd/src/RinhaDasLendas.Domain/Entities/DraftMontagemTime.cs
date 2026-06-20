namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemTime
{
    private DraftMontagemTime()
    {
    }

    public DraftMontagemTime(string nome, int ordem, string cor)
    {
        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Ordem = ordem;
        Cor = cor.Trim();
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Ordem { get; private set; }
    public string Cor { get; private set; } = string.Empty;
    public Guid? CapitaoId { get; private set; }

    public void Atualizar(string nome, Guid? capitaoId)
    {
        Nome = string.IsNullOrWhiteSpace(nome) ? Nome : nome.Trim();
        CapitaoId = capitaoId;
    }
}

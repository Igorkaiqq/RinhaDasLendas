using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class PreferenciaRota
{
    private PreferenciaRota()
    {
    }

    public PreferenciaRota(Rota rota, int prioridade, bool naoJogoNemLascando)
    {
        if (prioridade is < 1 or > 5)
        {
            throw new DomainException("A prioridade da rota deve estar entre 1 e 5.");
        }

        Id = Guid.NewGuid();
        Rota = rota;
        Prioridade = prioridade;
        NaoJogoNemLascando = naoJogoNemLascando;
    }

    public Guid Id { get; private set; }
    public Guid JogadorId { get; private set; }
    public Rota Rota { get; private set; }
    public int Prioridade { get; private set; }
    public bool NaoJogoNemLascando { get; private set; }

    public void Atualizar(int prioridade, bool naoJogoNemLascando)
    {
        if (prioridade is < 1 or > 5)
        {
            throw new DomainException("A prioridade da rota deve estar entre 1 e 5.");
        }

        Prioridade = prioridade;
        NaoJogoNemLascando = naoJogoNemLascando;
    }
}

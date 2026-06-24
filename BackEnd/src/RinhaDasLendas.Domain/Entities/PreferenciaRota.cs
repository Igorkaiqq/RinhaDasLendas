using RinhaDasLendas.Domain.Constants;
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
            throw new DomainException(MessageCodes.RoutePrioritiesRange);
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
            throw new DomainException(MessageCodes.RoutePrioritiesRange);
        }

        Prioridade = prioridade;
        NaoJogoNemLascando = naoJogoNemLascando;
    }
}

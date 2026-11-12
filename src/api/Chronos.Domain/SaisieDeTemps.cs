namespace Chronos.Domain;

public class SaisieDeTemps
{
    private static readonly decimal[] DureesPermises = [0.25m, 0.5m, 0.75m, 1m];

    internal SaisieDeTemps(int projetId, DateOnly jour, decimal duree)
    {
        if (!DureesPermises.Contains(duree))
        {
            throw new RegleMetierException("RG-30", "Le temps se saisit par quarts de journée : 0,25 ; 0,5 ; 0,75 ou 1.");
        }

        ProjetId = projetId;
        Jour = jour;
        Duree = duree;
    }

    public int Id { get; private set; }
    public int FeuilleDeTempsId { get; private set; }
    public int ProjetId { get; private set; }
    public DateOnly Jour { get; private set; }
    public decimal Duree { get; private set; }
}

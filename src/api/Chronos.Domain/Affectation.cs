namespace Chronos.Domain;

public class Affectation : ITracable
{
    private Affectation(int collaborateurId, int projetId, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        CollaborateurId = collaborateurId;
        ProjetId = projetId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        TauxAllocation = tauxAllocation;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public int ProjetId { get; private set; }
    public Projet? Projet { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly DateFin { get; private set; }
    public decimal TauxAllocation { get; private set; }

    public DateTimeOffset CreeLe { get; private set; }
    public string CreePar { get; private set; } = string.Empty;
    public DateTimeOffset? ModifieLe { get; private set; }
    public string? ModifiePar { get; private set; }

    public static Affectation Creer(
        Collaborateur collaborateur, Projet projet, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        Verifier(collaborateur, projet, dateDebut, dateFin, tauxAllocation);

        return new Affectation(collaborateur.Id, projet.Id, dateDebut, dateFin, tauxAllocation);
    }

    /// <summary>Change la période et le taux ; le collaborateur et le projet restent ceux de l'affectation.</summary>
    public void Modifier(
        Collaborateur collaborateur, Projet projet, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        if (collaborateur.Id != CollaborateurId || projet.Id != ProjetId)
        {
            throw new ArgumentException("Le collaborateur et le projet doivent être ceux de l'affectation.");
        }

        Verifier(collaborateur, projet, dateDebut, dateFin, tauxAllocation);

        DateDebut = dateDebut;
        DateFin = dateFin;
        TauxAllocation = tauxAllocation;
    }

    private static void Verifier(
        Collaborateur collaborateur, Projet projet, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        projet.VerifierOuvert();

        if (tauxAllocation is < 0.05m or > 1m)
        {
            throw new RegleMetierException("RG-21", "Le taux d'allocation est compris entre 5 % et 100 %.");
        }

        if (dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-20", "La fin d'une affectation ne précède pas son début.");
        }

        if (dateDebut < projet.DateDebut || dateFin > (projet.DateFin ?? DateOnly.MaxValue))
        {
            throw new RegleMetierException("RG-20", "L'affectation doit être comprise dans la période du projet.");
        }

        if (dateDebut < collaborateur.DateEntree || dateFin > (collaborateur.DateSortie ?? DateOnly.MaxValue))
        {
            throw new RegleMetierException("RG-20",
                "L'affectation doit être comprise dans la période de présence du collaborateur.");
        }
    }
}

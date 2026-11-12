namespace Chronos.Domain;

public class Projet : ITracable
{
    public Projet(
        string code,
        string libelle,
        NatureProjet nature,
        int? clientId,
        DateOnly dateDebut,
        DateOnly? dateFin,
        int budgetJours,
        int responsableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(libelle);
        ArgumentOutOfRangeException.ThrowIfNegative(budgetJours);
        VerifierClient(nature, clientId);
        VerifierDates(dateDebut, dateFin);

        Code = code.Trim().ToUpperInvariant();
        Libelle = libelle.Trim();
        Nature = nature;
        ClientId = clientId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        BudgetJours = budgetJours;
        ResponsableId = responsableId;
        Statut = StatutProjet.EnPreparation;
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public string Libelle { get; private set; }
    public NatureProjet Nature { get; private set; }
    public int? ClientId { get; private set; }
    public Client? Client { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly? DateFin { get; private set; }
    public int BudgetJours { get; private set; }
    public int ResponsableId { get; private set; }
    public Collaborateur? Responsable { get; private set; }
    public StatutProjet Statut { get; private set; }

    /// <summary>Jeton de concurrence optimiste : renouvelé à chaque modification du projet.</summary>
    public Guid Version { get; private set; } = Guid.NewGuid();

    public DateTimeOffset CreeLe { get; private set; }
    public string CreePar { get; private set; } = string.Empty;
    public DateTimeOffset? ModifieLe { get; private set; }
    public string? ModifiePar { get; private set; }

    public bool EstClos => Statut is StatutProjet.Termine or StatutProjet.Annule;

    public bool PeutDemarrer => Statut == StatutProjet.EnPreparation;

    public bool PeutTerminer => Statut == StatutProjet.EnCours;

    public bool PeutAnnuler => !EstClos;

    public void Modifier(
        string libelle,
        NatureProjet nature,
        int? clientId,
        DateOnly dateDebut,
        DateOnly? dateFin,
        int budgetJours,
        int responsableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libelle);
        ArgumentOutOfRangeException.ThrowIfNegative(budgetJours);
        VerifierClient(nature, clientId);
        VerifierDates(dateDebut, dateFin);

        Libelle = libelle.Trim();
        Nature = nature;
        ClientId = clientId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        BudgetJours = budgetJours;
        ResponsableId = responsableId;
        Version = Guid.NewGuid();
    }

    public void Demarrer()
    {
        if (!PeutDemarrer)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être démarré.");
        }

        Statut = StatutProjet.EnCours;
        Version = Guid.NewGuid();
    }

    public void Terminer()
    {
        if (!PeutTerminer)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être terminé.");
        }

        Statut = StatutProjet.Termine;
        Version = Guid.NewGuid();
    }

    public void Annuler()
    {
        if (!PeutAnnuler)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être annulé.");
        }

        Statut = StatutProjet.Annule;
        Version = Guid.NewGuid();
    }

    public void VerifierOuvert()
    {
        if (EstClos)
        {
            var etat = Statut == StatutProjet.Termine ? "terminé" : "annulé";
            throw new RegleMetierException("RG-13",
                $"Le projet {Code} est {etat} : il n'accepte plus ni affectation ni saisie de temps.");
        }
    }

    private static void VerifierClient(NatureProjet nature, int? clientId)
    {
        if (nature == NatureProjet.Facturable && clientId is null)
        {
            throw new RegleMetierException("RG-11", "Un projet facturable a obligatoirement un client.");
        }

        if (nature == NatureProjet.Interne && clientId is not null)
        {
            throw new RegleMetierException("RG-11", "Un projet interne n'a pas de client.");
        }
    }

    private static void VerifierDates(DateOnly dateDebut, DateOnly? dateFin)
    {
        if (dateFin is not null && dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-12",
                "La date de fin d'un projet est postérieure ou égale à sa date de début.");
        }
    }
}

namespace Chronos.Domain;

public class Projet
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

    public bool EstClos => Statut is StatutProjet.Termine or StatutProjet.Annule;

    public void Demarrer()
    {
        if (Statut != StatutProjet.EnPreparation)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être démarré.");
        }

        Statut = StatutProjet.EnCours;
    }

    public void Terminer()
    {
        if (Statut != StatutProjet.EnCours)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être terminé.");
        }

        Statut = StatutProjet.Termine;
    }

    public void Annuler()
    {
        if (EstClos)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être annulé.");
        }

        Statut = StatutProjet.Annule;
    }

    public void VerifierOuvert()
    {
        if (EstClos)
        {
            throw new RegleMetierException("RG-13",
                $"Le projet {Code} est {Statut} : il n'accepte plus ni affectation ni saisie de temps.");
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

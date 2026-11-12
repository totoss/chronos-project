namespace Chronos.Domain;

public class Absence
{
    public Absence(int collaborateurId, int typeAbsenceId, DateOnly dateDebut, DateOnly dateFin,
        bool debuteApresMidi, bool finitLeMatin)
    {
        if (dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-42", "La fin d'une absence est postérieure ou égale à son début.");
        }
        if (dateDebut == dateFin && debuteApresMidi && finitLeMatin)
        {
            throw new RegleMetierException("RG-42", "Une absence dure au moins une demi-journée.");
        }

        CollaborateurId = collaborateurId;
        TypeAbsenceId = typeAbsenceId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        DebuteApresMidi = debuteApresMidi;
        FinitLeMatin = finitLeMatin;
        Statut = StatutAbsence.Demandee;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public int TypeAbsenceId { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly DateFin { get; private set; }
    public bool DebuteApresMidi { get; private set; }
    public bool FinitLeMatin { get; private set; }
    public StatutAbsence Statut { get; private set; }

    /// <summary>Jeton de concurrence optimiste : renouvelé à chaque changement d'état de l'absence.</summary>
    public Guid Version { get; private set; } = Guid.NewGuid();

    public Periode Periode => new(DateDebut, DateFin);

    public decimal PartAbsente(DateOnly jour)
    {
        if (!Periode.Contient(jour))
        {
            return 0m;
        }

        var part = 1m;
        if (jour == DateDebut && DebuteApresMidi)
        {
            part -= 0.5m;
        }
        if (jour == DateFin && FinitLeMatin)
        {
            part -= 0.5m;
        }
        return part;
    }

    public decimal DureeEnJours(CalendrierDeTravail calendrier) =>
        calendrier.JoursOuvres(Periode).Sum(PartAbsente);

    /// <summary>RG-42 : une absence qui ne couvre aucun jour ouvré n'a pas de durée, elle est refusée.</summary>
    public void VerifierDuree(CalendrierDeTravail calendrier)
    {
        if (DureeEnJours(calendrier) == 0m)
        {
            throw new RegleMetierException("RG-42", "Cette absence ne couvre aucun jour ouvré.");
        }
    }

    /// <summary>
    /// RG-41 : vrai si les deux absences ont une demi-journée en commun. Une absence qui finit le matin
    /// et une autre qui commence l'après-midi du même jour ne se chevauchent pas.
    /// </summary>
    public bool Chevauche(Absence autre) =>
        PremiereDemiJournee <= autre.DerniereDemiJournee && autre.PremiereDemiJournee <= DerniereDemiJournee;

    /// <summary>
    /// RG-43 : jour par jour, le temps validé et les parts d'absence approuvée (toutes les absences du jour,
    /// voir <see cref="PartsAbsentes"/>) ne dépassent pas, ensemble, une journée. Une demi-journée d'absence
    /// reste donc compatible avec une demi-journée de temps validé.
    /// </summary>
    public static void VerifierTempsValide(
        IReadOnlyDictionary<DateOnly, decimal> tempsValideParJour, IReadOnlyDictionary<DateOnly, decimal> partsAbsentes)
    {
        foreach (var (jour, temps) in tempsValideParJour)
        {
            if (temps + partsAbsentes.GetValueOrDefault(jour) > 1m)
            {
                throw new RegleMetierException("RG-43",
                    $"Le {jour:dd/MM/yyyy} porte du temps validé qui ne laisse pas de place à cette absence.");
            }
        }
    }

    public void Approuver()
    {
        Exiger(StatutAbsence.Demandee, "Seule une absence demandée peut être approuvée.");
        Statut = StatutAbsence.Approuvee;
        Version = Guid.NewGuid();
    }

    public void Refuser()
    {
        Exiger(StatutAbsence.Demandee, "Seule une absence demandée peut être refusée.");
        Statut = StatutAbsence.Refusee;
        Version = Guid.NewGuid();
    }

    public void Annuler(DateOnly aujourdhui)
    {
        if (Statut is not (StatutAbsence.Demandee or StatutAbsence.Approuvee))
        {
            throw new RegleMetierException("RG-40", "Seule une absence demandée ou approuvée peut être annulée.");
        }
        if (DateDebut <= aujourdhui)
        {
            throw new RegleMetierException("RG-40", "Une absence ne peut plus être annulée une fois commencée.");
        }

        Statut = StatutAbsence.Annulee;
        Version = Guid.NewGuid();
    }

    /// <summary>
    /// Les parts d'absence, jour par jour, des absences données sur une période : la forme que le calcul de
    /// charge et la feuille de temps attendent. Plusieurs absences le même jour s'additionnent, jusqu'à 1.
    /// </summary>
    public static IReadOnlyDictionary<DateOnly, decimal> PartsAbsentes(IEnumerable<Absence> absences, Periode periode)
    {
        var parts = new Dictionary<DateOnly, decimal>();

        foreach (var absence in absences)
        {
            if (absence.Periode.Intersection(periode) is not { } commun)
            {
                continue;
            }

            for (var jour = commun.Debut; jour <= commun.Fin; jour = jour.AddDays(1))
            {
                parts[jour] = Math.Min(1m, parts.GetValueOrDefault(jour) + absence.PartAbsente(jour));
            }
        }

        return parts;
    }

    // Deux demi-journées par jour : 2 × le numéro du jour pour le matin, +1 pour l'après-midi.
    private int PremiereDemiJournee => DateDebut.DayNumber * 2 + (DebuteApresMidi ? 1 : 0);

    private int DerniereDemiJournee => DateFin.DayNumber * 2 + (FinitLeMatin ? 0 : 1);

    private void Exiger(StatutAbsence attendu, string message)
    {
        if (Statut != attendu)
        {
            throw new RegleMetierException("RG-40", message);
        }
    }
}

namespace Chronos.Domain;

public sealed record SaisieDemandee(int ProjetId, DateOnly Jour, decimal Duree);

public sealed record ContexteDeSaisie(
    CalendrierDeTravail Calendrier,
    IReadOnlyDictionary<DateOnly, decimal> Absences,
    IReadOnlyList<AffectationPlanifiee> Affectations);

public class FeuilleDeTemps
{
    private readonly List<SaisieDeTemps> _saisies = [];

    public FeuilleDeTemps(int collaborateurId, DateOnly semaine)
    {
        if (semaine.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException("Une feuille de temps commence un lundi.", nameof(semaine));
        }

        CollaborateurId = collaborateurId;
        Semaine = semaine;
        Statut = StatutFeuilleDeTemps.Brouillon;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public DateOnly Semaine { get; private set; }
    public StatutFeuilleDeTemps Statut { get; private set; }
    public string? MotifRejet { get; private set; }

    /// <summary>Jeton de concurrence optimiste : renouvelé à chaque modification de la feuille.</summary>
    public Guid Version { get; private set; } = Guid.NewGuid();

    public IReadOnlyCollection<SaisieDeTemps> Saisies => _saisies;

    public Periode Periode => new(Semaine, Semaine.AddDays(6));

    public void Soumettre()
    {
        Exiger(StatutFeuilleDeTemps.Brouillon, "Seule une feuille à l'état de brouillon peut être soumise.");
        Statut = StatutFeuilleDeTemps.Soumise;
        MotifRejet = null;
        Version = Guid.NewGuid();
    }

    public void Valider(int valideurId)
    {
        Exiger(StatutFeuilleDeTemps.Soumise, "Seule une feuille soumise peut être validée.");
        InterdireAuCollaborateur(valideurId);
        Statut = StatutFeuilleDeTemps.Validee;
        Version = Guid.NewGuid();
    }

    public void Rejeter(int valideurId, string motif)
    {
        Exiger(StatutFeuilleDeTemps.Soumise, "Seule une feuille soumise peut être rejetée.");
        InterdireAuCollaborateur(valideurId);
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("RG-34", "Le rejet d'une feuille de temps exige un motif.");
        }

        Statut = StatutFeuilleDeTemps.Brouillon;
        MotifRejet = motif.Trim();
        Version = Guid.NewGuid();
    }

    public void RemplacerSaisies(IReadOnlyCollection<SaisieDemandee> demandes, ContexteDeSaisie contexte)
    {
        if (Statut != StatutFeuilleDeTemps.Brouillon)
        {
            throw new RegleMetierException("RG-35", "Une feuille soumise ou validée n'est plus modifiable.");
        }

        var saisies = demandes.Select(d => new SaisieDeTemps(d.ProjetId, d.Jour, d.Duree)).ToList();

        foreach (var jour in saisies.GroupBy(s => s.Jour))
        {
            if (!Periode.Contient(jour.Key))
            {
                throw new ArgumentException($"Le {jour.Key:dd/MM/yyyy} n'appartient pas à la semaine de la feuille.", nameof(demandes));
            }

            var total = jour.Sum(s => s.Duree);
            if (total > 1m)
            {
                throw new RegleMetierException("RG-31", $"Le total des saisies du {jour.Key:dd/MM/yyyy} dépasse une journée.");
            }

            var disponible = contexte.Calendrier.EstOuvre(jour.Key)
                ? 1m - contexte.Absences.GetValueOrDefault(jour.Key)
                : 0m;
            if (total > disponible)
            {
                throw new RegleMetierException("RG-32",
                    $"Le {jour.Key:dd/MM/yyyy} n'est pas travaillé, ou pas entièrement : jour non ouvré ou absence approuvée.");
            }

            foreach (var saisie in jour)
            {
                var affecte = contexte.Affectations.Any(a => a.ProjetId == saisie.ProjetId && a.Periode.Contient(saisie.Jour));
                if (!affecte)
                {
                    throw new RegleMetierException("RG-33",
                        $"Le collaborateur n'est pas affecté au projet {saisie.ProjetId} le {saisie.Jour:dd/MM/yyyy}.");
                }
            }
        }

        _saisies.Clear();
        _saisies.AddRange(saisies);
        Version = Guid.NewGuid();
    }

    /// <summary>Le temps saisi dans la feuille, additionné par jour.</summary>
    public IReadOnlyDictionary<DateOnly, decimal> TempsParJour() =>
        _saisies.GroupBy(s => s.Jour).ToDictionary(g => g.Key, g => g.Sum(s => s.Duree));

    private void Exiger(StatutFeuilleDeTemps attendu, string message)
    {
        if (Statut != attendu)
        {
            throw new RegleMetierException("RG-34", message);
        }
    }

    private void InterdireAuCollaborateur(int valideurId)
    {
        if (valideurId == CollaborateurId)
        {
            throw new RegleMetierException("RG-36", "Nul ne valide ni ne rejette sa propre feuille de temps.");
        }
    }
}

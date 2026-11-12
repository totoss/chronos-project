namespace Chronos.Api.Projets;

public class ProjetsEnMemoire
{
    private readonly Lock _verrou = new();
    private readonly List<ProjetDto> _projets =
    [
        new(1, "CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            new DateOnly(2026, 1, 5), null, 120, StatutProjet.EnCours),
        new(2, "ACME-MIGR", "Migration du SI commercial", NatureProjet.Facturable,
            new DateOnly(2026, 3, 2), new DateOnly(2026, 9, 30), 300, StatutProjet.EnPreparation),
    ];
    private int _prochainId = 3;

    public IReadOnlyList<ProjetDto> Lister(StatutProjet? statut = null)
    {
        lock (_verrou)
        {
            return [.. _projets.Where(p => statut is null || p.Statut == statut)];
        }
    }

    public ProjetDto? Trouver(int id)
    {
        lock (_verrou)
        {
            return _projets.Find(p => p.Id == id);
        }
    }

    public ProjetDto Ajouter(CreerProjetRequete requete)
    {
        lock (_verrou)
        {
            var projet = new ProjetDto(_prochainId++, requete.Code, requete.Libelle, requete.Nature,
                requete.DateDebut, requete.DateFin, requete.BudgetJours, StatutProjet.EnPreparation);
            _projets.Add(projet);
            return projet;
        }
    }

    public bool Supprimer(int id)
    {
        lock (_verrou)
        {
            return _projets.RemoveAll(p => p.Id == id) > 0;
        }
    }
}

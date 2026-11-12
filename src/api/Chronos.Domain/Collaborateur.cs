namespace Chronos.Domain;

public class Collaborateur
{
    private readonly List<Contrat> _contrats = [];

    public Collaborateur(
        string matricule,
        string nom,
        string prenom,
        string email,
        CategorieCollaborateur categorie,
        string poste,
        DateOnly dateEntree)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matricule);
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);
        ArgumentException.ThrowIfNullOrWhiteSpace(prenom);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(poste);

        Matricule = matricule.Trim();
        Nom = nom.Trim();
        Prenom = prenom.Trim();
        Email = email.Trim();
        Categorie = categorie;
        Poste = poste.Trim();
        DateEntree = dateEntree;
    }

    public int Id { get; private set; }
    public string Matricule { get; private set; }
    public string Nom { get; private set; }
    public string Prenom { get; private set; }
    public string Email { get; private set; }
    public CategorieCollaborateur Categorie { get; private set; }
    public string Poste { get; private set; }
    public DateOnly DateEntree { get; private set; }
    public DateOnly? DateSortie { get; private set; }
    public int? ManagerId { get; private set; }
    public Collaborateur? Manager { get; private set; }
    public bool EstArchive { get; private set; }
    public IReadOnlyCollection<Contrat> Contrats => _contrats;

    public void Modifier(
        string nom, string prenom, string email, string poste, DateOnly? dateSortie, int? managerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);
        ArgumentException.ThrowIfNullOrWhiteSpace(prenom);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(poste);
        VerifierDateSortie(dateSortie);

        Nom = nom.Trim();
        Prenom = prenom.Trim();
        Email = email.Trim();
        Poste = poste.Trim();
        DateSortie = dateSortie;
        ManagerId = managerId;
    }

    public void DeclarerSortie(DateOnly dateSortie)
    {
        VerifierDateSortie(dateSortie);

        DateSortie = dateSortie;
    }

    public void RattacherA(int? managerId) => ManagerId = managerId;

    public void Archiver() => EstArchive = true;

    public void AjouterContrat(TypeContrat type, decimal quotite, DateOnly dateDebut)
    {
        var contrat = new Contrat(type, quotite, dateDebut);

        var enCours = _contrats.Find(c => c.DateFin is null);
        if (enCours is not null)
        {
            if (dateDebut <= enCours.DateDebut)
            {
                throw new InvalidOperationException(
                    "Un nouveau contrat commence après le début du contrat en cours.");
            }

            enCours.Cloturer(dateDebut.AddDays(-1));
        }

        _contrats.Add(contrat);
    }

    public decimal QuotiteAu(DateOnly jour) =>
        _contrats.Find(c => c.EstActifAu(jour))?.Quotite ?? 0m;

    private void VerifierDateSortie(DateOnly? dateSortie)
    {
        if (dateSortie is not null && dateSortie < DateEntree)
        {
            throw new RegleMetierException("RG-02",
                "La date de sortie est postérieure ou égale à la date d'entrée.");
        }
    }
}

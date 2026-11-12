namespace Chronos.Domain;

public class Contrat
{
    private const decimal QuotiteMinimale = 0.10m;
    private const decimal QuotiteMaximale = 1m;

    public Contrat(TypeContrat type, decimal quotite, DateOnly dateDebut)
    {
        if (quotite is < QuotiteMinimale or > QuotiteMaximale)
        {
            throw new RegleMetierException("RG-03", "La quotité est comprise entre 10 % et 100 %.");
        }

        Type = type;
        Quotite = quotite;
        DateDebut = dateDebut;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public TypeContrat Type { get; private set; }
    public decimal Quotite { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly? DateFin { get; private set; }

    public bool EstActifAu(DateOnly jour) => DateDebut <= jour && (DateFin is null || jour <= DateFin);

    internal void Cloturer(DateOnly dateFin) => DateFin = dateFin;
}

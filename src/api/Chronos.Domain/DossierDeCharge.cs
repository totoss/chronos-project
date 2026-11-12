namespace Chronos.Domain;

public sealed record QuotiteContractuelle(Periode Periode, decimal Quotite);

public sealed record AffectationPlanifiee(int ProjetId, Periode Periode, decimal TauxAllocation, bool Facturable);

public sealed record DossierDeCharge(
    Periode Presence,
    bool EstConsultant,
    IReadOnlyList<QuotiteContractuelle> Quotites,
    IReadOnlyList<AffectationPlanifiee> Affectations,
    IReadOnlyDictionary<DateOnly, decimal> Absences)
{
    public decimal QuotiteAu(DateOnly jour) =>
        Quotites.FirstOrDefault(q => q.Periode.Contient(jour))?.Quotite ?? 0m;
}

public sealed record ChargeSurPeriode(
    Periode Periode,
    decimal Capacite,
    decimal ChargePlanifiee,
    decimal? TauxDeCharge,
    bool Surcharge,
    bool Intercontrat);

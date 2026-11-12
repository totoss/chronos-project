namespace Chronos.Domain;

public sealed class CalculateurDeCharge(CalendrierDeTravail calendrier)
{
    /// <summary>
    /// RG-24 : vrai si, une fois l'affectation candidate ajoutée au dossier, au moins une semaine
    /// de sa période est en surcharge.
    /// </summary>
    public bool MetEnSurcharge(DossierDeCharge dossier, AffectationPlanifiee candidate)
    {
        var avecCandidate = dossier with { Affectations = [.. dossier.Affectations, candidate] };

        return Decoupage.ParSemaine(candidate.Periode).Any(semaine => Calculer(avecCandidate, semaine).Surcharge);
    }

    public ChargeSurPeriode Calculer(DossierDeCharge dossier, Periode periode)
    {
        var capacite = 0m;
        var charge = 0m;
        var chargeFacturable = 0m;

        foreach (var jour in calendrier.JoursOuvres(periode))
        {
            if (!dossier.Presence.Contient(jour))
            {
                continue;
            }

            var quotite = dossier.QuotiteAu(jour);
            var partAbsente = dossier.Absences.GetValueOrDefault(jour);
            capacite += (1m - partAbsente) * quotite;

            foreach (var affectation in dossier.Affectations)
            {
                if (!affectation.Periode.Contient(jour))
                {
                    continue;
                }

                var joursReserves = quotite * affectation.TauxAllocation;
                charge += joursReserves;
                if (affectation.Facturable)
                {
                    chargeFacturable += joursReserves;
                }
            }
        }

        decimal? taux = capacite == 0m
            ? null
            : Math.Round(charge / capacite, 4, MidpointRounding.AwayFromZero);

        return new ChargeSurPeriode(
            periode,
            capacite,
            charge,
            taux,
            Surcharge: charge > capacite,
            Intercontrat: dossier.EstConsultant && capacite > 0m && chargeFacturable == 0m);
    }
}

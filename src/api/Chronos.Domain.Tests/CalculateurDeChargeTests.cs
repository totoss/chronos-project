namespace Chronos.Domain.Tests;

public class CalculateurDeChargeTests
{
    // Semaine du 4 au 8 mai 2026 ; le vendredi 8 est férié.
    private static readonly DateOnly Lundi = new(2026, 5, 4);
    private static readonly Periode LaSemaine = new(Lundi, Lundi.AddDays(6));
    private static readonly Periode Toujours = new(DateOnly.MinValue, DateOnly.MaxValue);
    private static readonly CalendrierDeTravail Calendrier = new([new DateOnly(2026, 5, 8)]);

    private static DossierDeCharge Dossier(
        decimal quotite = 1m,
        IReadOnlyList<AffectationPlanifiee>? affectations = null,
        IReadOnlyDictionary<DateOnly, decimal>? absences = null,
        Periode? presence = null,
        IReadOnlyList<QuotiteContractuelle>? quotites = null,
        bool consultant = true) =>
        new(presence ?? Toujours, consultant, quotites ?? [new QuotiteContractuelle(Toujours, quotite)],
            affectations ?? [], absences ?? new Dictionary<DateOnly, decimal>());

    private static AffectationPlanifiee Affectation(
        decimal taux, int projetId = 1, Periode? periode = null, bool facturable = true) =>
        new(projetId, periode ?? LaSemaine, taux, facturable);

    private static DossierDeCharge Camille(bool congeLeLundi) => Dossier(
        quotite: 0.8m,
        affectations: [Affectation(0.5m, projetId: 1), Affectation(0.5m, projetId: 2)],
        absences: congeLeLundi ? new Dictionary<DateOnly, decimal> { [Lundi] = 1m } : null);

    [Fact]
    public void Exemple_du_cahier_des_charges()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Camille(congeLeLundi: true), LaSemaine);

        Assert.Equal(2.4m, charge.Capacite);
        Assert.Equal(3.2m, charge.ChargePlanifiee);
        Assert.Equal(1.3333m, charge.TauxDeCharge);
        Assert.True(charge.Surcharge);
    }

    [Fact]
    public void Un_temps_partiel_alloue_a_100_pour_cent_est_charge_a_100_pour_cent()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Camille(congeLeLundi: false), LaSemaine);

        Assert.Equal(3.2m, charge.Capacite);
        Assert.Equal(1m, charge.TauxDeCharge);
        Assert.False(charge.Surcharge);
    }

    public static IEnumerable<TheoryDataRow<DossierDeCharge, decimal, decimal>> CasLimites =>
    [
        new(Dossier(presence: new Periode(Lundi.AddDays(2), DateOnly.MaxValue), affectations: [Affectation(0.5m)]),
            2m, 1m)
            { Label = "entrée le mercredi" },

        new(Dossier(
                quotites:
                [
                    new QuotiteContractuelle(new Periode(DateOnly.MinValue, Lundi.AddDays(1)), 1m),
                    new QuotiteContractuelle(new Periode(Lundi.AddDays(2), DateOnly.MaxValue), 0.8m),
                ],
                affectations: [Affectation(1m)]),
            3.6m, 3.6m)
            { Label = "quotité qui passe de 100 % à 80 % le mercredi" },

        new(Dossier(affectations: [Affectation(0.5m, periode: new Periode(Lundi.AddDays(2), Lundi.AddDays(3)))]),
            4m, 1m)
            { Label = "affectation limitée au mercredi et au jeudi" },

        new(Dossier(quotite: 0.8m, absences: new Dictionary<DateOnly, decimal> { [Lundi.AddDays(1)] = 0.5m }),
            2.8m, 0m)
            { Label = "demi-journée d'absence à 80 %" },
    ];

    [Theory]
    [MemberData(nameof(CasLimites))]
    public void Capacite_et_charge_dans_les_cas_limites(
        DossierDeCharge dossier, decimal capaciteAttendue, decimal chargeAttendue)
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(dossier, LaSemaine);

        Assert.Equal(capaciteAttendue, charge.Capacite);
        Assert.Equal(chargeAttendue, charge.ChargePlanifiee);
    }

    [Fact]
    public void Sans_capacite_le_taux_de_charge_n_est_pas_defini()
    {
        var absentTouteLaSemaine = Dossier(
            affectations: [Affectation(0.5m)],
            absences: new Dictionary<DateOnly, decimal>
            {
                [Lundi] = 1m, [Lundi.AddDays(1)] = 1m, [Lundi.AddDays(2)] = 1m, [Lundi.AddDays(3)] = 1m,
            });

        var charge = new CalculateurDeCharge(Calendrier).Calculer(absentTouteLaSemaine, LaSemaine);

        Assert.Equal(0m, charge.Capacite);
        Assert.Null(charge.TauxDeCharge);
        Assert.True(charge.Surcharge);
    }

    [Fact]
    public void Sans_contrat_ni_affectation_le_taux_de_charge_n_est_pas_defini_et_il_n_y_a_pas_de_surcharge()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Dossier(quotites: []), LaSemaine);

        Assert.Equal(0m, charge.Capacite);
        Assert.Null(charge.TauxDeCharge);
        Assert.False(charge.Surcharge);
    }

    [Fact]
    public void Un_consultant_sans_charge_facturable_est_en_intercontrat()
    {
        var formation = Dossier(affectations: [Affectation(0.5m, facturable: false)]);

        var charge = new CalculateurDeCharge(Calendrier).Calculer(formation, LaSemaine);

        Assert.True(charge.Intercontrat);
    }

    [Fact]
    public void Un_consultant_avec_une_charge_facturable_n_est_pas_en_intercontrat()
    {
        var mission = Dossier(affectations: [Affectation(0.5m, facturable: true)]);

        var charge = new CalculateurDeCharge(Calendrier).Calculer(mission, LaSemaine);

        Assert.False(charge.Intercontrat);
    }

    [Fact]
    public void Un_salarie_interne_n_est_jamais_en_intercontrat()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Dossier(consultant: false), LaSemaine);

        Assert.False(charge.Intercontrat);
    }

    [Fact]
    public void Un_consultant_sans_capacite_n_est_pas_en_intercontrat()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Dossier(quotites: []), LaSemaine);

        Assert.False(charge.Intercontrat);
    }

    [Fact]
    public void Un_consultant_sans_aucune_affectation_est_en_intercontrat()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Dossier(), LaSemaine);

        Assert.Equal(0m, charge.ChargePlanifiee);
        Assert.True(charge.Intercontrat);
    }

    // Exercice 4 : sur une période plus longue qu'une semaine, l'intercontrat suppose une charge facturable
    // nulle sur TOUTE la période.
    private static readonly Periode Mai = new(Lundi, new DateOnly(2026, 5, 31));

    [Fact]
    public void Sur_un_mois_une_seule_semaine_facturable_suffit_a_ne_pas_etre_en_intercontrat()
    {
        var uneSemaineDeMission = Dossier(affectations: [Affectation(0.5m, periode: LaSemaine)]);

        var charge = new CalculateurDeCharge(Calendrier).Calculer(uneSemaineDeMission, Mai);

        Assert.False(charge.Intercontrat);
    }

    [Fact]
    public void Sur_un_mois_sans_aucune_charge_facturable_le_consultant_est_en_intercontrat()
    {
        var formationSeulement = Dossier(affectations: [Affectation(0.5m, periode: Mai, facturable: false)]);

        var charge = new CalculateurDeCharge(Calendrier).Calculer(formationSeulement, Mai);

        Assert.True(charge.Intercontrat);
    }

    // RG-24 : l'affectation candidate est comptée dans le calcul.
    private static readonly Periode DeuxSemaines = new(Lundi, Lundi.AddDays(13));
    private static readonly Periode SecondeSemaine = new(Lundi.AddDays(7), Lundi.AddDays(13));

    [Fact]
    public void Une_affectation_qui_remplit_exactement_la_capacite_ne_met_pas_en_surcharge()
    {
        var dossier = Dossier(quotite: 0.8m, affectations: [Affectation(0.5m, projetId: 1)]);

        var surcharge = new CalculateurDeCharge(Calendrier).MetEnSurcharge(dossier, Affectation(0.5m, projetId: 2));

        Assert.False(surcharge);
    }

    [Fact]
    public void Une_affectation_qui_depasse_la_capacite_met_en_surcharge()
    {
        var dossier = Dossier(affectations: [Affectation(0.6m, projetId: 1)]);

        var surcharge = new CalculateurDeCharge(Calendrier).MetEnSurcharge(dossier, Affectation(0.5m, projetId: 2));

        Assert.True(surcharge);
    }

    [Fact]
    public void Une_seule_semaine_en_surcharge_suffit()
    {
        var dossier = Dossier(affectations: [Affectation(0.6m, projetId: 1, periode: SecondeSemaine)]);
        var candidate = Affectation(0.5m, projetId: 2, periode: DeuxSemaines);
        var calculateur = new CalculateurDeCharge(Calendrier);

        Assert.True(calculateur.MetEnSurcharge(dossier, candidate));
        Assert.False(calculateur.Calculer(dossier, new Periode(Lundi, Lundi.AddDays(6))).Surcharge);
    }

    [Fact]
    public void Une_absence_reduit_la_capacite_et_peut_rendre_l_affectation_candidate_en_surcharge()
    {
        var dossier = Dossier(absences: new Dictionary<DateOnly, decimal> { [Lundi] = 1m });

        var surcharge = new CalculateurDeCharge(Calendrier).MetEnSurcharge(dossier, Affectation(1m));

        Assert.True(surcharge);
    }
}

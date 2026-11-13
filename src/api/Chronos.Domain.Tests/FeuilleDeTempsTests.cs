namespace Chronos.Domain.Tests;

public class FeuilleDeTempsTests
{
    private const int CamilleId = 2;
    private const int ManagerId = 1;
    private const int ProjetAffecte = 1;
    private const int ProjetNonAffecte = 9;

    private static readonly DateOnly Lundi = new(2026, 5, 4);
    private static readonly DateOnly Mardi = new(2026, 5, 5);
    private static readonly DateOnly Vendredi = new(2026, 5, 8); // jour férié
    private static readonly DateOnly Samedi = new(2026, 5, 9);

    private static FeuilleDeTemps CreerFeuille() => new(CamilleId, Lundi);

    // Camille est affectée au projet 1 toute la semaine ; le vendredi 8 mai est férié.
    private static ContexteDeSaisie Contexte(IReadOnlyDictionary<DateOnly, decimal>? absences = null) => new(
        new CalendrierDeTravail([Vendredi]),
        absences ?? new Dictionary<DateOnly, decimal>(),
        [new AffectationPlanifiee(ProjetAffecte, new Periode(Lundi, Lundi.AddDays(6)), 0.5m, Facturable: true)]);

    private static SaisieDemandee Saisie(DateOnly jour, decimal duree, int projetId = ProjetAffecte) =>
        new(projetId, jour, duree);

    private static FeuilleDeTemps CreerFeuilleAuStatut(StatutFeuilleDeTemps statut)
    {
        var feuille = CreerFeuille();

        if (statut != StatutFeuilleDeTemps.Brouillon)
        {
            feuille.Soumettre();
        }
        if (statut == StatutFeuilleDeTemps.Validee)
        {
            feuille.Valider(ManagerId);
        }

        return feuille;
    }

    private static RegleMetierException Refus(Action action) => Assert.Throws<RegleMetierException>(action);

    // ----- Le cycle (RG-34) -----

    [Fact]
    public void Une_nouvelle_feuille_est_un_brouillon_vide()
    {
        var feuille = CreerFeuille();

        Assert.Equal(StatutFeuilleDeTemps.Brouillon, feuille.Statut);
        Assert.Empty(feuille.Saisies);
        Assert.Null(feuille.MotifRejet);
        Assert.Equal(new Periode(Lundi, Lundi.AddDays(6)), feuille.Periode);
    }

    [Fact]
    public void Une_feuille_commence_un_lundi()
    {
        Assert.Throws<ArgumentException>(() => new FeuilleDeTemps(CamilleId, Mardi));
    }

    [Fact]
    public void Le_cycle_nominal_mene_du_brouillon_a_la_validation()
    {
        var feuille = CreerFeuille();

        feuille.Soumettre();
        Assert.Equal(StatutFeuilleDeTemps.Soumise, feuille.Statut);

        feuille.Valider(ManagerId);
        Assert.Equal(StatutFeuilleDeTemps.Validee, feuille.Statut);
    }

    [Fact]
    public void Le_rejet_rend_la_feuille_a_l_etat_de_brouillon_avec_son_motif()
    {
        var feuille = CreerFeuilleAuStatut(StatutFeuilleDeTemps.Soumise);

        feuille.Rejeter(ManagerId, "  Il manque le jeudi.  ");

        Assert.Equal(StatutFeuilleDeTemps.Brouillon, feuille.Statut);
        Assert.Equal("Il manque le jeudi.", feuille.MotifRejet);
    }

    [Fact]
    public void Le_motif_du_rejet_disparait_a_la_soumission_suivante()
    {
        var feuille = CreerFeuilleAuStatut(StatutFeuilleDeTemps.Soumise);
        feuille.Rejeter(ManagerId, "Il manque le jeudi.");

        feuille.Soumettre();

        Assert.Null(feuille.MotifRejet);
    }

    [Theory]
    [InlineData(StatutFeuilleDeTemps.Brouillon, "Soumettre", true)]
    [InlineData(StatutFeuilleDeTemps.Brouillon, "Valider", false)]
    [InlineData(StatutFeuilleDeTemps.Brouillon, "Rejeter", false)]
    [InlineData(StatutFeuilleDeTemps.Soumise, "Soumettre", false)]
    [InlineData(StatutFeuilleDeTemps.Soumise, "Valider", true)]
    [InlineData(StatutFeuilleDeTemps.Soumise, "Rejeter", true)]
    [InlineData(StatutFeuilleDeTemps.Validee, "Soumettre", false)]
    [InlineData(StatutFeuilleDeTemps.Validee, "Valider", false)]
    [InlineData(StatutFeuilleDeTemps.Validee, "Rejeter", false)]
    public void Les_neuf_combinaisons_etat_et_action(StatutFeuilleDeTemps depart, string action, bool permise)
    {
        var feuille = CreerFeuilleAuStatut(depart);
        Action transition = action switch
        {
            "Soumettre" => feuille.Soumettre,
            "Valider" => () => feuille.Valider(ManagerId),
            _ => () => feuille.Rejeter(ManagerId, "Motif"),
        };

        if (permise)
        {
            transition();
        }
        else
        {
            Assert.Equal("RG-34", Refus(transition).CodeRegle);
            Assert.Equal(depart, feuille.Statut);
        }
    }

    [Fact]
    public void Un_rejet_sans_motif_est_refuse()
    {
        var feuille = CreerFeuilleAuStatut(StatutFeuilleDeTemps.Soumise);

        Assert.Equal("RG-34", Refus(() => feuille.Rejeter(ManagerId, "   ")).CodeRegle);
        Assert.Equal(StatutFeuilleDeTemps.Soumise, feuille.Statut);
    }

    [Fact]
    public void Nul_ne_valide_sa_propre_feuille()
    {
        var feuille = CreerFeuilleAuStatut(StatutFeuilleDeTemps.Soumise);

        Assert.Equal("RG-36", Refus(() => feuille.Valider(CamilleId)).CodeRegle);
        Assert.Equal("RG-36", Refus(() => feuille.Rejeter(CamilleId, "Motif")).CodeRegle);
        Assert.Equal(StatutFeuilleDeTemps.Soumise, feuille.Statut);
    }

    // ----- Les saisies (RG-30 à RG-33, RG-35) -----

    [Fact]
    public void Des_saisies_valides_remplacent_celles_de_la_feuille()
    {
        var feuille = CreerFeuille();
        feuille.RemplacerSaisies([Saisie(Lundi, 0.5m), Saisie(Mardi, 1m)], Contexte());

        feuille.RemplacerSaisies([Saisie(Mardi, 0.25m)], Contexte());

        var saisie = Assert.Single(feuille.Saisies);
        Assert.Equal(Mardi, saisie.Jour);
        Assert.Equal(0.25m, saisie.Duree);
    }

    [Theory]
    [InlineData(0.25, true)]
    [InlineData(0.5, true)]
    [InlineData(0.75, true)]
    [InlineData(1, true)]
    [InlineData(0.3, false)]
    [InlineData(0, false)]
    [InlineData(1.25, false)]
    [InlineData(-0.5, false)]
    public void Le_temps_se_saisit_par_quarts_de_journee(decimal duree, bool accepte)
    {
        var feuille = CreerFeuille();
        Action action = () => feuille.RemplacerSaisies([Saisie(Mardi, duree)], Contexte());

        if (accepte)
        {
            action();
            Assert.Single(feuille.Saisies);
        }
        else
        {
            Assert.Equal("RG-30", Refus(action).CodeRegle);
            Assert.Empty(feuille.Saisies);
        }
    }

    [Fact]
    public void Le_total_d_une_journee_ne_depasse_pas_un_jour()
    {
        var feuille = CreerFeuille();
        var saisies = new[] { Saisie(Mardi, 0.75m), Saisie(Mardi, 0.5m, projetId: ProjetNonAffecte) };

        Assert.Equal("RG-31", Refus(() => feuille.RemplacerSaisies(saisies, Contexte())).CodeRegle);
        Assert.Empty(feuille.Saisies);
    }

    [Fact]
    public void Une_journee_exactement_pleine_est_acceptee()
    {
        var feuille = CreerFeuille();

        feuille.RemplacerSaisies([Saisie(Mardi, 0.75m), Saisie(Mardi, 0.25m)], Contexte());

        Assert.Equal(2, feuille.Saisies.Count);
    }

    [Theory]
    [MemberData(nameof(JoursFermes))]
    public void Aucune_saisie_un_jour_non_travaille(DateOnly jour)
    {
        var feuille = CreerFeuille();

        Assert.Equal("RG-32", Refus(() => feuille.RemplacerSaisies([Saisie(jour, 0.5m)], Contexte())).CodeRegle);
        Assert.Empty(feuille.Saisies);
    }

    public static TheoryData<DateOnly> JoursFermes => new() { Vendredi, Samedi, Samedi.AddDays(1) };

    [Fact]
    public void Aucune_saisie_un_jour_d_absence_approuvee()
    {
        var feuille = CreerFeuille();
        var absences = new Dictionary<DateOnly, decimal> { [Lundi] = 1m };

        Assert.Equal("RG-32", Refus(() => feuille.RemplacerSaisies([Saisie(Lundi, 0.25m)], Contexte(absences))).CodeRegle);
    }

    [Fact]
    public void Une_demi_journee_d_absence_laisse_une_demi_journee_de_saisie()
    {
        var absences = new Dictionary<DateOnly, decimal> { [Lundi] = 0.5m };
        var feuille = CreerFeuille();

        feuille.RemplacerSaisies([Saisie(Lundi, 0.5m)], Contexte(absences));
        Assert.Single(feuille.Saisies);

        Assert.Equal("RG-32", Refus(() => feuille.RemplacerSaisies([Saisie(Lundi, 0.75m)], Contexte(absences))).CodeRegle);
        Assert.Single(feuille.Saisies);
    }

    [Fact]
    public void Aucune_saisie_sur_un_projet_non_affecte()
    {
        var feuille = CreerFeuille();

        var exception = Refus(() => feuille.RemplacerSaisies([Saisie(Mardi, 0.5m, ProjetNonAffecte)], Contexte()));

        Assert.Equal("RG-33", exception.CodeRegle);
    }

    [Fact]
    public void Aucune_saisie_hors_de_la_periode_d_affectation()
    {
        var feuille = CreerFeuille();
        var contexte = Contexte() with
        {
            Affectations = [new AffectationPlanifiee(ProjetAffecte, new Periode(Mardi, Mardi.AddDays(2)), 0.5m, true)],
        };

        feuille.RemplacerSaisies([Saisie(Mardi, 0.5m)], contexte);

        Assert.Equal("RG-33", Refus(() => feuille.RemplacerSaisies([Saisie(Lundi, 0.5m)], contexte)).CodeRegle);
    }

    [Fact]
    public void Un_jour_hors_de_la_semaine_est_une_erreur_de_programmation()
    {
        var feuille = CreerFeuille();

        Assert.Throws<ArgumentException>(() =>
            feuille.RemplacerSaisies([Saisie(Lundi.AddDays(7), 0.5m)], Contexte()));
    }

    [Theory]
    [InlineData(StatutFeuilleDeTemps.Soumise)]
    [InlineData(StatutFeuilleDeTemps.Validee)]
    public void Une_feuille_soumise_ou_validee_n_est_plus_modifiable(StatutFeuilleDeTemps statut)
    {
        var feuille = CreerFeuilleAuStatut(statut);

        Assert.Equal("RG-35", Refus(() => feuille.RemplacerSaisies([Saisie(Mardi, 0.5m)], Contexte())).CodeRegle);
    }

    [Fact]
    public void Un_refus_laisse_les_saisies_d_origine_intactes()
    {
        var feuille = CreerFeuille();
        feuille.RemplacerSaisies([Saisie(Lundi, 0.5m), Saisie(Mardi, 1m)], Contexte());
        var version = feuille.Version;

        Assert.Throws<RegleMetierException>(() =>
            feuille.RemplacerSaisies([Saisie(Lundi, 0.5m), Saisie(Samedi, 0.5m)], Contexte()));

        Assert.Equal(2, feuille.Saisies.Count);
        Assert.Equal(version, feuille.Version);
    }

    [Fact]
    public void Le_temps_par_jour_additionne_les_saisies_d_un_meme_jour()
    {
        var feuille = CreerFeuille();
        feuille.RemplacerSaisies([Saisie(Mardi, 0.25m), Saisie(Mardi, 0.5m), Saisie(Lundi, 1m)], Contexte());

        var temps = feuille.TempsParJour();

        Assert.Equal(0.75m, temps[Mardi]);
        Assert.Equal(1m, temps[Lundi]);
        Assert.Equal(2, temps.Count);
    }

    // ----- Le jeton de concurrence : un test par méthode qui modifie -----

    private static void VerifierQueLaVersionChange(StatutFeuilleDeTemps depart, Action<FeuilleDeTemps> modification)
    {
        var feuille = CreerFeuilleAuStatut(depart);
        var avant = feuille.Version;

        modification(feuille);

        Assert.NotEqual(avant, feuille.Version);
    }

    [Fact]
    public void Soumettre_renouvelle_la_version() =>
        VerifierQueLaVersionChange(StatutFeuilleDeTemps.Brouillon, f => f.Soumettre());

    [Fact]
    public void Valider_renouvelle_la_version() =>
        VerifierQueLaVersionChange(StatutFeuilleDeTemps.Soumise, f => f.Valider(ManagerId));

    [Fact]
    public void Rejeter_renouvelle_la_version() =>
        VerifierQueLaVersionChange(StatutFeuilleDeTemps.Soumise, f => f.Rejeter(ManagerId, "Motif"));

    [Fact]
    public void RemplacerSaisies_renouvelle_la_version() =>
        VerifierQueLaVersionChange(StatutFeuilleDeTemps.Brouillon,
            f => f.RemplacerSaisies([Saisie(Mardi, 0.5m)], Contexte()));

    [Fact]
    public void Une_transition_refusee_laisse_la_version_inchangee()
    {
        var feuille = CreerFeuille();
        var avant = feuille.Version;

        Assert.Throws<RegleMetierException>(() => feuille.Valider(ManagerId));

        Assert.Equal(avant, feuille.Version);
    }
}

namespace Chronos.Domain.Tests;

public class AbsenceTests
{
    private static readonly DateOnly Lundi = new(2026, 5, 4);
    private static readonly DateOnly Mardi = new(2026, 5, 5);
    private static readonly DateOnly Mercredi = new(2026, 5, 6);
    private static readonly DateOnly Jeudi = new(2026, 5, 7);
    private static readonly DateOnly Vendredi = new(2026, 5, 8); // jour férié
    private static readonly DateOnly Samedi = new(2026, 5, 9);
    private static readonly DateOnly LundiSuivant = new(2026, 5, 11);

    private static readonly DateOnly Aujourdhui = new(2026, 5, 1);

    private static readonly CalendrierDeTravail Calendrier = new([Vendredi]);

    private static Absence Creer(DateOnly debut, DateOnly fin, bool debuteApresMidi = false, bool finitLeMatin = false) =>
        new(collaborateurId: 2, typeAbsenceId: 1, debut, fin, debuteApresMidi, finitLeMatin);

    private static Absence CreerAuStatut(StatutAbsence statut)
    {
        var absence = Creer(Lundi, Mardi);

        switch (statut)
        {
            case StatutAbsence.Approuvee:
                absence.Approuver();
                break;
            case StatutAbsence.Refusee:
                absence.Refuser();
                break;
            case StatutAbsence.Annulee:
                absence.Annuler(Aujourdhui);
                break;
        }

        return absence;
    }

    // ----- Création et durée (RG-42) -----

    [Fact]
    public void Une_nouvelle_absence_est_demandee()
    {
        Assert.Equal(StatutAbsence.Demandee, Creer(Lundi, Mardi).Statut);
    }

    [Fact]
    public void Une_fin_avant_le_debut_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() => Creer(Mardi, Lundi));

        Assert.Equal("RG-42", exception.CodeRegle);
    }

    [Fact]
    public void Une_absence_d_un_jour_dure_au_moins_une_demi_journee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Creer(Lundi, Lundi, debuteApresMidi: true, finitLeMatin: true));

        Assert.Equal("RG-42", exception.CodeRegle);
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 0.5)]
    [InlineData(false, true, 0.5)]
    public void Une_absence_d_un_jour_dure_un_jour_ou_une_demi_journee(bool apresMidi, bool matin, double attendu)
    {
        var absence = Creer(Lundi, Lundi, apresMidi, matin);

        Assert.Equal((decimal)attendu, absence.PartAbsente(Lundi));
        Assert.Equal((decimal)attendu, absence.DureeEnJours(Calendrier));
    }

    [Fact]
    public void Le_jeudi_apres_midi_au_lundi_matin_avec_un_vendredi_ferie_dure_un_jour()
    {
        var absence = Creer(Jeudi, LundiSuivant, debuteApresMidi: true, finitLeMatin: true);

        Assert.Equal(0.5m, absence.PartAbsente(Jeudi));
        Assert.Equal(1m, absence.PartAbsente(Vendredi));
        Assert.Equal(0.5m, absence.PartAbsente(LundiSuivant));
        Assert.Equal(1m, absence.DureeEnJours(Calendrier));
    }

    [Fact]
    public void Un_jour_hors_de_l_absence_n_est_pas_absent()
    {
        Assert.Equal(0m, Creer(Lundi, Mardi).PartAbsente(Mercredi));
    }

    [Fact]
    public void Une_absence_sans_jour_ouvre_est_refusee()
    {
        var weekEnd = Creer(Samedi, Samedi.AddDays(1));
        var ferie = Creer(Vendredi, Vendredi);

        Assert.Equal("RG-42", Assert.Throws<RegleMetierException>(() => weekEnd.VerifierDuree(Calendrier)).CodeRegle);
        Assert.Equal("RG-42", Assert.Throws<RegleMetierException>(() => ferie.VerifierDuree(Calendrier)).CodeRegle);
    }

    [Fact]
    public void Une_absence_avec_un_jour_ouvre_est_acceptee()
    {
        Creer(Vendredi, LundiSuivant).VerifierDuree(Calendrier);
    }

    // ----- Le cycle (RG-40) -----

    [Fact]
    public void Une_absence_demandee_peut_etre_approuvee_refusee_ou_annulee()
    {
        Assert.Equal(StatutAbsence.Approuvee, CreerAuStatut(StatutAbsence.Approuvee).Statut);
        Assert.Equal(StatutAbsence.Refusee, CreerAuStatut(StatutAbsence.Refusee).Statut);
        Assert.Equal(StatutAbsence.Annulee, CreerAuStatut(StatutAbsence.Annulee).Statut);
    }

    [Theory]
    [InlineData(StatutAbsence.Approuvee)]
    [InlineData(StatutAbsence.Refusee)]
    [InlineData(StatutAbsence.Annulee)]
    public void Seule_une_absence_demandee_est_approuvee_ou_refusee(StatutAbsence statut)
    {
        var absence = CreerAuStatut(statut);

        Assert.Equal("RG-40", Assert.Throws<RegleMetierException>(absence.Approuver).CodeRegle);
        Assert.Equal("RG-40", Assert.Throws<RegleMetierException>(absence.Refuser).CodeRegle);
        Assert.Equal(statut, absence.Statut);
    }

    [Fact]
    public void Une_absence_approuvee_peut_etre_annulee_avant_son_debut()
    {
        var absence = CreerAuStatut(StatutAbsence.Approuvee);

        absence.Annuler(Aujourdhui);

        Assert.Equal(StatutAbsence.Annulee, absence.Statut);
    }

    [Theory]
    [InlineData(StatutAbsence.Refusee)]
    [InlineData(StatutAbsence.Annulee)]
    public void Une_absence_refusee_ou_annulee_ne_peut_plus_etre_annulee(StatutAbsence statut)
    {
        var absence = CreerAuStatut(statut);

        Assert.Equal("RG-40", Assert.Throws<RegleMetierException>(() => absence.Annuler(Aujourdhui)).CodeRegle);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void Une_absence_s_annule_tant_qu_elle_n_a_pas_commence(int joursAvantLeDebut, bool annulable)
    {
        var absence = Creer(Lundi, Mardi);
        var aujourdhui = Lundi.AddDays(joursAvantLeDebut);

        if (annulable)
        {
            absence.Annuler(aujourdhui);
            Assert.Equal(StatutAbsence.Annulee, absence.Statut);
        }
        else
        {
            Assert.Equal("RG-40", Assert.Throws<RegleMetierException>(() => absence.Annuler(aujourdhui)).CodeRegle);
            Assert.Equal(StatutAbsence.Demandee, absence.Statut);
        }
    }

    // ----- Le jeton de concurrence : un test par méthode qui modifie -----

    private static void VerifierQueLaVersionChange(Action<Absence> modification)
    {
        var absence = Creer(Lundi, Mardi);
        var avant = absence.Version;

        modification(absence);

        Assert.NotEqual(avant, absence.Version);
    }

    [Fact]
    public void Approuver_renouvelle_la_version() => VerifierQueLaVersionChange(a => a.Approuver());

    [Fact]
    public void Refuser_renouvelle_la_version() => VerifierQueLaVersionChange(a => a.Refuser());

    [Fact]
    public void Annuler_renouvelle_la_version() => VerifierQueLaVersionChange(a => a.Annuler(Aujourdhui));

    [Fact]
    public void Une_transition_refusee_laisse_la_version_inchangee()
    {
        var absence = CreerAuStatut(StatutAbsence.Refusee);
        var avant = absence.Version;

        Assert.Throws<RegleMetierException>(absence.Approuver);

        Assert.Equal(avant, absence.Version);
    }

    // ----- Le chevauchement (RG-41) -----

    [Theory]
    [InlineData(4, 8, 6, 12, true)]    // se croisent
    [InlineData(4, 8, 8, 12, true)]    // un jour commun
    [InlineData(4, 8, 9, 12, false)]   // se suivent
    [InlineData(6, 7, 4, 15, true)]    // l'une contient l'autre
    [InlineData(4, 5, 11, 12, false)]  // éloignées
    public void Deux_absences_de_jours_entiers_se_chevauchent_si_elles_ont_un_jour_commun(
        int debutA, int finA, int debutB, int finB, bool attendu)
    {
        var a = Creer(new DateOnly(2026, 5, debutA), new DateOnly(2026, 5, finA));
        var b = Creer(new DateOnly(2026, 5, debutB), new DateOnly(2026, 5, finB));

        Assert.Equal(attendu, a.Chevauche(b));
        Assert.Equal(attendu, b.Chevauche(a));
    }

    [Fact]
    public void Une_absence_qui_finit_le_matin_et_une_autre_qui_debute_l_apres_midi_se_touchent_sans_se_chevaucher()
    {
        var jusqueMercrediMatin = Creer(Lundi, Mercredi, finitLeMatin: true);
        var depuisLeMercrediApresMidi = Creer(Mercredi, Jeudi, debuteApresMidi: true);

        Assert.False(jusqueMercrediMatin.Chevauche(depuisLeMercrediApresMidi));
        Assert.False(depuisLeMercrediApresMidi.Chevauche(jusqueMercrediMatin));
    }

    [Fact]
    public void Deux_absences_qui_partagent_une_demi_journee_se_chevauchent()
    {
        var jusqueMercrediInclus = Creer(Lundi, Mercredi);
        var depuisLeMercrediApresMidi = Creer(Mercredi, Jeudi, debuteApresMidi: true);

        Assert.True(jusqueMercrediInclus.Chevauche(depuisLeMercrediApresMidi));
    }

    // ----- Les parts d'absence par jour -----

    [Fact]
    public void Les_parts_absentes_couvrent_la_periode_demandee()
    {
        var absence = Creer(Jeudi, LundiSuivant, debuteApresMidi: true, finitLeMatin: true);
        var semaine = new Periode(Lundi, Lundi.AddDays(6));

        var parts = Absence.PartsAbsentes([absence], semaine);

        Assert.Equal(0.5m, parts[Jeudi]);
        Assert.Equal(1m, parts[Vendredi]);
        Assert.False(parts.ContainsKey(LundiSuivant));
        Assert.False(parts.ContainsKey(Mercredi));
    }

    [Fact]
    public void Les_parts_absentes_de_plusieurs_absences_s_additionnent_jusqu_a_un_jour()
    {
        var matin = Creer(Lundi, Lundi, finitLeMatin: true);
        var apresMidi = Creer(Lundi, Lundi, debuteApresMidi: true);
        var semaine = new Periode(Lundi, Lundi.AddDays(6));

        Assert.Equal(1m, Absence.PartsAbsentes([matin, apresMidi], semaine)[Lundi]);
        Assert.Equal(1m, Absence.PartsAbsentes([Creer(Lundi, Lundi), matin], semaine)[Lundi]);
        Assert.Empty(Absence.PartsAbsentes([], semaine));
    }

    // ----- RG-43 -----

    private static IReadOnlyDictionary<DateOnly, decimal> Parts(params Absence[] absences) =>
        Absence.PartsAbsentes(absences, new Periode(Lundi, LundiSuivant));

    private static Dictionary<DateOnly, decimal> Valide(DateOnly jour, decimal duree) => new() { [jour] = duree };

    [Fact]
    public void Du_temps_valide_un_jour_d_absence_complete_est_incompatible()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Absence.VerifierTempsValide(Valide(Mardi, 0.25m), Parts(Creer(Lundi, Mardi))));

        Assert.Equal("RG-43", exception.CodeRegle);
    }

    [Fact]
    public void Une_demi_journee_d_absence_est_compatible_avec_une_demi_journee_de_temps_valide()
    {
        var parts = Parts(Creer(Mardi, Mardi, debuteApresMidi: true));

        Absence.VerifierTempsValide(Valide(Mardi, 0.5m), parts);

        Assert.Equal("RG-43", Assert.Throws<RegleMetierException>(() =>
            Absence.VerifierTempsValide(Valide(Mardi, 0.75m), parts)).CodeRegle);
    }

    [Fact]
    public void Deux_demi_absences_le_meme_jour_et_une_demi_journee_validee_sont_incompatibles()
    {
        var parts = Parts(Creer(Mardi, Mardi, debuteApresMidi: true), Creer(Mardi, Mardi, finitLeMatin: true));

        var exception = Assert.Throws<RegleMetierException>(() =>
            Absence.VerifierTempsValide(Valide(Mardi, 0.5m), parts));

        Assert.Equal("RG-43", exception.CodeRegle);
    }

    [Fact]
    public void Du_temps_valide_hors_des_absences_est_compatible()
    {
        Absence.VerifierTempsValide(Valide(Mercredi, 1m), Parts(Creer(Lundi, Mardi)));
    }
}

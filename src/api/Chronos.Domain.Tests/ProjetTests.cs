namespace Chronos.Domain.Tests;

public class ProjetTests
{
    private static readonly DateOnly Debut = new(2026, 5, 4);

    private static Projet CreerProjetInterne() =>
        new("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1);

    private static Projet CreerProjetAuStatut(StatutProjet statut)
    {
        var projet = CreerProjetInterne();

        switch (statut)
        {
            case StatutProjet.EnCours:
                projet.Demarrer();
                break;
            case StatutProjet.Termine:
                projet.Demarrer();
                projet.Terminer();
                break;
            case StatutProjet.Annule:
                projet.Annuler();
                break;
        }

        return projet;
    }

    [Fact]
    public void Un_nouveau_projet_est_en_preparation()
    {
        var projet = CreerProjetInterne();

        Assert.Equal(StatutProjet.EnPreparation, projet.Statut);
    }

    [Fact]
    public void Un_projet_facturable_sans_client_est_refuse()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            new Projet("ACME-MIGR", "Migration du SI commercial", NatureProjet.Facturable,
                clientId: null, Debut, dateFin: null, budgetJours: 300, responsableId: 1));

        Assert.Equal("RG-11", exception.CodeRegle);
    }

    [Fact]
    public void Un_projet_interne_avec_client_est_refuse()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            new Projet("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
                clientId: 7, Debut, dateFin: null, budgetJours: 120, responsableId: 1));

        Assert.Equal("RG-11", exception.CodeRegle);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(31)]
    public void Une_date_de_fin_egale_ou_posterieure_au_debut_est_acceptee(int jourDeFin)
    {
        var dateFin = new DateOnly(2026, 5, jourDeFin);

        var projet = new Projet("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            clientId: null, Debut, dateFin, budgetJours: 120, responsableId: 1);

        Assert.Equal(dateFin, projet.DateFin);
    }

    [Fact]
    public void Une_date_de_fin_anterieure_au_debut_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            new Projet("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
                clientId: null, Debut, Debut.AddDays(-1), budgetJours: 120, responsableId: 1));

        Assert.Equal("RG-12", exception.CodeRegle);
    }

    [Fact]
    public void Un_projet_termine_n_accepte_plus_ni_affectation_ni_saisie()
    {
        var projet = CreerProjetInterne();
        projet.Demarrer();
        projet.Terminer();

        var exception = Assert.Throws<RegleMetierException>(projet.VerifierOuvert);

        Assert.Equal("RG-13", exception.CodeRegle);
    }

    [Fact]
    public void Un_projet_annule_n_accepte_plus_ni_affectation_ni_saisie()
    {
        var projet = CreerProjetInterne();
        projet.Annuler();

        var exception = Assert.Throws<RegleMetierException>(projet.VerifierOuvert);

        Assert.Equal("RG-13", exception.CodeRegle);
    }

    [Theory]
    [InlineData(StatutProjet.EnPreparation)]
    [InlineData(StatutProjet.EnCours)]
    public void Un_projet_non_clos_accepte_les_affectations_et_les_saisies(StatutProjet statut)
    {
        var projet = CreerProjetAuStatut(statut);

        projet.VerifierOuvert();

        Assert.False(projet.EstClos);
    }

    [Fact]
    public void Un_projet_en_preparation_peut_demarrer()
    {
        var projet = CreerProjetInterne();

        projet.Demarrer();

        Assert.Equal(StatutProjet.EnCours, projet.Statut);
    }

    [Theory]
    [InlineData(StatutProjet.EnCours)]
    [InlineData(StatutProjet.Termine)]
    [InlineData(StatutProjet.Annule)]
    public void Un_projet_deja_demarre_ou_clos_ne_peut_pas_demarrer(StatutProjet statut)
    {
        var projet = CreerProjetAuStatut(statut);

        Assert.Throws<InvalidOperationException>(projet.Demarrer);
    }

    [Fact]
    public void Un_projet_en_cours_peut_se_terminer()
    {
        var projet = CreerProjetAuStatut(StatutProjet.EnCours);

        projet.Terminer();

        Assert.Equal(StatutProjet.Termine, projet.Statut);
    }

    [Theory]
    [InlineData(StatutProjet.EnPreparation)]
    [InlineData(StatutProjet.Termine)]
    [InlineData(StatutProjet.Annule)]
    public void Un_projet_qui_n_est_pas_en_cours_ne_peut_pas_se_terminer(StatutProjet statut)
    {
        var projet = CreerProjetAuStatut(statut);

        Assert.Throws<InvalidOperationException>(projet.Terminer);
    }

    [Theory]
    [InlineData(StatutProjet.EnPreparation)]
    [InlineData(StatutProjet.EnCours)]
    public void Un_projet_non_clos_peut_etre_annule(StatutProjet statut)
    {
        var projet = CreerProjetAuStatut(statut);

        projet.Annuler();

        Assert.Equal(StatutProjet.Annule, projet.Statut);
    }

    [Theory]
    [InlineData(StatutProjet.Termine)]
    [InlineData(StatutProjet.Annule)]
    public void Un_projet_clos_ne_peut_pas_etre_annule(StatutProjet statut)
    {
        var projet = CreerProjetAuStatut(statut);

        Assert.Throws<InvalidOperationException>(projet.Annuler);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_code_vide_ou_fait_d_espaces_est_refuse(string code)
    {
        Assert.Throws<ArgumentException>(() =>
            new Projet(code, "Refonte du suivi des temps", NatureProjet.Interne,
                clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1));
    }

    [Fact]
    public void Un_code_absent_est_refuse()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Projet(null!, "Refonte du suivi des temps", NatureProjet.Interne,
                clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1));
    }

    [Fact]
    public void Un_libelle_vide_est_refuse()
    {
        Assert.Throws<ArgumentException>(() =>
            new Projet("CHRONOS", "", NatureProjet.Interne,
                clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1));
    }

    [Fact]
    public void Un_budget_negatif_est_refuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Projet("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
                clientId: null, Debut, dateFin: null, budgetJours: -1, responsableId: 1));
    }

    [Fact]
    public void Le_code_et_le_libelle_sont_debarrasses_de_leurs_espaces()
    {
        var projet = new Projet("  CHRONOS ", " Refonte du suivi des temps  ", NatureProjet.Interne,
            clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1);

        Assert.Equal("CHRONOS", projet.Code);
        Assert.Equal("Refonte du suivi des temps", projet.Libelle);
    }
}

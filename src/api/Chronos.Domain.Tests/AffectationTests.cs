namespace Chronos.Domain.Tests;

public class AffectationTests
{
    private static readonly DateOnly Debut = new(2026, 5, 4);
    private static readonly DateOnly Fin = new(2026, 5, 29);

    // Le projet court du 1er mai au 30 juin 2026 ; le collaborateur est entré le 4 mai et sorti le 30 mai.
    private static Collaborateur CreerCollaborateur(DateOnly? sortie = null)
    {
        var collaborateur = new Collaborateur("M0002", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", new DateOnly(2026, 5, 4));
        collaborateur.DeclarerSortie(sortie ?? new DateOnly(2026, 5, 30));
        return collaborateur;
    }

    private static Projet CreerProjet() =>
        new("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            clientId: null, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30), budgetJours: 120, responsableId: 1);

    private static Affectation CreerAffectation() =>
        Affectation.Creer(CreerCollaborateur(), CreerProjet(), Debut, Fin, 0.5m);

    [Fact]
    public void Une_affectation_valide_est_creee()
    {
        var affectation = CreerAffectation();

        Assert.Equal(Debut, affectation.DateDebut);
        Assert.Equal(Fin, affectation.DateFin);
        Assert.Equal(0.5m, affectation.TauxAllocation);
    }

    [Theory]
    [InlineData(0.04, false)]
    [InlineData(0.05, true)]
    [InlineData(1, true)]
    [InlineData(1.01, false)]
    public void Le_taux_d_allocation_est_compris_entre_5_et_100_pour_cent(decimal taux, bool accepte)
    {
        var action = () => Affectation.Creer(CreerCollaborateur(), CreerProjet(), Debut, Fin, taux);

        if (accepte)
        {
            Assert.Equal(taux, action().TauxAllocation);
        }
        else
        {
            Assert.Equal("RG-21", Assert.Throws<RegleMetierException>(action).CodeRegle);
        }
    }

    [Fact]
    public void Une_fin_avant_le_debut_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), CreerProjet(), Fin, Debut, 0.5m));

        Assert.Equal("RG-20", exception.CodeRegle);
    }

    [Fact]
    public void Une_affectation_commencant_avant_le_projet_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), CreerProjet(), new DateOnly(2026, 4, 30), Fin, 0.5m));

        Assert.Equal("RG-20", exception.CodeRegle);
    }

    [Fact]
    public void Une_affectation_finissant_apres_le_projet_est_refusee()
    {
        var collaborateur = CreerCollaborateur(sortie: new DateOnly(2026, 12, 31));

        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(collaborateur, CreerProjet(), Debut, new DateOnly(2026, 7, 1), 0.5m));

        Assert.Equal("RG-20", exception.CodeRegle);
    }

    [Fact]
    public void Une_affectation_commencant_avant_l_entree_du_collaborateur_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), CreerProjet(), new DateOnly(2026, 5, 1), Fin, 0.5m));

        Assert.Equal("RG-20", exception.CodeRegle);
    }

    [Fact]
    public void Une_affectation_finissant_apres_la_sortie_du_collaborateur_est_refusee()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), CreerProjet(), Debut, new DateOnly(2026, 5, 31), 0.5m));

        Assert.Equal("RG-20", exception.CodeRegle);
    }

    [Fact]
    public void Un_projet_sans_date_de_fin_accepte_toute_fin_d_affectation()
    {
        var projet = new Projet("FORMATION", "Formation interne", NatureProjet.Interne,
            clientId: null, new DateOnly(2026, 1, 5), dateFin: null, budgetJours: 60, responsableId: 1);
        var collaborateur = new Collaborateur("M0002", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", new DateOnly(2026, 1, 5));

        var affectation = Affectation.Creer(collaborateur, projet, Debut, new DateOnly(2030, 1, 1), 0.5m);

        Assert.Equal(new DateOnly(2030, 1, 1), affectation.DateFin);
    }

    [Fact]
    public void Une_affectation_sur_un_projet_termine_est_refusee()
    {
        var projet = CreerProjet();
        projet.Demarrer();
        projet.Terminer();

        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), projet, Debut, Fin, 0.5m));

        Assert.Equal("RG-13", exception.CodeRegle);
    }

    [Fact]
    public void Une_affectation_sur_un_projet_annule_est_refusee()
    {
        var projet = CreerProjet();
        projet.Annuler();

        var exception = Assert.Throws<RegleMetierException>(() =>
            Affectation.Creer(CreerCollaborateur(), projet, Debut, Fin, 0.5m));

        Assert.Equal("RG-13", exception.CodeRegle);
    }

    [Fact]
    public void Modifier_remplace_la_periode_et_le_taux()
    {
        var affectation = CreerAffectation();

        affectation.Modifier(CreerCollaborateur(), CreerProjet(), Debut.AddDays(1), Fin.AddDays(-1), 0.8m);

        Assert.Equal(Debut.AddDays(1), affectation.DateDebut);
        Assert.Equal(Fin.AddDays(-1), affectation.DateFin);
        Assert.Equal(0.8m, affectation.TauxAllocation);
    }

    [Fact]
    public void Modifier_reverifie_les_regles_et_laisse_l_affectation_intacte()
    {
        var affectation = CreerAffectation();

        var exception = Assert.Throws<RegleMetierException>(() =>
            affectation.Modifier(CreerCollaborateur(), CreerProjet(), Debut, Fin, 1.5m));

        Assert.Equal("RG-21", exception.CodeRegle);
        Assert.Equal(0.5m, affectation.TauxAllocation);
    }

    [Fact]
    public void Modifier_sur_un_projet_termine_est_refuse()
    {
        var affectation = CreerAffectation();
        var projet = CreerProjet();
        projet.Demarrer();
        projet.Terminer();

        var exception = Assert.Throws<RegleMetierException>(() =>
            affectation.Modifier(CreerCollaborateur(), projet, Debut, Fin, 0.5m));

        Assert.Equal("RG-13", exception.CodeRegle);
    }

    [Fact]
    public void Modifier_avec_un_autre_collaborateur_ou_un_autre_projet_est_refuse()
    {
        var affectation = CreerAffectation();
        var autreCollaborateur = CreerCollaborateur();
        // L'identifiant n'a pas de setter public : seul EF Core l'écrit, le test passe par la réflexion.
        typeof(Collaborateur).GetProperty(nameof(Collaborateur.Id))!.SetValue(autreCollaborateur, 7);
        var autreProjet = CreerProjet();
        typeof(Projet).GetProperty(nameof(Projet.Id))!.SetValue(autreProjet, 7);

        Assert.Throws<ArgumentException>(() =>
            affectation.Modifier(autreCollaborateur, CreerProjet(), Debut, Fin, 0.5m));
        Assert.Throws<ArgumentException>(() =>
            affectation.Modifier(CreerCollaborateur(), autreProjet, Debut, Fin, 0.5m));
    }
}

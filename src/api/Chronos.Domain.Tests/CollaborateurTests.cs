namespace Chronos.Domain.Tests;

public class CollaborateurTests
{
    private static readonly DateOnly Entree = new(2026, 1, 5);

    private static Collaborateur CreerCollaborateur() =>
        new("M0002", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", Entree);

    [Fact]
    public void Le_constructeur_renseigne_la_fiche_sans_sortie_ni_manager_ni_contrat()
    {
        var collaborateur = CreerCollaborateur();

        Assert.Null(collaborateur.DateSortie);
        Assert.Null(collaborateur.ManagerId);
        Assert.Empty(collaborateur.Contrats);
        Assert.False(collaborateur.EstArchive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_collaborateur_sans_matricule_est_refuse(string matricule)
    {
        Assert.Throws<ArgumentException>(() =>
            new Collaborateur(matricule, "Durand", "Camille", "camille.durand@chronos.example",
                CategorieCollaborateur.Consultant, "Développeuse", Entree));
    }

    [Fact]
    public void Une_sortie_le_jour_de_l_entree_est_acceptee()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.DeclarerSortie(Entree);

        Assert.Equal(Entree, collaborateur.DateSortie);
    }

    [Fact]
    public void Une_sortie_posterieure_a_l_entree_est_acceptee()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.DeclarerSortie(Entree.AddDays(200));

        Assert.Equal(Entree.AddDays(200), collaborateur.DateSortie);
    }

    [Fact]
    public void Une_sortie_la_veille_de_l_entree_est_refusee()
    {
        var collaborateur = CreerCollaborateur();

        var exception = Assert.Throws<RegleMetierException>(() =>
            collaborateur.DeclarerSortie(Entree.AddDays(-1)));

        Assert.Equal("RG-02", exception.CodeRegle);
        Assert.Null(collaborateur.DateSortie);
    }

    [Fact]
    public void Modifier_une_fiche_avec_une_sortie_anterieure_a_l_entree_est_refuse()
    {
        var collaborateur = CreerCollaborateur();

        var exception = Assert.Throws<RegleMetierException>(() =>
            collaborateur.Modifier("Durand", "Camille", "camille.durand@chronos.example", "Cheffe de projet",
                Entree.AddDays(-1), managerId: null));

        Assert.Equal("RG-02", exception.CodeRegle);
        Assert.Equal("Développeuse", collaborateur.Poste);
    }

    [Fact]
    public void Modifier_une_fiche_remplace_l_identite_le_poste_la_sortie_et_le_manager()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.Modifier("Durand-Martin", "Camille", "c.durand@chronos.example", "Cheffe de projet",
            Entree.AddDays(300), managerId: 1);

        Assert.Equal("Durand-Martin", collaborateur.Nom);
        Assert.Equal("c.durand@chronos.example", collaborateur.Email);
        Assert.Equal("Cheffe de projet", collaborateur.Poste);
        Assert.Equal(Entree.AddDays(300), collaborateur.DateSortie);
        Assert.Equal(1, collaborateur.ManagerId);
    }

    [Fact]
    public void Le_matricule_est_enregistre_en_majuscules()
    {
        var collaborateur = new Collaborateur(" m0002 ", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", Entree);

        Assert.Equal("M0002", collaborateur.Matricule);
    }

    [Fact]
    public void L_adresse_electronique_est_enregistree_en_minuscules()
    {
        var collaborateur = new Collaborateur("M0002", "Durand", "Camille", "Camille.Durand@Chronos.Example",
            CategorieCollaborateur.Consultant, "Développeuse", Entree);

        Assert.Equal("camille.durand@chronos.example", collaborateur.Email);
    }

    [Fact]
    public void Modifier_une_fiche_enregistre_l_adresse_electronique_en_minuscules()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.Modifier("Durand", "Camille", "C.Durand@Chronos.Example", "Développeuse",
            dateSortie: null, managerId: null);

        Assert.Equal("c.durand@chronos.example", collaborateur.Email);
    }

    [Fact]
    public void Rattacher_un_collaborateur_a_un_manager_renseigne_son_manager()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.RattacherA(1);

        Assert.Equal(1, collaborateur.ManagerId);
    }

    [Fact]
    public void Rattacher_un_collaborateur_a_personne_retire_son_manager()
    {
        var collaborateur = CreerCollaborateur();
        collaborateur.RattacherA(1);

        collaborateur.RattacherA(null);

        Assert.Null(collaborateur.ManagerId);
    }

    [Fact]
    public void Archiver_un_collaborateur_le_marque_archive_sans_toucher_a_sa_fiche()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.Archiver();

        Assert.True(collaborateur.EstArchive);
        Assert.Equal("M0002", collaborateur.Matricule);
        Assert.Null(collaborateur.DateSortie);
    }

    [Fact]
    public void Archiver_deux_fois_laisse_le_collaborateur_archive()
    {
        var collaborateur = CreerCollaborateur();
        collaborateur.Archiver();

        collaborateur.Archiver();

        Assert.True(collaborateur.EstArchive);
    }

    [Fact]
    public void Un_contrat_ajoute_est_visible_dans_l_historique()
    {
        var collaborateur = CreerCollaborateur();

        collaborateur.AjouterContrat(TypeContrat.Cdi, 1m, Entree);

        var contrat = Assert.Single(collaborateur.Contrats);
        Assert.Equal(TypeContrat.Cdi, contrat.Type);
        Assert.Equal(1m, contrat.Quotite);
    }

    [Fact]
    public void Un_contrat_de_quotite_hors_des_bornes_est_refuse_et_n_est_pas_ajoute()
    {
        var collaborateur = CreerCollaborateur();

        var exception = Assert.Throws<RegleMetierException>(() =>
            collaborateur.AjouterContrat(TypeContrat.Cdi, 0.05m, Entree));

        Assert.Equal("RG-03", exception.CodeRegle);
        Assert.Empty(collaborateur.Contrats);
    }

    [Fact]
    public void Un_passage_de_100_a_80_pour_cent_ne_change_pas_la_quotite_d_avant()
    {
        var collaborateur = CreerCollaborateur();
        var changement = new DateOnly(2026, 7, 1);
        collaborateur.AjouterContrat(TypeContrat.Cdi, 1m, Entree);

        collaborateur.AjouterContrat(TypeContrat.Cdi, 0.8m, changement);

        Assert.Equal(2, collaborateur.Contrats.Count);
        Assert.Equal(1m, collaborateur.QuotiteAu(Entree));
        Assert.Equal(1m, collaborateur.QuotiteAu(changement.AddDays(-1)));
        Assert.Equal(0.8m, collaborateur.QuotiteAu(changement));
        Assert.Equal(0.8m, collaborateur.QuotiteAu(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Sans_contrat_a_la_date_demandee_la_quotite_est_nulle()
    {
        var collaborateur = CreerCollaborateur();
        collaborateur.AjouterContrat(TypeContrat.Cdi, 1m, Entree);

        Assert.Equal(0m, collaborateur.QuotiteAu(Entree.AddDays(-1)));
    }

    [Fact]
    public void Un_nouveau_contrat_ne_commence_pas_avant_le_contrat_en_cours()
    {
        var collaborateur = CreerCollaborateur();
        collaborateur.AjouterContrat(TypeContrat.Cdi, 1m, Entree);

        Assert.Throws<InvalidOperationException>(() =>
            collaborateur.AjouterContrat(TypeContrat.Cdi, 0.8m, Entree));
    }

    private static void VerifierQueLaVersionChange(Action<Collaborateur> modification)
    {
        var collaborateur = CreerCollaborateur();
        var avant = collaborateur.Version;

        modification(collaborateur);

        Assert.NotEqual(avant, collaborateur.Version);
    }

    [Fact]
    public void Modifier_renouvelle_la_version() =>
        VerifierQueLaVersionChange(c =>
            c.Modifier("Durand", "Camille", "camille.durand@chronos.example", "Cheffe de projet", null, null));

    [Fact]
    public void DeclarerSortie_renouvelle_la_version() =>
        VerifierQueLaVersionChange(c => c.DeclarerSortie(Entree.AddDays(30)));

    [Fact]
    public void RattacherA_renouvelle_la_version() =>
        VerifierQueLaVersionChange(c => c.RattacherA(1));

    [Fact]
    public void Archiver_renouvelle_la_version() =>
        VerifierQueLaVersionChange(c => c.Archiver());

    [Fact]
    public void AjouterContrat_renouvelle_la_version() =>
        VerifierQueLaVersionChange(c => c.AjouterContrat(TypeContrat.Cdi, 1m, Entree));

    [Fact]
    public void Une_modification_refusee_ne_renouvelle_pas_la_version()
    {
        var collaborateur = CreerCollaborateur();
        var avant = collaborateur.Version;

        Assert.Throws<RegleMetierException>(() => collaborateur.DeclarerSortie(Entree.AddDays(-1)));

        Assert.Equal(avant, collaborateur.Version);
    }
}

using Chronos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public static class DonneesInitiales
{
    public static void Alimenter(DbContext context)
    {
        if (context.Set<Collaborateur>().Any())
        {
            return;
        }

        var (responsable, client) = AjouterReferentiel(context);
        context.SaveChanges();

        context.AddRange(CreerProjets(responsable.Id, client.Id));
        context.Add(CreerCamille(responsable.Id));
        context.Add(new JourFerie(new DateOnly(2026, 5, 8), "Victoire 1945"));
        context.SaveChanges();
    }

    public static async Task AlimenterAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<Collaborateur>().AnyAsync(cancellationToken))
        {
            return;
        }

        var (responsable, client) = AjouterReferentiel(context);
        await context.SaveChangesAsync(cancellationToken);

        context.AddRange(CreerProjets(responsable.Id, client.Id));
        context.Add(CreerCamille(responsable.Id));
        context.Add(new JourFerie(new DateOnly(2026, 5, 8), "Victoire 1945"));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static (Collaborateur Responsable, Client Client) AjouterReferentiel(DbContext context)
    {
        var sophie = new Collaborateur("M0001", "Martin", "Sophie", "sophie.martin@chronos.example",
            CategorieCollaborateur.Interne, "Directrice de projets", new DateOnly(2019, 9, 2));
        sophie.AjouterContrat(TypeContrat.Cdi, 1m, new DateOnly(2019, 9, 2));
        var client = new Client("Banque du Littoral");

        context.AddRange(sophie, client);
        return (sophie, client);
    }

    private static Collaborateur CreerCamille(int managerId)
    {
        var camille = new Collaborateur("M0002", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", new DateOnly(2025, 9, 1));
        camille.RattacherA(managerId);
        camille.AjouterContrat(TypeContrat.Cdi, 0.8m, new DateOnly(2025, 9, 1));

        return camille;
    }

    private static Projet[] CreerProjets(int responsableId, int clientId) =>
    [
        new("BDL-REFONTE", "Refonte du portail client", NatureProjet.Facturable, clientId,
            new DateOnly(2026, 3, 2), new DateOnly(2026, 12, 18), 240, responsableId),
        new("INT-FORMATION", "Formation interne", NatureProjet.Interne, null,
            new DateOnly(2026, 1, 5), null, 60, responsableId),
    ];
}

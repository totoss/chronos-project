using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Chronos.Infrastructure.Tracabilite;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Securite;

/// <summary>
/// RG-50 et RG-51 : les collaborateurs dont l'utilisateur peut voir les données. Le périmètre vient de la
/// hiérarchie enregistrée en base, pas du rôle <c>Manager</c> : qui n'encadre personne ne voit que lui-même.
/// </summary>
public sealed class PerimetreUtilisateur(IUtilisateurCourant utilisateur, ChronosDbContext db)
{
    /// <summary>
    /// Seul le gestionnaire RH voit tout le monde (cahier des charges § 2). L'administrateur gère les comptes
    /// et les rôles, pas les données des collaborateurs : il n'a que le périmètre de sa propre fiche, s'il en a une.
    /// </summary>
    public bool VoitTout => utilisateur.APourRole(Roles.GestionnaireRh);

    public bool EstMoi(int collaborateurId) => utilisateur.CollaborateurId == collaborateurId;

    public async Task<IReadOnlySet<int>> IdentifiantsVisiblesAsync(CancellationToken ct = default)
    {
        var visibles = new HashSet<int>();
        if (utilisateur.CollaborateurId is not { } moi)
        {
            return visibles;
        }

        visibles.Add(moi);

        // Les collaborateurs archivés restent des maillons de la chaîne : sans eux, l'équipe indirecte serait coupée.
        var liens = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .Where(c => c.ManagerId != null)
            .Select(c => new { c.Id, ManagerId = c.ManagerId!.Value })
            .ToListAsync(ct);
        var equipes = liens.ToLookup(l => l.ManagerId, l => l.Id);

        // Un identifiant déjà vu n'est pas reparcouru : une chaîne circulaire ne boucle pas.
        var aVisiter = new Queue<int>([moi]);
        while (aVisiter.TryDequeue(out var manager))
        {
            foreach (var membre in equipes[manager])
            {
                if (visibles.Add(membre))
                {
                    aVisiter.Enqueue(membre);
                }
            }
        }

        return visibles;
    }

    public async Task<bool> PeutVoirAsync(int collaborateurId, CancellationToken ct = default) =>
        VoitTout || (await IdentifiantsVisiblesAsync(ct)).Contains(collaborateurId);

    /// <summary>RG-36 : le manager direct du collaborateur ou un gestionnaire RH, jamais le collaborateur lui-même.</summary>
    public async Task<bool> PeutValiderPourAsync(int collaborateurId, CancellationToken ct = default)
    {
        if (utilisateur.CollaborateurId is not { } moi || collaborateurId == moi)
        {
            return false;
        }

        return utilisateur.APourRole(Roles.GestionnaireRh)
            || await db.Collaborateurs.AnyAsync(c => c.Id == collaborateurId && c.ManagerId == moi, ct);
    }
}

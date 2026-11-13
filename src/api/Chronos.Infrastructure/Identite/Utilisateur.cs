using Chronos.Domain;
using Microsoft.AspNetCore.Identity;

namespace Chronos.Infrastructure.Identite;

/// <summary>
/// Un compte de connexion. Il <em>référence</em> un collaborateur sans se confondre avec lui : un administrateur
/// peut n'être rattaché à aucune fiche, et le domaine ne connaît rien d'Identity.
/// </summary>
public class Utilisateur : IdentityUser<int>
{
    public int? CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }
}

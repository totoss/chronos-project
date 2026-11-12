using System.Globalization;
using System.Security.Claims;
using Chronos.Infrastructure.Tracabilite;

namespace Chronos.Api.Securite;

/// <summary>
/// La seule classe qui touche à <see cref="IHttpContextAccessor"/> : le reste du code dépend de
/// <see cref="IUtilisateurCourant"/>, qui se remplace facilement dans un test.
/// </summary>
public sealed class UtilisateurCourant(IHttpContextAccessor accesseur) : IUtilisateurCourant
{
    private ClaimsPrincipal? Principal => accesseur.HttpContext?.User;

    public string Identifiant => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "systeme";

    public int? CollaborateurId =>
        int.TryParse(Principal?.FindFirstValue(ClaimsChronos.CollaborateurId), CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public bool APourRole(string role) => Principal?.IsInRole(role) ?? false;
}

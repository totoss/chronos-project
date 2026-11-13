using System.Globalization;
using System.Security.Claims;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Chronos.Api.Securite;

/// <summary>Ajoute au cookie l'identifiant du collaborateur, pour ne pas le relire en base à chaque requête.</summary>
public sealed class FabriqueDeClaims(UserManager<Utilisateur> userManager,
    RoleManager<IdentityRole<int>> roleManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<Utilisateur, IdentityRole<int>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Utilisateur user)
    {
        var identite = await base.GenerateClaimsAsync(user);
        if (user.CollaborateurId is { } collaborateurId)
        {
            identite.AddClaim(new Claim(ClaimsChronos.CollaborateurId,
                collaborateurId.ToString(CultureInfo.InvariantCulture)));
        }

        return identite;
    }
}

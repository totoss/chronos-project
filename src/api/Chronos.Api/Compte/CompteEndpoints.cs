using System.Security.Claims;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Chronos.Infrastructure.Tracabilite;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Compte;

public record MoiDto(int? CollaborateurId, string? Nom, string? Prenom, string Email, IReadOnlyList<string> Roles);

public static class CompteEndpoints
{
    public const string CookieJetonCsrf = "XSRF-TOKEN";

    public static RouteGroupBuilder MapCompte(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/compte").WithTags("Compte");

        groupe.MapGet("/moi", Moi);
        groupe.MapPost("/logout", Deconnecter);

        return groupe;
    }

    private static async Task<Ok<MoiDto>> Moi(
        ClaimsPrincipal principal, IUtilisateurCourant utilisateur, ChronosDbContext db,
        IAntiforgery antiforgery, HttpContext contexte, CancellationToken ct)
    {
        // Le jeton anti-CSRF est lié à l'utilisateur : il est émis ici, aux moments où l'interface en a besoin.
        var jetons = antiforgery.GetAndStoreTokens(contexte);
        contexte.Response.Cookies.Append(CookieJetonCsrf, jetons.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = contexte.Request.IsHttps,
            Path = "/",
        });

        var identite = await db.Collaborateurs
            .Where(c => c.Id == utilisateur.CollaborateurId)
            .Select(c => new { c.Nom, c.Prenom })
            .FirstOrDefaultAsync(ct);

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Order().ToList();
        return TypedResults.Ok(new MoiDto(utilisateur.CollaborateurId, identite?.Nom, identite?.Prenom,
            principal.FindFirstValue(ClaimTypes.Email) ?? "", roles));
    }

    // La forme recommandée par la documentation : la requête porte un corps JSON, même vide (« {} »).
    private static async Task<Results<NoContent, BadRequest>> Deconnecter(
        SignInManager<Utilisateur> signInManager, HttpContext contexte, [FromBody] object? vide)
    {
        if (vide is null)
        {
            return TypedResults.BadRequest();
        }

        await signInManager.SignOutAsync();
        contexte.Response.Cookies.Delete(CookieJetonCsrf);
        return TypedResults.NoContent();
    }
}

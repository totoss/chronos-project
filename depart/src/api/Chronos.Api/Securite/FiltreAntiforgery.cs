// TODO [module 12] Exiger le jeton anti-CSRF sur toute écriture : la classe implémente le filtre d'endpoint d'ASP.NET Core (interface
//    IEndpointFilter) et reçoit le service anti-CSRF. Lectures (GET, HEAD, OPTIONS) laissées passer ; pour toute autre méthode, valider
//    la requête (jeton dans l'en-tête X-XSRF-TOKEN, lié à l'utilisateur) et répondre 400 « Jeton anti-CSRF absent ou invalide »
//    en cas d'échec. Appliqué au groupe racine dans Program.cs. Classe publique et scellée.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 5 « Le jeton anti-CSRF »

namespace Chronos.Api.Securite;

public sealed class FiltreAntiforgery
{
}

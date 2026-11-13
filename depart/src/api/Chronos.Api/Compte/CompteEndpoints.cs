// Les routes du compte connecté, groupe « /api/compte » (étiquette OpenAPI « Compte »), pour ce qu'Identity ne fournit pas.
// La méthode d'extension MapCompte(this IEndpointRouteBuilder) renvoie son groupe ; elle n'est pas déclarée ici.
//
// TODO [module 12] MapCompte : GET /moi (200) dit à l'interface qui est connecté : identifiant de sa fiche (facultatif), nom et prénom
//    (absents si le compte n'a pas de fiche), adresse électronique et rôles triés. Le type de réponse MoiDto est déclaré dans ce
//    fichier. POST /logout (204) ferme la session ; la requête doit porter un corps JSON, même vide.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 3 « Tout protéger, n'exposer que la connexion »
// TODO [module 12] « /moi » émet aussi le jeton anti-CSRF : cookie lisible par l'interface (non HttpOnly), nom « XSRF-TOKEN » (constante
//    publique de la classe), SameSite strict, sécurisé seulement si la requête l'est, chemin racine ; « /logout » le supprime.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 5 « Le jeton anti-CSRF »

namespace Chronos.Api.Compte;

public record MoiDto;

public static class CompteEndpoints
{
}

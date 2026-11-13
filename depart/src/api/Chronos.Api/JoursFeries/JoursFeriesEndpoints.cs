// Les routes de la ressource « jours fériés », groupe « /api/jours-feries » (étiquette OpenAPI « Jours fériés »).
// La méthode d'extension MapJoursFeries(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
//
// TODO [module 09] Écrire GET, POST et DELETE sur « /api/jours-feries » : liste triée par date, création (201), suppression (204 ou 404). Choisir la clé
//    d'un jour férié dans l'URL et la réponse à une seconde création pour la même date (voir les questions de l'exercice), en s'appuyant
//    sur la lecture de date de Commun/DatesIso.cs.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 2 « Les jours fériés. »
// TODO [module 12] Appliquer aux écritures la politique que le § 2 du cahier des charges désigne pour le calendrier ; GET reste ouvert à tout utilisateur connecté.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 1 « Protéger le reste. »

namespace Chronos.Api.JoursFeries;

public static class JoursFeriesEndpoints
{
}

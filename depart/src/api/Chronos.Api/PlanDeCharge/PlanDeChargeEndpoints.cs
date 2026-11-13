// La route du plan de charge, groupe « /api/plan-de-charge » (étiquette OpenAPI « Plan de charge »).
// La méthode d'extension MapPlanDeCharge(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
//
// TODO [module 09] GET / renvoie le plan de charge (RG-22 à RG-25) en un nombre fixe de requêtes, sans requête par collaborateur : paramètres
//    facultatifs « du » et « au » (400 si illisibles ; par défaut la semaine de l'horloge, donc TimeProvider ; période d'un an
//    au plus), « pas » (« semaine » ou « mois », 400 sinon) et « managerId ». Les absences n'existent pas encore : le calcul
//    en reçoit une liste vide.
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` » (découpage : étape 8 « Découper une période » ; semaine par défaut : étape 7 « « La semaine en cours » »)
// TODO [module 09] Lire les dates du et au sans ambiguïté, avec la fonction de Commun/DatesIso.cs (400 sinon) : « 04/05/2026 » ne doit pas être lu de travers.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 5 « Une date mal écrite. »
// TODO [module 10] Ajouter la requête des absences approuvées qui touchent la période et les passer au calcul (RG-22).
//    — cours/10-temps-et-absences.md, Pas à pas, étape 9 « Les absences entrent dans la capacité »
// TODO [module 12] RG-50 et RG-51 : seuls les collaborateurs du périmètre de l'utilisateur figurent au plan, quel que soit le manager demandé
//    (voir la question de l'exercice sur le plan d'un manager étranger au périmètre).
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 2 « Le périmètre partout. »

namespace Chronos.Api.PlanDeCharge;

public static class PlanDeChargeEndpoints
{
}

// Les routes de la ressource « affectations », groupe « /api/affectations » (étiquette OpenAPI « Affectations »).
// La méthode d'extension MapAffectations(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
//
// TODO [module 09] POST / crée une affectation : charge le collaborateur et le projet désignés (400 si l'un des deux n'existe pas), appelle la création
//    du domaine, enregistre et répond 201. Les règles violées répondent 422 (RG-20 période, RG-21 taux, RG-13 projet terminé ou
//    annulé), grâce au gestionnaire d'exceptions du module 08.
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` » (passage « Il reste à créer des affectations »)
// TODO [module 09] Compléter : GET / (paramètres facultatifs collaborateurId et projetId), GET /{id:int}, PUT /{id:int} (le domaine revérifie
//    RG-13, RG-20 et RG-21 sans dupliquer la création) et DELETE /{id:int} (204 ou 404).
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 1 « Les affectations au complet. »
// TODO [module 09] RG-24 : une affectation qui met le collaborateur en surcharge sur au moins une semaine répond 422 avec la règle « RG-24 »
//    tant que la requête ne porte pas « surchargeConfirmee » à vrai ; le calcul, celui du plan de charge, inclut l'affectation
//    en cours de création ou de modification.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 3 « Confirmer une surcharge (RG-24). »
// TODO [module 10] Le calcul de surcharge reçoit les absences approuvées du collaborateur (RG-22) : une absence peut mettre en surcharge.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 9 « Les absences entrent dans la capacité »
// TODO [module 12] Appliquer aux écritures la politique que le § 2 du cahier des charges désigne ; limiter lectures et écritures au périmètre de l'utilisateur
//    (RG-50, RG-51) : la liste ne contient que les collaborateurs du périmètre, et ce qui est hors périmètre se traite comme une fiche
//    hors périmètre (étape 7).
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 1 « Protéger le reste. » et exercice 2 « Le périmètre partout. »

namespace Chronos.Api.Affectations;

public static class AffectationsEndpoints
{
}

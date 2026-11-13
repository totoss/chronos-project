// Les routes des absences, groupe « /api/absences » (étiquette OpenAPI « Absences »).
// La méthode d'extension MapAbsences(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
// Chaque transition est un POST vers un nom d'action, jamais une modification directe du statut.
//
// TODO [module 10] POST / demande une absence : 201 avec l'adresse de la ressource ; 400 si le type est inconnu ; 422 pour RG-42 (une absence couvre au moins
//    une demi-journée ouvrée, calendrier des jours fériés compris) et RG-41 (pas de chevauchement avec une autre absence du même
//    collaborateur). RG-41 lit puis écrit : toute l'opération se déroule dans une transaction sérialisable que la stratégie
//    d'exécution rejoue si la base la fait échouer.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 7 « RG-41 : deux demandes au même instant »
// TODO [module 10] POST /{id:int}/approbation (RG-43 : refus 422 si du temps validé couvre déjà la période ; transaction sérialisable),
//    POST /{id:int}/refus et POST /{id:int}/annulation (RG-40), chacun 200, 404 ou 422 selon le cycle de l'absence.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 8 « RG-43 : approuver une absence »
// TODO [module 10] Compléter la lecture : GET / (paramètre facultatif collaborateurId) et GET /{id:int} (200 ou 404).
//    — cours/10-temps-et-absences.md, À vous, exercice 1 « Compléter l'API. »
// TODO [module 12] Réserver l'approbation et le refus à la politique de validation, et les ouvrir seulement au manager direct du demandeur ou à un
//    gestionnaire RH (RG-36 : 403 sinon, 404 hors périmètre), la décision étant tracée au nom du valideur ; limiter lectures et
//    liste au périmètre (RG-50, RG-51), en traitant le hors-périmètre comme une fiche hors périmètre (étape 7) ; le demandeur est
//    l'utilisateur connecté (403 pour un compte sans fiche) ; l'annulation revient au seul demandeur (RG-40, 403 pour les autres).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre » ; À vous, exercice 2 « Le périmètre partout. » et exercice 3 « RG-36 jusqu'au bout. »

namespace Chronos.Api.Absences;

public static class AbsencesEndpoints
{
}

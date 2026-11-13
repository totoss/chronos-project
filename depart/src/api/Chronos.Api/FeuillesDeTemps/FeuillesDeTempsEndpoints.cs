// Les routes des feuilles de temps, groupe « /api/feuilles-de-temps » (étiquette OpenAPI « Feuilles de temps »).
// La méthode d'extension MapFeuillesDeTemps(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
// Chaque transition du cycle est un POST vers un nom d'action, jamais un PUT du statut : le client ne choisit pas l'état d'arrivée.
//
// TODO [module 10] GET / lit la feuille d'un collaborateur pour une semaine (paramètres collaborateurId et semaine, un lundi au format ISO ; 400
//    sinon) et la crée si elle n'existe pas, en brouillon et vide (un GET qui écrit : compromis assumé par le cours).
//    — cours/10-temps-et-absences.md, Pas à pas, étape 4 « Les endpoints de la feuille »
// TODO [module 10] PUT /{id:int}/saisies remplace toutes les saisies (200, 404, 422 pour RG-30 à RG-33, RG-13 et RG-35), POST
//    /{id:int}/soumission (200, 404, 422), POST /{id:int}/validation et POST /{id:int}/rejet (200, 404, 422) avec, au module 10, l'identifiant du valideur
//    dans la requête (RG-36, seule sa seconde phrase : nul ne valide sa propre feuille). Enregistrer l'agrégat feuille et ses saisies d'un seul
//    enregistrement.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 3 « Enregistrer l'agrégat » et étape 4 « Les endpoints de la feuille »
// TODO [module 10] GET /soumises liste les feuilles à valider (statut soumise), sans jamais en créer.
//    — cours/10-temps-et-absences.md, À vous, exercice 1 « Compléter l'API. »
// TODO [module 12] RG-52 et RG-36 : la validation, le rejet et la liste des feuilles à valider exigent la politique de validation (403 sinon) ;
//    la lecture sans collaborateurId donne la feuille de l'utilisateur connecté (400 s'il n'a pas de fiche), celle d'un autre n'est lisible
//    que dans le périmètre (404 sinon, RG-50 et RG-51) ; chacun saisit et soumet ses propres temps (403 pour la feuille d'un autre du
//    périmètre) ; valider ou rejeter la feuille d'un autre est réservé à son manager direct ou à un gestionnaire RH (403 sinon,
//    404 hors périmètre) ; le valideur est l'utilisateur courant et les requêtes ne portent plus son identifiant ;
//    la liste des feuilles à valider exclut la sienne et se limite à son équipe directe, sauf pour le gestionnaire RH.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre » et À vous, exercice 2 « Le périmètre partout. » et exercice 3 « RG-36 jusqu'au bout. »
// TODO [module 25] Journaliser, avec les messages de JournalFeuillesDeTemps, la validation refusée (avec la règle), la feuille validée et la
//    feuille rejetée (avec l'identifiant technique du valideur) : les gestionnaires reçoivent un journal typé sur Program,
//    car une classe statique ne peut pas servir de type de catégorie.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 1 « Regrouper les messages d'une ressource »
// TODO [module 25] Compter les décisions : appeler la métrique des feuilles de temps après chaque validation et chaque rejet réussis.
//    — cours/25-exploiter-l-application.md, À vous, exercice 3 « Une métrique métier. »

namespace Chronos.Api.FeuillesDeTemps;

public static class FeuillesDeTempsEndpoints
{
}

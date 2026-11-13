// Les routes de la ressource « clients », groupe « /api/clients » (étiquette OpenAPI « Clients »).
// La méthode d'extension MapClients(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici.
//
// TODO [module 03] MapClients : GET / (liste), GET /{id:int} (200 ou 404), POST / (201), sur le modèle des projets, avec un
//    stockage provisoire en mémoire, ClientsEnMemoire.cs, à créer dans ce dossier puis supprimé au module 06 (non présent ici).
//    — cours/03-premiere-api.md, À vous, exercice 4 « Une deuxième ressource. »
// TODO [module 06] Servir les clients depuis la base : supprimer le stockage provisoire (fichier et enregistrement dans Program.cs),
//    réécrire la liste (triée par nom, un tableau simple, non paginé), la lecture et la création avec le contexte ;
//    chaque client porte le nombre de ses projets en cours, en une seule requête SQL.
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 6 « Compter sans charger. »
// TODO [module 08] Compléter en CRUD : PUT /{id:int} (200 ou 404) et DELETE /{id:int} (204 ou 404 ; le cas d'un client qui a des
//    projets reçoit une réponse décidée avant d'appeler la base).
//    — cours/08-une-api-robuste.md, À vous, exercice 2 « Les clients. »
// TODO [module 12] Appliquer aux écritures (POST, PUT, DELETE) la politique que le § 2 du cahier des charges désigne ; les lectures restent ouvertes à tout utilisateur connecté.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 1 « Protéger le reste. »

namespace Chronos.Api.Clients;

public static class ClientsEndpoints
{
}

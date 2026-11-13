// Les routes de la ressource « projets », groupe « /api/projets » (étiquette OpenAPI « Projets »).
// La méthode d'extension MapProjets(this IEndpointRouteBuilder) renvoie son groupe ; ses gestionnaires sont des méthodes
// privées. Elle n'est pas déclarée ici : chaque module y ajoute ses routes.
//
// TODO [module 03] MapProjets : GET / (liste, 200, filtre facultatif par statut), GET /{id:int} (200 ou 404), POST / (201 avec
//    l'adresse de la ressource créée), DELETE /{id:int} (204 ou 404). Les réponses sont typées par TypedResults. Les données
//    viennent d'un stockage provisoire en mémoire, ProjetsEnMemoire.cs, à créer dans ce dossier à l'étape 7 puis supprimé
//    au module 06 (non présent ici).
//    — cours/03-premiere-api.md, Pas à pas, étape 7 « Une liste en mémoire, fournie par injection », étape 8 « Les endpoints de lecture » et étape 9 « L'endpoint de création »
// TODO [module 04] Les énumérations viennent désormais du domaine : retoucher les imports.
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 10 « Brancher l'API sur le domaine »
// TODO [module 06] Servir les projets depuis la base (contexte EF Core) : la liste devient paginée (réponse PageDto : éléments,
//    page, taille, total), la correspondance entité vers DTO s'écrit une fois, en projection. POST répond 409 si le code
//    existe déjà (RG-10). Les projets d'un responsable archivé restent lisibles (désactiver le filtre d'archivage, RG-05).
//    Le DELETE provisoire disparaît.
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 9 « Servir `/api/projets` depuis la base » et étape 11 « Archiver sans supprimer (RG-05) »
// TODO [module 07] Le domaine normalise le code (majuscules) : construire le projet avant le contrôle d'unicité et comparer le
//    code normalisé, pour que PostgreSQL et SQL Server donnent la même réponse (RG-10).
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] Compléter la ressource : PUT /{id:int} avec jeton de concurrence (200, 404, 409 si la version est périmée),
//    filtres « recherche » et « statut », tri à liste blanche, réponses 400 homogènes ; RG-11 et RG-12 en 422 avec la
//    règle en cause ; client ou responsable inconnu en 400 ; POST /{id:int}/demarrage, /achevement et /annulation
//    (200, 404 ; réponse à décider si la transition est impossible : un nom d'action, non un nouveau statut).
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. », exercice 3 « Changer le statut d'un projet. » et exercice 5 « Finir RG-04. »
// TODO [module 10] La projection du DTO reçoit le contexte pour calculer, côté base et sans requête par projet, le total des
//    jours validés des feuilles de temps.
//    — cours/10-temps-et-absences.md, À vous, exercice 4 « Le temps consommé. »
// TODO [module 12] Appliquer aux écritures (création, modification, transitions) la politique que le § 2 du cahier des charges désigne ; la lecture
//    reste ouverte à tout utilisateur connecté.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 1 « Protéger le reste. »
// TODO [module 17] Le 409 de doublon de code porte la règle « RG-10 » (extension « regle » du problème) pour que l'interface
//    ne propose pas de recharger la ressource.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)
// TODO [module 22] La recherche insensible à la casse est traduite en SQL par EF Core, jamais exécutée en .NET : supprimer
//    localement les avertissements CA1304, CA1311 et CA1862 qu'elle déclenche, en écrivant la raison à côté.
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Projets;

public static class ProjetsEndpoints
{
}

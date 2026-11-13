// Les routes de la ressource « collaborateurs », groupe « /api/collaborateurs » (étiquette OpenAPI « Collaborateurs »).
// La méthode d'extension MapCollaborateurs(this IEndpointRouteBuilder) renvoie son groupe. Elle n'est pas déclarée ici :
// chaque module y ajoute ses routes.
//
// TODO [module 06] MapCollaborateurs : GET / renvoie une page de CollaborateurDto (paramètres page et taille ; identité, catégorie,
//    poste, nom du manager nul s'il n'y en a pas, quotité du contrat en cours sans date de fin, 0 s'il n'y en a pas), avec la
//    correspondance entité vers DTO écrite une seule fois, sans lire l'horloge. Le journal d'EF Core ne doit montrer qu'une requête.
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 3 « Lister les collaborateurs. »
// TODO [module 06] DELETE /{id:int} archive le collaborateur au lieu de le supprimer (204 ; 404 pour un second appel ou un
//    identifiant inconnu), par la méthode Archiver du domaine (RG-05).
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 4 « Archiver. »
// TODO [module 07] La liste accepte un paramètre « recherche » sur le nom ou le prénom, insensible à la casse, avec le même résultat sur
//    PostgreSQL et sur SQL Server (comparer le SQL produit).
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 2 « Une recherche portable. »
// TODO [module 07] Le domaine normalise le matricule (majuscules) et l'adresse (minuscules) : construire le collaborateur avant le
//    contrôle d'unicité et comparer les valeurs normalisées.
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] GET /{id:int} lit une fiche (200 ou 404).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 3 « Le même format pour toutes les erreurs »
// TODO [module 08] POST / crée un collaborateur : 201 avec l'adresse de la fiche, 409 si le matricule ou l'adresse existent déjà
//    (RG-01, y compris pour un collaborateur archivé), 422 si le domaine refuse (RG-02, RG-03), 400 si le manager est
//    inconnu. La réponse d'erreur « conflit » vient de Commun/Reponses.cs, « invalide » aussi.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 » ; manager inconnu : À vous, exercice 5 « Finir RG-04. »
// TODO [module 08] La liste se pagine avec bornes, se filtre (recherche, statut « actif » ou « sorti » selon la date du jour fournie
//    par TimeProvider) et se trie à liste blanche (400 sinon).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 6 « Paginer, filtrer, trier »
// TODO [module 08] PUT /{id:int} modifie un collaborateur : 200, 404, 409 si la version envoyée est périmée (jeton de concurrence
//    comparé par la base) ou si la nouvelle adresse est déjà prise (RG-01), 422 si le domaine refuse (RG-02), 400 si le manager est inconnu.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 7 « Modifier sans écraser le travail d'un autre »
// TODO [module 08] RG-04 : à la modification, remonter la chaîne des managers et refuser (422, règle RG-04) toute boucle, même
//    indirecte, sans tourner indéfiniment ; les managers archivés comptent dans la chaîne.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 8 « RG-04 : pas de boucle dans la chaîne de managers »
// TODO [module 08] DELETE /{id:int} (méthode rebaptisée Supprimer) archive si le collaborateur a des données liées (RG-05) — ici :
//    des collaborateurs qu'il manage ou des projets dont il est responsable — et le supprime réellement sinon.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 9 « Supprimer ou archiver (RG-05) »
// TODO [module 09] La condition d'archivage gagne les affectations.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 1 « Les affectations au complet. »
// TODO [module 10] La condition d'archivage gagne les absences et les feuilles de temps (même vides, à ce stade : leur clé étrangère
//    interdirait la suppression).
//    — cours/10-temps-et-absences.md, À vous, exercice 1 « Compléter l'API. »
// TODO [module 12] RG-52 : réserver POST, PUT et DELETE au gestionnaire RH (politique de gestion RH) ; GET reste ouvert à tout utilisateur connecté.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 6 « Autoriser par rôle (RG-52) »
// TODO [module 12] RG-50 et RG-51 : la liste ne contient que les collaborateurs du périmètre de l'utilisateur ; une fiche hors
//    périmètre répond 404 comme un identifiant inconnu.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) »
// TODO [module 12] Deux formes de fiche : la version complète (CollaborateurRhDto, avec dates et quotité) pour le gestionnaire RH et
//    l'intéressé, la version réduite (CollaborateurDto) pour les autres ; la liste renvoie toujours la réduite. La création
//    et la modification renvoient la version complète.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 10 « Ne montrer que le nécessaire »
// TODO [module 16] Calculer l'indicateur « EstSorti » dans les deux projections (elles deviennent des méthodes qui reçoivent la date du
//    jour, fournie par TimeProvider), avec la règle du filtre « statut=sorti » ; ajouter les tests d'intégration correspondants.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs » (passage « Cet indicateur n'existe pas encore dans votre API », qui tient lieu d'encadré « Compléter le contrat de l'API »)
// TODO [module 17] Les 409 de doublon (matricule ou adresse, RG-01) portent la règle « RG-01 », pour que l'interface ne propose pas
//    de recharger la fiche.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)
// TODO [module 18] RG-05 ne compte plus une feuille de temps vide (brouillon sans saisie ni motif de rejet), que la lecture crée pour
//    chaque semaine consultée ; les feuilles vides restantes partent avec le collaborateur supprimé.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 8 « Enregistrer, puis soumettre » (encadré « Compléter le contrat de l'API. »)
// TODO [module 22] La recherche insensible à la casse est traduite en SQL par EF Core, jamais exécutée en .NET : supprimer
//    localement les avertissements CA1304, CA1311 et CA1862 qu'elle déclenche, en écrivant la raison à côté.
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Collaborateurs;

public static class CollaborateursEndpoints
{
}

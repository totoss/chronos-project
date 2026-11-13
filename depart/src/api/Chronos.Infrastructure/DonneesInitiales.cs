// Fichier d'assemblage : les données de démonstration, rangées par module. Classe statique de deux méthodes, une synchrone « Alimenter(contexte) » et une asynchrone
// « AlimenterAsync(contexte, jeton d'annulation) », parce que l'outil en ligne de commande appelle la première et « MigrateAsync » la seconde. Elles se déclarent dans les options du contexte
// (UseSeeding et UseAsyncSeeding) : voir Program.cs, dans Chronos.Api.

// TODO [module 06] Alimenter la base de développement : ne rien faire s'il existe déjà un collaborateur (la fonction est rappelée à chaque mise à jour de la base), sinon ajouter Sophie Martin
//    (matricule M0001, catégorie interne, directrice de projets, entrée le 2 septembre 2019) et le client « Banque du Littoral », enregistrer en DEUX temps (les identifiants sont générés au premier
//    enregistrement), puis deux projets : BDL-REFONTE (facturable, pour ce client, du 2 mars au 18 décembre 2026, 240 jours) et INT-FORMATION (interne, du 5 janvier 2026, sans fin, 60 jours), tous
//    deux sous la responsabilité de Sophie. Adapter aux constructeurs de vos entités
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 8 « Alimenter la base de développement »
// TODO [module 06] Ajouter Camille (contrat à 80 %, Sophie pour manager) et donner à Sophie un contrat à temps plein ; repartir d'une base vide pour les voir
//    — cours/06-persister-avec-ef-core.md, À vous, exercice 3 « Lister les collaborateurs. »
// TODO [module 08] Non détaillé dans le module : la solution de référence donne à Sophie son contrat à temps plein dans la création du référentiel (l'exercice 3 du module 06 le demandait déjà) ;
//    comparer avec la solution au tag module-08
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 »
// TODO [module 09] Ajouter le 8 mai 2026 aux jours fériés (« Victoire 1945 ») ; les deux affectations de Camille pour la semaine du 4 mai se créent par l'API, pas ici
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` »
// TODO [module 10] Donner à chaque donnée de démonstration sa propre condition (« si aucun collaborateur », « si la table des types d'absence est vide ») dans les deux méthodes, car la base est déjà
//    alimentée ; ajouter les trois types d'absence CP, RTT et MAL
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 12] Ajouter un gestionnaire RH (Hugo Bernard, matricule M0003, hugo.bernard@chronos.example, avec un contrat), dans un bloc qui vérifie d'abord que le matricule n'existe pas : une base déjà
//    alimentée le reçoit aussi. Les données initiales passent par l'intercepteur de traçabilité, au nom de « systeme »
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 11 « Des comptes pour le développement »
// TODO [module 25] Retirer les deux blocs « types d'absence » et la méthode qui les crée (ils passent dans les migrations) et documenter la classe : données de DÉMONSTRATION seulement, les données de
//    référence sont dans les migrations
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 10 « Données de référence : les types d'absence dans les migrations »

namespace Chronos.Infrastructure;

public static class DonneesInitiales
{
}

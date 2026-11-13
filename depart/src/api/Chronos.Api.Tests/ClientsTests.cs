// TODO [module 11] Tester les clients de bout en bout contre la vraie base : le cycle complet d'un client (création, lecture, modification, liste ;
//    la liste des clients est un simple tableau, non paginé) et le refus de supprimer un client qui a des projets (409). La classe
//    reçoit la fabrique d'application en fixture de classe (une base par classe) et ouvre son client dans l'initialisation asynchrone.
//    Se demander pour quelles règles un test d'intégration est justifié, et pour lesquelles le test unitaire suffit.
//    — cours/11-tests-d-integration.md, À vous, exercice 1 « Clients et projets. »
// TODO [module 12] Remplacer le client anonyme par un client connecté en gestionnaire RH (l'API est désormais protégée).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 »

// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class ClientsTests
{
}

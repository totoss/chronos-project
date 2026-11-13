// TODO [module 11] Tester les projets contre la vraie base. D'abord, une règle du domaine arrive en 422 avec le code de la règle : un projet facturable
//    sans client (RG-11). Ensuite, le cycle complet d'un projet (création, lecture, modification, liste paginée) et le code déjà
//    utilisé (409, RG-10). Enfin, la concurrence optimiste : deux modifications avec la même version : décider de ce que doit renvoyer la seconde.
//    — cours/11-tests-d-integration.md, Pas à pas, étape 5 « Les erreurs : 400, 409, 422 » ; À vous, exercice 1 « Clients et projets. » et exercice 3 « Concurrence optimiste. »
// TODO [module 12] Remplacer le client anonyme par un client connecté en gestionnaire RH.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 »
// TODO [module 17] Ajouter les cas du contrat complété : un doublon de code répond 409 avec la règle « RG-10 » et sans code ; une version périmée
//    répond 409 avec le code « concurrence » ; un champ obligatoire manquant à la création renvoie un message de validation en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class ProjetsTests
{
}

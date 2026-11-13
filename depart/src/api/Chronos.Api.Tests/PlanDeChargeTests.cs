// TODO [module 11] Tester le plan de charge par HTTP : reproduire l'exemple chiffré du cahier des charges (jour férié du 8 mai, absence du lundi, deux
//    affectations à 50 %) en appelant le plan de la semaine du 4 au 8 mai 2026 au pas « semaine », et vérifier capacité, charge et taux
//    décidés au module 09 ; sans paramètre, le plan porte sur la semaine de l'horloge pilotée. Se demander si ce test fait double emploi
//    avec les tests unitaires du calcul.
//    — cours/11-tests-d-integration.md, À vous, exercice 4 « L'exemple de Camille, par HTTP. »
// TODO [module 12] Remplacer le client anonyme par un client connecté (gestionnaire RH).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 »

// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class PlanDeChargeTests
{
}

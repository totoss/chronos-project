// TODO [module 12] Tester RG-36 sur les feuilles et les absences : le manager direct valide la feuille de son collaborateur ; un manager indirect la voit
//    mais ne la valide pas (403) ; un manager d'une autre équipe ne la voit pas (404) ; nul ne valide ni ne rejette sa propre feuille ; le
//    gestionnaire RH valide la feuille de n'importe qui sauf la sienne ; la liste des feuilles à valider est celle que l'appelant peut valider ;
//    la validation est tracée au nom du valideur ; le manager direct approuve et refuse les absences de son collaborateur ; seul le
//    demandeur annule son absence ; un compte sans fiche ne pose pas d'absence.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre » ; À vous, exercice 3 « RG-36 jusqu'au bout. »
// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class ValidationTests
{
}

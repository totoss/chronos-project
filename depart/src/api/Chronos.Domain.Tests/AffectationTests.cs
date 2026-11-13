// TODO [module 09] Tester RG-20, RG-21 et RG-13 sur le modèle du module 04 : une affectation valide est créée ; le taux d'allocation par une théorie aux bornes (0,04 refusé ; 0,05 et 1 acceptés ;
//    1,01 refusé, avec un paramètre decimal) ; une fin avant le début, un début avant celui du projet, une fin après celle du projet, un début avant l'entrée du collaborateur et une fin après sa
//    sortie sont refusés ; un projet sans date de fin accepte toute fin d'affectation ; un projet terminé ou annulé refuse l'affectation
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 1 « `Affectation` et `JourFerie` »
// TODO [module 09] Tester « Modifier » : elle remplace la période et le taux, revérifie les règles et laisse l'affectation intacte en cas de refus, refuse un projet terminé, refuse un autre
//    collaborateur ou un autre projet
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 1 « Les affectations au complet. »

namespace Chronos.Domain.Tests;

public class AffectationTests
{
}

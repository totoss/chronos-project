// TODO [module 09] Une théorie à données de membre (type de ligne TheoryDataRow, pas TheoryData ni IEnumerable d'objets : dépréciés en xUnit v3) sur un calendrier dont les jours fériés sont les
//    1er, 8 et 14 mai 2026 : le nombre de jours ouvrés d'une semaine avec un férié (4 au 10 mai : 4), d'une semaine complète (18 au 24 mai : 5), d'un week-end (9 et 10 mai : 0) et de mai 2026 (18).
//    « dotnet test » avec le filtre de classe exécute un test par ligne
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 2 « Le calendrier de travail »

namespace Chronos.Domain.Tests;

public class CalendrierDeTravailTests
{
}

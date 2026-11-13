// TODO [module 04] Tester l'objet valeur Periode. Une théorie : deux périodes se chevauchent si elles ont un jour commun, dans les deux sens (quatre cas en mai 2026 : 4-8 et 11-15 disjointes ;
//    4-8 et 8-12 communes ; 4-15 contient 6-7 ; 11-15 et 4-8 disjointes). Un test d'égalité : deux périodes de mêmes bornes sont égales (essayer de remplacer « readonly record struct » par
//    « class » pour voir le test échouer)
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 9 « Un objet valeur : `Periode` »
// TODO [module 04] Couvrir NombreDeJours (les deux bornes comptent) et Intersection (la partie commune, et rien pour deux périodes disjointes), écrits avant le code. Prévoir aussi qu'une période à
//    l'envers est refusée et qu'une période contient ses deux bornes mais pas le jour suivant
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 2 « Compléter `Periode`. »

namespace Chronos.Domain.Tests;

public class PeriodeTests
{
}

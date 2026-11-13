// TODO [module 10] Une saisie de temps : Id, FeuilleDeTempsId, ProjetId, Jour, Duree (decimal), accesseurs d'écriture privés. Le constructeur (projet, jour, durée) est INTERNE : seul le
//    projet du domaine crée une saisie, ni l'API ni les tests (ils passent par la feuille). RG-30 : la durée se saisit par quarts de journée (0,25 ; 0,5 ; 0,75 ou 1) ;
//    violation = RegleMetierException avec le code de la règle
//    — cours/10-temps-et-absences.md, Pas à pas, étape 2 « Les saisies : quatre règles en un seul endroit »

namespace Chronos.Domain;

public class SaisieDeTemps
{
}

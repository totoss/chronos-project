// TODO [module 09] Fournir l'unique lecture de date acceptée par l'API pour les paramètres de chaîne de requête et de chemin, sans dépendre de la culture :
//    une date mal écrite comme « 04/05/2026 » ne doit pas être lue de travers. La fonction est une méthode statique de tentative de lecture
//    (réussite ou échec, la date en sortie) ; ses utilisateurs répondent 400 quand elle échoue : le plan de charge (paramètres du et au),
//    la suppression d'un jour férié, et, au module 10, la lecture d'une feuille de temps par semaine. À toi de décider de la parade (voir la
//    question de l'exercice) et de la tester.
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 5 « Une date mal écrite. »

namespace Chronos.Api.Commun;

public static class DatesIso
{
}

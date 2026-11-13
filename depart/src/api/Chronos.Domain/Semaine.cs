// TODO [module 09] Fichier de trois types statiques. « Semaine » : une méthode « Contenant(jour) » qui renvoie la période du lundi au dimanche de la semaine du jour (attention :
//    DayOfWeek vaut 0 le dimanche). « Horloge » : une méthode d'extension « Aujourdhui » sur TimeProvider qui renvoie la date LOCALE du jour (pas la date UTC) ; elle sert à
//    toute règle « aujourd'hui », écrite à un seul endroit. Le domaine ne dépend toujours de rien. Tester avec l'horloge factice, dont un test de fuseau (dimanche 10 mai 2026
//    à 22 h 30 UTC, fuseau de Paris : déjà lundi 11)
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 7 « « La semaine en cours » »
// TODO [module 09] Ajouter à ce fichier le type statique « Decoupage » : « ParSemaine(période) » et « ParMois(période) » découpent une période en tranches (la première et la dernière
//    tranche sont tronquées aux bornes de la période). Du 6 au 19 mai 2026, par semaine : 6-10, 11-17, 18-19. Tester ce cas et un découpage par mois à cheval sur février
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 8 « Découper une période »

namespace Chronos.Domain;

public static class Semaine
{
}

public static class Horloge
{
}

public static class Decoupage
{
}

// TODO [module 09] Service métier PUR (classe scellée, construite avec le calendrier de travail) : il reçoit tout sous forme de données et ne touche ni base ni horloge. Méthode
//    « Calculer(dossier, période) » qui renvoie une ChargeSurPeriode, en raisonnant jour par jour sur les jours ouvrés de la période, dans la présence du collaborateur, en
//    decimal partout. RG-22 : capacité = pour chaque jour, (1 - part absente) x quotité. RG-23 : charge planifiée = somme, pour chaque affectation, des jours ouvrés couverts x
//    quotité x taux d'allocation (lecture précisée par le module, à ne pas écrire à la lettre du cahier des charges) ; les absences ne réduisent pas la charge. Taux de charge =
//    charge / capacité arrondi à quatre décimales, arrondi « loin de zéro », et ABSENT (pas zéro) quand la capacité est nulle. Surcharge = charge supérieure à la capacité
//    (comparaison des valeurs, pas du taux arrondi). RG-25 : intercontrat = consultant qui a de la capacité et aucune charge facturable.
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 4 « L'exemple du cahier des charges devient un test »
// TODO [module 09] Le test de l'étape 5 impose de corriger RG-23 : la charge tient compte de la quotité. Faire échouer le test avec la lecture littérale, puis le faire passer
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 5 « Le test qui pose la question »
// TODO [module 09] Gérer les cas limites listés par le module (entrée en milieu de semaine, quotité qui change, affectation partielle, demi-journée d'absence), le taux indéfini
//    sans capacité (pas de division par zéro) et l'intercontrat (RG-25)
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 6 « Les cas limites »
// TODO [module 09] RG-24 : méthode « MetEnSurcharge(dossier, affectation candidate) » qui dit si, une fois la candidate ajoutée au dossier, au moins une semaine de sa période est
//    en surcharge (découper la période par semaine avec Decoupage). Le calcul inclut l'affectation qu'on est en train de créer. Écrire d'abord le test du calcul
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 3 « Confirmer une surcharge (RG-24). »

namespace Chronos.Domain;

public sealed class CalculateurDeCharge
{
}

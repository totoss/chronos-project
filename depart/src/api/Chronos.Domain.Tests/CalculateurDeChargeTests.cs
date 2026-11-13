// TODO [module 09] Écrire d'abord l'exemple du § 7 du cahier des charges (semaine du 4 au 8 mai 2026, vendredi 8 férié ; Camille à 80 %, deux affectations à 50 % sur deux projets, un congé le
//    lundi). Les valeurs attendues sont celles du document, pas celles que produirait le code : capacité 2,4 jours, charge 4 jours, taux 1,6667, surcharge. Prévoir des fonctions qui fabriquent un
//    dossier, une affectation et ce dossier de Camille
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 4 « L'exemple du cahier des charges devient un test »
// TODO [module 09] Ajouter « Camille sans congé » (80 %, deux affectations à 50 %) : elle est exactement à plein, capacité 3,2 et taux 1, pas de surcharge. Il échoue avec la lecture littérale de RG-23
//    (125 %) ; après la décision du module, corriger aussi l'exemple du § 7 : capacité 2,4, charge 3,2, taux 1,3333, surcharge
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 5 « Le test qui pose la question »
// TODO [module 09] Quatre cas limites en une théorie à libellés (« entrée le mercredi » : capacité 2 et charge 1 ; « quotité qui passe de 100 % à 80 % le mercredi » : 3,6 et 3,6 ; « affectation limitée
//    au mercredi et au jeudi » : 4 et 1 ; « demi-journée d'absence à 80 % » : 2,8 et 0), calculés à la main avant de lancer les tests. Puis, en faits séparés : sans capacité (absent toute la
//    semaine, ou sans contrat) le taux n'est pas défini, il n'y a pas de division par zéro, absent avec des affectations il y a surcharge, sans contrat il n'y en a pas ; intercontrat (RG-25) :
//    consultant avec capacité et sans charge facturable (formation interne comprise), jamais pour un salarié interne ni sans capacité
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 6 « Les cas limites »
// TODO [module 09] RG-24 : une affectation qui remplit exactement la capacité ne met pas en surcharge, une qui la dépasse si, une seule semaine en surcharge suffit, une absence réduit la capacité et
//    peut rendre la candidate en surcharge ; le calcul inclut l'affectation candidate
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 3 « Confirmer une surcharge (RG-24). »
// TODO [module 09] Intercontrat sur un mois : écrire les deux exemples chiffrés (une seule semaine facturable suffit-elle à ne pas être en intercontrat ? un mois sans aucune charge facturable ?), faire
//    trancher, puis coder
//    — cours/09-affectations-et-calcul-de-charge.md, À vous, exercice 4 « Intercontrat sur un mois. »

namespace Chronos.Domain.Tests;

public class CalculateurDeChargeTests
{
}

// TODO [module 10] Transitions de la feuille, sur le modèle du module 04 : le cycle nominal ; le rejet qui la rend à l'état de brouillon avec son motif, effacé à la soumission suivante ; une nouvelle
//    feuille est un brouillon vide ; une feuille commence un lundi ; CHAQUE transition interdite, soit neuf combinaisons état x action en une théorie (aucun cas ne doit manquer) ; un rejet sans
//    motif (RG-34) ; valider sa propre feuille (RG-36) ; chaque transition renouvelle la version et une transition refusée la laisse inchangée
//    — cours/10-temps-et-absences.md, Pas à pas, étape 1 « La feuille de temps et son cycle »
// TODO [module 10] Saisies, sans base de données (un contexte de saisie construit à la main, une ligne par règle) : des saisies valides remplacent celles de la feuille ; RG-30 par une théorie (0,25 ; 0,5 ;
//    0,75 et 1 acceptés ; 0,3, 0, 1,25 et -0,5 refusés) ; RG-31 (total d'un jour au plus égal à 1, une journée exactement pleine est acceptée) ; RG-32 (aucune saisie un jour non ouvré ou d'absence
//    approuvée, une demi-journée d'absence laisse une demi-journée de saisie) ; RG-33 (aucune saisie sur un projet non affecté, ni hors de la période d'affectation) ; un jour hors de la semaine est
//    une erreur de programmation ; RG-35 (une feuille soumise ou validée n'est plus modifiable) ; un refus laisse les saisies d'origine intactes ; le temps par jour additionne les saisies d'un
//    même jour
//    — cours/10-temps-et-absences.md, Pas à pas, étape 2 « Les saisies : quatre règles en un seul endroit »

namespace Chronos.Domain.Tests;

public class FeuilleDeTempsTests
{
}

// TODO [module 10] Absence : une nouvelle absence est « demandée » ; une fin avant le début est refusée (RG-42) ; une absence d'un jour qui débute l'après-midi ET finit le matin est refusée ; la durée d'un
//    jour par une théorie (journée entière : 1 ; débute l'après-midi : 0,5 ; finit le matin : 0,5) ; du jeudi après-midi au lundi matin avec un vendredi férié : 1 jour ; un jour hors de l'absence
//    n'est pas absent ; le cycle (RG-40) : une absence demandée peut être approuvée, refusée ou annulée, seule une demandée est approuvée ou refusée, une approuvée s'annule avant son début, une
//    refusée ou annulée ne s'annule plus, une absence s'annule tant qu'elle n'a pas commencé (théorie sur le nombre de jours avant le début, avec une date du jour choisie par le test) ; la version est
//    renouvelée par chaque transition et laissée intacte par une transition refusée ; les parts absentes couvrent la période demandée et s'additionnent jusqu'à un jour (jeudi après-midi au lundi matin :
//    0,5 le jeudi, 1 le vendredi, rien pour un lundi hors période)
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 10] RG-41 : deux absences de jours entiers se chevauchent si elles ont un jour commun (théorie : se croisent, un jour commun, se suivent, l'une contient l'autre, éloignées) ; une absence qui finit
//    le matin et une autre qui débute l'après-midi du même jour se touchent sans se chevaucher ; deux absences qui partagent une demi-journée se chevauchent. RG-43 : écrire les cas chiffrés de l'exercice (journée d'absence complète, demi-journée
//    d'absence, deux absences le même jour, temps validé hors des absences) une fois la décision prise
//    — cours/10-temps-et-absences.md, À vous, exercice 2 « Des absences qui se touchent sans se chevaucher. »
// TODO [module 10] RG-42 : une absence sans jour ouvré (samedi-dimanche) est refusée, une absence avec un jour ouvré est acceptée
//    — cours/10-temps-et-absences.md, À vous, exercice 3 « Une absence sans jour ouvré. »

namespace Chronos.Domain.Tests;

public class AbsenceTests
{
}

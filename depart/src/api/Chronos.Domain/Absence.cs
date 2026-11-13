// TODO [module 10] Entité absence : Id, CollaborateurId, TypeAbsenceId, DateDebut, DateFin, deux indicateurs de demi-journée (« débute l'après-midi », « finit le matin »), Statut
//    (StatutAbsence, « demandée » à la création), Version (Guid), « Periode » calculée ; accesseurs d'écriture privés. Le constructeur lève RegleMetierException (RG-42) si la fin précède
//    le début, ou si une absence d'un seul jour est à la fois « débute l'après-midi » et « finit le matin ». « PartAbsente(jour) » donne 0, 0,5 ou 1 pour un jour (0 hors de l'absence) ;
//    « DureeEnJours(calendrier) » somme les parts sur les jours ouvrés (du jeudi après-midi au lundi matin avec un vendredi férié : 1 jour). Cycle (RG-40) : « Approuver » et « Refuser »
//    n'acceptent qu'une absence demandée ; « Annuler(aujourd'hui) » accepte une absence demandée ou approuvée, et seulement tant qu'elle n'a pas commencé (la date du jour est reçue en
//    paramètre : le domaine reste pur) ; une transition refusée lève RegleMetierException et laisse la version intacte ; chaque transition réussie renouvelle la version. Pas de motif
//    médical ni de commentaire libre (cahier des charges, § 6). Méthode statique « PartsAbsentes(absences, période) » : pour chaque jour commun à une absence et à la période
//    (Periode.Intersection), la somme des parts, plafonnée à 1, sous forme de dictionnaire jour -> part. Tester le cycle, la durée en demi-journées et PartsAbsentes (jeudi après-midi au
//    lundi matin : 0,5 le jeudi, 1 le vendredi, rien pour un lundi hors période)
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 10] RG-41 (pas de chevauchement) : méthode « Chevauche(autre) » qui raisonne en demi-journées : une absence qui finit le mercredi matin et une autre qui débute le
//    mercredi après-midi ne se chevauchent pas. Écrire les tests
//    — cours/10-temps-et-absences.md, À vous, exercice 2 « Des absences qui se touchent sans se chevaucher. »
// TODO [module 10] RG-43 : méthode statique « VerifierTempsValide(tempsValideParJour, partsAbsentes) » : jour par jour, le temps validé et les parts d'absence ne dépassent pas ensemble
//    une journée ; violation = RegleMetierException avec le code de la règle. Que faire d'une demi-journée d'absence face à une demi-journée de temps validé, puis d'une seconde
//    absence le même jour : questions de l'exercice, à trancher et à tester
//    — cours/10-temps-et-absences.md, À vous, exercice 2 « Des absences qui se touchent sans se chevaucher. »
// TODO [module 10] RG-42 : une absence qui ne couvre aucun jour ouvré (du samedi au dimanche) a une durée nulle et est refusée ; la vérification a besoin du calendrier : où la placer est la question de l'exercice
//    (la solution de référence est à comparer au tag module-10)
//    — cours/10-temps-et-absences.md, À vous, exercice 3 « Une absence sans jour ouvré. »
// TODO [module 12] Implémenter ITracable (RG-53) : les quatre propriétés de trace, à accesseurs d'écriture privés ; l'approbation d'une absence est une validation, elle est donc tracée
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Domain;

public class Absence
{
}

// TODO [module 10] L'absence telle que l'API la renvoie : identifiant, collaborateur, type d'absence, dates de début et de fin, indicateurs « débute l'après-midi »
//    et « finit le matin » (demi-journées), statut (demandée, approuvée, refusée, annulée). Une absence porte un type (par exemple « MAL »),
//    jamais un motif. Une fonction de conversion depuis l'entité du domaine se déclare sur le type.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 7 « RG-41 : deux demandes au même instant » et étape 8 « RG-43 : approuver une absence » (entité et statuts : étape 6 « L'absence »)

namespace Chronos.Api.Absences;

public record AbsenceDto;

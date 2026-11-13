// TODO [module 10] La requête de demande d'absence : collaborateur, type d'absence, dates de début et de fin (tous obligatoires, donc nullables pour que
//    l'absence soit détectée, messages en français), indicateurs « débute l'après-midi » et « finit le matin » (faux par défaut).
//    — cours/10-temps-et-absences.md, Pas à pas, étape 7 « RG-41 : deux demandes au même instant »
// TODO [module 12] Le demandeur est l'utilisateur connecté : retirer l'identifiant du collaborateur de la requête (un identifiant fourni par le
//    client serait un moyen d'agir au nom d'un autre).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre »

namespace Chronos.Api.Absences;

public record CreerAbsenceRequete;

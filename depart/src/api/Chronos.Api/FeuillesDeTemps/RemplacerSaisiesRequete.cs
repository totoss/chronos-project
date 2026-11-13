// TODO [module 10] Les requêtes de saisie, deux types dans ce fichier : SaisieRequete (projet, jour, durée, tous obligatoires, donc nullables pour
//    que l'absence soit détectée) et RemplacerSaisiesRequete (la liste des saisies, obligatoire) : le PUT remplace toutes les saisies
//    de la feuille d'un coup. La durée n'a pas de borne d'annotation : ses valeurs permises sont celles de RG-30, qui répond 422
//    et non 400. Messages en français.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 4 « Les endpoints de la feuille »

namespace Chronos.Api.FeuillesDeTemps;

public record SaisieRequete;

public record RemplacerSaisiesRequete;

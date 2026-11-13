// TODO [module 10] Deux types de réponse : SaisieDto (projet, jour, durée) et FeuilleDeTempsDto (identifiant, collaborateur, lundi de la semaine,
//    statut, motif de rejet facultatif, saisies triées par jour puis par projet). La feuille se construit depuis l'entité du
//    domaine (une fonction de conversion sur le type). Les saisies n'ont pas de DTO propre ni de route : elles sont lues
//    et écrites avec la feuille (agrégat). Le fichier déclare les deux types.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 4 « Les endpoints de la feuille »

namespace Chronos.Api.FeuillesDeTemps;

public record SaisieDto;

public record FeuilleDeTempsDto;

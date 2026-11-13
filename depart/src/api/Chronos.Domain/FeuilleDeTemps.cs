// TODO [module 10] Agrégat « feuille de temps » : Id, CollaborateurId, Semaine (le lundi), Statut (StatutFeuilleDeTemps), MotifRejet (facultatif), Version (Guid, jeton du module 08), une
//    collection de saisies lisible mais non modifiable de l'extérieur (champ privé « _saisies » exposé sous le nom « Saisies »), et « Periode » calculée (la semaine). Le constructeur
//    (collaborateur, lundi) refuse un jour qui n'est pas un lundi par une ArgumentException (garde-fou, pas une règle) et crée un brouillon. Trois transitions, chacune en trois temps
//    (exiger l'état de départ, vérifier ce que l'action demande, changer d'état) et chacune renouvelant la version : « Soumettre » (brouillon -> soumise, efface le motif), « Valider(valideur) »
//    (soumise -> validée), « Rejeter(valideur, motif) » (soumise -> brouillon avec le motif, qui reste visible jusqu'à la soumission suivante). RG-34 : toute transition interdite et un rejet
//    sans motif lèvent RegleMetierException avec le code de la règle. RG-36 : nul ne valide ni ne rejette sa propre feuille (le valideur n'est pas le collaborateur). Tester le cycle nominal, le
//    rejet, les neuf combinaisons état x action, RG-34 et RG-36
//    — cours/10-temps-et-absences.md, Pas à pas, étape 1 « La feuille de temps et son cycle »
// TODO [module 10] Ajouter à ce fichier deux enregistrements scellés : « SaisieDemandee » (projet, jour, durée) et « ContexteDeSaisie » (calendrier de travail, absences jour -> part absente, liste
//    d'AffectationPlanifiee), puis la méthode « RemplacerSaisies(demandes, contexte) » : on remplace tout ou rien (la liste n'est touchée qu'une fois toutes les vérifications passées), la
//    version est renouvelée. RG-35 : une feuille soumise ou validée n'est plus modifiable. Un jour hors de la semaine est une ArgumentException. RG-30 par le constructeur de saisie ; RG-31 : au plus
//    une journée par jour ; RG-32 : pas de saisie un jour non ouvré, ni au-delà de ce que laisse l'absence approuvée (une demi-journée d'absence laisse une demi-journée) ; RG-33 : seulement sur
//    un projet auquel le collaborateur est affecté ce jour-là. Les tests se passent de base : un contexte construit à la main, une ligne par règle, et « aucune saisie après un refus »
//    — cours/10-temps-et-absences.md, Pas à pas, étape 2 « Les saisies : quatre règles en un seul endroit »
// TODO [module 10] Une méthode qui additionne par jour le temps saisi dans la feuille : la forme dont RG-43 a besoin pour comparer du temps validé aux absences. Non détaillé dans le module (nom
//    « TempsParJour » dans la solution de référence) : comparer avec la solution au tag module-10
//    — cours/10-temps-et-absences.md, À vous, exercice 2 « Des absences qui se touchent sans se chevaucher. »
// TODO [module 12] Implémenter ITracable (RG-53) : les quatre propriétés de trace, à accesseurs d'écriture privés
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Domain;

public sealed record SaisieDemandee
{
}

public sealed record ContexteDeSaisie
{
}

public class FeuilleDeTemps
{
}

// TODO [module 10] Configuration de Absence (implémente IEntityTypeConfiguration de Absence) : « Version » en jeton de concurrence, relations vers le collaborateur et vers le type d'absence en suppression
//    restreinte (sans navigation) ; le statut suit la convention du contexte. Non détaillé dans le module : la solution de référence ajoute un index sur le collaborateur et la date de début ; comparer avec
//    la solution au tag module-10. Créer la migration Absences dans chaque jeu
//    — cours/10-temps-et-absences.md, Pas à pas, étape 6 « L'absence »
// TODO [module 12] Limiter à 64 caractères les deux colonnes d'auteur de la trace
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Infrastructure.Configurations;

public class AbsenceConfiguration
{
}

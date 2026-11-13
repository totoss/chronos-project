// TODO [module 10] Configuration de FeuilleDeTemps, un agrégat (implémente IEntityTypeConfiguration de FeuilleDeTemps) : table « FeuillesDeTemps » ; index UNIQUE sur le collaborateur et la semaine (une feuille
//    par collaborateur et par semaine : c'est lui qui rend RG-31 vérifiable dans l'agrégat) ; motif de rejet limité à 500 caractères ; « Version » en jeton de concurrence ; relation vers le collaborateur
//    en suppression restreinte ; relation vers les saisies (champ privé lu par convention) en suppression en CASCADE, voulue : une saisie n'existe pas sans sa feuille. Le statut suit la convention du contexte.
//    Créer la migration FeuillesDeTemps dans chaque jeu
//    — cours/10-temps-et-absences.md, Pas à pas, étape 3 « Enregistrer l'agrégat »
// TODO [module 12] Limiter à 64 caractères les deux colonnes d'auteur de la trace
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Infrastructure.Configurations;

public class FeuilleDeTempsConfiguration
{
}

// TODO [module 25] Regrouper en un seul endroit, sur le modèle de JournalFeuillesDeTemps, les messages de journal de la gestion des
//    comptes, générés à la compilation (série d'identifiants 4000) : compte créé, compte modifié (avec la fiche et le verrouillage),
//    modification refusée (avec le motif). Ils ne portent que des identifiants, jamais d'adresse électronique.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 1 « Regrouper les messages d'une ressource » (le module ne détaille que les feuilles de temps et les erreurs : comparer avec la solution au tag `module-25`)

namespace Chronos.Api.Compte;

public static class JournalComptes
{
}

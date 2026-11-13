// TODO [module 08] Traduire en réponse 409 les conflits d'enregistrement : la classe implémente l'interface IExceptionHandler, construite
//    comme RegleMetierExceptionHandler. Deux cas : une modification concurrente (DbUpdateConcurrencyException, à tester en
//    premier car elle hérite de DbUpdateException) et une violation d'index unique, reconnue par l'aide
//    ErreursBaseDeDonnees de l'infrastructure. Toute autre exception : « pas la mienne » (une clé étrangère inconnue reste une
//    500). La réponse est un ProblemDetails de statut 409 et de titre « Conflit », avec un détail propre à chaque cas.
//    Enregistrement : dans Program.cs, après le gestionnaire des règles de gestion. Classe interne et scellée.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 » (le cas de concurrence sert à l'étape 7 « Modifier sans écraser le travail d'un autre »)
// TODO [module 17] Marquer le conflit de concurrence d'un code stable, l'extension « code » valant « concurrence », pour que l'interface
//    propose de recharger la ressource seulement dans ce cas ; la violation d'unicité n'a ni code ni règle.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)
// TODO [module 25] Journaliser chaque conflit signalé (en précisant s'il s'agit de concurrence) : journal à la catégorie de la classe
//    dans le constructeur principal, appel à JournalErreurs avant d'écrire le statut.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 1 « Regrouper les messages d'une ressource »

namespace Chronos.Api.Erreurs;

internal sealed class ConflitExceptionHandler
{
}

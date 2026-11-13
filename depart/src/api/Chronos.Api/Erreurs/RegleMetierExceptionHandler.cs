// TODO [module 08] Traduire en réponse 422 toute exception de règle de gestion du domaine (RegleMetierException) : la classe implémente
//    l'interface IExceptionHandler d'ASP.NET Core et reçoit le service des ProblemDetails. Pour toute autre exception elle
//    répond « pas la mienne » pour que le gestionnaire suivant soit consulté ; sinon elle écrit un ProblemDetails de statut 422,
//    titre « Règle de gestion non respectée », détail égal au message de la règle (rédigé pour le client), et l'extension « regle »
//    qui porte le code (RG-04, RG-11…). Enregistrement : dans Program.cs.
//    Classe interne et scellée, dans l'espace de noms Chronos.Api.Erreurs.
//    — cours/08-une-api-robuste.md, Pas à pas, étape 4 « Une règle de gestion violée : 422 »
// TODO [module 25] Journaliser chaque refus par un avertissement qui porte le code de la règle : le gestionnaire reçoit un journal à
//    son propre nom de catégorie dans son constructeur principal, et appelle JournalErreurs avant d'écrire le statut.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 1 « Regrouper les messages d'une ressource »

namespace Chronos.Api.Erreurs;

internal sealed class RegleMetierExceptionHandler
{
}

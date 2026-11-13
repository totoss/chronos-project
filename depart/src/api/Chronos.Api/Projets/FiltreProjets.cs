// TODO [module 08] Les paramètres de la liste des projets, liés d'un coup à la chaîne de requête : page (de 1 à 1 000 000,
//    1 par défaut), taille (de 1 à 100, 20 par défaut), recherche (100 caractères au plus), statut et tri. Les valeurs
//    de statut et de tri sont vérifiées par l'endpoint, à liste blanche (400 sinon).
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. » (même forme que FiltreCollaborateurs, étape 6 « Paginer, filtrer, trier »)
// TODO [module 17] Donner aux bornes de page, de taille et de recherche un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Projets;

public record FiltreProjets;

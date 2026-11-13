// TODO [module 08] Les paramètres de la liste des collaborateurs, liés d'un coup à la chaîne de requête : page (de 1 à 1 000 000,
//    1 par défaut), taille (de 1 à 100, 20 par défaut), recherche (100 caractères au plus), statut (« actif » ou « sorti ») et
//    tri (« nom », « -nom », « matricule »). Les valeurs de statut et de tri sont vérifiées par l'endpoint, à liste blanche
//    (400 sinon, par la réponse « Invalide » de Commun/Reponses.cs).
//    — cours/08-une-api-robuste.md, Pas à pas, étape 6 « Paginer, filtrer, trier »
// TODO [module 17] Donner aux bornes de page, de taille et de recherche un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Collaborateurs;

public record FiltreCollaborateurs;

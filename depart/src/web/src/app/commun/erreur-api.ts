// TODO [module 16] Classe `ErreurApi` qui hérite de `Error` : `statut` (0 si l'API n'a pas
//    répondu), message, règle `RG-xx` d'un 422, messages par champ d'un 400.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 8 « Traduire et notifier les erreurs »
// TODO [module 16] `versErreurApi(erreur)` traduit une `HttpErrorResponse` : message du `detail` de
//    l'API, sinon libellé français par statut (le `title` est en anglais).
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 8 « Traduire et notifier les erreurs »
// TODO [module 17] Un accesseur `estDeSaisie` (400, 409, 422) et un champ `code` (par exemple
//    « concurrence » pour un 409 de version périmée).
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API »
// TODO [module 19] Des libellés pour les statuts 401 (session expirée) et 403 (droit manquant).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 7 « Réagir aux `401` et aux `403` »

export class ErreurApi {}

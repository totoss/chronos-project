// TODO [module 16] Intercepteur fonctionnel `erreursInterceptor` : traduit toute erreur en
//    `ErreurApi` et la RELANCE ; notifie le message (bouton « Fermer », 8 secondes) pour les
//    écritures seulement.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 8 « Traduire et notifier les erreurs »
// TODO [module 16] Test, erreurs-interceptor.spec.ts : 422 traduit et notifié, 400 en libellé
//    français avec messages par champ, lecture relancée sans notification, API injoignable (statut
//    0).
//    — cours/16-dialoguer-avec-l-api.md, non détaillé dans le module : comparer avec la solution au tag `module-16`
// TODO [module 17] Exporter un jeton de contexte booléen `ERREURS_DE_SAISIE_GEREES` : posé, il
//    évite la notification d'un 400, 409 ou 422, pas d'une panne.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API »
// TODO [module 17] Test, erreurs-interceptor.spec.ts : avec le jeton : 400, 409 et 422 non
//    notifiés, une 500 l'est ; code d'un 409 de concurrence.
//    — cours/17-formulaires.md, non détaillé dans le module : comparer avec la solution au tag `module-17`
// TODO [module 19] Ne pas notifier non plus les refus des routes /api/compte/.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 7 « Réagir aux `401` et aux `403` »
// TODO [module 19] Test, erreurs-interceptor.spec.ts : libellé français d'un 403 ; connexion
//    refusée non notifiée.
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`

export {};

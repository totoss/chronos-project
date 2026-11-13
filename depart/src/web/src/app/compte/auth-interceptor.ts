// TODO [module 19] Intercepteur `authInterceptor` (le cours le fait créer par `ng generate
//    interceptor compte/auth` : fichier déjà fourni, ne relancez pas la commande) : sur un 401 en
//    cours d'usage (hors /api/compte/), oublier la session et naviguer vers /connexion avec le
//    retour ; l'erreur reste transmise à l'appelant.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 7 « Réagir aux `401` et aux `403` »
// TODO [module 19] Test, auth-interceptor.spec.ts : créer à la main le fichier : test « se crée ».
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 7 « Réagir aux `401` et aux `403` »
// TODO [module 20] Test, auth-interceptor.spec.ts : les deux intercepteurs ensemble, dans l'ordre
//    de app.config.ts : 401, 403, connexion refusée, panne du serveur, réponse réussie.
//    — cours/20-tester-l-interface.md, À vous, exercice 2 « Les intercepteurs, ensemble. »

export {};

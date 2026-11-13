// TODO [module 19] `roleGuard(...roles)` : fabrique d'un `CanMatchFn` ; sans le rôle, la route
//    n'existe pas et le routeur essaie la suivante.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 6 « Réserver une route à un rôle : `canMatch` »
// TODO [module 19] `roleGuardAvecRepli(repli, ...roles)` : fabrique d'un `CanActivateFn` ; sans le
//    rôle, redirige vers `repli` au lieu d'annuler la navigation.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 6 « Réserver une route à un rôle : `canMatch` »
// TODO [module 20] Test, role-guard.spec.ts : créer à la main le fichier : sans le rôle, la route
//    est ignorée et son code n'est pas chargé ; chaque rôle suffit ; la version avec repli redirige
//    ou laisse passer.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 7 « Tester une garde et une navigation »

export {};

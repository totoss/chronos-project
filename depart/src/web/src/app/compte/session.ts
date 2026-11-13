// TODO [module 19] Service (le cours le fait créer par `ng generate service compte/session` :
//    fichier déjà fourni, ne relancez pas la commande) ; le décorateur `@Service()`, que la CLI
//    aurait posé, est à écrire ici à la main. Un SEUL signal modifiable, PRIVÉ ; `utilisateur` en
//    lecture seule.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 19] `estConnecte` et `roles` en `computed` ; `aLeRole(...roles)`, méthode qui lit le
//    signal `roles` ; `collaborateurId` (null sans fiche).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 19] `charger()` (GET /api/compte/moi) ne lève JAMAIS : un 401 signifie « personne ».
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 19] `connecter(email, motDePasse)` laisse passer l'erreur, puis enchaîne sur
//    `charger()` (qui dépose le cookie anti-CSRF).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 19] `deconnecter()` (corps vide exigé par l'API) ne lève jamais ; `oublier()` aligne
//    l'interface sur un 401.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 19] Test, session.spec.ts : créer à la main le fichier : test « se crée », et la
//    déconnexion même quand l'API répond en erreur (`it.each`).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 2 « Le service `Session` »
// TODO [module 20] Test, session.spec.ts : état initial, chargement, 401 sans exception, compte
//    sans fiche, connexion puis profil, refus de connexion, `oublier()`
//    (`provideHttpClientTesting`).
//    — cours/20-tester-l-interface.md, Pas à pas, étape 3 « Tester un service qui appelle l'API »

export class Session {}

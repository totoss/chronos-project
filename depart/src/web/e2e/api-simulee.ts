// TODO [module 20] Simuler l'API par `page.route` : une fonction `simulerApi(page, etatInitial)`
//    qui répond à tout, et une interface d'état qui expose ce que l'interface a réellement envoyé
//    (saisies, jeton anti-CSRF, appels non simulés).
//    — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »
// TODO [module 20] Simuler chaque appel de `Session` (login sans corps, moi qui dépose le cookie
//    XSRF-TOKEN) et de `FeuillesDeTempsApi`, avec les données de démonstration (Camille Durand,
//    semaine du 4 mai 2026, vendredi 8 férié).
//    — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »
// TODO [module 20] Réponses typées par les types de l'application (`import type`), comparées une
//    fois à la vraie API ; on compare chemin et méthode ; une route de rattrapage répond 501 aux
//    appels inconnus ; couper les polices Google.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »
// TODO [module 20] Playwright ne vérifie pas les types : lancer `tsc -p e2e/tsconfig.json
//    --noEmit`.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 9 « Un parcours de bout en bout avec Playwright »
// TODO [module 21] Étendre la simulation aux collaborateurs, fiche, clients, projets, feuilles à
//    valider et rejet (motif reçu).
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 9 « Vérifier »
// TODO [module 21] Ne plus bloquer les polices Google : elles sont désormais auto-hébergées.
//    — cours/21-accessibilite-et-finitions.md, À vous, exercice 6 « Alléger le démarrage. »

export {};

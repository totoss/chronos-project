// TODO [module 17] `erreursDeFormulaire(erreur, formulaire)` traduit une `ErreurApi` en erreurs de
//    Signal Form : un 400 va au champ du même nom ; 409, 422 ou 400 sans champ reconnu : UNE erreur
//    du formulaire entier, de `kind` règle, code ou `http-<statut>` ; panne ou 500 : rien.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API »
// TODO [module 17] Test, erreurs-de-formulaire.spec.ts : 400 rattaché au champ, clé inconnue
//    réduite au message général, `kind` d'un 409, d'un doublon, d'un 422, rien pour une panne.
//    — cours/17-formulaires.md, non détaillé dans le module : comparer avec la solution au tag `module-17`

export {};

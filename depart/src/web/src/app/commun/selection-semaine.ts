// TODO [module 18] Service d'état à signals (le cours le fait créer par `ng generate service
//    commun/selection-semaine` : fichier déjà fourni, ne relancez pas la commande), partagé par la
//    feuille de temps et le plan de charge. Signal modifiable PRIVÉ, démarrant sur le lundi de la
//    semaine courante.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 2 « La semaine sélectionnée »
// TODO [module 18] `semaine` en lecture seule (toujours un lundi), `jours` (les sept jours),
//    méthodes `decaler(semaines)` et `allerA(date)`.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 2 « La semaine sélectionnée »
// TODO [module 18] Test, selection-semaine.spec.ts : créer à la main le fichier : test « se crée ».
//    — cours/18-ecrans-metier.md, Pas à pas, étape 2 « La semaine sélectionnée »
// TODO [module 20] Test, selection-semaine.spec.ts : sans `TestBed` : démarrage sur le lundi
//    (horloge simulée), `jours` qui suit, recul, changement de mois et d'année, dimanche.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 2 « Tester de la logique, sans Angular »

export class SelectionSemaine {}

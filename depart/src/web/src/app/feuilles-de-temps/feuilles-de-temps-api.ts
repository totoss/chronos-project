// TODO [module 18] Service sur le modèle de `CollaborateursApi` : feuille d'un collaborateur pour
//    une semaine (l'API la crée en brouillon si besoin), affectations, jours fériés, projets.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 5 « Le service d'accès »
// TODO [module 18] Les listes d'appoint ont une valeur par défaut vide : `value()` lève pourtant en
//    cas d'erreur, la lecture doit s'en protéger (RG-33 : les projets viennent des affectations).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 5 « Le service d'accès »
// TODO [module 18] `enregistrer(id, saisies)` et `soumettre(id)` (Observables).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 8 « Enregistrer, puis soumettre »
// TODO [module 18] Lecture des absences d'un collaborateur.
//    — cours/18-ecrans-metier.md, À vous, exercice 1 « Les absences (RG-32). »
// TODO [module 18] `soumises()`, `valider(id)` (sans corps : le valideur est l'utilisateur
//    connecté) et `rejeter(id, motif)`.
//    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
// TODO [module 18] Test, feuilles-de-temps-api.spec.ts : enregistrement, soumission, validation,
//    rejet, lecture d'une feuille pour un lundi.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 19] L'identifiant du collaborateur peut être `null` (compte sans fiche) : aucune
//    requête ne part alors (feuille, affectations, absences).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 11 « Remplacer `COLLABORATEUR_COURANT_ID` »

export class FeuillesDeTempsApi {}

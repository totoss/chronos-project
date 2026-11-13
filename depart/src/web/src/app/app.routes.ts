// TODO [module 15] Test, app.routes.spec.ts : créer à la main le fichier ; naviguer dans les vraies
//    routes avec `RouterTestingHarness` : l'adresse vide mène aux collaborateurs, /collaborateurs/1
//    affiche une fiche, /collaborateurs/nouveau affiche le formulaire et non une fiche, une adresse
//    inconnue affiche la page introuvable.
//    — cours/15-routage.md, non détaillé dans le module : comparer avec la solution au tag `module-15`
// TODO [module 16] Test, app.routes.spec.ts : fournir un faux serveur HTTP et répondre à la requête
//    de l'écran affiché.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 19] Test, app.routes.spec.ts : doublure de `Session` ; visiteur renvoyé à la
//    connexion en retenant l'adresse, plan de charge et feuilles à valider réservés aux managers,
//    /projets/nouveau et création d'un collaborateur selon le rôle.
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
import { Routes } from '@angular/router';

export const routes: Routes = [
  // TODO [module 15] Redirection de l'adresse vide vers 'collaborateurs' (sans oublier
  //    `pathMatch`), puis 'collaborateurs' confié à `collaborateursRoutes` par `loadChildren`.
  //    — cours/15-routage.md, Pas à pas, étape 6 « Les routes de l'application »
  // TODO [module 15] Joker '**' en DERNIER : `PageIntrouvable`, chargé à la demande. Un `title` « …
  //    — Chronos » par route.
  //    — cours/15-routage.md, Pas à pas, étape 6 « Les routes de l'application »
  // TODO [module 15] Les routes 'projets' (par `loadChildren`, vers projets.routes.ts),
  //    'plan-de-charge', 'feuille-de-temps', 'absences' et 'connexion', chacune vers un composant
  //    provisoire chargé à la demande. Provisoires de la solution : `FeuilleDeTempsProvisoire`
  //    (feuilles-de-temps/feuille-de-temps-provisoire/) et `ConnexionProvisoire`
  //    (compte/connexion-provisoire/), non fournis ; `PlanDeCharge` est déjà à son nom définitif.
  //    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
  // TODO [module 18] Faire pointer 'feuille-de-temps' sur `FeuilleDeTempsHebdo` à la place du
  //    composant provisoire (qui disparaît).
  //    — cours/18-ecrans-metier.md, Pas à pas, étape 7 « L'écran de la feuille de temps »
  // TODO [module 18] Ajouter la route 'feuilles-a-valider' vers `FeuillesAValider`, avec un titre.
  //    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
  // TODO [module 19] 'connexion' vers `Connexion`, HORS de toute garde.
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
  // TODO [module 19] Regrouper toutes les autres routes, joker compris, sous une route parente sans
  //    chemin protégée par `authGuard`.
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 5 « Exiger la connexion : `authGuard` »
  // TODO [module 19] Réserver 'plan-de-charge' et 'feuilles-a-valider' aux rôles Manager et
  //    GestionnaireRh par `canMatch` avec `roleGuard`.
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 6 « Réserver une route à un rôle : `canMatch` »
];

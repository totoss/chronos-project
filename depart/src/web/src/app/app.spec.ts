// TODO [module 13] Test, app.spec.ts : Ajouter le test « affiche le nom de l'application », qui
//    cherche « Chronos » dans le `<h1>` (le squelette ne contient pas le second test généré que
//    l'étape 5 fait corriger).
//    — cours/13-premiere-application-angular.md, Pas à pas, étape 5 « Remplacer la page d'accueil »
// TODO [module 15] Test, app.spec.ts : Ajouter `provideRouter([])` aux fournisseurs du test,
//    vérifier qu'un seul lien du menu porte `aria-current`, tester le contenu du menu.
//    — cours/15-routage.md, Pas à pas, étape 7 « La coquille : barre de titre, menu, emplacement »
// TODO [module 16] Test, app.spec.ts : Fournir un faux serveur HTTP et répondre à la requête de
//    l'écran affiché.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 18] Test, app.spec.ts : Mettre à jour le test du menu : le lien des feuilles à
//    valider.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 19] Test, app.spec.ts : Remplacer `Session` par une doublure : menu filtré par rôle,
//    adresse d'un compte sans fiche, déconnexion puis retour à /connexion.
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
// TODO [module 20] Test, app.spec.ts : Attendre la stabilité de l'application dans les tests, au
//    lieu de forcer la détection de changements.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 4 « Relire un test de composant »
// TODO [module 21] Test, app.spec.ts : Compléter : menu présenté comme une liste et absent pour qui
//    n'est pas connecté ; focus sur le titre après un changement de chemin, et pas après un simple
//    paramètre ; lien d'évitement.
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  it('se crée', async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();

    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });
});

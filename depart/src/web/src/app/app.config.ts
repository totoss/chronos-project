import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // TODO [module 15] Lier les paramètres d'adresse (chemin et requête) aux entrées des composants
    //    : option `withComponentInputBinding` du routeur.
    //    — cours/15-routage.md, Pas à pas, étape 1 « Activer la liaison des paramètres »
    // TODO [module 16] Fournir le client HTTP avec une liste d'intercepteurs qui contient
    //    `erreursInterceptor`.
    //    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 8 « Traduire et notifier les erreurs »
    // TODO [module 19] Ajouter `authInterceptor` à la liste, après `erreursInterceptor` : il doit
    //    voir la réponse en premier, avant sa traduction en `ErreurApi`.
    //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 7 « Réagir aux `401` et aux `403` »
    // TODO [module 19] Un initialiseur d'application qui appelle `Session.charger()` avant la
    //    première navigation.
    //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 3 « Charger la session au démarrage »
  ],
};

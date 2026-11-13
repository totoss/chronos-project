import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

// TODO [module 22] Laisser Prettier reformater ce fichier (l'appel de `bootstrapApplication` tient
//    sur une ligne), à valider seul dans son commit. Même chose pour tsconfig.app.json et
//    tsconfig.spec.json.
//    — cours/22-qualite-du-code.md, Pas à pas, étape 10 « Vérifier le formatage avec Prettier »

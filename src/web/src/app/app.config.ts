import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './compte/auth-interceptor';
import { Session } from './compte/session';
import { erreursInterceptor } from './commun/erreurs-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // L'ordre compte : authInterceptor, dernier, voit la réponse en premier, avant sa traduction en ErreurApi.
    provideHttpClient(withInterceptors([erreursInterceptor, authInterceptor])),
    // Avant la première navigation : les gardes doivent savoir si quelqu'un est connecté.
    provideAppInitializer(() => inject(Session).charger()),
  ],
};

import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { authGuard } from './auth-guard';
import { Session } from './session';

@Component({ selector: 'app-ecran-connexion', template: '<h1>Connexion</h1>' })
class EcranConnexion {}

@Component({ selector: 'app-ecran-protege', template: '<h1>Écran protégé</h1>' })
class EcranProtege {}

describe('authGuard', () => {
  /** La session est une doublure d'un signal : chaque test décide si quelqu'un est connecté. */
  const connecte = signal(false);

  beforeEach(() => {
    connecte.set(false);
    TestBed.configureTestingModule({
      providers: [
        { provide: Session, useValue: { estConnecte: connecte } },
        provideRouter([
          { path: 'connexion', component: EcranConnexion },
          {
            path: '',
            canActivate: [authGuard],
            children: [{ path: 'protege', component: EcranProtege }],
          },
        ]),
      ],
    });
  });

  it('appelée directement, renvoie un UrlTree vers la connexion qui retient la page demandée', () => {
    const etat = { url: '/plan-de-charge?semaine=2026-05-04' } as RouterStateSnapshot;

    const resultat = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, etat),
    );

    expect(resultat).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(resultat as UrlTree)).toBe(
      '/connexion?retour=%2Fplan-de-charge%3Fsemaine%3D2026-05-04',
    );
  });

  it('appelée directement, laisse passer une personne connectée', () => {
    connecte.set(true);

    const resultat = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/protege' } as RouterStateSnapshot),
    );

    expect(resultat).toBe(true);
  });

  it('renvoie un visiteur non connecté à la connexion, en retenant la page demandée', async () => {
    const harnais = await RouterTestingHarness.create();

    await harnais.navigateByUrl('/protege');

    expect(TestBed.inject(Router).url).toBe('/connexion?retour=%2Fprotege');
    expect(harnais.routeNativeElement?.textContent).toContain('Connexion');
  });

  it('laisse une personne connectée ouvrir l’écran protégé', async () => {
    connecte.set(true);
    const harnais = await RouterTestingHarness.create();

    await harnais.navigateByUrl('/protege');

    expect(TestBed.inject(Router).url).toBe('/protege');
    expect(harnais.routeNativeElement?.textContent).toContain('Écran protégé');
  });
});

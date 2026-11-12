import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { roleGuard, roleGuardAvecRepli } from './role-guard';
import { Session } from './session';
import { Role } from './utilisateur';

@Component({ selector: 'app-ecran-plan', template: '<h1>Plan de charge</h1>' })
class EcranPlan {}

@Component({ selector: 'app-ecran-introuvable', template: '<h1>Page introuvable</h1>' })
class EcranIntrouvable {}

@Component({ selector: 'app-ecran-liste', template: '<h1>Liste</h1>' })
class EcranListe {}

describe('gardes de rôle', () => {
  /** La session est une doublure d'un signal : chaque test règle les rôles de l'utilisateur. */
  const roles = signal<Role[]>([]);
  const chargerPlan = vi.fn(async () => EcranPlan);
  const session = {
    aLeRole: (...attendus: Role[]) => roles().some((role) => attendus.includes(role)),
  };

  beforeEach(() => {
    roles.set([]);
    chargerPlan.mockClear();
  });

  describe('roleGuard (canMatch)', () => {
    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          { provide: Session, useValue: session },
          provideRouter([
            {
              path: 'plan-de-charge',
              canMatch: [roleGuard('Manager', 'GestionnaireRh')],
              loadComponent: chargerPlan,
            },
            { path: '**', component: EcranIntrouvable },
          ]),
        ],
      });
    });

    it('sans le rôle, la route n’existe pas et son code n’est pas chargé', async () => {
      roles.set(['Collaborateur']);
      const harnais = await RouterTestingHarness.create();

      await harnais.navigateByUrl('/plan-de-charge');

      expect(harnais.routeNativeElement?.textContent).toContain('Page introuvable');
      expect(chargerPlan).not.toHaveBeenCalled();
    });

    it.each<Role>(['Manager', 'GestionnaireRh'])(
      'avec le rôle %s, charge et affiche l’écran',
      async (role) => {
        roles.set(['Collaborateur', role]);
        const harnais = await RouterTestingHarness.create();

        await harnais.navigateByUrl('/plan-de-charge');

        expect(harnais.routeNativeElement?.textContent).toContain('Plan de charge');
        expect(chargerPlan).toHaveBeenCalledOnce();
      },
    );
  });

  describe('roleGuardAvecRepli (canActivate)', () => {
    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          { provide: Session, useValue: session },
          provideRouter([
            { path: 'liste', component: EcranListe },
            {
              path: 'nouveau',
              canActivate: [roleGuardAvecRepli('/liste', 'GestionnaireRh')],
              component: EcranPlan,
            },
          ]),
        ],
      });
    });

    it('sans le rôle, redirige vers le repli au lieu d’annuler la navigation', async () => {
      roles.set(['Manager']);
      const harnais = await RouterTestingHarness.create();

      await harnais.navigateByUrl('/nouveau');

      expect(TestBed.inject(Router).url).toBe('/liste');
      expect(harnais.routeNativeElement?.textContent).toContain('Liste');
    });

    it('avec le rôle, laisse passer', async () => {
      roles.set(['GestionnaireRh']);
      const harnais = await RouterTestingHarness.create();

      await harnais.navigateByUrl('/nouveau');

      expect(TestBed.inject(Router).url).toBe('/nouveau');
      expect(harnais.routeNativeElement?.textContent).toContain('Plan de charge');
    });
  });
});

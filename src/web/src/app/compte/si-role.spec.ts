import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Session } from './session';
import { SiRole } from './si-role';
import { Role } from './utilisateur';

@Component({
  imports: [SiRole],
  template: `
    <button *appSiRole="roles()">Ajouter</button>
    <p *appSiRole="'Administrateur'">Réservé</p>
  `,
})
class Hote {
  readonly roles = signal<Role | Role[]>('GestionnaireRh');
}

describe('SiRole', () => {
  /** Les rôles de l'utilisateur connecté : un signal, pour les changer après l'affichage. */
  const rolesDeLUtilisateur = signal<Role[]>([]);

  beforeEach(() => {
    rolesDeLUtilisateur.set(['Collaborateur']);
    TestBed.configureTestingModule({
      imports: [Hote],
      providers: [
        {
          provide: Session,
          useValue: {
            aLeRole: (...attendus: Role[]) =>
              rolesDeLUtilisateur().some((role) => attendus.includes(role)),
          },
        },
      ],
    });
  });

  async function afficher() {
    const fixture = TestBed.createComponent(Hote);
    await fixture.whenStable();
    return fixture;
  }

  function texte(fixture: { nativeElement: unknown }): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('n’affiche pas l’élément sans le rôle', async () => {
    const fixture = await afficher();

    expect(texte(fixture)).not.toContain('Ajouter');
  });

  it('affiche l’élément avec le rôle', async () => {
    rolesDeLUtilisateur.set(['Collaborateur', 'GestionnaireRh']);
    const fixture = await afficher();

    expect(texte(fixture)).toContain('Ajouter');
    expect(texte(fixture)).not.toContain('Réservé');
  });

  it('fait apparaître puis disparaître l’élément quand le rôle de l’utilisateur change après l’affichage', async () => {
    const fixture = await afficher();

    rolesDeLUtilisateur.set(['Collaborateur', 'GestionnaireRh']);
    await fixture.whenStable();
    expect(texte(fixture)).toContain('Ajouter');

    rolesDeLUtilisateur.set(['Collaborateur']);
    await fixture.whenStable();
    expect(texte(fixture)).not.toContain('Ajouter');
  });

  it('suit aussi le rôle demandé, qui peut être une liste', async () => {
    rolesDeLUtilisateur.set(['Manager']);
    const fixture = await afficher();
    expect(texte(fixture)).not.toContain('Ajouter');

    fixture.componentInstance.roles.set(['Manager', 'GestionnaireRh']);
    await fixture.whenStable();

    expect(texte(fixture)).toContain('Ajouter');
  });
});

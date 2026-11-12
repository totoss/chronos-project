import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatInputHarness } from '@angular/material/input/testing';
import { provideRouter, Router } from '@angular/router';
import { ErreurApi } from '../../commun/erreur-api';
import { Session } from '../session';
import { Connexion } from './connexion';

describe('Connexion', () => {
  let fixture: ComponentFixture<Connexion>;
  let loader: HarnessLoader;
  let navigation: ReturnType<typeof vi.spyOn>;
  const connecter = vi.fn<Session['connecter']>();

  beforeEach(() => {
    connecter.mockReset();
    TestBed.configureTestingModule({
      imports: [Connexion],
      providers: [provideRouter([]), { provide: Session, useValue: { connecter } }],
    });
    navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(Connexion);
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  async function saisir(email: string, password: string): Promise<void> {
    const champEmail = await loader.getHarness(MatInputHarness.with({ selector: '[type=email]' }));
    const champMotDePasse = await loader.getHarness(
      MatInputHarness.with({ selector: '[type=password]' }),
    );
    await champEmail.setValue(email);
    await champMotDePasse.setValue(password);
  }

  /** La soumission d'un formulaire à signaux n'est pas suivie par `whenStable` : on attend sa fin visible. */
  async function valider(): Promise<void> {
    const bouton = await loader.getHarness(MatButtonHarness.with({ text: 'Se connecter' }));
    await bouton.click();
    await vi.waitFor(async () => expect(await bouton.isDisabled()).toBe(false));
  }

  function alerte(): string | undefined {
    return (fixture.nativeElement as HTMLElement)
      .querySelector('[role=alert]')
      ?.textContent?.trim();
  }

  it('connecte puis retourne à la page demandée', async () => {
    connecter.mockResolvedValue();
    fixture.componentRef.setInput('retour', '/feuille-de-temps?semaine=2026-05-04');

    await saisir('camille.durand@chronos.example', 'secret');
    await valider();

    expect(connecter).toHaveBeenCalledWith('camille.durand@chronos.example', 'secret');
    expect(navigation).toHaveBeenCalledWith('/feuille-de-temps?semaine=2026-05-04');
    expect(alerte()).toBeUndefined();
  });

  it('retourne à la racine quand aucune page n’est demandée', async () => {
    connecter.mockResolvedValue();

    await saisir('camille.durand@chronos.example', 'secret');
    await valider();

    expect(navigation).toHaveBeenCalledWith('/');
  });

  it.each(['https://pirate.example/', '//pirate.example/'])(
    'ne suit pas un retour vers un autre site (%s)',
    async (retour) => {
      connecter.mockResolvedValue();
      fixture.componentRef.setInput('retour', retour);

      await saisir('camille.durand@chronos.example', 'secret');
      await valider();

      expect(navigation).toHaveBeenCalledWith('/');
    },
  );

  it("n'appelle pas l'API quand le formulaire est invalide", async () => {
    await saisir('pas-une-adresse', '');
    await valider();

    expect(connecter).not.toHaveBeenCalled();
    expect(navigation).not.toHaveBeenCalled();
    const champs = await loader.getAllHarnesses(MatFormFieldHarness);
    expect(await champs[0].getTextErrors()).toEqual(["L'adresse électronique est invalide."]);
    expect(await champs[1].getTextErrors()).toEqual(['Le mot de passe est obligatoire.']);
  });

  it('refuse une connexion par un message générique, sans naviguer', async () => {
    connecter.mockRejectedValue(new ErreurApi(401, 'Non autorisé.'));

    await saisir('camille.durand@chronos.example', 'mauvais');
    await valider();

    expect(alerte()).toBe('Adresse électronique ou mot de passe incorrect.');
    expect(navigation).not.toHaveBeenCalled();
  });

  it('dit que l’API est injoignable plutôt que d’accuser le mot de passe', async () => {
    connecter.mockRejectedValue(
      new ErreurApi(0, "L'API ne répond pas. Vérifiez votre connexion, puis réessayez."),
    );

    await saisir('camille.durand@chronos.example', 'secret');
    await valider();

    expect(alerte()).toBe("L'API ne répond pas. Vérifiez votre connexion, puis réessayez.");
    expect(navigation).not.toHaveBeenCalled();
  });

  it('efface le message d’échec à la tentative suivante', async () => {
    connecter.mockRejectedValueOnce(new ErreurApi(401, 'Non autorisé.'));
    connecter.mockResolvedValueOnce();

    await saisir('camille.durand@chronos.example', 'mauvais');
    await valider();
    expect(alerte()).toBeDefined();

    await valider();

    expect(alerte()).toBeUndefined();
    expect(navigation).toHaveBeenCalledWith('/');
  });
});

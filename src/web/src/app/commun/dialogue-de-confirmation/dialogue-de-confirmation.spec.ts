import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { demanderConfirmation } from './dialogue-de-confirmation';

describe('demanderConfirmation', () => {
  const donnees = {
    titre: 'Supprimer le collaborateur',
    message: 'Supprimer Camille Durand ?',
    libelleConfirmer: 'Supprimer',
  };

  function bouton(libelle: string): HTMLButtonElement {
    return Array.from(
      document.querySelectorAll<HTMLButtonElement>('mat-dialog-actions button'),
    ).find((b) => b.textContent?.includes(libelle))!;
  }

  async function ouvrir(): Promise<{ reponse: Promise<boolean> }> {
    const reponse = demanderConfirmation(TestBed.inject(MatDialog), donnees);
    await vi.waitFor(() => expect(bouton('Annuler')).toBeTruthy());
    TestBed.tick();
    return { reponse };
  }

  it('affiche la question et rend true quand on confirme', async () => {
    const { reponse } = await ouvrir();

    expect(document.querySelector('mat-dialog-content')?.textContent).toContain(
      'Supprimer Camille Durand ?',
    );
    bouton('Supprimer').click();

    await expect(reponse).resolves.toBe(true);
  });

  it('rend false quand on annule', async () => {
    const { reponse } = await ouvrir();

    bouton('Annuler').click();

    await expect(reponse).resolves.toBe(false);
  });

  it('rend false quand on ferme par Échap', async () => {
    const { reponse } = await ouvrir();

    document.body.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', keyCode: 27, bubbles: true }),
    );

    await expect(reponse).resolves.toBe(false);
  });
});

import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

test('se connecter, saisir sa feuille de temps et la soumettre', async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  const api = await simulerApi(page);

  await page.goto('/feuille-de-temps');
  await expect(page).toHaveURL(/\/connexion\?retour=%2Ffeuille-de-temps/);

  await page.getByLabel('Adresse électronique').fill('camille.durand@chronos.example');
  await page.getByLabel('Mot de passe').fill('un-mot-de-passe-de-test');
  await page.getByRole('button', { name: 'Se connecter' }).click();

  await expect(page.getByText('Semaine du lun. 4 mai')).toBeVisible();
  await page.getByLabel('Refonte du portail client, lun. 4 mai').selectOption('0.5');
  await page.getByLabel('Refonte du portail client, mar. 5 mai').selectOption('1');
  await expect(page.getByLabel('Refonte du portail client, ven. 8 mai')).toBeDisabled();

  await page.getByRole('button', { name: 'Enregistrer le brouillon' }).click();
  await expect.poll(() => api.saisies).toHaveLength(2);
  await expect(page.getByText('État : Brouillon')).toBeVisible();

  await page.getByRole('button', { name: 'Soumettre' }).click();

  await expect(page.getByText('État : Soumise')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Soumettre' })).toHaveCount(0);
  expect(api.jetonRecu).toBe('jeton-de-test');
  expect(api.saisies).toEqual([
    { projetId: 1, jour: '2026-05-04', duree: 0.5 },
    { projetId: 1, jour: '2026-05-05', duree: 1 },
  ]);
  expect(api.appelsNonSimules).toEqual([]);
});

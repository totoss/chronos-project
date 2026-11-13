import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

test.beforeEach(async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
});

test('un collaborateur sans autre rôle ne voit pas le plan de charge', async ({ page }) => {
  const api = await simulerApi(page, { connecte: true, roles: ['Collaborateur'] });

  await page.goto('/plan-de-charge');

  await expect(page.getByRole('heading', { name: 'Page introuvable' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Plan de charge' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Feuille de temps' })).toBeVisible();
  expect(api.pasDemandes).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('un manager voit la surcharge, par semaine puis par mois', async ({ page }) => {
  const api = await simulerApi(page, { connecte: true, roles: ['Collaborateur', 'Manager'] });

  await page.goto('/plan-de-charge');

  await expect(page.getByRole('heading', { name: 'Plan de charge' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Plan de charge' })).toBeVisible();
  const ligne = page.getByRole('row', { name: /Camille Durand/ });
  await expect(ligne).toContainText('Surcharge');
  await expect(ligne).toContainText('143');
  await expect(page.getByRole('radio', { name: 'Par semaine' })).toBeChecked();

  await page.getByRole('radio', { name: 'Par mois' }).click();

  await expect(page.getByRole('radio', { name: 'Par mois' })).toBeChecked();
  await expect(page.getByRole('columnheader', { name: /mai 2026/ })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: /août 2026/ })).toBeVisible();
  expect(api.pasDemandes).toEqual(['semaine', 'mois']);
  expect(api.appelsNonSimules).toEqual([]);
});

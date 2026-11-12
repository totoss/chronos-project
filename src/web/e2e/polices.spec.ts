import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

test('les polices viennent de l’application : aucune requête ne part vers un autre site', async ({
  page,
  baseURL,
}) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  await simulerApi(page, { connecte: true });
  const autresSites: string[] = [];
  page.on('request', (requete) => {
    const adresse = requete.url();
    if (!adresse.startsWith(baseURL ?? '') && !adresse.startsWith('data:'))
      autresSites.push(adresse);
  });

  await page.goto('/collaborateurs');
  await expect(page.getByRole('heading', { name: 'Collaborateurs' })).toBeVisible();
  await page.evaluate(() => document.fonts.ready);

  const polices = await page.evaluate(() => ({
    roboto: document.fonts.check('16px Roboto'),
    icones: document.fonts.check('24px "Material Symbols Outlined"'),
    classeDesIcones: document.querySelector('mat-icon')?.className ?? '',
  }));
  expect(polices.roboto).toBe(true);
  expect(polices.icones).toBe(true);
  expect(polices.classeDesIcones).toContain('material-symbols-outlined');
  expect(autresSites).toEqual([]);
});

import AxeBuilder from '@axe-core/playwright';
import { expect, Page, test } from '@playwright/test';
import { EtatApi, simulerApi } from './api-simulee';

const regles = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

/** Les violations que l'analyse d'axe trouve sur la page affichée. */
async function violations(page: Page) {
  const resultats = await new AxeBuilder({ page }).withTags(regles).analyze();
  return resultats.violations;
}

/** Ouvre un écran avec l'API simulée, vérifie son titre et sa langue, puis l'analyse avec axe. */
async function controler(
  page: Page,
  adresse: string,
  titre: string,
  etat: Partial<EtatApi> = { connecte: true },
): Promise<EtatApi> {
  const api = await simulerApi(page, etat);
  await page.goto(adresse);
  await expect(page).toHaveTitle(titre);
  await expect(page.locator('html')).toHaveAttribute('lang', 'fr');
  await expect(page.getByRole('heading', { level: 2 })).toBeVisible();
  return api;
}

const manager: Partial<EtatApi> = { connecte: true, roles: ['Collaborateur', 'Manager'] };
const gestionnaireRh: Partial<EtatApi> = {
  connecte: true,
  roles: ['Collaborateur', 'GestionnaireRh'],
};

test.beforeEach(async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
});

test('la connexion ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/connexion', 'Connexion — Chronos', {});
  await expect(page.getByLabel('Adresse électronique')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('la connexion en erreur ne présente aucune violation détectable', async ({ page }) => {
  await controler(page, '/connexion', 'Connexion — Chronos', {});
  await page.getByRole('button', { name: 'Se connecter' }).click();
  await expect(page.getByText("L'adresse électronique est obligatoire.")).toBeVisible();

  expect(await violations(page)).toEqual([]);
});

test('la liste des collaborateurs ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/collaborateurs', 'Collaborateurs — Chronos');
  await expect(page.getByRole('main').getByText('Camille Durand')).toBeVisible();
  await expect(page.getByText('1 à 3 sur 3')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('la fiche d’un collaborateur ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/collaborateurs/2', 'Fiche collaborateur — Chronos');
  await expect(page.getByText('01/09/2021')).toBeVisible();
  await expect(page.getByText('80 %')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('le formulaire de création d’un collaborateur ne présente aucune violation détectable', async ({
  page,
}) => {
  const api = await controler(
    page,
    '/collaborateurs/nouveau',
    'Nouveau collaborateur — Chronos',
    gestionnaireRh,
  );
  await expect(page.getByLabel('Matricule')).toBeVisible();
  await page.getByRole('button', { name: 'Créer' }).click();
  await expect(page.getByText('Le matricule est obligatoire.')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('le formulaire de modification d’un collaborateur ne présente aucune violation détectable', async ({
  page,
}) => {
  const api = await controler(
    page,
    '/collaborateurs/2',
    'Fiche collaborateur — Chronos',
    gestionnaireRh,
  );
  await page.getByRole('button', { name: 'Modifier' }).click();
  await expect(page.getByRole('heading', { name: 'Modifier la fiche' })).toBeVisible();
  await expect(page.getByLabel('Matricule')).toBeDisabled();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('la liste des projets ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/projets', 'Projets — Chronos', manager);
  await expect(page.getByText('BDL-REFONTE')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('le formulaire de projet ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/projets/nouveau', 'Nouveau projet — Chronos', manager);
  await expect(page.getByLabel('Code')).toBeVisible();
  await page.getByRole('button', { name: 'Créer' }).click();
  await expect(page.getByText('Le code est obligatoire.')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('la feuille de temps ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/feuille-de-temps', 'Feuille de temps — Chronos');
  await expect(page.getByRole('grid')).toBeVisible();
  await expect(page.getByLabel('Refonte du portail client, lun. 4 mai')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('la feuille de temps en dépassement ne présente aucune violation détectable', async ({
  page,
}) => {
  await controler(page, '/feuille-de-temps', 'Feuille de temps — Chronos');
  await page.getByLabel('Refonte du portail client, lun. 4 mai').selectOption('0.75');
  await page.getByLabel('Formation interne, lun. 4 mai').selectOption('0.5');
  await expect(page.getByRole('alert')).toContainText('Le total dépasse une journée');
  await expect(page.getByText("Plus d'un jour")).toBeVisible();

  expect(await violations(page)).toEqual([]);
});

test('la feuille de temps rejetée ne présente aucune violation détectable', async ({ page }) => {
  await controler(page, '/feuille-de-temps', 'Feuille de temps — Chronos', {
    connecte: true,
    motifRejet: 'Il manque le jeudi.',
  });
  await expect(page.getByRole('status')).toContainText('Il manque le jeudi.');

  expect(await violations(page)).toEqual([]);
});

test('les feuilles à valider ne présentent aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/feuilles-a-valider', 'Feuilles à valider — Chronos', {
    ...manager,
    statut: 'Soumise',
    saisies: [{ projetId: 1, jour: '2026-05-04', duree: 0.5 }],
  });
  await page.getByRole('button', { name: /Camille Durand — semaine du lun. 4 mai/ }).click();
  await expect(page.getByRole('grid')).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('le dialogue de rejet ne présente aucune violation détectable', async ({ page }) => {
  await controler(page, '/feuilles-a-valider', 'Feuilles à valider — Chronos', {
    ...manager,
    statut: 'Soumise',
  });
  await page.getByRole('button', { name: /Camille Durand — semaine du lun. 4 mai/ }).click();
  await page.getByRole('button', { name: 'Rejeter' }).click();
  await page.getByRole('button', { name: 'Rejeter', exact: true }).last().click();
  await expect(page.getByText('Le motif du rejet est obligatoire.')).toBeVisible();
  await expect(page.getByLabel('Motif du rejet')).toHaveAttribute('aria-invalid', 'true');
  await page.waitForTimeout(500); // la fin de l'animation d'ouverture : axe mesure les contrastes

  expect(await violations(page)).toEqual([]);
});

test('le plan de charge ne présente aucune violation détectable', async ({ page }) => {
  const api = await controler(page, '/plan-de-charge', 'Plan de charge — Chronos', manager);
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByRole('row', { name: /Camille Durand/ })).toContainText('Surcharge');

  expect(await violations(page)).toEqual([]);

  await page.getByRole('radio', { name: 'Par mois' }).click();
  await expect(page.getByRole('columnheader', { name: /mai 2026/ })).toBeVisible();

  expect(await violations(page)).toEqual([]);
  expect(api.appelsNonSimules).toEqual([]);
});

test('les dialogues du plan de charge ne présentent aucune violation détectable', async ({
  page,
}) => {
  await controler(page, '/plan-de-charge', 'Plan de charge — Chronos', manager);
  await page.getByRole('button', { name: /Confirmer la surcharge de Camille Durand/ }).click();
  await expect(page.getByRole('dialog', { name: 'Confirmer la surcharge' })).toBeVisible();
  await page.waitForTimeout(500);
  expect(await violations(page)).toEqual([]);
  await page.keyboard.press('Escape');

  await page.getByRole('button', { name: 'Nouvelle affectation' }).click();
  await expect(page.getByRole('dialog', { name: 'Nouvelle affectation' })).toBeVisible();
  await page.waitForTimeout(500);
  expect(await violations(page)).toEqual([]);
});

test('les écrans sans contenu propre ne présentent aucune violation détectable', async ({
  page,
}) => {
  await controler(page, '/absences', 'Absences — Chronos');
  expect(await violations(page)).toEqual([]);

  await page.goto('/une-adresse-inconnue');
  await expect(page).toHaveTitle('Page introuvable — Chronos');
  expect(await violations(page)).toEqual([]);
});

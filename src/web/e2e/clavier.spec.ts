import { expect, Locator, Page, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

/** Le jeudi 6 mai 2026 : la semaine du lun. 4 mai, où se trouve le jour férié du 8 mai. */
async function dateFixe(page: Page): Promise<void> {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
}

/**
 * Entrée ouvre une cellule de la grille, Échap la referme : l'état passe de "cell" à "widget" et retour.
 * Il se lit sur l'attribut que pose Angular Aria (non documenté) ; attendre ce changement évite d'envoyer
 * la touche suivante avant qu'il soit pris en compte.
 */
async function attendreLeMode(select: Locator, mode: 'cell' | 'widget'): Promise<void> {
  await expect(select).toHaveAttribute('data-active-control', mode);
}

test('la coquille se parcourt au clavier, et le lien d’évitement mène au titre', async ({
  page,
}) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true });
  await page.goto('/feuille-de-temps');
  await expect(page.getByRole('heading', { name: 'Feuille de temps' })).toBeVisible();

  const ordre = [
    page.getByRole('link', { name: 'Aller au contenu' }),
    page.getByRole('button', { name: 'Afficher ou masquer le menu' }),
    page.getByRole('button', { name: 'Se déconnecter' }),
    page.getByRole('link', { name: 'Collaborateurs' }),
    page.getByRole('link', { name: 'Projets' }),
    page.getByRole('link', { name: 'Feuille de temps' }),
    page.getByRole('link', { name: 'Absences' }),
  ];
  for (const element of ordre) {
    await page.keyboard.press('Tab');
    await expect(element).toBeFocused();
  }

  await page.getByRole('link', { name: 'Aller au contenu' }).focus();
  await expect(page.getByRole('link', { name: 'Aller au contenu' })).toBeInViewport();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Feuille de temps' })).toBeFocused();
  await expect(page).toHaveURL(/\/feuille-de-temps$/);

  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Semaine précédente' })).toBeFocused();
});

test('le menu est une liste, et le lien de l’écran courant est marqué', async ({ page }) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true });
  await page.goto('/projets');

  const menu = page.getByRole('navigation', { name: 'Menu principal' });
  await expect(menu.getByRole('list')).toBeVisible();
  await expect(menu.getByRole('listitem')).toHaveCount(4);
  await expect(menu.getByRole('link', { name: 'Projets' })).toHaveAttribute('aria-current', 'page');
});

test('après un changement d’écran, le focus est sur le titre du nouvel écran', async ({ page }) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true });
  await page.goto('/collaborateurs');
  await expect(page.getByRole('heading', { name: 'Collaborateurs' })).toBeVisible();

  await page.getByRole('link', { name: 'Projets' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Projets' })).toBeFocused();

  await page.getByRole('link', { name: 'Collaborateurs' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Collaborateurs' })).toBeFocused();

  await page.getByRole('link', { name: 'Voir la fiche' }).first().focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Fiche collaborateur' })).toBeFocused();
});

test('écrire la recherche dans l’adresse ne retire pas le focus du champ', async ({ page }) => {
  await simulerApi(page, { connecte: true });
  await page.goto('/collaborateurs');
  const champ = page.getByLabel('Filtrer par nom');
  await champ.focus();

  await page.keyboard.type('dur');

  await expect(page).toHaveURL(/recherche=dur/);
  await expect(champ).toBeFocused();
});

test('la grille se parcourt au clavier', async ({ page }) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true });
  await page.goto('/feuille-de-temps');
  const lundi = page.getByLabel('Refonte du portail client, lun. 4 mai');
  await expect(lundi).toBeVisible();

  // Un seul arrêt pour toute la grille : Tab y entre par son premier en-tête.
  await page.getByRole('button', { name: 'Semaine suivante' }).focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('columnheader', { name: 'Projet' })).toBeFocused();

  await page.keyboard.press('ArrowDown');
  await expect(page.getByRole('rowheader', { name: 'Refonte du portail client' })).toBeFocused();
  await page.keyboard.press('ArrowRight');
  await expect(lundi).toBeFocused();

  // Entrée ouvre la cellule : les flèches changent alors la valeur de la liste.
  await page.keyboard.press('Enter');
  await attendreLeMode(lundi, 'widget');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('ArrowDown');
  await expect(lundi).toHaveValue('0.5');
  await page.keyboard.press('Escape');
  await attendreLeMode(lundi, 'cell');

  await page.keyboard.press('ArrowRight');
  await expect(page.getByLabel('Refonte du portail client, mar. 5 mai')).toBeFocused();

  // Les jours fermés (ven. 8 mai férié, week-end) affichent un texte et restent atteignables.
  await page.getByLabel('Refonte du portail client, jeu. 7 mai').focus();
  const ligne = page.locator('tbody tr').first().locator('td');
  await page.keyboard.press('ArrowRight');
  await expect(ligne.nth(4)).toBeFocused();
  await expect(ligne.nth(4)).toHaveText('—');
  for (let i = 0; i < 3; i++) await page.keyboard.press('ArrowRight');
  await expect(ligne.last()).toBeFocused();

  // Tab sort de la grille en une fois.
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Enregistrer le brouillon' })).toBeFocused();
});

test('se connecter, saisir une semaine et la soumettre se fait sans souris', async ({ page }) => {
  await dateFixe(page);
  const api = await simulerApi(page);
  await page.goto('/feuille-de-temps');
  await expect(page).toHaveURL(/\/connexion/);

  await page.getByLabel('Adresse électronique').focus();
  await page.keyboard.type('camille.durand@chronos.example');
  await page.keyboard.press('Tab');
  await page.keyboard.type('un-mot-de-passe-de-test');
  await page.keyboard.press('Enter');

  // Après la connexion, le focus est sur le titre de la feuille de temps.
  await expect(page.getByRole('heading', { name: 'Feuille de temps' })).toBeFocused();
  await expect(page.getByLabel('Refonte du portail client, lun. 4 mai')).toBeVisible();

  // Tab (bouton « Semaine précédente », « Semaine suivante »), puis la grille.
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('columnheader', { name: 'Projet' })).toBeFocused();
  await page.keyboard.press('ArrowDown');
  await expect(page.getByRole('rowheader', { name: 'Refonte du portail client' })).toBeFocused();
  await page.keyboard.press('ArrowRight');
  const lundi = page.getByLabel('Refonte du portail client, lun. 4 mai');
  await expect(lundi).toBeFocused();
  await page.keyboard.press('Enter');
  await attendreLeMode(lundi, 'widget');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Escape');
  await attendreLeMode(lundi, 'cell');
  await page.keyboard.press('ArrowRight');
  const mardi = page.getByLabel('Refonte du portail client, mar. 5 mai');
  await expect(mardi).toBeFocused();
  await page.keyboard.press('Enter');
  await attendreLeMode(mardi, 'widget');
  await page.keyboard.press('End');
  await page.keyboard.press('Escape');
  await attendreLeMode(mardi, 'cell');
  await expect(lundi).toHaveValue('0.5');
  await expect(mardi).toHaveValue('1');

  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Enregistrer le brouillon' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect.poll(() => api.saisies).toHaveLength(2);
  await expect(page.locator('.cdk-live-announcer-element')).toHaveText('Brouillon enregistré.');
  // Le bouton se désactive pendant l'envoi sans perdre le focus.
  await expect(page.getByRole('button', { name: 'Enregistrer le brouillon' })).toBeFocused();

  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Soumettre' })).toBeFocused();
  await page.keyboard.press('Enter');

  await expect(page.getByText('État : Soumise')).toBeVisible();
  await expect(page.locator('.cdk-live-announcer-element')).toHaveText('Feuille de temps soumise.');
  // Les boutons ont disparu avec le brouillon : le focus est revenu au titre, il ne s'est pas perdu.
  await expect(page.getByRole('heading', { name: 'Feuille de temps' })).toBeFocused();
  expect(api.appelsNonSimules).toEqual([]);
});

test('un total supérieur à une journée est annoncé et écrit en toutes lettres', async ({
  page,
}) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true });
  await page.goto('/feuille-de-temps');
  await page.getByLabel('Refonte du portail client, lun. 4 mai').selectOption('0.75');
  await page.getByLabel('Formation interne, lun. 4 mai').selectOption('0.5');

  await expect(page.getByRole('alert')).toContainText('Le total dépasse une journée : lun. 4 mai.');
  await expect(page.getByText("Plus d'un jour")).toBeVisible();
});

test('le dialogue de confirmation d’une surcharge retient le focus et le rend à la fermeture', async ({
  page,
}) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true, roles: ['Collaborateur', 'Manager'] });
  await page.goto('/plan-de-charge');
  const bouton = page.getByRole('button', { name: /Confirmer la surcharge de Camille Durand/ });
  await expect(bouton).toBeVisible();
  await expect(bouton).toContainText('Confirmer');

  await bouton.focus();
  await page.keyboard.press('Enter');
  const dialogue = page.getByRole('dialog', { name: 'Confirmer la surcharge' });
  await expect(dialogue).toBeVisible();
  await expect(dialogue).toContainText('143\u00a0%');
  await expect(dialogue.getByRole('button', { name: 'Annuler' })).toBeFocused();

  // Le focus ne sort pas du dialogue, quel que soit le nombre de Tab.
  for (let i = 0; i < 5; i++) {
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => !!document.activeElement?.closest('[role=dialog]'))).toBe(
      true,
    );
  }

  await page.keyboard.press('Escape');
  await expect(dialogue).toBeHidden();
  await expect(bouton).toBeFocused();

  // Confirmer remplace le bouton par « confirmée » : le focus s'y place plutôt que de se perdre.
  await page.keyboard.press('Enter');
  await dialogue.getByRole('button', { name: 'Confirmer' }).focus();
  await page.keyboard.press('Enter');
  await expect(dialogue).toBeHidden();
  await expect(page.getByText('confirmée')).toBeFocused();
});

test('le dialogue de rejet relie son message d’erreur au champ du motif', async ({ page }) => {
  await dateFixe(page);
  const api = await simulerApi(page, {
    connecte: true,
    roles: ['Collaborateur', 'Manager'],
    statut: 'Soumise',
  });
  await page.goto('/feuilles-a-valider');
  await page.getByRole('button', { name: /Camille Durand — semaine du lun. 4 mai/ }).click();
  const rejeter = page.getByRole('button', { name: 'Rejeter' });
  await rejeter.focus();
  await page.keyboard.press('Enter');

  const dialogue = page.getByRole('dialog', { name: 'Rejeter la feuille de temps' });
  const motif = dialogue.getByLabel('Motif du rejet');
  await expect(motif).toBeFocused();
  await expect(motif).toHaveAttribute('aria-required', 'true');
  await expect(motif).not.toHaveAttribute('aria-invalid', 'true');

  await page.keyboard.press('Escape');
  await expect(dialogue).toBeHidden();
  await expect(rejeter).toBeFocused();

  await page.keyboard.press('Enter');
  await dialogue.getByRole('button', { name: 'Rejeter' }).focus();
  await page.keyboard.press('Enter');
  await expect(motif).toHaveAttribute('aria-invalid', 'true');
  await expect(dialogue.getByRole('alert')).toHaveText('Le motif du rejet est obligatoire.');
  const idDuMessage = await dialogue.getByRole('alert').getAttribute('id');
  await expect(motif).toHaveAttribute('aria-describedby', idDuMessage ?? '');

  await motif.focus();
  await page.keyboard.type('Il manque le jeudi.');
  await expect(motif).not.toHaveAttribute('aria-invalid', 'true');
  await dialogue.getByRole('button', { name: 'Rejeter' }).focus();
  await page.keyboard.press('Enter');

  await expect(dialogue).toBeHidden();
  await expect.poll(() => api.motifRejet).toBe('Il manque le jeudi.');
  await expect(page.locator('.cdk-live-announcer-element')).toHaveText('Feuille de temps rejetée.');
  await expect(page.getByRole('heading', { name: 'Feuilles à valider' })).toBeFocused();
});

test('modifier une fiche place le focus sur le formulaire, puis le rend à la fiche', async ({
  page,
}) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true, roles: ['Collaborateur', 'GestionnaireRh'] });
  await page.goto('/collaborateurs/2');
  await page.getByRole('button', { name: 'Modifier' }).focus();
  await page.keyboard.press('Enter');

  await expect(page.getByRole('heading', { name: 'Modifier la fiche' })).toBeFocused();
  await expect(page.getByLabel('Matricule')).toBeDisabled();
  await expect(page.getByText('Ne se modifie plus après la création.')).toHaveCount(4);

  await page.getByRole('button', { name: 'Annuler' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Fiche collaborateur' })).toBeFocused();
});

test('les formats sont français : dates, nombres, pourcentages et paginateur', async ({ page }) => {
  await dateFixe(page);
  await simulerApi(page, { connecte: true, roles: ['Collaborateur', 'Manager'] });

  await page.goto('/collaborateurs/2');
  await expect(page.getByText('01/09/2021')).toBeVisible();
  await expect(page.getByText('80\u00a0%')).toBeVisible();

  await page.goto('/collaborateurs');
  await expect(page.getByText('Lignes par page')).toBeVisible();
  await expect(page.getByText('1 à 3 sur 3')).toBeVisible();

  await page.goto('/plan-de-charge');
  await expect(page.getByRole('row', { name: /Camille Durand/ })).toContainText('143\u00a0%');
});

test('sur une feuille soumise, les flèches font lire chaque valeur, cellule par cellule', async ({
  page,
}) => {
  await dateFixe(page);
  await simulerApi(page, {
    connecte: true,
    statut: 'Soumise',
    saisies: [
      { projetId: 1, jour: '2026-05-04', duree: 0.5 },
      { projetId: 1, jour: '2026-05-05', duree: 1 },
    ],
  });
  await page.goto('/feuille-de-temps');
  await expect(page.getByRole('grid')).toBeVisible();
  await expect(page.locator('select')).toHaveCount(0);

  await page.getByRole('button', { name: 'Semaine suivante' }).focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('columnheader', { name: 'Projet' })).toBeFocused();
  await page.keyboard.press('ArrowDown');
  await expect(page.getByRole('rowheader', { name: 'Refonte du portail client' })).toBeFocused();

  const ligne = page.locator('tbody tr').first().locator('td');
  const attendu = ['0,5', '1', '—', '—', '—', '—', '—', '1,5'];
  for (const [i, texte] of attendu.entries()) {
    await page.keyboard.press('ArrowRight');
    await expect(ligne.nth(i)).toBeFocused();
    await expect(ligne.nth(i)).toHaveText(texte);
  }
});

test('l’écran de connexion n’a ni menu ni bouton de menu : le champ est atteint dès la deuxième tabulation', async ({
  page,
}) => {
  await simulerApi(page);
  await page.goto('/connexion');
  await expect(page.getByRole('heading', { name: 'Connexion' })).toBeVisible();
  await expect(page.getByRole('navigation')).toHaveCount(0);
  // Le panneau latéral est fermé : le contenu commence au bord, sans marge de 240 px.
  await expect(page.locator('mat-sidenav')).toBeHidden();
  const titre = await page.getByRole('heading', { name: 'Connexion' }).boundingBox();
  expect(titre!.x).toBeLessThan(100);

  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Aller au contenu' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel('Adresse électronique')).toBeFocused();
});

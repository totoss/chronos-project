import type { Page, Route } from '@playwright/test';
import type { Page as PageApi } from '../src/app/commun/page';
import type { Utilisateur } from '../src/app/compte/utilisateur';
import type {
  Absence,
  Affectation,
  FeuilleDeTemps,
  JourFerie,
  SaisieDeTemps,
  StatutFeuilleDeTemps,
} from '../src/app/feuilles-de-temps/feuille-de-temps';
import type { PlanDeChargeDto } from '../src/app/plan-de-charge/charge';
import type { Projet } from '../src/app/projets/projet';

export interface EtatApi {
  connecte: boolean;
  roles: Utilisateur['roles'];
  statut: StatutFeuilleDeTemps;
  saisies: SaisieDeTemps[];
  /** Le jeton reçu dans l'en-tête `X-XSRF-TOKEN` de la dernière écriture. */
  jetonRecu?: string;
  /** Le paramètre `pas` de chaque demande de plan de charge, dans l'ordre. */
  pasDemandes: string[];
  /** Les appels que l'API simulée ne connaît pas : un parcours vérifie qu'il n'y en a aucun. */
  appelsNonSimules: string[];
}

// Les réponses reprennent celles de la vraie API (document OpenAPI, appels réels avec les comptes de
// démonstration). Les types de l'application, qui en sont le miroir, en font vérifier la forme à la compilation.

const moi: Utilisateur = {
  collaborateurId: 2,
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  roles: ['Collaborateur'],
};

const affectations: Affectation[] = [
  {
    id: 1,
    collaborateurId: 2,
    projetId: 1,
    dateDebut: '2026-04-20',
    dateFin: '2026-12-18',
    tauxAllocation: 0.6,
  },
  {
    id: 2,
    collaborateurId: 2,
    projetId: 2,
    dateDebut: '2026-04-27',
    dateFin: '2026-12-31',
    tauxAllocation: 0.4,
  },
];

const projets: PageApi<Projet> = {
  elements: [
    {
      id: 1,
      code: 'BDL-REFONTE',
      libelle: 'Refonte du portail client',
      nature: 'Facturable',
      clientId: 1,
      clientNom: 'Banque du Littoral',
      dateDebut: '2026-03-02',
      dateFin: '2026-12-18',
      budgetJours: 240,
      responsableId: 1,
      responsableNom: 'Sophie Martin',
      statut: 'EnCours',
      version: '183c529c-97b0-4db1-8188-55f84c0fd61f',
      joursValides: 8.25,
    },
    {
      id: 2,
      code: 'INT-FORMATION',
      libelle: 'Formation interne',
      nature: 'Interne',
      clientId: null,
      clientNom: null,
      dateDebut: '2026-01-05',
      dateFin: null,
      budgetJours: 60,
      responsableId: 1,
      responsableNom: 'Sophie Martin',
      statut: 'EnCours',
      version: '40701001-2984-4828-b0b7-d40dcaac509a',
      joursValides: 2.5,
    },
  ],
  page: 1,
  taille: 100,
  total: 2,
};

const joursFeries: JourFerie[] = [{ date: '2026-05-08', libelle: 'Victoire 1945' }];

const absences: Absence[] = [];

/** Camille, en mai 2026 : la dernière semaine est en surcharge (RG-24). */
const planParSemaine: PlanDeChargeDto = {
  du: '2026-05-04',
  au: '2026-05-31',
  pas: 'semaine',
  lignes: [
    {
      collaborateurId: 2,
      nom: 'Durand',
      prenom: 'Camille',
      periodes: [
        {
          debut: '2026-05-04',
          fin: '2026-05-10',
          capacite: 3.2,
          chargePlanifiee: 3.2,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
        {
          debut: '2026-05-11',
          fin: '2026-05-17',
          capacite: 4,
          chargePlanifiee: 4,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
        {
          debut: '2026-05-18',
          fin: '2026-05-24',
          capacite: 4,
          chargePlanifiee: 4,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
        {
          debut: '2026-05-25',
          fin: '2026-05-31',
          capacite: 2.8,
          chargePlanifiee: 4,
          tauxDeCharge: 1.4286,
          surcharge: true,
          intercontrat: false,
        },
      ],
    },
  ],
};

const planParMois: PlanDeChargeDto = {
  du: '2026-05-01',
  au: '2026-08-31',
  pas: 'mois',
  lignes: [
    {
      collaborateurId: 2,
      nom: 'Durand',
      prenom: 'Camille',
      periodes: [
        {
          debut: '2026-05-01',
          fin: '2026-05-31',
          capacite: 14.8,
          chargePlanifiee: 16,
          tauxDeCharge: 1.0811,
          surcharge: true,
          intercontrat: false,
        },
        {
          debut: '2026-06-01',
          fin: '2026-06-30',
          capacite: 17.6,
          chargePlanifiee: 17.6,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
        {
          debut: '2026-07-01',
          fin: '2026-07-31',
          capacite: 18.4,
          chargePlanifiee: 18.4,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
        {
          debut: '2026-08-01',
          fin: '2026-08-31',
          capacite: 16.8,
          chargePlanifiee: 16.8,
          tauxDeCharge: 1,
          surcharge: false,
          intercontrat: false,
        },
      ],
    },
  ],
};

/**
 * Remplace l'API par des réponses simulées. Renvoie l'état, que le test peut lire et modifier.
 * Tout appel de `/api/` qui n'est pas simulé échoue (501) et se retrouve dans `appelsNonSimules` :
 * il ne part jamais vers une vraie API.
 */
export async function simulerApi(page: Page, initial: Partial<EtatApi> = {}): Promise<EtatApi> {
  const etat: EtatApi = {
    connecte: false,
    roles: ['Collaborateur'],
    statut: 'Brouillon',
    saisies: [],
    pasDemandes: [],
    appelsNonSimules: [],
    ...initial,
  };
  const feuille = (): FeuilleDeTemps => ({
    id: 42,
    collaborateurId: 2,
    semaine: '2026-05-04',
    statut: etat.statut,
    motifRejet: null,
    saisies: etat.saisies,
  });

  // Les polices viennent d'Internet : le parcours n'en dépend pas.
  await page.route(/fonts\.(googleapis|gstatic)\.com/, (route) => route.abort());

  // Quand plusieurs routes correspondent, la dernière enregistrée passe en premier : celle-ci, la première,
  // ne reçoit donc que les appels qu'aucune des suivantes ne connaît.
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => {
      const requete = route.request();
      etat.appelsNonSimules.push(`${requete.method()} ${requete.url()}`);
      return route.fulfill({ status: 501 });
    },
  );

  const repondre = (methode: string, chemin: string, reponse: (route: Route) => Promise<void>) =>
    page.route(
      (url) => url.pathname === chemin,
      (route) => (route.request().method() === methode ? reponse(route) : route.fallback()),
    );

  await repondre('GET', '/api/compte/moi', (route) =>
    etat.connecte
      ? route.fulfill({
          json: { ...moi, roles: etat.roles },
          headers: { 'Set-Cookie': 'XSRF-TOKEN=jeton-de-test; Path=/' },
        })
      : route.fulfill({ status: 401 }),
  );
  await repondre('POST', '/api/compte/login', (route) => {
    etat.connecte = true;
    return route.fulfill({ status: 200 });
  });
  await repondre('GET', '/api/affectations', (route) => route.fulfill({ json: affectations }));
  await repondre('GET', '/api/projets', (route) => route.fulfill({ json: projets }));
  await repondre('GET', '/api/jours-feries', (route) => route.fulfill({ json: joursFeries }));
  await repondre('GET', '/api/absences', (route) => route.fulfill({ json: absences }));
  await repondre('GET', '/api/feuilles-de-temps', (route) => route.fulfill({ json: feuille() }));
  await repondre('PUT', '/api/feuilles-de-temps/42/saisies', (route) => {
    etat.saisies = route.request().postDataJSON().saisies;
    etat.jetonRecu = route.request().headers()['x-xsrf-token'];
    return route.fulfill({ json: feuille() });
  });
  await repondre('POST', '/api/feuilles-de-temps/42/soumission', (route) => {
    etat.statut = 'Soumise';
    return route.fulfill({ json: feuille() });
  });
  await repondre('GET', '/api/plan-de-charge', (route) => {
    const pas = new URL(route.request().url()).searchParams.get('pas') ?? '';
    etat.pasDemandes.push(pas);
    return route.fulfill({ json: pas === 'mois' ? planParMois : planParSemaine });
  });

  return etat;
}

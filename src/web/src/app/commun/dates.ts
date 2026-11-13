/**
 * Dates calendaires au format ISO « aaaa-mm-jj », comme les échange l'API.
 * Tous les calculs passent par l'UTC : le résultat ne dépend pas du fuseau du poste.
 */

function versDate(iso: string): Date {
  return new Date(`${iso}T00:00:00Z`);
}

function versIso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function ajouterJours(iso: string, jours: number): string {
  const date = versDate(iso);
  date.setUTCDate(date.getUTCDate() + jours);
  return versIso(date);
}

/** Le lundi de la semaine qui contient la date. */
export function lundiDe(iso: string): string {
  const jour = versDate(iso).getUTCDay(); // 0 = dimanche, 1 = lundi … 6 = samedi
  return ajouterJours(iso, -((jour + 6) % 7));
}

export function estWeekEnd(iso: string): boolean {
  const jour = versDate(iso).getUTCDay();
  return jour === 0 || jour === 6;
}

/** La date du jour, telle que la lit l'utilisateur sur son calendrier. */
export function aujourdhui(): string {
  const maintenant = new Date();
  return versIso(
    new Date(Date.UTC(maintenant.getFullYear(), maintenant.getMonth(), maintenant.getDate())),
  );
}

const formatJour = new Intl.DateTimeFormat('fr-FR', {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  timeZone: 'UTC',
});

/** « lun. 4 mai » */
export function libelleJour(iso: string): string {
  return formatJour.format(versDate(iso));
}

/** Le dernier jour du mois qui contient la date, décalé de `mois` mois (0 : le mois de la date). */
export function finDuMois(iso: string, mois = 0): string {
  const date = versDate(iso);
  return versIso(new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + mois + 1, 0)));
}

const formatMois = new Intl.DateTimeFormat('fr-FR', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
});

/** « mai 2026 » */
export function libelleMois(iso: string): string {
  return formatMois.format(versDate(iso));
}

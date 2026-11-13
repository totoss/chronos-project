import { computed, Service, signal } from '@angular/core';
import { ajouterJours, aujourdhui, lundiDe } from './dates';

/** La semaine affichée, partagée par la feuille de temps et le plan de charge. */
@Service()
export class SelectionSemaine {
  private readonly _semaine = signal(lundiDe(aujourdhui()));

  /** Le lundi de la semaine, au format de l'API : '2026-05-04'. */
  readonly semaine = this._semaine.asReadonly();

  /** Les sept jours de la semaine, du lundi au dimanche. */
  readonly jours = computed(() =>
    [0, 1, 2, 3, 4, 5, 6].map((ecart) => ajouterJours(this._semaine(), ecart)),
  );

  decaler(semaines: number): void {
    this._semaine.update((lundi) => ajouterJours(lundi, 7 * semaines));
  }

  allerA(date: string): void {
    this._semaine.set(lundiDe(date));
  }
}

import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-compteur-de-resultats',
  styleUrl: './compteur-de-resultats.scss',
  templateUrl: './compteur-de-resultats.html',
})
export class CompteurDeResultats {
  readonly nombre = input.required<number>();
  readonly total = input.required<number>();
  readonly nom = input('collaborateur');

  protected readonly texte = computed(() => {
    const nombre = this.nombre();
    const nom = this.nom();

    if (nombre === 0) {
      return `Aucun ${nom} sur ${this.total()}`;
    }

    return `${nombre} ${nombre > 1 ? `${nom}s` : nom} sur ${this.total()}`;
  });
}

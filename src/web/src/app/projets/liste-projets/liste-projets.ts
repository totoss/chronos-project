import { Component, inject, linkedSignal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { debounceTime, Subject } from 'rxjs';
import { BarreDeRecherche } from '../../commun/barre-de-recherche/barre-de-recherche';
import { CompteurDeResultats } from '../../commun/compteur-de-resultats/compteur-de-resultats';
import { CarteProjet } from '../carte-projet/carte-projet';
import { StatutProjet } from '../projet';
import { ProjetsApi } from '../projets-api';

@Component({
  imports: [
    BarreDeRecherche,
    CarteProjet,
    CompteurDeResultats,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    RouterLink,
  ],
  selector: 'app-liste-projets',
  styleUrl: './liste-projets.scss',
  templateUrl: './liste-projets.html',
})
export class ListeProjets {
  private readonly api = inject(ProjetsApi);

  protected readonly recherche = signal('');
  protected readonly statutFiltre = signal<StatutProjet | ''>('');
  protected readonly taille = signal(20);

  /** Revient à la première page dès que la recherche ou le filtre change. */
  protected readonly page = linkedSignal({
    source: () => ({ recherche: this.recherche(), statut: this.statutFiltre() }),
    computation: () => 1,
  });

  protected readonly projets = this.api.liste(() => ({
    page: this.page(),
    taille: this.taille(),
    recherche: this.recherche(),
    statut: this.statutFiltre(),
  }));

  /** Garde le dernier total connu pendant qu'une autre page se charge. */
  protected readonly total = linkedSignal<number | undefined, number>({
    source: () => (this.projets.hasValue() ? this.projets.value().total : undefined),
    computation: (total, precedent) => total ?? precedent?.value ?? 0,
  });

  /** Les frappes de l'utilisateur : un flux d'événements, temporisé avant d'agir. */
  private readonly saisies = new Subject<string>();

  constructor() {
    this.saisies
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((texte) => this.recherche.set(texte));
  }

  protected rechercher(texte: string): void {
    this.saisies.next(texte);
  }

  protected filtrerParStatut(valeur: string): void {
    this.statutFiltre.set(valeur as StatutProjet | '');
  }

  protected changerDePage(evenement: PageEvent): void {
    this.taille.set(evenement.pageSize);
    this.page.set(evenement.pageIndex + 1);
  }
}

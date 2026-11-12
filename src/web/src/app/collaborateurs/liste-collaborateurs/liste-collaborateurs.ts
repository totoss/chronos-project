import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { debounceTime, Subject } from 'rxjs';
import { BarreDeRecherche } from '../../commun/barre-de-recherche/barre-de-recherche';
import { CompteurDeResultats } from '../../commun/compteur-de-resultats/compteur-de-resultats';
import { demanderConfirmation } from '../../commun/dialogue-de-confirmation/dialogue-de-confirmation';
import { SiRole } from '../../compte/si-role';
import { CarteCollaborateur } from '../carte-collaborateur/carte-collaborateur';
import { Collaborateur } from '../collaborateur';
import { CollaborateursApi, TriCollaborateurs } from '../collaborateurs-api';

@Component({
  imports: [
    BarreDeRecherche,
    CarteCollaborateur,
    CompteurDeResultats,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    RouterLink,
    SiRole,
  ],
  selector: 'app-liste-collaborateurs',
  styleUrl: './liste-collaborateurs.scss',
  templateUrl: './liste-collaborateurs.html',
})
export class ListeCollaborateurs {
  private readonly api = inject(CollaborateursApi);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly recherche = input('', { transform: (valeur: string | undefined) => valeur ?? '' });

  /** `?statut=tous` affiche aussi les collaborateurs sortis ; sinon, seuls les actifs. */
  readonly statut = input('', { transform: (valeur: string | undefined) => valeur ?? '' });

  protected readonly afficherSortis = computed(() => this.statut() === 'tous');

  protected readonly tri = signal<TriCollaborateurs>('nom');
  protected readonly taille = signal(20);

  /** Revient à la première page dès que la recherche, le filtre ou le tri change. */
  protected readonly page = linkedSignal({
    source: () => ({
      recherche: this.recherche(),
      avecSortis: this.afficherSortis(),
      tri: this.tri(),
    }),
    computation: () => 1,
  });

  protected readonly collaborateurs = this.api.liste(() => ({
    page: this.page(),
    taille: this.taille(),
    recherche: this.recherche(),
    avecSortis: this.afficherSortis(),
    tri: this.tri(),
  }));

  /** Garde le dernier total connu pendant qu'une autre page se charge. */
  protected readonly total = linkedSignal<number | undefined, number>({
    source: () => (this.collaborateurs.hasValue() ? this.collaborateurs.value().total : undefined),
    computation: (total, precedent) => total ?? precedent?.value ?? 0,
  });

  /** Les frappes de l'utilisateur : un flux d'événements, temporisé avant d'agir. */
  private readonly saisies = new Subject<string>();

  constructor() {
    this.saisies
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((texte) => this.afficherRecherche(texte));
  }

  protected rechercher(texte: string): void {
    this.saisies.next(texte);
  }

  protected basculerSortis(): void {
    this.changerParametres({ statut: this.afficherSortis() ? null : 'tous' });
  }

  protected basculerTri(): void {
    this.tri.update((valeur) => (valeur === 'nom' ? 'matricule' : 'nom'));
  }

  protected changerDePage(evenement: PageEvent): void {
    this.taille.set(evenement.pageSize);
    this.page.set(evenement.pageIndex + 1);
  }

  protected async supprimer(collaborateur: Collaborateur): Promise<void> {
    const confirme = await demanderConfirmation(this.dialog, {
      titre: 'Supprimer le collaborateur',
      message: `Supprimer ${collaborateur.prenom} ${collaborateur.nom} ? Cette action est définitive.`,
      libelleConfirmer: 'Supprimer',
    });
    if (!confirme) return;

    this.api.supprimer(collaborateur.id).subscribe({
      next: () => this.collaborateurs.reload(),
      error: () => {
        // l'intercepteur a déjà notifié l'utilisateur
      },
    });
  }

  private afficherRecherche(texte: string): void {
    this.changerParametres({ recherche: texte === '' ? null : texte });
  }

  /** Ne change que les paramètres donnés (`null` retire un paramètre) et garde les autres. */
  private changerParametres(parametres: Params): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: parametres,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}

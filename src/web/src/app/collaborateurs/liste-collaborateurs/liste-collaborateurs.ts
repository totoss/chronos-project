import { Component, computed, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { BarreDeRecherche } from '../../commun/barre-de-recherche/barre-de-recherche';
import { CompteurDeResultats } from '../../commun/compteur-de-resultats/compteur-de-resultats';
import { contientSansAccents } from '../../commun/contient-sans-accents';
import { CarteCollaborateur } from '../carte-collaborateur/carte-collaborateur';
import { COLLABORATEURS_EN_DUR } from '../collaborateurs-en-dur';

@Component({
  imports: [
    BarreDeRecherche,
    CarteCollaborateur,
    CompteurDeResultats,
    MatButtonModule,
    MatIconModule,
    RouterLink,
  ],
  selector: 'app-liste-collaborateurs',
  styleUrl: './liste-collaborateurs.scss',
  templateUrl: './liste-collaborateurs.html',
})
export class ListeCollaborateurs {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly recherche = input('', { transform: (valeur: string | undefined) => valeur ?? '' });

  /** `?statut=tous` affiche aussi les collaborateurs sortis ; sinon, seuls les actifs. */
  readonly statut = input('', { transform: (valeur: string | undefined) => valeur ?? '' });

  protected readonly collaborateurs = signal(COLLABORATEURS_EN_DUR);

  protected readonly afficherSortis = computed(() => this.statut() === 'tous');

  protected readonly tri = signal<'nom' | 'dateEntree'>('nom');

  protected readonly collaborateursFiltres = computed(() => {
    const texte = this.recherche().trim();
    const avecSortis = this.afficherSortis();
    const critere = this.tri();

    // `filter` renvoie un nouveau tableau : `sort` ne modifie pas celui du signal.
    return this.collaborateurs()
      .filter(
        (c) =>
          (avecSortis || c.dateSortie === null) &&
          (contientSansAccents(c.nom, texte) ||
            contientSansAccents(c.prenom, texte) ||
            contientSansAccents(c.matricule, texte)),
      )
      .sort((a, b) =>
        critere === 'nom'
          ? a.nom.localeCompare(b.nom, 'fr')
          : a.dateEntree.localeCompare(b.dateEntree),
      );
  });

  protected readonly repartition = computed(() => {
    const liste = this.collaborateursFiltres();
    const consultants = liste.filter((c) => c.categorie === 'Consultant').length;
    const internes = liste.length - consultants;

    return `${consultants} ${consultants > 1 ? 'consultants' : 'consultant'}, ${internes} ${internes > 1 ? 'internes' : 'interne'}`;
  });

  protected rechercher(texte: string): void {
    this.changerParametres({ recherche: texte === '' ? null : texte });
  }

  protected basculerSortis(): void {
    this.changerParametres({ statut: this.afficherSortis() ? null : 'tous' });
  }

  protected basculerTri(): void {
    this.tri.update((valeur) => (valeur === 'nom' ? 'dateEntree' : 'nom'));
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

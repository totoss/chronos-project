import { afterNextRender, Component, computed, inject, Injector, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { distinctUntilChanged, filter, map, skip } from 'rxjs';
import { Session } from './compte/session';
import { Role } from './compte/utilisateur';

interface LienDeNavigation {
  chemin: string;
  libelle: string;
  icone: string;
  /** Les rôles qui ont droit à ce lien ; absent : tout utilisateur connecté. */
  roles?: Role[];
}

@Component({
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly titre = signal('Chronos');
  protected readonly session = inject(Session);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  private readonly liens: LienDeNavigation[] = [
    { chemin: '/collaborateurs', libelle: 'Collaborateurs', icone: 'group' },
    { chemin: '/projets', libelle: 'Projets', icone: 'folder' },
    {
      chemin: '/plan-de-charge',
      libelle: 'Plan de charge',
      icone: 'bar_chart',
      roles: ['Manager', 'GestionnaireRh'],
    },
    { chemin: '/feuille-de-temps', libelle: 'Feuille de temps', icone: 'schedule' },
    {
      chemin: '/feuilles-a-valider',
      libelle: 'Feuilles à valider',
      icone: 'fact_check',
      roles: ['Manager', 'GestionnaireRh'],
    },
    { chemin: '/absences', libelle: 'Absences', icone: 'event_busy' },
  ];

  /** Ne garde que les liens permis : le menu suit la session, à la connexion comme à la déconnexion. */
  protected readonly liensVisibles = computed(() =>
    this.session.estConnecte()
      ? this.liens.filter((lien) => !lien.roles || this.session.aLeRole(...lien.roles))
      : [],
  );

  constructor() {
    // Dans une application à page unique, changer d'écran ne déplace pas le focus : on l'envoie sur
    // le titre du nouvel écran. Seul un changement de chemin compte : écrire la recherche ou un filtre
    // dans l'adresse ne doit pas arracher le focus à l'utilisateur qui est en train de taper.
    this.router.events
      .pipe(
        filter((evenement) => evenement instanceof NavigationEnd),
        map((evenement) => evenement.urlAfterRedirects.split(/[?#]/)[0]),
        distinctUntilChanged(),
        skip(1), // la première navigation : le navigateur a déjà placé le focus
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        afterNextRender(() => this.donnerLeFocusAuTitre(), { injector: this.injector });
      });
  }

  /** Le lien d'évitement : il mène au titre de l'écran, sans passer par la barre ni par le menu. */
  protected allerAuContenu(evenement: Event): void {
    evenement.preventDefault();
    this.donnerLeFocusAuTitre();
  }

  protected async seDeconnecter(): Promise<void> {
    await this.session.deconnecter();
    await this.router.navigateByUrl('/connexion');
  }

  private donnerLeFocusAuTitre(): void {
    document.querySelector<HTMLElement>('[role=main] h2')?.focus();
  }
}

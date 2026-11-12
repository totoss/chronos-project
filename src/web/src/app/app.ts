import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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
    this.liens.filter((lien) => !lien.roles || this.session.aLeRole(...lien.roles)),
  );

  protected async seDeconnecter(): Promise<void> {
    await this.session.deconnecter();
    await this.router.navigateByUrl('/connexion');
  }
}

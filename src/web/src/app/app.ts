import { Component, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

interface LienDeNavigation {
  chemin: string;
  libelle: string;
  icone: string;
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

  protected readonly liens: LienDeNavigation[] = [
    { chemin: '/collaborateurs', libelle: 'Collaborateurs', icone: 'group' },
    { chemin: '/projets', libelle: 'Projets', icone: 'folder' },
    { chemin: '/plan-de-charge', libelle: 'Plan de charge', icone: 'bar_chart' },
    { chemin: '/feuille-de-temps', libelle: 'Feuille de temps', icone: 'schedule' },
    { chemin: '/feuilles-a-valider', libelle: 'Feuilles à valider', icone: 'fact_check' },
    { chemin: '/absences', libelle: 'Absences', icone: 'event_busy' },
  ];
}

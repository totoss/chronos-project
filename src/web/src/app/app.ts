import { Component, signal } from '@angular/core';
import { ListeCollaborateurs } from './collaborateurs/liste-collaborateurs/liste-collaborateurs';
import { ListeProjets } from './projets/liste-projets/liste-projets';

@Component({
  imports: [ListeCollaborateurs, ListeProjets],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly titre = signal('Chronos');
}

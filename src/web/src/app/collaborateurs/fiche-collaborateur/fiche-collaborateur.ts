import { DatePipe, PercentPipe } from '@angular/common';
import { Component, computed, inject, input, numberAttribute } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { ErreurApi } from '../../commun/erreur-api';
import { CollaborateursApi } from '../collaborateurs-api';

@Component({
  imports: [
    DatePipe,
    PercentPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressBarModule,
    RouterLink,
  ],
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {
  private readonly api = inject(CollaborateursApi);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly identifiantValide = computed(() => !Number.isNaN(this.id()));

  /** Pour un identifiant qui n'est pas un nombre, aucune requête ne part. */
  protected readonly collaborateur = this.api.parId(() =>
    this.identifiantValide() ? this.id() : undefined,
  );

  protected readonly introuvable = computed(() => {
    const erreur = this.collaborateur.error();
    return erreur instanceof ErreurApi && erreur.statut === 404;
  });
}

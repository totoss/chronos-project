import { DatePipe, PercentPipe } from '@angular/common';
import { Component, computed, input, numberAttribute } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { COLLABORATEURS_EN_DUR } from '../collaborateurs-en-dur';

@Component({
  imports: [
    DatePipe,
    PercentPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    RouterLink,
  ],
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {
  readonly id = input.required({ transform: numberAttribute });

  protected readonly identifiantValide = computed(() => !Number.isNaN(this.id()));

  protected readonly collaborateur = computed(() =>
    COLLABORATEURS_EN_DUR.find((c) => c.id === this.id()),
  );
}

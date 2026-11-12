import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  selector: 'app-carte-collaborateur',
  styleUrl: './carte-collaborateur.scss',
  templateUrl: './carte-collaborateur.html',
})
export class CarteCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();

  protected readonly nomComplet = computed(
    () => `${this.collaborateur().prenom} ${this.collaborateur().nom}`,
  );
  protected readonly estSorti = computed(() => this.collaborateur().dateSortie !== null);
}

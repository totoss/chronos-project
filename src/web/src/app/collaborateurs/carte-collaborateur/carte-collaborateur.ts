import { DatePipe } from '@angular/common';
import { booleanAttribute, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule],
  selector: 'app-carte-collaborateur',
  styleUrl: './carte-collaborateur.scss',
  templateUrl: './carte-collaborateur.html',
})
export class CarteCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();
  readonly active = input(false, { transform: booleanAttribute });
  readonly selection = output<Collaborateur>();

  protected readonly nomComplet = computed(
    () => `${this.collaborateur().prenom} ${this.collaborateur().nom}`,
  );
  protected readonly estSorti = computed(() => this.collaborateur().dateSortie !== null);
}

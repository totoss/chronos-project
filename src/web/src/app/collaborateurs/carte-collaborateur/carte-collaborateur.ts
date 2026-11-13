import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { SiRole } from '../../compte/si-role';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink, SiRole],
  selector: 'app-carte-collaborateur',
  styleUrl: './carte-collaborateur.scss',
  templateUrl: './carte-collaborateur.html',
})
export class CarteCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();
  readonly supprimer = output<void>();

  protected readonly nomComplet = computed(
    () => `${this.collaborateur().prenom} ${this.collaborateur().nom}`,
  );
}

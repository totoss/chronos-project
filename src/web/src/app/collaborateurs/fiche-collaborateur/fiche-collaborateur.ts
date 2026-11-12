import { DatePipe, PercentPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [DatePipe, PercentPipe, MatCardModule, MatIconModule, MatListModule],
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();
}

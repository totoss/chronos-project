import { Component, input, model } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

@Component({
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  selector: 'app-barre-de-recherche',
  styleUrl: './barre-de-recherche.scss',
  templateUrl: './barre-de-recherche.html',
})
export class BarreDeRecherche {
  readonly libelle = input('Rechercher');
  readonly recherche = model('');
}

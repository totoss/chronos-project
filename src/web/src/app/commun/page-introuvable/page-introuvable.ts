import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  imports: [MatButtonModule, RouterLink],
  selector: 'app-page-introuvable',
  styleUrl: './page-introuvable.scss',
  templateUrl: './page-introuvable.html',
})
export class PageIntrouvable {}

// TODO [module 15] Écran du joker '**' (le cours le fait créer par `ng generate component
//    commun/page-introuvable` : fichier déjà fourni, ne relancez pas la commande) : ajouter les
//    boutons Material et `RouterLink` aux `imports`.
//    — cours/15-routage.md, Pas à pas, étape 4 « Deux écrans provisoires »
// TODO [module 15] Test, page-introuvable.spec.ts : créer à la main le fichier : test « se crée »,
//    avec le routeur (`RouterLink`).
//    — cours/15-routage.md, Pas à pas, étape 4 « Deux écrans provisoires »
import { Component } from '@angular/core';

@Component({
  selector: 'app-page-introuvable',
  styleUrl: './page-introuvable.scss',
  templateUrl: './page-introuvable.html',
})
export class PageIntrouvable {}

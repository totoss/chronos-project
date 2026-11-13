// TODO [module 14] Champ de recherche réutilisable, qui ne sait pas ce qu'on filtre : entrée
//    `libelle` (« Rechercher » par défaut) et modèle `recherche` à deux sens.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 3 « Extraire la barre de recherche »
// TODO [module 14] Test, barre-de-recherche.spec.ts : créer à la main le fichier : test « se
//    crée ».
//    — cours/14-composer-des-composants.md, Pas à pas, étape 3 « Extraire la barre de recherche »
import { Component } from '@angular/core';

@Component({
  selector: 'app-barre-de-recherche',
  styleUrl: './barre-de-recherche.scss',
  templateUrl: './barre-de-recherche.html',
})
export class BarreDeRecherche {}

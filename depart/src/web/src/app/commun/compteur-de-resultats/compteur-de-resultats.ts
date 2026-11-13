// TODO [module 14] Deux entrées OBLIGATOIRES (nombre affiché, total) et un `computed` qui accorde
//    le texte (« 1 collaborateur sur 5 », « Aucun collaborateur ») ; le mot « collaborateur » doit
//    pouvoir être remplacé par « projet ».
//    — cours/14-composer-des-composants.md, À vous, exercice 1 « Extraire le compteur. »
// TODO [module 14] Test, compteur-de-resultats.spec.ts : créer à la main le fichier : accord au
//    singulier et au pluriel, « Aucun » quand il n'y a aucun résultat, un autre mot que
//    « collaborateur ».
//    — cours/14-composer-des-composants.md, non détaillé dans le module : comparer avec la solution au tag `module-14`
import { Component } from '@angular/core';

@Component({
  selector: 'app-compteur-de-resultats',
  styleUrl: './compteur-de-resultats.scss',
  templateUrl: './compteur-de-resultats.html',
})
export class CompteurDeResultats {}

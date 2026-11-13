// TODO [module 14] Composant de présentation : entrée obligatoire `projet`, `computed` « est clos »
//    (terminé ou annulé). Client affiché pour un projet facturable, aucun pour un projet interne
//    (RG-11).
//    — cours/14-composer-des-composants.md, À vous, exercice 5 « Les projets. »
// TODO [module 14] Test, carte-projet.spec.ts : créer à la main le fichier : test « se crée »,
//    client d'un projet facturable, mention « Projet interne ».
//    — cours/14-composer-des-composants.md, non détaillé dans le module : comparer avec la solution au tag `module-14`
// TODO [module 16] Test, carte-projet.spec.ts : adapter les données du test au nouveau type
//    `Projet` (client par son nom).
//    — cours/16-dialoguer-avec-l-api.md, non détaillé dans le module : comparer avec la solution au tag `module-16`
import { Component } from '@angular/core';

@Component({
  selector: 'app-carte-projet',
  styleUrl: './carte-projet.scss',
  templateUrl: './carte-projet.html',
})
export class CarteProjet {}

// TODO [module 18] Composant de PRÉSENTATION (le cours le fait créer par `ng generate component
//    feuilles-de-temps/grille-de-saisie` : fichier déjà fourni, ne relancez pas la commande) :
//    entrées jours, lignes, jours fermés, lecture seule (RG-35) ; il ne connaît pas l'API.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 6 « La grille, composant de présentation »
// TODO [module 18] Un `model` obligatoire `grille`, lié à deux sens par le parent ; totaux par jour
//    et par ligne en `computed`.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 6 « La grille, composant de présentation »
// TODO [module 18] La saisie crée un NOUVEL objet : le changement de référence prévient les
//    `computed`. Case ouverte seulement si une affectation du projet couvre le jour (RG-33).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 6 « La grille, composant de présentation »
// TODO [module 18] Un jour d'absence le matin ou l'après-midi : que devient la liste des durées
//    proposées ce jour-là ?
//    — cours/18-ecrans-metier.md, À vous, exercice 1 « Les absences (RG-32). »
// TODO [module 18] Test, grille-de-saisie.spec.ts : créer à la main le fichier : lignes et listes
//    par jour, quarts de journée (RG-30), totaux, dépassement écrit (RG-31), jours fermés et hors
//    affectation, lecture seule, demi-journée d'absence.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Importer `Grid`, `GridRow`, `GridCell` et `GridCellWidget` de
//    `@angular/aria/grid` : le tableau se parcourt aux flèches.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 8 « Parcourir la grille aux flèches »
// TODO [module 21] Test, grille-de-saisie.spec.ts : grille au sens d'Angular Aria aux en-têtes
//    préservés ; un jour fermé affiche un texte, pas une liste ; lecture seule par valeurs.
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { Component } from '@angular/core';

@Component({
  selector: 'app-grille-de-saisie',
  styleUrl: './grille-de-saisie.scss',
  templateUrl: './grille-de-saisie.html',
})
export class GrilleDeSaisie {}

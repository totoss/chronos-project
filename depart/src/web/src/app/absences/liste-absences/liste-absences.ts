// TODO [module 15] Composant provisoire de la route /absences : un titre et une phrase « Cet écran
//    n'est pas encore construit ». La solution de référence le laisse ainsi.
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
// TODO [module 15] Test, liste-absences.spec.ts : créer à la main le fichier : test « se crée ».
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
import { Component } from '@angular/core';

@Component({
  selector: 'app-liste-absences',
  styleUrl: './liste-absences.scss',
  templateUrl: './liste-absences.html',
})
export class ListeAbsences {}

// Facultatif : la solution de référence ne contient pas cet écran. L'étape « Finitions » propose de le
// construire (liste, demande d'absence, approbation) avec l'API du module 10.
// — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 10 « Finitions »

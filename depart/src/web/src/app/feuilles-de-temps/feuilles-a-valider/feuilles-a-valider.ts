// TODO [module 18] Écran du manager (RG-36), route /feuilles-a-valider : la liste vient de
//    `FeuillesDeTempsApi.soumises()` ; la feuille choisie s'affiche dans la grille en lecture
//    seule, avec le nom du collaborateur.
//    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
// TODO [module 18] Valider (sans corps) et rejeter (dialogue de motif ; rien d'envoyé si l'on
//    annule) ; la feuille quitte la liste : la redemander.
//    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
// TODO [module 18] Test, feuilles-a-valider.spec.ts : rien à valider, liste des feuilles, relecture
//    en lecture seule, validation, rejet avec motif, annulation.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Annoncer le résultat aux lecteurs d'écran et rendre le focus au titre, car les
//    boutons disparaissent avec la feuille.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 5 « Annoncer ce qui change »
// TODO [module 21] Test, feuilles-a-valider.spec.ts : doublure du `LiveAnnouncer` ; la grille n'a
//    plus de listes désactivées ; focus sur le titre après la validation.
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { Component } from '@angular/core';

@Component({
  selector: 'app-feuilles-a-valider',
  styleUrl: './feuilles-a-valider.scss',
  templateUrl: './feuilles-a-valider.html',
})
export class FeuillesAValider {}

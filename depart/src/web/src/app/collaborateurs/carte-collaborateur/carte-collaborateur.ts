// TODO [module 14] Composant de présentation : entrée OBLIGATOIRE `collaborateur`, nom complet
//    dérivé par un `computed`, sortie `selection` émise au clic.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 4 « La carte d'un collaborateur »
// TODO [module 14] Entrée `active` pour colorer la carte choisie. Imports : boutons, carte, icônes.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 6 « Assembler : la sélection »
// TODO [module 14] Test, carte-collaborateur.spec.ts : créer à la main le fichier : test « se
//    crée », en fournissant l'entrée obligatoire (NG0950).
//    — cours/14-composer-des-composants.md, Pas à pas, étape 4 « La carte d'un collaborateur »
// TODO [module 15] Supprimer l'entrée `active` et la sortie `selection` : le bouton devient un lien
//    vers la fiche (`RouterLink`).
//    — cours/15-routage.md, Pas à pas, étape 8 « Des liens à la place de la sélection »
// TODO [module 15] Test, carte-collaborateur.spec.ts : fournir le routeur (`provideRouter([])`), la
//    carte utilise `RouterLink`.
//    — cours/15-routage.md, Pas à pas, étape 8 « Des liens à la place de la sélection »
// TODO [module 16] Ajouter la sortie `supprimer`, émise par un bouton « Supprimer ».
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 9 « Supprimer, puis recharger »
// TODO [module 16] Afficher en TEXTE « A quitté la société » quand `estSorti` est vrai (la liste ne
//    porte plus les dates).
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs »
// TODO [module 16] Test, carte-collaborateur.spec.ts : pas de date d'entrée affichée sans date,
//    mention textuelle d'un collaborateur sorti, sortie `supprimer` émise au clic.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 19] Réserver « Supprimer » au gestionnaire RH par la directive `SiRole` (RG-52).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 10 « RG-52 : ne proposer la modification qu'à qui peut la faire »
// TODO [module 19] Test, carte-collaborateur.spec.ts : suppression proposée au seul gestionnaire RH
//    (RG-52), avec une doublure de `Session`.
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
import { Component } from '@angular/core';

@Component({
  selector: 'app-carte-collaborateur',
  styleUrl: './carte-collaborateur.scss',
  templateUrl: './carte-collaborateur.html',
})
export class CarteCollaborateur {}

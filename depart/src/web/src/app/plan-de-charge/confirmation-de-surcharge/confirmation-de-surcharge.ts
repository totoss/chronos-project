// TODO [module 18] Dialogue de confirmation d'une surcharge (RG-24), gabarit en ligne : titre,
//    texte qui nomme collaborateur, taux de charge et période, boutons « Annuler » et
//    « Confirmer ».
//    — cours/18-ecrans-metier.md, Pas à pas, étape 10 « Confirmer une surcharge »
// TODO [module 18] Données : interface (collaborateur, période, taux) lue par `MAT_DIALOG_DATA` ;
//    `afterClosed()` émet vrai, faux ou `undefined` (Échap).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 10 « Confirmer une surcharge »
// TODO [module 18] Test, confirmation-de-surcharge.spec.ts : le texte nomme le collaborateur, le
//    taux et la période.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Examiner ce dialogue au clavier, puis celui du rejet d'une feuille : focus à
//    l'ouverture, sortie par Tab et par Échap, retour du focus à la fermeture.
//    — cours/21-accessibilite-et-finitions.md, À vous, exercice 4 « Les dialogues. »
// TODO [module 21] Test, confirmation-de-surcharge.spec.ts : adapter l'attendu du taux au format
//    français.
//    — cours/21-accessibilite-et-finitions.md, À vous, exercice 1 « Partout en français. »
import { Component } from '@angular/core';

@Component({
  selector: 'app-confirmation-de-surcharge',
  template: '',
})
export class ConfirmationDeSurcharge {}

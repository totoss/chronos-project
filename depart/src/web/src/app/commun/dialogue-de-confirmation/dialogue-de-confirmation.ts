// TODO [module 18] Dialogue de confirmation GÉNÉRIQUE (titre, message, libellé du bouton de
//    confirmation), gabarit en ligne, données lues dans `MAT_DIALOG_DATA`.
//    — cours/18-ecrans-metier.md, À vous, exercice 2 « Confirmer avant de supprimer. »
// TODO [module 18] Ce que le dialogue rend à l'écran qui l'ouvre : une confirmation, ou non (Échap
//    et « Annuler » valent un refus).
//    — cours/18-ecrans-metier.md, À vous, exercice 2 « Confirmer avant de supprimer. »
// TODO [module 18] Test, dialogue-de-confirmation.spec.ts : la question s'affiche et `true` est
//    rendu à la confirmation ; `false` à « Annuler » et à Échap.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
import { Component } from '@angular/core';

@Component({
  selector: 'app-dialogue-de-confirmation',
  template: '',
})
export class DialogueDeConfirmation {}

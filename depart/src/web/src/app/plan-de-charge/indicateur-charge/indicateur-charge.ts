// TODO [module 18] Composant de présentation, gabarit en ligne : entrée obligatoire `charge`,
//    entrée `confirmee`, sortie `confirmer`.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 9 « Le plan de charge »
// TODO [module 18] Niveau et libellé en `computed` ; le niveau est posé comme classe CSS de
//    l'élément hôte (propriété `host` du décorateur).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 9 « Le plan de charge »
// TODO [module 18] Gabarit : taux (si non nul, par le pipe `percent`), libellé du niveau TOUJOURS ;
//    pour une surcharge, bouton « Confirmer » qui émet `confirmer`, ou mention « confirmée ».
//    — cours/18-ecrans-metier.md, Pas à pas, étape 10 « Confirmer une surcharge »
// TODO [module 18] Test, indicateur-charge.spec.ts : charge normale sans bouton, « Surcharge » en
//    toutes lettres, bouton qui émet `confirmer`, « confirmée », « Intercontrat » et
//    « Indisponible ».
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Le bouton « Confirmer » dit-il quelle surcharge il confirme à qui ne voit pas la
//    case ? Corriger.
//    — cours/21-accessibilite-et-finitions.md, À vous, exercice 2 « Le plan de charge. »
// TODO [module 21] Quand le bouton « Confirmer » disparaît, le focus passe à la mention
//    « confirmée » s'il s'est perdu.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 7 « Maîtriser le focus »
// TODO [module 21] Test, indicateur-charge.spec.ts : le bouton dit quelle surcharge il confirme ;
//    taux attendu au format français (« 133 % », espace insécable).
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { Component } from '@angular/core';

@Component({
  selector: 'app-indicateur-charge',
  styleUrl: './indicateur-charge.scss',
  template: '',
})
export class IndicateurCharge {}

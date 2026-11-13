// TODO [module 18] Dialogue (gabarit et styles en ligne) qui contient un Signal Form pour le motif
//    du rejet (RG-34).
//    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
// TODO [module 18] Deux règles de `RejeterFeuilleRequete` : motif obligatoire (des espaces ne
//    comptent pas) et 500 caractères au plus. Se ferme sur le motif nettoyé, ou sans rien.
//    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
// TODO [module 18] Test, dialogue-de-rejet.spec.ts : motif rendu sans espaces autour ; plus de 500
//    caractères refusé ; motif vide refusé (RG-34).
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 21] Relier l'erreur au champ : libellé relié par `for` et `id`, champ obligatoire et
//    invalide déclarés, message désigné par le champ, annoncé à son apparition.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 4 « Relier chaque erreur à son champ »
// TODO [module 21] Test, dialogue-de-rejet.spec.ts : le message d'erreur est relié au champ.
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
import { Component } from '@angular/core';

@Component({
  selector: 'app-dialogue-de-rejet',
  template: '',
})
export class DialogueDeRejet {}

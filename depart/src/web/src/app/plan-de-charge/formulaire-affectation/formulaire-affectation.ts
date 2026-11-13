// TODO [module 18] Dialogue de création d'une affectation en Signal Form : collaborateur, projet,
//    dates, taux saisi en pourcentage (1 à 100), tous obligatoires ; fin non antérieure au début.
//    — cours/18-ecrans-metier.md, À vous, exercice 4 « Une confirmation qui compte. »
// TODO [module 18] Sur un 422 de règle RG-24 : ouvrir le dialogue de confirmation générique, puis
//    renvoyer la requête confirmée ; un refus laisse le formulaire ouvert ; tout autre refus
//    s'affiche dans le formulaire. Se ferme sur `true` quand l'API a enregistré.
//    — cours/18-ecrans-metier.md, À vous, exercice 4 « Une confirmation qui compte. »
// TODO [module 18] Test, formulaire-affectation.spec.ts : création puis fermeture, 422 RG-24 avec
//    confirmation puis requête confirmée, refus de la surcharge, autre refus affiché, formulaire
//    incomplet.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
import { Component } from '@angular/core';

@Component({
  selector: 'app-formulaire-affectation',
  styleUrl: './formulaire-affectation.scss',
  templateUrl: './formulaire-affectation.html',
})
export class FormulaireAffectation {}

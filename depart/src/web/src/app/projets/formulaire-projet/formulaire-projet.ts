// TODO [module 17] Création d'un projet en Signal Form : modèle initialisé ; champs obligatoires
//    (code, libellé, date de début, budget, responsable).
//    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
// TODO [module 17] RG-11 : client obligatoire pour un projet facturable, champ absent pour un
//    projet interne. RG-12 : la date de fin ne précède pas la date de début.
//    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
// TODO [module 17] Listes alimentées par l'API : clients (service d'accès à l'API des clients) et
//    responsables (collaborateurs en activité, 100 au plus).
//    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
// TODO [module 17] Soumission : `ProjetsApi.creer`, puis retour à /projets ; refus de l'API
//    traduits par `erreursDeFormulaire` (409 RG-10, 422 RG-11).
//    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
// TODO [module 17] Test, formulaire-projet.spec.ts : client exigé pour un projet facturable, RG-12,
//    ce qui est affiché et envoyé pour un projet interne, même après le choix d'un client, 409
//    (RG-10) et 422 (RG-11) affichés dans le formulaire.
//    — cours/17-formulaires.md, non détaillé dans le module : comparer avec la solution au tag `module-17`
import { Component } from '@angular/core';

@Component({
  selector: 'app-formulaire-projet',
  styleUrl: './formulaire-projet.scss',
  templateUrl: './formulaire-projet.html',
})
export class FormulaireProjet {}

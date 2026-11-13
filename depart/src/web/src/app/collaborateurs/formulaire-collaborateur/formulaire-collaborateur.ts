// TODO [module 15] Écran provisoire (le cours le fait créer par `ng generate component
//    collaborateurs/formulaire-collaborateur` : fichier déjà fourni, ne relancez pas la commande) :
//    son gabarit annonce que le formulaire sera construit au module 17.
//    — cours/15-routage.md, Pas à pas, étape 4 « Deux écrans provisoires »
// TODO [module 15] Test, formulaire-collaborateur.spec.ts : créer à la main le fichier : test « se
//    crée » de l'écran provisoire.
//    — cours/15-routage.md, Pas à pas, étape 4 « Deux écrans provisoires »
// TODO [module 17] Signal Form : modèle en `linkedSignal` recalculé quand l'entrée facultative
//    `collaborateur` change (absent : création) ; tous les champs initialisés ; quotité saisie en
//    POURCENTAGE, vide si l'API ne l'a pas envoyée.
//    — cours/17-formulaires.md, Pas à pas, étape 2 « Le modèle et un premier champ »
// TODO [module 17] Règles : champs obligatoires avec message en français, e-mail valide, RG-03
//    (quotité de 10 à 100 %), RG-02 (sortie non antérieure à l'entrée, erreur de `kind` « RG-02 »).
//    — cours/17-formulaires.md, Pas à pas, étape 3 « Les règles »
// TODO [module 17] En modification : matricule, catégorie, date d'entrée et quotité grisés, type de
//    contrat masqué ; en création : date de sortie masquée.
//    — cours/17-formulaires.md, Pas à pas, étape 7 « Créer et modifier avec le même composant »
// TODO [module 17] Soumission : création puis navigation vers la fiche, ou modification avec la
//    `version` lue puis sortie `enregistre` ; la conversion pourcentage vers fraction se fait ICI.
//    — cours/17-formulaires.md, Pas à pas, étape 5 « Soumettre »
// TODO [module 17] Traduire les refus de l'API (400, 409, 422) par `erreursDeFormulaire` et les
//    afficher dans le formulaire, sans notification.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API »
// TODO [module 17] Sorties `annule` et `recharger` : recharger n'est proposé qu'après un conflit de
//    version, pas après un doublon RG-01.
//    — cours/17-formulaires.md, Pas à pas, étape 8 « La modification concurrente »
// TODO [module 17] Le choix du manager : liste chargée depuis l'API ; provoquer RG-04 et observer
//    où s'affiche le refus.
//    — cours/17-formulaires.md, À vous, exercice 2 « Le manager. »
// TODO [module 17] « Annuler » demande confirmation quand la saisie a été modifiée ; que faire si
//    l'utilisateur quitte par le menu ?
//    — cours/17-formulaires.md, À vous, exercice 6 « Ne pas perdre une saisie. »
// TODO [module 17] Test, formulaire-collaborateur.spec.ts : création, modification et annulation :
//    erreurs, RG-01 à RG-04, 409 dans le formulaire, quotité en fraction, version, conflit ;
//    attendre avec `vi.waitFor`, pas avec `whenStable`.
//    — cours/17-formulaires.md, Pas à pas, étape 8 « La modification concurrente »
// TODO [module 21] En modification, le formulaire remplace la fiche : placer le focus sur son titre
//    après le premier rendu.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 7 « Maîtriser le focus »
import { Component } from '@angular/core';

@Component({
  selector: 'app-formulaire-collaborateur',
  styleUrl: './formulaire-collaborateur.scss',
  templateUrl: './formulaire-collaborateur.html',
})
export class FormulaireCollaborateur {}

// TODO [module 14] Composant de présentation de la fiche : entrée obligatoire `collaborateur`,
//    `<ng-content />` pour les actions projetées par le parent ; pipes `date` et `percent` ; liste
//    Material.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 5 « La fiche »
// TODO [module 14] Test, fiche-collaborateur.spec.ts : créer à la main le fichier : test « se
//    crée », en fournissant l'entrée obligatoire ; date d'entrée et quotité affichées.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 4 « La carte d'un collaborateur »
// TODO [module 15] La fiche devient une PAGE : entrée `id` (comme le paramètre `:id`) convertie en
//    nombre par `numberAttribute`.
//    — cours/15-routage.md, Pas à pas, étape 3 « La fiche devient une page »
// TODO [module 15] Elle retrouve le collaborateur dans des données partagées avec la liste (fichier
//    provisoire collaborateurs-en-dur.ts, non fourni ; supprimé au module 16).
//    — cours/15-routage.md, Pas à pas, étape 2 « Partager les données en dur »
// TODO [module 15] Test, fiche-collaborateur.spec.ts : l'identifiant arrive en chaîne (« 1 ») ;
//    fournir le routeur ; identifiant inconnu annoncé ; identifiant non numérique annoncé.
//    — cours/15-routage.md, Pas à pas, étape 3 « La fiche devient une page »
// TODO [module 16] Lire la fiche par `CollaborateursApi.parId` ; aucune requête pour un identifiant
//    non numérique. Déclarer `api` et l'identifiant valide AVANT la ressource.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 4 « Lire une fiche »
// TODO [module 16] Distinguer l'erreur 404 des autres erreurs (`ErreurApi`).
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 8 « Traduire et notifier les erreurs »
// TODO [module 16] Test, fiche-collaborateur.spec.ts : avec un faux serveur HTTP : dates et quotité
//    affichées ou absentes, 404, « Réessayer », identifiant non numérique sans requête.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 17] Un signal `enEdition` : `FormulaireCollaborateur` remplace la fiche ; sur
//    `enregistre`, remplacer la valeur de la ressource sans nouvel appel.
//    — cours/17-formulaires.md, Pas à pas, étape 7 « Créer et modifier avec le même composant »
// TODO [module 17] Sur la sortie `recharger` du formulaire, redemander la fiche.
//    — cours/17-formulaires.md, Pas à pas, étape 8 « La modification concurrente »
// TODO [module 17] Test, fiche-collaborateur.spec.ts : fiche enregistrée affichée sans nouvelle
//    requête ; rechargement après un conflit de version.
//    — cours/17-formulaires.md, Pas à pas, étape 8 « La modification concurrente »
// TODO [module 19] Injecter `Session` (champ `protected`) : « Modifier » n'est proposé qu'au
//    gestionnaire RH (RG-52).
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 10 « RG-52 : ne proposer la modification qu'à qui peut la faire »
// TODO [module 19] Test, fiche-collaborateur.spec.ts : « Modifier » proposé au seul gestionnaire RH
//    (RG-52).
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
// TODO [module 21] Quand on quitte l'édition, le bouton « Modifier » a disparu : rendre le focus au
//    titre de la fiche une fois le rendu fait.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 7 « Maîtriser le focus »
// TODO [module 21] Test, fiche-collaborateur.spec.ts : adapter l'attendu de la quotité au format
//    français (espace insécable avant « % »).
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 1 « Passer l'application en français »
import { Component } from '@angular/core';

@Component({
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {}

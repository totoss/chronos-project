// TODO [module 15] La route /feuille-de-temps pointe d'abord sur `FeuilleDeTempsProvisoire`
//    (dossier provisoire non fourni ; supprimé au module 18).
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
// TODO [module 18] Composant ÉCRAN (le cours le fait créer par `ng generate component
//    feuilles-de-temps/feuille-de-temps-hebdo` : fichier déjà fourni, ne relancez pas la commande)
//    : trois sources (`SelectionSemaine`, les réponses de l'API, la grille en cours) et tout le
//    reste en `computed`.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 7 « L'écran de la feuille de temps »
// TODO [module 18] Copie de travail des saisies en `linkedSignal` qui repart de la feuille à chaque
//    rechargement ; lignes (RG-33) ; jours fermés (week-ends, jours fériés : RG-32).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 7 « L'écran de la feuille de temps »
// TODO [module 18] Lecture seule si la feuille n'est plus en brouillon (RG-34, RG-35) ; libellé du
//    statut ; jours en dépassement (RG-31) ; signal `peutEnvoyer`.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 7 « L'écran de la feuille de temps »
// TODO [module 18] L'identifiant du collaborateur vient d'un fichier provisoire
//    (commun/collaborateur-courant.ts, non fourni ; supprimé au module 19).
//    — cours/18-ecrans-metier.md, Pas à pas, étape 3 « Le collaborateur courant, provisoirement »
// TODO [module 18] `enregistrer` (sans cases vides) et `soumettre` (enregistre puis soumet) ; le
//    bouton ne reste jamais bloqué ; un refus laisse la saisie à l'écran.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 8 « Enregistrer, puis soumettre »
// TODO [module 18] Fermer aussi les jours d'absence approuvée. Un jour de demi-absence, quel total
//    l'écran admet-il, sur l'ensemble des projets ?
//    — cours/18-ecrans-metier.md, À vous, exercice 1 « Les absences (RG-32). »
// TODO [module 18] Test, feuille-de-temps-hebdo.spec.ts : feuille de la semaine, une ligne par
//    affectation, jours fermés, état et motif de rejet, dépassement bloquant, enregistrement,
//    soumission, refus, changement de semaine (faux serveur HTTP).
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 19] L'identifiant vient de `Session.collaborateurId` (peut être `null`) ; sans
//    fiche, aucune requête et un message d'explication ; supprimer le fichier provisoire.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 11 « Remplacer `COLLABORATEUR_COURANT_ID` »
// TODO [module 19] Test, feuille-de-temps-hebdo.spec.ts : collaborateur de la session et non un
//    identifiant en dur ; aucune requête pour un compte sans fiche.
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
// TODO [module 21] Annoncer le résultat aux lecteurs d'écran (« Brouillon enregistré. », « Feuille
//    de temps soumise. »).
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 5 « Annoncer ce qui change »
// TODO [module 21] Une fois la feuille soumise, ses boutons disparaissent : le focus va au titre.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 5 « Annoncer ce qui change »
// TODO [module 21] Les boutons restent cliquables quand ils sont indisponibles : `enregistrer` et
//    `soumettre` vérifient eux-mêmes qu'un envoi est permis.
//    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 7 « Maîtriser le focus »
// TODO [module 21] Test, feuille-de-temps-hebdo.spec.ts : doublure du `LiveAnnouncer` ; un jour
//    fermé n'a plus de liste ; boutons indisponibles vérifiés par `aria-disabled` ; focus sur le
//    titre après la soumission.
//    — cours/21-accessibilite-et-finitions.md, non détaillé dans le module : comparer avec la solution au tag `module-21`
// TODO [module 22] `ng lint` refuse une fonction vide : la fonction `error` de l'abonnement garde
//    son commentaire DANS ses accolades.
//    — cours/22-qualite-du-code.md, Pas à pas, étape 9 « Lire la configuration et l'éprouver »
import { Component } from '@angular/core';

@Component({
  selector: 'app-feuille-de-temps-hebdo',
  styleUrl: './feuille-de-temps-hebdo.scss',
  templateUrl: './feuille-de-temps-hebdo.html',
})
export class FeuilleDeTempsHebdo {}

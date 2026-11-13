// TODO [module 13] Premier composant métier (le cours le fait créer par `ng generate component
//    collaborateurs/liste-collaborateurs` : fichier déjà fourni, ne relancez pas la commande) : un
//    signal de cinq collaborateurs en dur (dont Sarah Petit, sortie en mars 2026).
//    — cours/13-premiere-application-angular.md, Pas à pas, étape 6 « Générer le premier composant métier »
// TODO [module 13] Un signal de filtre, un `computed` de la liste filtrée et une méthode qui vide
//    le filtre.
//    — cours/13-premiere-application-angular.md, Pas à pas, étape 8 « Filtrer par nom »
// TODO [module 13] Un signal booléen `afficherSortis` (faux par défaut), pris en compte dans le
//    calcul, et une méthode qui le bascule.
//    — cours/13-premiere-application-angular.md, Pas à pas, étape 9 « Afficher ou masquer les collaborateurs sortis »
// TODO [module 13] Filtrer aussi sur le prénom et le matricule.
//    — cours/13-premiere-application-angular.md, À vous, exercice 1 « Élargir la recherche. »
// TODO [module 13] Ignorer les accents, dans un fichier à part de commun/ (provisoire, non fourni ;
//    supprimé au module 16).
//    — cours/13-premiere-application-angular.md, À vous, exercice 2 « Ignorer les accents. »
// TODO [module 13] Afficher sous le compteur la répartition par catégorie (« 3 consultants, 1
//    interne »), calculée dans la classe.
//    — cours/13-premiere-application-angular.md, À vous, exercice 3 « Compter par catégorie. »
// TODO [module 13] Un bouton qui bascule entre le tri par nom et par date d'entrée : attention,
//    `sort` modifie le tableau sur lequel on l'appelle.
//    — cours/13-premiere-application-angular.md, À vous, exercice 4 « Trier. »
// TODO [module 13] Test, liste-collaborateurs.spec.ts : créer à la main le fichier : test « se
//    crée ».
//    — cours/13-premiere-application-angular.md, Pas à pas, étape 6 « Générer le premier composant métier »
// TODO [module 14] Importer le module des boutons : `matButton` sur le bouton des sortis.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 2 « Un premier composant Material »
// TODO [module 14] Renommer `filtre` en `recherche`, retirer la méthode qui vide le filtre,
//    utiliser `BarreDeRecherche` liée par une liaison à deux sens.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 3 « Extraire la barre de recherche »
// TODO [module 14] Un `linkedSignal` `selection` qui se réinitialise quand le collaborateur sort de
//    la liste filtrée ; une carte par collaborateur, la fiche pour la sélection.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 6 « Assembler : la sélection »
// TODO [module 14] Utiliser `CompteurDeResultats` pour la ligne de comptage.
//    — cours/14-composer-des-composants.md, À vous, exercice 1 « Extraire le compteur. »
// TODO [module 14] Test, liste-collaborateurs.spec.ts : ouvrir la fiche du collaborateur choisi ;
//    la garder tant qu'il reste dans la liste, la fermer sinon.
//    — cours/14-composer-des-composants.md, Pas à pas, étape 6 « Assembler : la sélection »
// TODO [module 15] Les données en dur viennent d'un fichier partagé avec la fiche (provisoire,
//    collaborateurs-en-dur.ts, non fourni ; supprimé au module 16).
//    — cours/15-routage.md, Pas à pas, étape 2 « Partager les données en dur »
// TODO [module 15] Supprimer `selection` et `FicheCollaborateur` ; ajouter le lien vers le
//    formulaire (`RouterLink`, icônes).
//    — cours/15-routage.md, Pas à pas, étape 8 « Des liens à la place de la sélection »
// TODO [module 15] `recherche` devient une ENTRÉE alimentée par le routeur (?recherche=…) ; la
//    saisie est écrite dans l'adresse, qui est la seule source de vérité.
//    — cours/15-routage.md, Pas à pas, étape 9 « La recherche dans l'adresse »
// TODO [module 15] Le statut « afficher les sortis » dans l'adresse, sur le modèle de `recherche`,
//    sans faire disparaître l'un en modifiant l'autre.
//    — cours/15-routage.md, À vous, exercice 4 « Le statut dans l'adresse. »
// TODO [module 15] Test, liste-collaborateurs.spec.ts : fournir le routeur ; recherche écrite et
//    lue dans l'adresse, statut sans perdre la recherche, liens vers la fiche et le formulaire.
//    — cours/15-routage.md, Pas à pas, étape 8 « Des liens à la place de la sélection »
// TODO [module 16] Retirer données en dur, filtre calculé et répartition : la liste vient de
//    `CollaborateursApi.liste`. Le tri alterne entre nom et matricule.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 6 « Brancher la liste, avec pagination »
// TODO [module 16] `page` en `linkedSignal` qui revient à 1 quand la recherche, le filtre ou le tri
//    change ; `total` qui garde le dernier total connu ; taille de page.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 6 « Brancher la liste, avec pagination »
// TODO [module 16] Le paginateur de Material compte les pages à partir de 0, l'API à partir de 1.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 6 « Brancher la liste, avec pagination »
// TODO [module 16] Temporiser la recherche : un `Subject` des frappes, une pause de 300 ms, arrêt à
//    la destruction du composant.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 7 « Temporiser la recherche »
// TODO [module 16] Suppression : appeler `CollaborateursApi.supprimer`, s'abonner, recharger la
//    liste en cas de succès, gérer l'erreur déjà notifiée.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 9 « Supprimer, puis recharger »
// TODO [module 16] Supprimer les fichiers provisoires des modules 13 et 15.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 16] Test, liste-collaborateurs.spec.ts : avec un faux serveur HTTP : première page
//    des actifs triés par nom, total de l'API, recherche en une requête puis après une pause, tri,
//    « Réessayer », rechargement après suppression, aucun rechargement si l'API refuse.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 18] Demander confirmation avant de supprimer, avec un dialogue générique de commun/
//    ; rien n'est supprimé si l'on annule.
//    — cours/18-ecrans-metier.md, À vous, exercice 2 « Confirmer avant de supprimer. »
// TODO [module 18] Test, liste-collaborateurs.spec.ts : confirmation avant de supprimer, rien de
//    supprimé si l'on annule.
//    — cours/18-ecrans-metier.md, non détaillé dans le module : comparer avec la solution au tag `module-18`
// TODO [module 19] Importer `SiRole` : le lien « Nouveau collaborateur » n'est proposé qu'au
//    gestionnaire RH.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 10 « RG-52 : ne proposer la modification qu'à qui peut la faire »
// TODO [module 19] Test, liste-collaborateurs.spec.ts : ni création ni suppression sans le rôle de
//    gestionnaire RH (RG-52).
//    — cours/19-authentification-cote-interface.md, non détaillé dans le module : comparer avec la solution au tag `module-19`
// TODO [module 20] Test, liste-collaborateurs.spec.ts : la barre de progression pendant le
//    chargement, puis les collaborateurs.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 5 « Tester un écran qui lit l'API »
// TODO [module 22] `ng lint` refuse une fonction vide : la fonction `error` de l'abonnement garde
//    son commentaire DANS ses accolades.
//    — cours/22-qualite-du-code.md, Pas à pas, étape 9 « Lire la configuration et l'éprouver »
import { Component } from '@angular/core';

@Component({
  selector: 'app-liste-collaborateurs',
  styleUrl: './liste-collaborateurs.scss',
  templateUrl: './liste-collaborateurs.html',
})
export class ListeCollaborateurs {}

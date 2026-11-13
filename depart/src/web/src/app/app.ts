import { Component } from '@angular/core';

@Component({
  // TODO [module 13] Déclarer dans les `imports` le composant `ListeCollaborateurs` : sans cela,
  //    NG8001.
  //    — cours/13-premiere-application-angular.md, Pas à pas, étape 6 « Générer le premier composant métier »
  // TODO [module 13] Déclarer aussi `ListeProjets`, affichée dans la même page.
  //    — cours/13-premiere-application-angular.md, À vous, exercice 5 « Une deuxième ressource. »
  // TODO [module 15] Les `imports` de la coquille : le routeur et les modules Material de la barre,
  //    du menu et des icônes. Les deux listes quittent `App`.
  //    — cours/15-routage.md, Pas à pas, étape 7 « La coquille : barre de titre, menu, emplacement »
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  // TODO [module 13] Un signal en lecture seule `titre`, de valeur « Chronos », que le gabarit
  //    affiche dans un `<h1>`.
  //    — cours/13-premiere-application-angular.md, Pas à pas, étape 5 « Remplacer la page d'accueil »
  // TODO [module 15] Un tableau de liens de navigation (chemin, libellé, icône), qui commence par
  //    les collaborateurs.
  //    — cours/15-routage.md, Pas à pas, étape 7 « La coquille : barre de titre, menu, emplacement »
  // TODO [module 15] Compléter le menu avec les écrans de l'exercice, une icône par entrée. La page
  //    de connexion doit-elle y figurer ?
  //    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
  // TODO [module 18] Ajouter au menu le lien de l'écran des feuilles à valider
  //    (/feuilles-a-valider).
  //    — cours/18-ecrans-metier.md, À vous, exercice 3 « Le manager relit. »
  // TODO [module 19] Injecter `Session` et le routeur ; réserver aux rôles Manager et
  //    GestionnaireRh les liens du plan de charge et des feuilles à valider (RG-36).
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 9 « Adapter l'interface au rôle »
  // TODO [module 19] Un `computed` `liensVisibles` qui suit la session ; une méthode asynchrone
  //    `seDeconnecter` : déconnexion, puis retour à /connexion.
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 9 « Adapter l'interface au rôle »
  // TODO [module 21] Ne proposer le menu qu'à un utilisateur connecté.
  //    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 2 « Vérifier l'ossature de la page »
  // TODO [module 21] Après un changement de CHEMIN (ni simple paramètre de requête, ni première
  //    navigation), mettre le focus sur le titre du nouvel écran une fois le rendu fait.
  //    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 7 « Maîtriser le focus »
  // TODO [module 21] Une méthode pour le lien d'évitement « Aller au contenu » : elle place le
  //    focus sans changer l'adresse.
  //    — cours/21-accessibilite-et-finitions.md, À vous, exercice 5 « Un lien d'évitement. »
}

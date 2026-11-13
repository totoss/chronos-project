// TODO [module 13] Deuxième ressource (le cours le fait créer par `ng generate component
//    projets/liste-projets` : fichier déjà fourni, ne relancez pas la commande) : un signal de
//    projets en dur, un signal de filtre de statut, un `computed` de la liste filtrée.
//    — cours/13-premiere-application-angular.md, À vous, exercice 5 « Une deuxième ressource. »
// TODO [module 13] Test, liste-projets.spec.ts : créer à la main le fichier : test « se crée ».
//    — cours/13-premiere-application-angular.md, À vous, exercice 5 « Une deuxième ressource. »
// TODO [module 14] Réutiliser `BarreDeRecherche` TELLE QUELLE et créer `CarteProjet`.
//    — cours/14-composer-des-composants.md, À vous, exercice 5 « Les projets. »
// TODO [module 16] Lire les projets par `ProjetsApi.liste` : signaux de recherche, de statut et de
//    taille ; `page` en `linkedSignal` qui revient à 1 quand la recherche ou le statut change ;
//    `total` qui garde le dernier total connu.
//    — cours/16-dialoguer-avec-l-api.md, À vous, exercice 2 « Les projets. »
// TODO [module 16] Recherche temporisée (`Subject`, pause de 300 ms) ; changement de page du
//    paginateur.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 7 « Temporiser la recherche »
// TODO [module 16] Test, liste-projets.spec.ts : avec un faux serveur HTTP : première page triée
//    par code sans filtre, projets et total affichés, filtre de statut envoyé à l'API.
//    — cours/16-dialoguer-avec-l-api.md, non détaillé dans le module : comparer avec la solution au tag `module-16`
// TODO [module 17] Ajouter le lien vers le formulaire de projet (`RouterLink`, icônes).
//    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
// TODO [module 17] Test, liste-projets.spec.ts : fournir le routeur, le gabarit porte maintenant un
//    lien.
//    — cours/17-formulaires.md, non détaillé dans le module : comparer avec la solution au tag `module-17`
// TODO [module 19] Importer `SiRole` : « Nouveau projet » est réservé aux rôles Manager et
//    GestionnaireRh.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 10 « RG-52 : ne proposer la modification qu'à qui peut la faire »
import { Component } from '@angular/core';

@Component({
  selector: 'app-liste-projets',
  styleUrl: './liste-projets.scss',
  templateUrl: './liste-projets.html',
})
export class ListeProjets {}

// TODO [module 15] Pour la route /connexion, un composant provisoire `ConnexionProvisoire`
//    (compte/connexion-provisoire/, non fourni ; supprimé au module 19).
//    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
// TODO [module 19] Écran de connexion (le cours le fait créer par `ng generate component
//    compte/connexion` : fichier déjà fourni, ne relancez pas la commande), en Signal Form : e-mail
//    obligatoire et valide, mot de passe obligatoire ; la route 'connexion' pointe dessus.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
// TODO [module 19] Soumission : `Session.connecter`, puis navigation vers la destination ; bouton
//    désactivé pendant l'envoi.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
// TODO [module 19] Entrée `retour` alimentée par `?retour=` (« / » par défaut) ; ne suivre que les
//    chemins internes à l'application.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
// TODO [module 19] Message d'échec volontairement vague, sauf API injoignable (statut 0), dite
//    comme telle.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
// TODO [module 19] Test, connexion.spec.ts : créer à la main le fichier : test « se crée ».
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 4 « L'écran de connexion »
// TODO [module 20] Test, connexion.spec.ts : doublure de `Session` et harnais Material : retour à
//    la page demandée, retour vers un autre site refusé (`it.each`), formulaire invalide, refus,
//    API injoignable, message effacé à l'essai suivant.
//    — cours/20-tester-l-interface.md, Pas à pas, étape 6 « Piloter un écran Material par ses harnais »
import { Component } from '@angular/core';

@Component({
  selector: 'app-connexion',
  styleUrl: './connexion.scss',
  templateUrl: './connexion.html',
})
export class Connexion {}

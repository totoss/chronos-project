// TODO [module 19] Directive STRUCTURELLE `SiRole`, sélecteur `[appSiRole]` : entrée obligatoire
//    (un rôle ou une liste de rôles) ; injecte `Session`, le gabarit et le conteneur de vues.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 9 « Adapter l'interface au rôle »
// TODO [module 19] Un `effect` (ici à sa place : il synchronise le DOM avec un signal) vide le
//    conteneur puis recrée la vue si l'utilisateur a l'un des rôles.
//    — cours/19-authentification-cote-interface.md, Pas à pas, étape 9 « Adapter l'interface au rôle »
// TODO [module 20] Test, si-role.spec.ts : créer à la main le fichier : directive testée par un
//    composant hôte écrit dans le fichier ; absent sans le rôle, présent avec, qui suit un
//    changement de rôle APRÈS l'affichage ; liste de rôles.
//    — cours/20-tester-l-interface.md, À vous, exercice 3 « La directive `*appSiRole`. »
import { Directive } from '@angular/core';

@Directive({
  selector: '[appSiRole]',
})
export class SiRole {}

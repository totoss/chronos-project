// TODO [module 16] Service d'accès à l'API (le cours le fait créer par `ng generate service
//    collaborateurs/collaborateurs-api` : fichier déjà fourni, ne relancez pas la commande) ; le
//    décorateur `@Service()`, que la CLI aurait posé, est à écrire ici à la main.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 5 « Regrouper les appels dans un service »
// TODO [module 16] `liste(criteres)` et `parId(id)` reçoivent des FONCTIONS et renvoient un
//    `httpResource` ; tris acceptés : nom, -nom, matricule ; le filtre de statut n'est envoyé que
//    s'il sert ; `parId` n'envoie rien tant que l'identifiant est `undefined`.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 5 « Regrouper les appels dans un service »
// TODO [module 16] Ajouter `supprimer(id)` (Observable : rien ne part sans abonnement).
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 9 « Supprimer, puis recharger »
// TODO [module 16] Test, collaborateurs-api.spec.ts : créer à la main le fichier : test « se
//    crée », puis suppression par DELETE.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 10 « Mettre à jour les tests, retirer les données en dur »
// TODO [module 17] Ajouter `creer(requete)` et `modifier(id, requete)` (Observables).
//    — cours/17-formulaires.md, Pas à pas, étape 1 « Les requêtes et le service »
// TODO [module 17] Poser sur ces deux requêtes le jeton de contexte `ERREURS_DE_SAISIE_GEREES`,
//    pour que l'intercepteur ne notifie pas ce que le formulaire affiche.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API »
// TODO [module 17] Test, collaborateurs-api.spec.ts : création par POST et modification par PUT,
//    avec le jeton de contexte posé.
//    — cours/17-formulaires.md, non détaillé dans le module : comparer avec la solution au tag `module-17`

export class CollaborateursApi {}

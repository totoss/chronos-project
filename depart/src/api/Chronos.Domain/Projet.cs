// TODO [module 04] Entité projet. Un seul constructeur, qui exige tout : code, libellé, nature, identifiant du client (facultatif), date de début, date de fin
//    (facultative), budget en jours, identifiant du responsable. Il refuse par une exception d'argument un code ou un libellé vides et un budget négatif (garde-fous,
//    pas des règles) et retire les espaces superflus. Le statut n'est pas un paramètre : un projet naît « en préparation ». Propriétés à accesseurs d'écriture
//    privés ; chaque paramètre du constructeur porte le nom de sa propriété, pour qu'EF Core reconstruise l'entité au module 06 (types d'entités avec
//    constructeurs).
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 3 « L'entité `Projet` : un constructeur et des *setters* privés »
// TODO [module 04] RG-11 : un projet facturable a un client, un projet interne n'en a pas ; violation = RegleMetierException avec le code de la règle. Écrire le test
//    avant le code
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 6 « RG-11 : le test d'abord »
// TODO [module 04] RG-12 : la date de fin n'est pas antérieure à la date de début (postérieure ou égale, donc) ; tester toutes les valeurs aux limites par une théorie
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 7 « RG-12 : une théorie »
// TODO [module 04] Cycle de vie : le statut ne se modifie pas, le projet démarre (Demarrer, seulement depuis « en préparation »), se termine (Terminer, seulement s'il est
//    en cours) ou est annulé (Annuler, tant qu'il n'est pas clos) ; une transition impossible lève InvalidOperationException, pas RegleMetierException. Une propriété
//    calculée « EstClos » (terminé ou annulé). RG-13 : « VerifierOuvert » lève RegleMetierException si le projet est clos, avec un message pour l'utilisateur qui dit
//    « terminé » ou « annulé », pas le nom de l'énumération
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 8 « Le cycle de vie et RG-13 »
// TODO [module 04] Ajouter une propriété de navigation « Client? Client » (accesseur d'écriture privé, non renseignée par le constructeur : EF Core la remplira au
//    module 06)
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 3 « `Client`. »
// TODO [module 04] Ajouter de même « Collaborateur? Responsable »
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 4 « `Collaborateur` et RG-02. »
// TODO [module 04] Couvrir par des tests chaque transition du cycle de vie (accepté ou refusé, un test par statut de départ) et les garde-fous
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 1 « Finir les tests de `Projet`. »
// TODO [module 07] Normaliser le code du projet en majuscules dans le constructeur, pour que RG-10 (unicité) se comporte de la même façon sur les deux SGBD. Écrire
//    d'abord les tests
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] Jeton de concurrence optimiste : une propriété « Version » (un Guid, nouvelle valeur à la création) renouvelée par chaque méthode qui modifie le
//    projet, y compris les transitions
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 08] Méthode « Modifier » (libellé, nature, client, dates, budget, responsable) qui rejoue les mêmes vérifications que le constructeur (RG-11, RG-12,
//    garde-fous) et renouvelle la version ; un refus la laisse intacte. Les vérifications de RG-11 et RG-12 se partagent entre constructeur et « Modifier »
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 08] Concevoir une réponse plus juste qu'une 500 à une transition impossible (question de l'exercice ; ce que le domaine peut exposer pour y aider est à décider,
//    la solution de référence est à comparer au tag module-08)
//    — cours/08-une-api-robuste.md, À vous, exercice 3 « Changer le statut d'un projet. »
// TODO [module 12] Implémenter ITracable (RG-53) : les quatre propriétés de trace, à accesseurs d'écriture privés
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 9 « Tracer (RG-53) »

namespace Chronos.Domain;

public class Projet
{
}

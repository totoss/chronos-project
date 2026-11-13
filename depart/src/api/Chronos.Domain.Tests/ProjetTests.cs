// TODO [module 04] Premier test : un nouveau projet est en préparation (un projet interne sans client sert de modèle aux autres tests ; écrire d'abord une fonction qui en fabrique un, le projet du module
//    s'appelle « CHRONOS », budget de 120 jours)
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 5 « Un premier test »
// TODO [module 04] RG-11, écrit AVANT le code : un projet facturable sans client est refusé, un projet interne avec client aussi ; vérifier le code de la règle de l'exception
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 6 « RG-11 : le test d'abord »
// TODO [module 04] RG-12 par une théorie, avec toutes les valeurs aux limites : une fin égale ou postérieure au début est acceptée, une fin antérieure est refusée
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 7 « RG-12 : une théorie »
// TODO [module 04] Cycle de vie et RG-13 : amener le projet à l'état voulu, appeler la vérification, contrôler l'exception ; un projet terminé n'accepte plus ni affectation ni saisie, un projet
//    annulé non plus ; une transition refusée se teste par une exception d'opération invalide
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 8 « Le cycle de vie et RG-13 »
// TODO [module 04] Couvrir chaque transition (Demarrer, Terminer, Annuler) pour chaque statut de départ, accepté ou refusé (une théorie peut-elle recevoir un StatutProjet dans une ligne de données ? question de l'exercice), et les
//    garde-fous : code vide, code fait d'espaces, budget négatif. Attention au type exact de l'exception attendue (voir « Pièges » du module)
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 1 « Finir les tests de `Projet`. »
// TODO [module 07] Écrire d'abord les tests de la normalisation du code en majuscules
//    — cours/07-deux-sgbd-un-seul-code.md, À vous, exercice 1 « Décider de la casse (RG-01, RG-10). »
// TODO [module 08] Couvrir Modifier (les champs sont remplacés et la version renouvelée ; un refus RG-11 ou RG-12 laisse la version intacte), le renouvellement de la version par chaque transition et
//    par l'annulation, et la table des transitions possibles selon le statut
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »

namespace Chronos.Domain.Tests;

public class ProjetTests
{
}

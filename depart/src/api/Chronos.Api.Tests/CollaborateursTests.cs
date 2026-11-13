// TODO [module 11] Tester les collaborateurs contre la vraie base : créer puis relire une fiche ; les refus 400 (champs invalides, une erreur par champ),
//    409 (matricule déjà utilisé) ; l'index unique refuse un doublon même sans passer par l'API (test qui atteint le contexte de l'application) ;
//    le filtre « sorti » dépend de la date du jour, que le test pilote par l'horloge de la fabrique.
//    — cours/11-tests-d-integration.md, Pas à pas, étape 4 « Un premier test : créer puis relire », étape 5 « Les erreurs : 400, 409, 422 », étape 6 « Ce que seule la vraie base peut dire » et étape 7 « Piloter le temps »
// TODO [module 11] Archivage RG-05 : supprimer un collaborateur qui a une affectation l'archive (invisible dans la liste, sa ligne existe toujours,
//    avec le filtre d'archivage désactivé dans le test) ; sans donnée liée, il est réellement supprimé.
//    — cours/11-tests-d-integration.md, À vous, exercice 2 « Archivage (RG-05). »
// TODO [module 12] Remplacer le client anonyme par un client connecté en gestionnaire RH.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 »
// TODO [module 16] Tester l'indicateur « estSorti » : sortie passée vrai dans la liste et dans la fiche ; pas de sortie, sortie aujourd'hui ou à venir
//    faux (cas paramétrés) ; la fiche réduite vue par un manager porte l'indicateur, et toujours ni dates ni quotité.
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs » (passage « Cet indicateur n'existe pas encore dans votre API », qui tient lieu d'encadré « Compléter le contrat de l'API »)
// TODO [module 17] Tester le contrat complété : les 409 de doublon portent la règle « RG-01 » et aucun code, y compris à la modification ; la version
//    périmée répond 409 avec le code « concurrence » ; un champ obligatoire manquant à la modification donne un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)
// TODO [module 18] Tester RG-05 avec les feuilles de temps : un collaborateur dont les seules traces sont des feuilles vides est réellement supprimé ;
//    une feuille avec une saisie, une feuille soumise ou une feuille rejetée l'archive.
//    — cours/18-ecrans-metier.md, Pas à pas, étape 8 « Enregistrer, puis soumettre » (encadré « Compléter le contrat de l'API. »)

// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class CollaborateursTests
{
}

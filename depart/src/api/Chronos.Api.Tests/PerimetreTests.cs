// TODO [module 12] Tester le périmètre (RG-50, RG-51) : construire une hiérarchie A manage B manage C et une personne D sans lien. A voit A, B et C ; un manager
//    intermédiaire ne voit que son sous-arbre ; un collaborateur ne voit que sa fiche ; le gestionnaire RH voit tout le monde ; A reçoit 404
//    sur la fiche de D ; une chaîne de managers circulaire ne fait pas boucler le calcul.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 7 « Autoriser par périmètre (RG-50, RG-51) » ; À vous, exercice 2 « Le périmètre partout. »
// TODO [module 12] Tester la minimisation : un manager lit la fiche réduite de son équipe, le gestionnaire RH la fiche complète, l'intéressé sa propre fiche
//    complète ; la liste ne contient jamais les champs contractuels, même pour le gestionnaire RH ; décision de l'exercice 4 sur ce que voit
//    l'administrateur, fixée par un test.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 10 « Ne montrer que le nécessaire » ; À vous, exercice 4 « L'administrateur. »
// TODO [module 12] Étendre aux autres ressources : le plan de charge, les affectations, les feuilles de temps et les absences se limitent au périmètre
//    (un manager qui demande le plan d'un autre manager obtient un plan vide ; un manager n'affecte que son équipe ; la feuille hors périmètre
//    n'existe pas ; le manager lit la feuille de son équipe sans pouvoir la modifier).
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 2 « Le périmètre partout. »
// TODO [module 16] Vérifier que la fiche réduite lue par un manager porte l'indicateur « estSorti » (faux pour un collaborateur présent).
//    — cours/16-dialoguer-avec-l-api.md, Pas à pas, étape 3 « Écrire les types miroirs » (passage « Cet indicateur n'existe pas encore dans votre API », qui tient lieu d'encadré « Compléter le contrat de l'API »)
// TODO [module 22] Si la classe libère ses ressources de façon asynchrone (DisposeAsync), supprimer la finalisation dans cette méthode (règle CA1816).
//    — cours/22-qualite-du-code.md, Pas à pas, étape 5 « Décider du sort de chaque avertissement »

namespace Chronos.Api.Tests;

public class PerimetreTests
{
}

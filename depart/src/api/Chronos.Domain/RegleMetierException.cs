// TODO [module 04] Exception métier qui porte le code de la règle violée (« RG-xx ») en plus du message, destiné à l'utilisateur et affiché tel quel par
//    l'interface. Elle se déclare avec un constructeur principal (code de la règle, message) ; le message est transmis à la classe de base ; le code est
//    exposé en lecture seule. L'API s'en sert pour répondre 422 (module 08).
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 2 « Les énumérations et l'exception métier »
// TODO [module 22] Le niveau d'analyse « all » signale CA1032 (constructeurs standard d'une exception) : lire la page de la règle et décider (grille de l'étape 5)
//    — cours/22-qualite-du-code.md, À vous, exercice 2 « Un cran plus haut pour le domaine. »

namespace Chronos.Domain;

public class RegleMetierException : Exception
{
}

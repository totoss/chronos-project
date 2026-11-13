// Les réponses d'erreur que plusieurs endpoints produisent de la même façon : des méthodes statiques qui renvoient un
// résultat typé (TypedResults). Elles ne sont pas déclarées ici.
//
// TODO [module 08] Écrire deux réponses : « Invalide » (400 : un paramètre est refusé ; reçoit le nom du champ, en PascalCase comme les
//    annotations, et le message) et « Conflit » (409, titre « Conflit » : la demande est correcte mais l'état de la ressource ne la
//    permet pas ; reçoit le détail). Les endpoints les importent par « using static ».
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 »
// TODO [module 17] « Conflit » reçoit un second paramètre facultatif, la règle de gestion en cause (RG-01, RG-10), publié dans l'extension
//    « regle » du problème comme pour un 422 : le client sait alors que recharger ne sert à rien.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)

namespace Chronos.Api.Commun;

public static class Reponses
{
}

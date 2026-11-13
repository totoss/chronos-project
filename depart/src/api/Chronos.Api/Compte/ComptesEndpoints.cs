// Les routes de gestion des comptes, groupe « /api/comptes » (étiquette OpenAPI « Comptes »), toutes réservées à l'administrateur
// (politique d'administration). La méthode d'extension MapComptes(this IEndpointRouteBuilder) renvoie son groupe ; elle n'est pas
// déclarée ici. Ce fichier déclare aussi les types CreerCompteRequete, ModifierCompteRequete et CompteDto.
//
// TODO [module 12] POST / crée un compte pour un collaborateur (l'inscription libre est neutralisée) : adresse électronique, mot de passe
//    initial (choix à justifier, voir les questions de l'exercice ; jamais renvoyé), fiche facultative, rôles. 201 ; 400 pour un rôle ou une fiche
//    inconnus (clés « Roles », « CollaborateurId ») ; 409 si la fiche a déjà un compte ou si l'adresse est prise. Un compte
//    ne reste jamais sans ses rôles. Les erreurs de validation de la requête ont des messages en français.
//    — cours/12-securite-et-tracabilite.md, À vous, exercice 5 « Les comptes. »
// TODO [module 25] Ajouter GET / (200, liste des comptes triée par adresse : adresse, fiche, rôles, verrouillage, jamais de secret) et
//    PUT /{id:int} (200 ; 404 compte inconnu ; 400 rôle, fiche ou champ manquant, clés « Roles », « CollaborateurId », « Verrouille » ;
//    409 si la fiche est déjà rattachée à un autre compte ou si l'administrateur se retire son rôle ou verrouille son propre
//    compte) qui remplace en bloc la fiche, les rôles et le verrouillage (verrouillage d'Identity, déverrouillage qui remet le
//    compteur d'échecs à zéro), dans la transaction à niveau d'isolation sérialisable du module 10, et renouvelle le tampon de sécurité
//    pour révoquer les sessions ouvertes.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes » (manque exposé par l'encadré « Compléter le déploiement »)
// TODO [module 25] Journaliser la création, la modification et les refus avec les messages de JournalComptes.cs, qui ne portent que des
//    identifiants.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 1 « Regrouper les messages d'une ressource » (modèle) ; non détaillé dans le module pour les comptes : comparer avec la solution au tag `module-25`

namespace Chronos.Api.Compte;

public record CreerCompteRequete;

public record ModifierCompteRequete;

public record CompteDto;

public static class ComptesEndpoints
{
}

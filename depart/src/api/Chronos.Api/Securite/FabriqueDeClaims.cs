// TODO [module 12] Ajouter au cookie de session l'identifiant de la fiche du collaborateur (claim ClaimsChronos.CollaborateurId), quand le compte en
//    a une, pour ne pas le relire en base à chaque requête. La classe étend la fabrique de claims d'Identity pour le type
//    Utilisateur et ses rôles à clé entière (hérite de UserClaimsPrincipalFactory, constructeur à trois services) et complète
//    la génération des claims ; elle est enregistrée dans Program.cs à l'appel d'Identity. Classe publique et scellée.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 2 « Brancher Identity »

namespace Chronos.Api.Securite;

public sealed class FabriqueDeClaims
{
}

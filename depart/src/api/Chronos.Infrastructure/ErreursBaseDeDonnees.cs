// TODO [module 08] Enfermer dans l'infrastructure le savoir propre à chaque SGBD : une méthode statique « EstViolationUnicite » qui reçoit une DbUpdateException et dit si elle vient d'une violation d'index
//    unique, sur PostgreSQL (code d'état de violation d'unicité) comme sur SQL Server (erreurs 2601 et 2627). Elle sert de filet quand la vérification préalable ne suffit pas (deux requêtes simultanées) ;
//    l'API en fait un 409. Cela suppose le pilote SQL Server et Npgsql, déjà amenés par les fournisseurs
//    — cours/08-une-api-robuste.md, Pas à pas, étape 5 « Créer un collaborateur : 201, 409 ou 422 »

namespace Chronos.Infrastructure;

public static class ErreursBaseDeDonnees
{
}

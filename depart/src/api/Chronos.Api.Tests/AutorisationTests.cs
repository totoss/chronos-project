// TODO [module 12] Tester l'autorisation par rôle (RG-52) : pour chaque ressource, un test « rôle insuffisant : 403 » et un test du bon rôle
//    (y compris la création d'un compte par l'administrateur et le refus des demandes invalides, et l'accès aux feuilles à valider). Le même
//    gestionnaire RH reçoit 400 sans l'en-tête du jeton anti-CSRF. Les rôles à tester pour chaque route se déduisent du cahier des charges (§ 2).
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 6 « Autoriser par rôle (RG-52) » ; À vous, exercice 1 « Protéger le reste. » et exercice 5 « Les comptes. »
// TODO [module 25] Ajouter la modification d'un compte (PUT /api/comptes/{id}) aux routes que seul l'administrateur appelle.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes » (le module ne détaille pas cette retouche : comparer avec la solution au tag `module-25`)

namespace Chronos.Api.Tests;

public class AutorisationTests
{
}

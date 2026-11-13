// TODO [module 12] Tester la sécurité par HTTP : un client non connecté reçoit 401 sur les routes de l'API ; l'inscription d'Identity est neutralisée (404,
//    connecté ou non) et ne crée aucun compte ; la documentation OpenAPI reste ouverte en développement ; la connexion pose un cookie de
//    session HttpOnly et SameSite strict ; « /moi » décrit l'utilisateur (ou, sans fiche, n'a ni nom ni identifiant de fiche) et pose le cookie
//    du jeton anti-CSRF ; toute écriture sans jeton répond 400, et le jeton est lié à l'utilisateur ; la connexion elle-même n'exige pas de jeton.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 » et étape 5 « Le jeton anti-CSRF »
// TODO [module 25] Ajouter « /api/comptes » aux routes qui répondent 401 à un client non connecté.
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes » (le module ne détaille pas cette retouche : comparer avec la solution au tag `module-25`)

namespace Chronos.Api.Tests;

public class SecuriteTests
{
}

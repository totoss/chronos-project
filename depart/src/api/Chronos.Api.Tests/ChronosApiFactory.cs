// TODO [module 11] La fabrique de l'application de test : hérite de WebApplicationFactory<Program> (la classe Program est publique par défaut)
//    et implémente IAsyncLifetime (xUnit) ; son constructeur principal reçoit ServeurDeBase (au module 11 : ServeurPostgres).
//    Elle donne à chaque instance sa propre base (nom unique par instance), fixe les réglages de configuration visibles dès le début
//    de Program.cs (chaîne de connexion « Chronos », fournisseur « Chronos:Fournisseur » : par réglage de l'hôte, non par
//    configuration d'application), remplace l'horloge de l'application par une horloge pilotée réglée sur le lundi 4 mai 2026 (la
//    semaine de l'exemple du cahier des charges), exposée en propriété ; à l'initialisation, elle migre la base. Pas de DisposeAsync à écrire.
//    — cours/11-tests-d-integration.md, Pas à pas, étape 3 « La fabrique de l'application »
// TODO [module 12] Rendre les tests compatibles avec la sécurité : à la fin de l'initialisation, créer les quatre rôles de Securite/Roles ; ajouter un mot de
//    passe de test (constante), les identifiants des trois fiches de démonstration (Sophie, Camille, Hugo) et une méthode qui crée
//    le compte au premier appel, le connecte par le vrai endpoint de connexion (cookie de session, sans cookie persistant à cause de
//    l'horloge pilotée), appelle « /moi » pour obtenir le jeton anti-CSRF et le place dans l'en-tête de ce client ; puis une méthode
//    de commodité par profil : gestionnaire RH, Sophie (manager), Camille (collaboratrice), administrateur ; et une lecture de l'identifiant
//    du compte pour une adresse. Ajouter le réglage de la clé « Chronos:Demo:MotDePasse » à vide : sinon, les secrets du poste font
//    créer les comptes de démonstration avant la migration de la base et tous les tests échouent.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 4 « Adapter les tests, et constater le 401 » et étape 11 « Des comptes pour le développement » (piège « les tests lisent aussi vos secrets »)
// TODO [module 25] Séparer la connexion de la création du compte : une méthode publique se connecte à un compte qui existe déjà (adresse et mot de
//    passe), prépare le jeton anti-CSRF, et sert à la méthode du module 12 ; les tests de la gestion des comptes en ont besoin (compte verrouillé, session révoquée).
//    — cours/25-exploiter-l-application.md, Pas à pas, étape 13 « Gérer les comptes » (le module ne détaille pas cette méthode : comparer avec la solution au tag `module-25`)

namespace Chronos.Api.Tests;

public sealed class ChronosApiFactory
{
}

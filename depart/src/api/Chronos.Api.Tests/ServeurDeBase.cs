// TODO [module 11] Un seul serveur PostgreSQL pour tous les tests : une fixture d'assembly (déclarée par un attribut d'assembly de xUnit v3, que
//    ce fichier ne contient pas encore) qui démarre un conteneur Testcontainers (image « postgres:18 », nommée : l'image est obligatoire
//    depuis Testcontainers 4.10) avant le premier test et l'arrête après le dernier. Elle fournit la chaîne de connexion pour une
//    base de nom donné : chaque classe de tests aura la sienne. Elle s'appelle d'abord ServeurPostgres.
//    — cours/11-tests-d-integration.md, Pas à pas, étape 1 « Créer le projet de tests » puis étape 2 « Un serveur PostgreSQL pour tous les tests »
// TODO [module 11] La renommer ServeurDeBase et choisir le SGBD par la variable d'environnement CHRONOS_TESTS_SGBD (« PostgreSql » par
//    défaut, ou « SqlServer », image mcr.microsoft.com/mssql/server:2025-latest) : seul le conteneur choisi démarre ; toute autre valeur
//    arrête avec un message clair. La fixture expose aussi le fournisseur choisi. Classe publique et scellée, qui implémente
//    l'interface IAsyncLifetime de xUnit (retirée de la déclaration ci-dessous).
//    — cours/11-tests-d-integration.md, À vous, exercice 5 « Les mêmes tests sur SQL Server. »

namespace Chronos.Api.Tests;

public sealed class ServeurDeBase
{
}

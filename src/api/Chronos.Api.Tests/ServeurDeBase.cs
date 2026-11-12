using Chronos.Api.Tests;
using Microsoft.Data.SqlClient;
using Npgsql;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ServeurDeBase))]

namespace Chronos.Api.Tests;

/// <summary>
/// Un seul serveur de base de données pour tous les tests. Le SGBD se choisit par la variable d'environnement
/// <c>CHRONOS_TESTS_SGBD</c> (<c>PostgreSql</c> par défaut, ou <c>SqlServer</c>) : seul le conteneur choisi démarre.
/// </summary>
public sealed class ServeurDeBase : IAsyncLifetime
{
    private readonly IDatabaseContainer _conteneur;

    public ServeurDeBase()
    {
        Fournisseur = Environment.GetEnvironmentVariable("CHRONOS_TESTS_SGBD") ?? "PostgreSql";
        _conteneur = Fournisseur switch
        {
            "PostgreSql" => new PostgreSqlBuilder("postgres:18").Build(),
            "SqlServer" => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build(),
            _ => throw new InvalidOperationException(
                $"CHRONOS_TESTS_SGBD vaut « {Fournisseur} ». Valeurs admises : PostgreSql, SqlServer."),
        };
    }

    /// <summary>La valeur de <c>Chronos:Fournisseur</c> que la fabrique donne à l'application.</summary>
    public string Fournisseur { get; }

    public async ValueTask InitializeAsync() => await _conteneur.StartAsync();

    public async ValueTask DisposeAsync() => await _conteneur.DisposeAsync();

    public string ChaineDeConnexion(string nomDeBase) => Fournisseur switch
    {
        "SqlServer" => new SqlConnectionStringBuilder(_conteneur.GetConnectionString()) { InitialCatalog = nomDeBase }.ConnectionString,
        _ => new NpgsqlConnectionStringBuilder(_conteneur.GetConnectionString()) { Database = nomDeBase }.ConnectionString,
    };
}

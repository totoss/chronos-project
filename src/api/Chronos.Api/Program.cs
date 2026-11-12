using System.Diagnostics;
using System.Text.Json.Serialization;
using Chronos.Api.Clients;
using Chronos.Api.Collaborateurs;
using Chronos.Api.Erreurs;
using Chronos.Api.Projets;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RegleMetierExceptionHandler>();
builder.Services.AddExceptionHandler<ConflitExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var fournisseur = builder.Configuration["Chronos:Fournisseur"] ?? "PostgreSql";
var chaineDeConnexion = builder.Configuration.GetConnectionString("Chronos");

if (fournisseur is not ("PostgreSql" or "SqlServer"))
{
    throw new InvalidOperationException(
        $"Fournisseur de base de données inconnu : « {fournisseur} ». Valeurs admises : PostgreSql, SqlServer.");
}

builder.Services.AddDbContext<ChronosDbContext>(options =>
{
    _ = fournisseur switch
    {
        "PostgreSql" => options.UseNpgsql(chaineDeConnexion,
            npgsql => npgsql.MigrationsAssembly("Chronos.Migrations.PostgreSql")),

        "SqlServer" => options.UseSqlServer(chaineDeConnexion,
            sqlServer => sqlServer.MigrationsAssembly("Chronos.Migrations.SqlServer")),

        _ => throw new UnreachableException()
    };

    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => DonneesInitiales.Alimenter(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                DonneesInitiales.AlimenterAsync(context, cancellationToken));
    }
});

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException requeteIllisible
        ? requeteIllisible.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProjets();
app.MapClients();
app.MapCollaborateurs();

app.Run();

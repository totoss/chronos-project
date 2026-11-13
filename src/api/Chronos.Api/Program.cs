using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Chronos.Api.Absences;
using Chronos.Api.Affectations;
using Chronos.Api.Clients;
using Chronos.Api.Collaborateurs;
using Chronos.Api.Compte;
using Chronos.Api.Erreurs;
using Chronos.Api.FeuillesDeTemps;
using Chronos.Api.JoursFeries;
using Chronos.Api.PlanDeCharge;
using Chronos.Api.Projets;
using Chronos.Api.Securite;
using Chronos.Api.TypesAbsence;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Chronos.Infrastructure.Tracabilite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Les secrets montés par l'hébergeur (Docker : /run/secrets) : un fichier par valeur, dont le nom est la clé, avec
// « __ » pour « : » (Chronos__Admin__MotDePasse). Sans dossier configuré, rien ne change.
if (builder.Configuration["Chronos:DossierDesSecrets"] is { Length: > 0 } dossierDesSecrets)
{
    builder.Configuration.AddKeyPerFile(dossierDesSecrets, optional: true);
}

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RegleMetierExceptionHandler>();
builder.Services.AddExceptionHandler<ConflitExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();
builder.Services.AddScoped<PerimetreUtilisateur>();
builder.Services.AddScoped<TracabiliteInterceptor>();

var fournisseur = builder.Configuration["Chronos:Fournisseur"] ?? "PostgreSql";
var chaineDeConnexion = builder.Configuration.GetConnectionString("Chronos");

if (fournisseur is not ("PostgreSql" or "SqlServer"))
{
    throw new InvalidOperationException(
        $"Fournisseur de base de données inconnu : « {fournisseur} ». Valeurs admises : PostgreSql, SqlServer.");
}

builder.Services.AddDbContext<ChronosDbContext>((services, options) =>
{
    _ = fournisseur switch
    {
        "PostgreSql" => options.UseNpgsql(chaineDeConnexion, npgsql => npgsql
            .MigrationsAssembly("Chronos.Migrations.PostgreSql")
            .EnableRetryOnFailure()),

        "SqlServer" => options.UseSqlServer(chaineDeConnexion, sqlServer => sqlServer
            .MigrationsAssembly("Chronos.Migrations.SqlServer")
            .EnableRetryOnFailure()),

        _ => throw new UnreachableException()
    };

    options.AddInterceptors(services.GetRequiredService<TracabiliteInterceptor>());

    // Les données de démonstration se demandent explicitement (appsettings.Development.json) : le nom de
    // l'environnement ne suffit pas, les outils d'EF Core (bundle compris) démarrent en Development par défaut.
    if (builder.Configuration.GetValue<bool>("Chronos:Demo:Donnees"))
    {
        options
            .UseSeeding((context, _) => DonneesInitiales.Alimenter(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                DonneesInitiales.AlimenterAsync(context, cancellationToken));
    }
});

builder.Services.AddIdentityApiEndpoints<Utilisateur>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ChronosDbContext>()
    .AddClaimsPrincipalFactory<FabriqueDeClaims>();

// Un compte verrouillé, ou dont les rôles changent, perd sa session au plus tard une minute après : Identity
// compare alors le tampon de sécurité du cookie à celui de la base (une requête SQL par session et par minute).
// Avec les 30 minutes par défaut, un administrateur rétrogradé et verrouillé par un autre pourrait se rendre ses droits.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "chronos.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Politiques.GestionRh, politique => politique.RequireRole(Roles.GestionnaireRh))
    .AddPolicy(Politiques.Validation, politique => politique.RequireRole(Roles.Manager, Roles.GestionnaireRh))
    .AddPolicy(Politiques.Pilotage, politique => politique.RequireRole(Roles.Manager, Roles.GestionnaireRh))
    .AddPolicy(Politiques.Administration, politique => politique.RequireRole(Roles.Administrateur));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Deux questions distinctes (voir les routes plus bas) : /alive « le processus tourne-t-il ? » n'exécute aucun
// contrôle ; /health « peut-il servir des requêtes ? » vérifie que la base répond.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChronosDbContext>();

// Journaux, traces et métriques partent par OTLP vers l'adresse de OTEL_EXPORTER_OTLP_ENDPOINT. Sans cette variable,
// rien n'est exporté : `dotnet run` et les tests ne cherchent aucun collecteur.
builder.Logging.AddOpenTelemetry(journal =>
{
    journal.IncludeFormattedMessage = true;
    journal.IncludeScopes = true;
});

builder.Services.AddSingleton<MetriquesFeuillesDeTemps>();

var telemetrie = builder.Services.AddOpenTelemetry()
    .ConfigureResource(ressource => ressource.AddService("chronos-api"))
    .WithTracing(traces => traces
        .AddAspNetCoreInstrumentation(options =>
            options.Filter = contexte => !contexte.Request.Path.StartsWithSegments("/health")
                && !contexte.Request.Path.StartsWithSegments("/alive"))
        .AddHttpClientInstrumentation()
        .AddNpgsql())
    .WithMetrics(metriques => metriques
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Npgsql")
        .AddMeter(MetriquesFeuillesDeTemps.NomDuMeter));

if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetrie.UseOtlpExporter();
}

// Derrière un relais (nginx), l'API ne voit ni l'adresse du client ni son protocole : elle les lit dans les
// en-têtes X-Forwarded-*, mais seulement s'ils viennent du réseau du relais, désigné par la configuration.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var reseau = builder.Configuration["Chronos:ReseauDuRelais"];
    if (!string.IsNullOrEmpty(reseau))
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(reseau));
    }
});

// Les clés qui protègent le cookie de session et les jetons anti-CSRF. Sans dossier configuré, elles restent
// en mémoire et sont perdues au redémarrage ; en conteneur, le dossier est un volume.
var dossierDesCles = builder.Configuration["Chronos:DossierDesCles"];
if (!string.IsNullOrEmpty(dossierDesCles))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("Chronos")
        .PersistKeysToFileSystem(new DirectoryInfo(dossierDesCles));
}

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException requeteIllisible
        ? requeteIllisible.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// Tous les messages d'une requête (les nôtres, ceux d'EF Core) portent l'identifiant technique du compte,
// jamais son adresse électronique.
app.Use(async (contexte, suivant) =>
{
    var utilisateurId = contexte.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonyme";

    using (app.Logger.BeginScope(new Dictionary<string, object> { ["UtilisateurId"] = utilisateurId }))
    {
        await suivant(contexte);
    }
});

if (app.Environment.IsDevelopment())
{
    // La documentation de l'API ne contient aucune donnée : elle reste lisible sans connexion.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

// Les sondes de l'hébergeur ne sont pas connectées : sans AllowAnonymous, la politique de repli les refuserait.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();

// Seule la connexion d'Identity est exposée ; les neuf autres routes (inscription, jetons, gestion du compte…)
// répondent 404. C'est une liste d'autorisation : une route ajoutée par une version future sera neutralisée d'office.
app.MapGroup("/api/compte")
    .AllowAnonymous()
    .AddEndpointFilter(async (contexte, suivant) =>
        contexte.HttpContext.Request.Path.Equals("/api/compte/login", StringComparison.OrdinalIgnoreCase)
            ? await suivant(contexte)
            : TypedResults.NotFound())
    .MapIdentityApi<Utilisateur>();

// Toute écriture sous ce groupe exige le jeton anti-CSRF.
var api = app.MapGroup("").AddEndpointFilter<FiltreAntiforgery>();
api.MapCompte();
api.MapComptes();
api.MapProjets();
api.MapClients();
api.MapCollaborateurs();
api.MapAffectations();
api.MapJoursFeries();
api.MapPlanDeCharge();
api.MapFeuillesDeTemps();
api.MapAbsences();
api.MapTypesAbsence();

await AdministrateurInitial.CreerAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment() && app.Configuration["Chronos:Demo:MotDePasse"] is { Length: > 0 } motDePasseDemo)
{
    await ComptesDeDemonstration.CreerAsync(app.Services, motDePasseDemo);
}

app.Run();

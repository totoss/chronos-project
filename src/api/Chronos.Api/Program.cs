using System.Diagnostics;
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
using Microsoft.AspNetCore.Identity;
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

    if (builder.Environment.IsDevelopment())
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

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException requeteIllisible
        ? requeteIllisible.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // La documentation de l'API ne contient aucune donnée : elle reste lisible sans connexion.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

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

if (app.Environment.IsDevelopment() && app.Configuration["Chronos:Demo:MotDePasse"] is { Length: > 0 } motDePasseDemo)
{
    await ComptesDeDemonstration.CreerAsync(app.Services, motDePasseDemo);
}

app.Run();

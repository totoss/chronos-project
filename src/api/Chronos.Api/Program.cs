using System.Text.Json.Serialization;
using Chronos.Api.Clients;
using Chronos.Api.Collaborateurs;
using Chronos.Api.Projets;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddDbContext<ChronosDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chronos"));

    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => DonneesInitiales.Alimenter(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                DonneesInitiales.AlimenterAsync(context, cancellationToken));
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProjets();
app.MapClients();
app.MapCollaborateurs();

app.Run();

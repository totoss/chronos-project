using System.Text.Json.Serialization;
using Chronos.Api.Clients;
using Chronos.Api.Projets;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddSingleton<ProjetsEnMemoire>();
builder.Services.AddSingleton<ClientsEnMemoire>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProjets();
app.MapClients();

app.Run();

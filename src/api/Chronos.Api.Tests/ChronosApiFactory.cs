using Chronos.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Chronos.Api.Tests;

public sealed class ChronosApiFactory(ServeurDeBase serveur) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _chaineDeConnexion = serveur.ChaineDeConnexion($"chronos_{Guid.NewGuid():N}");

    public FakeTimeProvider Horloge { get; } = new(new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Chronos", _chaineDeConnexion);
        builder.UseSetting("Chronos:Fournisseur", serveur.Fournisseur);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Horloge);
        });
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        await db.Database.MigrateAsync();
    }
}

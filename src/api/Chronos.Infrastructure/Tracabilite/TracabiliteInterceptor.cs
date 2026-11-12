using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chronos.Infrastructure.Tracabilite;

/// <summary>RG-53 : renseigne qui et quand à chaque enregistrement, pour toute entité <see cref="ITracable"/>.</summary>
public sealed class TracabiliteInterceptor(TimeProvider horloge, IUtilisateurCourant utilisateur)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Tracer(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Tracer(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Tracer(DbContext? contexte)
    {
        if (contexte is null)
        {
            return;
        }

        var maintenant = horloge.GetUtcNow();
        foreach (var entree in contexte.ChangeTracker.Entries<ITracable>())
        {
            if (entree.State == EntityState.Added)
            {
                entree.Property(nameof(ITracable.CreeLe)).CurrentValue = maintenant;
                entree.Property(nameof(ITracable.CreePar)).CurrentValue = utilisateur.Identifiant;
            }
            else if (entree.State == EntityState.Modified)
            {
                entree.Property(nameof(ITracable.ModifieLe)).CurrentValue = maintenant;
                entree.Property(nameof(ITracable.ModifiePar)).CurrentValue = utilisateur.Identifiant;
            }
        }
    }
}

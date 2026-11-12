using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.TypesAbsence;

public static class TypesAbsenceEndpoints
{
    public static RouteGroupBuilder MapTypesAbsence(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/types-absence").WithTags("Types d'absence");

        groupe.MapGet("/", Lister);

        return groupe;
    }

    private static async Task<Ok<List<TypeAbsenceDto>>> Lister(ChronosDbContext db, CancellationToken cancellationToken)
    {
        var types = await db.TypesAbsence
            .OrderBy(t => t.Code)
            .Select(t => new TypeAbsenceDto(t.Id, t.Code, t.Libelle))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(types);
    }
}

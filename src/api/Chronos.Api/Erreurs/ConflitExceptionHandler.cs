using Chronos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Erreurs;

internal sealed class ConflitExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // DbUpdateConcurrencyException hérite de DbUpdateException : il doit être testé le premier.
        var detail = exception switch
        {
            DbUpdateConcurrencyException =>
                "La ressource a été modifiée ou supprimée depuis que vous l'avez lue. Rechargez-la, puis recommencez.",
            DbUpdateException e when ErreursBaseDeDonnees.EstViolationUnicite(e) =>
                "Une ressource porte déjà cette valeur, qui doit être unique.",
            _ => null,
        };

        if (detail is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflit",
                Detail = detail,
            },
        });
    }
}

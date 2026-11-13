using Chronos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Erreurs;

internal sealed class ConflitExceptionHandler(
    IProblemDetailsService problemDetailsService, ILogger<ConflitExceptionHandler> logger) : IExceptionHandler
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

        JournalErreurs.ConflitSignale(logger, exception is DbUpdateConcurrencyException);
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        var probleme = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflit",
            Detail = detail,
        };

        // Le conflit de concurrence se reconnaît à son "code" : le client propose alors de recharger la ressource.
        // La violation d'index unique reste sans code ni règle : l'index violé ne se lit pas de la même façon
        // sur PostgreSQL et sur SQL Server, et les doublons connus sont déjà détectés avant l'enregistrement.
        if (exception is DbUpdateConcurrencyException)
        {
            probleme.Extensions["code"] = "concurrence";
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = probleme,
        });
    }
}

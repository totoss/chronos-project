using Microsoft.AspNetCore.Antiforgery;

namespace Chronos.Api.Securite;

/// <summary>
/// Exige le jeton anti-CSRF (en-tête <c>X-XSRF-TOKEN</c>) sur toute écriture. <c>UseAntiforgery()</c> ne
/// valide que les endpoints à formulaire : pour une API JSON, la validation est explicite.
/// </summary>
public sealed class FiltreAntiforgery(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requete = context.HttpContext.Request;
        if (HttpMethods.IsGet(requete.Method) || HttpMethods.IsHead(requete.Method) || HttpMethods.IsOptions(requete.Method))
        {
            return await next(context);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Jeton anti-CSRF absent ou invalide");
        }

        return await next(context);
    }
}

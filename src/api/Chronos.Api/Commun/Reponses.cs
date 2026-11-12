using Microsoft.AspNetCore.Http.HttpResults;

namespace Chronos.Api.Commun;

/// <summary>Les réponses d'erreur que plusieurs endpoints produisent de la même façon.</summary>
public static class Reponses
{
    /// <summary>400 : un paramètre est refusé. Le nom du champ est écrit comme celui des annotations (PascalCase).</summary>
    public static ValidationProblem Invalide(string champ, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });

    /// <summary>
    /// 409 : la demande est correcte, mais l'état de la ressource ne la permet pas.
    /// Quand le conflit est un doublon numéroté (RG-01, RG-10), l'extension "regle" le nomme, comme pour les 422 :
    /// le client sait alors que recharger la ressource ne servirait à rien.
    /// </summary>
    public static ProblemHttpResult Conflit(string detail, string? regle = null) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflit",
            detail: detail,
            extensions: regle is null ? null : new Dictionary<string, object?> { ["regle"] = regle });
}

using Microsoft.AspNetCore.Http.HttpResults;

namespace Chronos.Api.Commun;

/// <summary>Les réponses d'erreur que plusieurs endpoints produisent de la même façon.</summary>
public static class Reponses
{
    /// <summary>400 : un paramètre est refusé. Le nom du champ est écrit comme celui des annotations (PascalCase).</summary>
    public static ValidationProblem Invalide(string champ, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });

    /// <summary>409 : la demande est correcte, mais l'état de la ressource ne la permet pas.</summary>
    public static ProblemHttpResult Conflit(string detail) =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflit", detail: detail);
}

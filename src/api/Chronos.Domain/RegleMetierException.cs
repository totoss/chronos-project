namespace Chronos.Domain;

// CA1032 demande les constructeurs standard d'une exception (sans argument, message seul…). Ils n'ont pas de sens ici :
// une violation de règle sans son code (RG-xx) serait inexploitable par l'API, qui s'en sert pour répondre 422.
#pragma warning disable CA1032
public class RegleMetierException(string codeRegle, string message) : Exception(message)
#pragma warning restore CA1032
{
    public string CodeRegle { get; } = codeRegle;
}

namespace Chronos.Domain;

public class RegleMetierException(string codeRegle, string message) : Exception(message)
{
    public string CodeRegle { get; } = codeRegle;
}

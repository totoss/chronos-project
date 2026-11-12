namespace Chronos.Domain;

public class TypeAbsence(string code, string libelle)
{
    public int Id { get; private set; }
    public string Code { get; private set; } = code;
    public string Libelle { get; private set; } = libelle;
}

namespace Chronos.Domain;

/// <summary>RG-53 : qui a créé et modifié l'entité, et quand. Ces valeurs sont renseignées par l'intercepteur, jamais à la main.</summary>
public interface ITracable
{
    DateTimeOffset CreeLe { get; }
    string CreePar { get; }
    DateTimeOffset? ModifieLe { get; }
    string? ModifiePar { get; }
}

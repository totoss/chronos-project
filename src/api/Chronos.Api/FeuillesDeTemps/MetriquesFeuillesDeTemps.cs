using System.Diagnostics.Metrics;

namespace Chronos.Api.FeuillesDeTemps;

/// <summary>
/// Une métrique métier : les décisions prises sur les feuilles de temps. L'étiquette « decision » n'a que deux
/// valeurs ; jamais d'identifiant de feuille ni de collaborateur, qui feraient exploser le nombre de séries.
/// </summary>
public sealed class MetriquesFeuillesDeTemps
{
    public const string NomDuMeter = "Chronos.FeuillesDeTemps";

    private readonly Counter<long> _decisions;

    public MetriquesFeuillesDeTemps(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(NomDuMeter);
        _decisions = meter.CreateCounter<long>("chronos.feuilles_de_temps.decisions", unit: "{decision}",
            description: "Feuilles de temps validées ou rejetées.");
    }

    public void Validee() => _decisions.Add(1, new KeyValuePair<string, object?>("decision", "validee"));

    public void Rejetee() => _decisions.Add(1, new KeyValuePair<string, object?>("decision", "rejetee"));
}

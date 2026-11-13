// TODO [module 09] Les données que le calcul de charge reçoit pour UN collaborateur (ce ne sont pas les entités : le calcul n'a besoin que de périodes et de taux). Quatre
//    enregistrements scellés dans ce fichier : « QuotiteContractuelle » (une période et une quotité) ; « AffectationPlanifiee » (identifiant du projet, période, taux
//    d'allocation, indicateur « facturable ») ; « DossierDeCharge » (période de présence, indicateur « est consultant », liste des quotités, liste des affectations, et pour les
//    absences un dictionnaire jour -> part de journée absente, 1 ou 0,5), avec une méthode « QuotiteAu(jour) » qui vaut 0 hors de toute quotité ; « ChargeSurPeriode » (période,
//    capacité, charge planifiée, taux de charge qui peut être absent, surcharge, intercontrat). Une date de fin absente se traduit par DateOnly.MaxValue. Les déclarations
//    ci-dessous n'ont pas leurs paramètres positionnels : ils obligeraient à écrire les membres
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 3 « Les données du calcul »

namespace Chronos.Domain;

public sealed record QuotiteContractuelle
{
}

public sealed record AffectationPlanifiee
{
}

public sealed record DossierDeCharge
{
}

public sealed record ChargeSurPeriode
{
}

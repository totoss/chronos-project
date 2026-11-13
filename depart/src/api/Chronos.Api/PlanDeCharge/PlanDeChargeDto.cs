// TODO [module 09] Les types de réponse du plan de charge, trois records imbriqués : PlanDeChargeDto (période du et au, pas, liste de lignes),
//    LignePlanDeChargeDto (collaborateur : identifiant, nom, prénom ; ses périodes) et ChargeDto (début et fin de la tranche, capacité,
//    charge planifiée, taux de charge facultatif, surcharge, intercontrat : RG-22 à RG-25). Le fichier déclare les trois types.
//    — cours/09-affectations-et-calcul-de-charge.md, Pas à pas, étape 9 « `GET /api/plan-de-charge` »

namespace Chronos.Api.PlanDeCharge;

public record PlanDeChargeDto;

public record LignePlanDeChargeDto;

public record ChargeDto;

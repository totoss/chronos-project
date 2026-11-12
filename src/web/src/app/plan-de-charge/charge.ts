/** Miroir de ChargeDto (module 09) : la charge d'un collaborateur sur une période. */
export interface Charge {
  debut: string;
  fin: string;
  capacite: number;
  chargePlanifiee: number;
  /** Une fraction : 1.33 pour 133 %. `null` quand la capacité est nulle. */
  tauxDeCharge: number | null;
  surcharge: boolean;
  intercontrat: boolean;
}

/** Miroir de LignePlanDeChargeDto (module 09). */
export interface LigneDePlanDeCharge {
  collaborateurId: number;
  nom: string;
  prenom: string;
  periodes: Charge[];
}

export type PasDuPlan = 'semaine' | 'mois';

/** Miroir de PlanDeChargeDto (module 09). */
export interface PlanDeChargeDto {
  du: string;
  au: string;
  pas: PasDuPlan;
  lignes: LigneDePlanDeCharge[];
}

export type NiveauDeCharge =
  'indisponible' | 'intercontrat' | 'sous-charge' | 'normale' | 'surcharge';

/** L'interface ne recalcule rien : elle traduit les indicateurs de l'API en un niveau à afficher. */
export function niveauDeCharge(charge: Charge): NiveauDeCharge {
  if (charge.tauxDeCharge === null) return 'indisponible';
  if (charge.surcharge) return 'surcharge'; // RG-24
  if (charge.intercontrat) return 'intercontrat'; // RG-25
  return charge.tauxDeCharge < 0.8 ? 'sous-charge' : 'normale';
}

export const LIBELLES_NIVEAU: Record<NiveauDeCharge, string> = {
  indisponible: 'Indisponible',
  intercontrat: 'Intercontrat',
  'sous-charge': 'Sous-charge',
  normale: 'Charge normale',
  surcharge: 'Surcharge',
};

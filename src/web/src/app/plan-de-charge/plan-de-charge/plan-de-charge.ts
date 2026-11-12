import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ajouterJours, finDuMois, libelleJour, libelleMois } from '../../commun/dates';
import { SelectionSemaine } from '../../commun/selection-semaine';
import { Charge, LigneDePlanDeCharge, PasDuPlan, PlanDeChargeDto } from '../charge';
import {
  ConfirmationDeSurcharge,
  DonneesDeConfirmation,
} from '../confirmation-de-surcharge/confirmation-de-surcharge';
import { FormulaireAffectation } from '../formulaire-affectation/formulaire-affectation';
import { IndicateurCharge } from '../indicateur-charge/indicateur-charge';

const NOMBRE_DE_SEMAINES = 4;
const NOMBRE_DE_MOIS = 4;

@Component({
  imports: [IndicateurCharge, MatButtonModule, MatButtonToggleModule, MatProgressBarModule],
  selector: 'app-plan-de-charge',
  styleUrl: './plan-de-charge.scss',
  templateUrl: './plan-de-charge.html',
})
export class PlanDeCharge {
  private readonly dialog = inject(MatDialog);
  protected readonly selection = inject(SelectionSemaine);
  protected readonly libelleJour = libelleJour;

  /** Le choix « semaine / mois » est propre à cet écran : il ne rejoint pas `SelectionSemaine`. */
  protected readonly pas = signal<PasDuPlan>('semaine');

  protected readonly plan = httpResource<PlanDeChargeDto>(() => {
    const parSemaine = this.pas() === 'semaine';
    const lundi = this.selection.semaine();
    // Par mois, chaque colonne est un mois entier : on part du 1er du mois de la semaine choisie.
    const du = parSemaine ? lundi : ajouterJours(finDuMois(lundi, -1), 1);
    return {
      url: '/api/plan-de-charge',
      params: {
        du,
        au: parSemaine
          ? ajouterJours(du, 7 * NOMBRE_DE_SEMAINES - 1)
          : finDuMois(lundi, NOMBRE_DE_MOIS - 1),
        pas: this.pas(),
      },
    };
  });

  /** Les périodes affichées en colonnes : celles de l'API, la même pour toutes les lignes. */
  protected readonly periodes = computed(() =>
    this.plan.hasValue() ? (this.plan.value().lignes[0]?.periodes ?? []) : [],
  );

  /** Surcharges confirmées pendant cette session, repérées par « collaborateurId|debut ». */
  private readonly confirmees = signal(new Set<string>());

  protected estConfirmee(ligne: LigneDePlanDeCharge, charge: Charge): boolean {
    return this.confirmees().has(this.cleDeConfirmation(ligne, charge));
  }

  protected libelleDePeriode(charge: Charge): string {
    return this.pas() === 'semaine' ? libelleJour(charge.debut) : libelleMois(charge.debut);
  }

  /** Ce que la période désigne pour l'utilisateur : « semaine du lun. 4 mai », « mai 2026 ». */
  protected descriptionDeLaPeriode(charge: Charge): string {
    return this.pas() === 'semaine'
      ? `semaine du ${libelleJour(charge.debut)}`
      : libelleMois(charge.debut);
  }

  protected confirmer(ligne: LigneDePlanDeCharge, charge: Charge): void {
    const data: DonneesDeConfirmation = {
      collaborateur: `${ligne.prenom} ${ligne.nom}`,
      periode: this.descriptionDeLaPeriode(charge),
      tauxDeCharge: charge.tauxDeCharge ?? 0,
    };

    this.dialog
      .open<ConfirmationDeSurcharge, DonneesDeConfirmation, boolean>(ConfirmationDeSurcharge, {
        data,
      })
      .afterClosed()
      .subscribe((confirme) => {
        if (confirme) {
          this.confirmees.update(
            (cles) => new Set([...cles, this.cleDeConfirmation(ligne, charge)]),
          );
        }
      });
  }

  /** Une affectation créée change le plan : on le recharge. */
  protected creerUneAffectation(): void {
    this.dialog
      .open<FormulaireAffectation, void, boolean>(FormulaireAffectation)
      .afterClosed()
      .subscribe((creee) => {
        if (creee) this.plan.reload();
      });
  }

  private cleDeConfirmation(ligne: LigneDePlanDeCharge, charge: Charge): string {
    return `${ligne.collaborateurId}|${charge.debut}`;
  }
}

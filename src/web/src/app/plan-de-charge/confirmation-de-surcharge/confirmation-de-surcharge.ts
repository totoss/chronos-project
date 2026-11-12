import { PercentPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface DonneesDeConfirmation {
  collaborateur: string;
  /** Ce que la période désigne pour l'utilisateur : « semaine du lun. 4 mai », « mai 2026 ». */
  periode: string;
  tauxDeCharge: number;
}

/** RG-24 : une surcharge est autorisée, mais le manager doit la confirmer. */
@Component({
  imports: [MatButtonModule, MatDialogModule, PercentPipe],
  selector: 'app-confirmation-de-surcharge',
  template: `
    <h2 mat-dialog-title>Confirmer la surcharge</h2>
    <mat-dialog-content>
      {{ donnees.collaborateur }} est planifié à {{ donnees.tauxDeCharge | percent }} de sa capacité
      ({{ donnees.periode }}). Confirmez-vous cette surcharge ?
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton type="button" [mat-dialog-close]="false">Annuler</button>
      <button matButton="filled" type="button" [mat-dialog-close]="true">Confirmer</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmationDeSurcharge {
  protected readonly donnees = inject<DonneesDeConfirmation>(MAT_DIALOG_DATA);
}

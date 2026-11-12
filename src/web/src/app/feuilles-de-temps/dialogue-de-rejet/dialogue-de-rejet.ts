import { Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, maxLength, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';

/** La limite de l'API (StringLength du motif). */
const MOTIF_MAX = 500;

/** RG-34 : le rejet d'une feuille de temps exige un motif. Se ferme sur le motif, ou sans rien. */
@Component({
  imports: [FormField, FormRoot, MatButtonModule, MatDialogModule],
  selector: 'app-dialogue-de-rejet',
  template: `
    <h2 mat-dialog-title>Rejeter la feuille de temps</h2>
    <form [formRoot]="formulaire">
      <mat-dialog-content>
        <label for="motif">Motif du rejet</label>
        <textarea id="motif" rows="4" [formField]="formulaire.motif"></textarea>
        @if (formulaire.motif().touched() && formulaire.motif().invalid()) {
          @for (erreur of formulaire.motif().errors(); track $index) {
            <p class="erreur">{{ erreur.message }}</p>
          }
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>Annuler</button>
        <button matButton="filled" type="submit">Rejeter</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    mat-dialog-content {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 24rem;
    }

    .erreur {
      color: var(--mat-sys-error);
      margin: 0;
    }
  `,
})
export class DialogueDeRejet {
  private readonly dialogRef = inject<MatDialogRef<DialogueDeRejet, string>>(MatDialogRef);

  protected readonly modele = signal({ motif: '' });

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      validate(champs.motif, ({ value }) =>
        value().trim() === ''
          ? { kind: 'required', message: 'Le motif du rejet est obligatoire.' }
          : null,
      );
      maxLength(champs.motif, MOTIF_MAX, {
        message: `Le motif ne dépasse pas ${MOTIF_MAX} caractères.`,
      });
    },
    {
      submission: {
        action: async (champs) => {
          this.dialogRef.close(champs().value().motif.trim());
        },
      },
    },
  );
}

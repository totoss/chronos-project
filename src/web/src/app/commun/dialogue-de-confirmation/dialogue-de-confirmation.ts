import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

export interface DonneesDeDialogueDeConfirmation {
  titre: string;
  message: string;
  /** Le libellé du bouton qui confirme : « Supprimer », « Confirmer »… */
  libelleConfirmer: string;
}

/** Un dialogue de confirmation générique : une question, deux boutons. */
@Component({
  imports: [MatButtonModule, MatDialogModule],
  selector: 'app-dialogue-de-confirmation',
  template: `
    <h2 mat-dialog-title>{{ donnees.titre }}</h2>
    <mat-dialog-content>{{ donnees.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton type="button" [mat-dialog-close]="false">Annuler</button>
      <button matButton="filled" type="button" [mat-dialog-close]="true">
        {{ donnees.libelleConfirmer }}
      </button>
    </mat-dialog-actions>
  `,
})
export class DialogueDeConfirmation {
  protected readonly donnees = inject<DonneesDeDialogueDeConfirmation>(MAT_DIALOG_DATA);
}

/** Ouvre le dialogue et rend `true` si l'utilisateur confirme ; `Échap` et « Annuler » rendent `false`. */
export async function demanderConfirmation(
  dialog: MatDialog,
  donnees: DonneesDeDialogueDeConfirmation,
): Promise<boolean> {
  const confirme = await firstValueFrom(
    dialog
      .open<DialogueDeConfirmation, DonneesDeDialogueDeConfirmation, boolean>(
        DialogueDeConfirmation,
        { data: donnees },
      )
      .afterClosed(),
  );
  return confirme === true;
}

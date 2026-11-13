import { Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, max, min, required, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';
import { CollaborateursApi } from '../../collaborateurs/collaborateurs-api';
import { demanderConfirmation } from '../../commun/dialogue-de-confirmation/dialogue-de-confirmation';
import { ErreurApi } from '../../commun/erreur-api';
import { erreursDeFormulaire } from '../../commun/erreurs-de-formulaire';
import { ProjetsApi } from '../../projets/projets-api';
import { AffectationsApi } from '../affectations-api';

/** Ce que le manager saisit. Le taux se saisit en pourcentage. */
interface ModeleAffectation {
  collaborateurId: number | null;
  projetId: number | null;
  dateDebut: string;
  dateFin: string;
  taux: number | null;
}

/** Crée une affectation. Se ferme sur `true` quand l'API l'a enregistrée. */
@Component({
  imports: [
    FormField,
    FormRoot,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  selector: 'app-formulaire-affectation',
  styleUrl: './formulaire-affectation.scss',
  templateUrl: './formulaire-affectation.html',
})
export class FormulaireAffectation {
  private readonly api = inject(AffectationsApi);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject<MatDialogRef<FormulaireAffectation, boolean>>(MatDialogRef);

  protected readonly collaborateurs = inject(CollaborateursApi).liste(() => ({
    page: 1,
    taille: 100,
    recherche: '',
    avecSortis: false,
    tri: 'nom',
  }));

  protected readonly projets = inject(ProjetsApi).liste(() => ({
    page: 1,
    taille: 100,
    recherche: '',
    statut: '',
  }));

  protected readonly modele = signal<ModeleAffectation>({
    collaborateurId: null,
    projetId: null,
    dateDebut: '',
    dateFin: '',
    taux: null,
  });

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      required(champs.collaborateurId, { message: 'Le collaborateur est obligatoire.' });
      required(champs.projetId, { message: 'Le projet est obligatoire.' });
      required(champs.dateDebut, { message: 'La date de début est obligatoire.' });
      required(champs.dateFin, { message: 'La date de fin est obligatoire.' });
      required(champs.taux, { message: "Le taux d'allocation est obligatoire." });
      min(champs.taux, 1, { message: "Le taux d'allocation est au minimum de 1 %." });
      max(champs.taux, 100, { message: "Le taux d'allocation est au maximum de 100 %." });

      validate(champs.dateFin, ({ value, valueOf }) => {
        const fin = value();
        const debut = valueOf(champs.dateDebut);
        return fin !== '' && debut !== '' && fin < debut
          ? { kind: 'dates', message: 'La date de fin ne peut pas précéder la date de début.' }
          : null;
      });
    },
    {
      submission: {
        action: async (champs) => {
          try {
            await this.enregistrer(champs().value());
            return;
          } catch (erreur) {
            return erreursDeFormulaire(erreur, champs);
          }
        },
      },
    },
  );

  /**
   * RG-24 : si l'API répond qu'une confirmation est nécessaire (422 `RG-24`), on la demande au manager
   * puis on renvoie la même requête avec `surchargeConfirmee`. Un refus laisse le formulaire ouvert.
   */
  private async enregistrer(m: ModeleAffectation): Promise<void> {
    const requete = {
      collaborateurId: m.collaborateurId ?? 0,
      projetId: m.projetId ?? 0,
      dateDebut: m.dateDebut,
      dateFin: m.dateFin,
      tauxAllocation: (m.taux ?? 0) / 100,
      surchargeConfirmee: false,
    };

    try {
      await firstValueFrom(this.api.creer(requete));
    } catch (erreur) {
      if (!(erreur instanceof ErreurApi) || erreur.regle !== 'RG-24') throw erreur;

      const confirme = await demanderConfirmation(this.dialog, {
        titre: 'Confirmer la surcharge',
        message: `${this.nomDuCollaborateur(requete.collaborateurId)} sera en surcharge sur au moins une semaine. Confirmez-vous cette affectation ?`,
        libelleConfirmer: 'Confirmer',
      });
      if (!confirme) return;
      await firstValueFrom(this.api.creer({ ...requete, surchargeConfirmee: true }));
    }
    this.dialogRef.close(true);
  }

  private nomDuCollaborateur(id: number): string {
    const collaborateur = this.collaborateurs.hasValue()
      ? this.collaborateurs.value().elements.find((c) => c.id === id)
      : undefined;
    return collaborateur ? `${collaborateur.prenom} ${collaborateur.nom}` : 'Ce collaborateur';
  }
}

import { Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, hidden, required, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ClientsApi } from '../../clients/clients-api';
import { CollaborateursApi } from '../../collaborateurs/collaborateurs-api';
import { erreursDeFormulaire } from '../../commun/erreurs-de-formulaire';
import { NatureProjet } from '../projet';
import { ProjetsApi } from '../projets-api';

/** Ce que l'utilisateur saisit. Tous les champs sont initialisés, jamais `undefined`. */
interface ModeleProjet {
  code: string;
  libelle: string;
  nature: NatureProjet;
  /** Conservé quand on passe à « Interne » : c'est la conversion qui l'écarte. */
  clientId: number | null;
  dateDebut: string;
  /** Chaîne vide : pas de date de fin. */
  dateFin: string;
  budgetJours: number | null;
  responsableId: number | null;
}

@Component({
  imports: [
    FormField,
    FormRoot,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    RouterLink,
  ],
  selector: 'app-formulaire-projet',
  styleUrl: './formulaire-projet.scss',
  templateUrl: './formulaire-projet.html',
})
export class FormulaireProjet {
  private readonly api = inject(ProjetsApi);
  private readonly router = inject(Router);

  protected readonly clients = inject(ClientsApi).liste();

  /** Les responsables possibles : les collaborateurs en activité (100 au plus, le maximum de l'API). */
  protected readonly responsables = inject(CollaborateursApi).liste(() => ({
    page: 1,
    taille: 100,
    recherche: '',
    avecSortis: false,
    tri: 'nom',
  }));

  protected readonly modele = signal<ModeleProjet>({
    code: '',
    libelle: '',
    nature: 'Facturable',
    clientId: null,
    dateDebut: '',
    dateFin: '',
    budgetJours: null,
    responsableId: null,
  });

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      required(champs.code, { message: 'Le code est obligatoire.' });
      required(champs.libelle, { message: 'Le libellé est obligatoire.' });
      required(champs.dateDebut, { message: 'La date de début est obligatoire.' });
      required(champs.budgetJours, { message: 'Le budget est obligatoire.' });
      required(champs.responsableId, { message: 'Le responsable est obligatoire.' });

      // RG-11 : un projet facturable a un client ; un projet interne n'en a pas.
      required(champs.clientId, {
        message: 'Un projet facturable a obligatoirement un client.',
        when: ({ valueOf }) => valueOf(champs.nature) === 'Facturable',
      });
      hidden(champs.clientId, { when: ({ valueOf }) => valueOf(champs.nature) === 'Interne' });

      // RG-12 : la date de fin ne précède pas la date de début.
      validate(champs.dateFin, ({ value, valueOf }) => {
        const fin = value();
        const debut = valueOf(champs.dateDebut);
        return fin !== '' && debut !== '' && fin < debut
          ? { kind: 'RG-12', message: 'La date de fin ne peut pas précéder la date de début.' }
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

  private async enregistrer(m: ModeleProjet): Promise<void> {
    await firstValueFrom(
      this.api.creer({
        code: m.code.trim(),
        libelle: m.libelle.trim(),
        nature: m.nature,
        // Le champ masqué garde sa valeur dans le modèle : seule la nature décide de l'envoi.
        clientId: m.nature === 'Facturable' ? m.clientId : null,
        dateDebut: m.dateDebut,
        dateFin: m.dateFin === '' ? null : m.dateFin,
        budgetJours: m.budgetJours ?? 0,
        responsableId: m.responsableId ?? 0,
      }),
    );
    await this.router.navigate(['/projets']);
  }
}

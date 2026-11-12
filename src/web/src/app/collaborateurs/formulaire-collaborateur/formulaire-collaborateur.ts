import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import {
  disabled,
  email,
  form,
  FormField,
  FormRoot,
  hidden,
  max,
  min,
  required,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { erreursDeFormulaire } from '../../commun/erreurs-de-formulaire';
import { CategorieCollaborateur, Collaborateur, TypeContrat } from '../collaborateur';
import { CollaborateursApi } from '../collaborateurs-api';

/** Ce que l'utilisateur saisit. Tous les champs sont initialisés, jamais `undefined`. */
interface ModeleCollaborateur {
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  dateEntree: string;
  /** Chaîne vide : pas de date de sortie. */
  dateSortie: string;
  typeContrat: TypeContrat;
  /** Saisie en pourcentage (80) ; l'API attend une fraction (0.8). */
  quotite: number | null;
  /** `null` : pas de manager. */
  managerId: number | null;
}

function versModele(c: Collaborateur | undefined): ModeleCollaborateur {
  return {
    matricule: c?.matricule ?? '',
    nom: c?.nom ?? '',
    prenom: c?.prenom ?? '',
    email: c?.email ?? '',
    categorie: c?.categorie ?? 'Consultant',
    poste: c?.poste ?? '',
    dateEntree: c?.dateEntree ?? '',
    dateSortie: c?.dateSortie ?? '',
    typeContrat: 'Cdi',
    quotite: c === undefined ? 100 : c.quotite === undefined ? null : Math.round(c.quotite * 100),
    managerId: c?.managerId ?? null,
  };
}

@Component({
  imports: [
    FormField,
    FormRoot,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  selector: 'app-formulaire-collaborateur',
  styleUrl: './formulaire-collaborateur.scss',
  templateUrl: './formulaire-collaborateur.html',
})
export class FormulaireCollaborateur {
  private readonly api = inject(CollaborateursApi);
  private readonly router = inject(Router);

  /** Absent : création. Présent : modification de ce collaborateur. */
  readonly collaborateur = input<Collaborateur>();
  readonly enregistre = output<Collaborateur>();
  readonly annule = output<void>();
  /** Demande au parent de recharger la fiche après un conflit (409). */
  readonly recharger = output<void>();

  /** Les managers possibles : les collaborateurs en activité (100 au plus, le maximum de l'API). */
  protected readonly managers = this.api.liste(() => ({
    page: 1,
    taille: 100,
    recherche: '',
    avecSortis: false,
    tri: 'nom',
  }));

  /** Tous sauf le collaborateur lui-même, qui ne peut pas être son propre manager. */
  protected readonly candidatsManager = computed(() => {
    const id = this.collaborateur()?.id;
    return (this.managers.hasValue() ? this.managers.value().elements : []).filter(
      (m) => m.id !== id,
    );
  });

  protected readonly modele = linkedSignal(() => versModele(this.collaborateur()));

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      const enCreation = () => this.collaborateur() === undefined;
      const enModification = () => this.collaborateur() !== undefined;

      required(champs.matricule, { message: 'Le matricule est obligatoire.' });
      required(champs.nom, { message: 'Le nom est obligatoire.' });
      required(champs.prenom, { message: 'Le prénom est obligatoire.' });
      required(champs.email, { message: "L'adresse électronique est obligatoire." });
      email(champs.email, { message: "L'adresse électronique est invalide." });
      required(champs.poste, { message: 'Le poste est obligatoire.' });
      required(champs.dateEntree, { message: "La date d'entrée est obligatoire." });

      // RG-03 : la quotité est comprise entre 10 % et 100 %.
      required(champs.quotite, { message: 'La quotité est obligatoire.' });
      min(champs.quotite, 10, { message: 'La quotité est au minimum de 10 %.' });
      max(champs.quotite, 100, { message: 'La quotité est au maximum de 100 %.' });

      // RG-02 : la date de sortie ne précède pas la date d'entrée.
      validate(champs.dateSortie, ({ value, valueOf }) => {
        const sortie = value();
        const entree = valueOf(champs.dateEntree);
        return sortie !== '' && entree !== '' && sortie < entree
          ? { kind: 'RG-02', message: "La date de sortie ne peut pas précéder la date d'entrée." }
          : null;
      });

      // Ce que l'API n'accepte qu'à la création n'est plus modifiable ensuite.
      disabled(champs.matricule, { when: enModification });
      disabled(champs.categorie, { when: enModification });
      disabled(champs.dateEntree, { when: enModification });
      disabled(champs.quotite, { when: enModification });
      hidden(champs.typeContrat, { when: enModification });
      hidden(champs.dateSortie, { when: enCreation });
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

  private async enregistrer(m: ModeleCollaborateur): Promise<void> {
    const communs = {
      nom: m.nom.trim(),
      prenom: m.prenom.trim(),
      email: m.email.trim(),
      poste: m.poste.trim(),
      managerId: m.managerId,
    };
    const existant = this.collaborateur();

    if (existant) {
      const modifie = await firstValueFrom(
        this.api.modifier(existant.id, {
          ...communs,
          dateSortie: m.dateSortie === '' ? null : m.dateSortie,
          version: existant.version,
        }),
      );
      this.enregistre.emit(modifie);
    } else {
      const cree = await firstValueFrom(
        this.api.creer({
          ...communs,
          matricule: m.matricule.trim(),
          categorie: m.categorie,
          dateEntree: m.dateEntree,
          typeContrat: m.typeContrat,
          quotite: (m.quotite ?? 0) / 100,
        }),
      );
      await this.router.navigate(['/collaborateurs', cree.id]);
    }
  }

  /** Demande confirmation quand l'utilisateur a modifié quelque chose. */
  protected async annuler(): Promise<void> {
    if (this.formulaire().dirty() && !confirm('Abandonner les modifications en cours ?')) {
      return;
    }
    if (this.collaborateur()) {
      this.annule.emit();
    } else {
      await this.router.navigate(['/collaborateurs']);
    }
  }
}

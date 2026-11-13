import { LiveAnnouncer } from '@angular/cdk/a11y';
import {
  Component,
  computed,
  ElementRef,
  inject,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { concatMap, finalize, Observable } from 'rxjs';
import { estWeekEnd, libelleJour } from '../../commun/dates';
import { SelectionSemaine } from '../../commun/selection-semaine';
import { Session } from '../../compte/session';
import {
  disponible,
  joursAbsents,
  joursEnDemiAbsence,
  LIBELLES_STATUT,
  lignesDeLaSemaine,
  totalDuJour,
  versGrille,
  versSaisies,
} from '../feuille-de-temps';
import { FeuillesDeTempsApi } from '../feuilles-de-temps-api';
import { GrilleDeSaisie } from '../grille-de-saisie/grille-de-saisie';

/** Écran de la feuille de temps : il charge les données et appelle l'API, la grille ne fait qu'afficher. */
@Component({
  imports: [GrilleDeSaisie, MatButtonModule, MatProgressBarModule],
  selector: 'app-feuille-de-temps-hebdo',
  styleUrl: './feuille-de-temps-hebdo.scss',
  templateUrl: './feuille-de-temps-hebdo.html',
})
export class FeuilleDeTempsHebdo {
  private readonly api = inject(FeuillesDeTempsApi);
  private readonly annonceur = inject(LiveAnnouncer);
  private readonly titre = viewChild.required<ElementRef<HTMLElement>>('titre');
  protected readonly selection = inject(SelectionSemaine);
  protected readonly libelleJour = libelleJour;

  /** La fiche de l'utilisateur connecté (RG-50) ; `null` pour un compte qui n'en a pas. */
  protected readonly collaborateurId = inject(Session).collaborateurId;

  protected readonly feuille = this.api.deLaSemaine(this.collaborateurId, this.selection.semaine);
  private readonly affectations = this.api.affectations(this.collaborateurId);
  private readonly absences = this.api.absences(this.collaborateurId);
  private readonly projets = this.api.projets();
  private readonly joursFeries = this.api.joursFeries();

  /** Copie de travail des saisies : repart de la feuille à chaque (re)chargement. */
  protected readonly grille = linkedSignal(() =>
    versGrille(this.feuille.hasValue() ? this.feuille.value().saisies : []),
  );

  /** RG-33 : une ligne par projet auquel le collaborateur est affecté pendant la semaine. */
  protected readonly lignes = computed(() =>
    lignesDeLaSemaine(
      this.affectations.hasValue() ? this.affectations.value() : [],
      this.projets.hasValue() ? this.projets.value().elements : [],
      this.selection.jours(),
    ),
  );

  /** RG-32 : week-ends, jours fériés et jours entièrement couverts par une absence approuvée. */
  protected readonly joursFermes = computed(() => {
    const feries = new Set(
      (this.joursFeries.hasValue() ? this.joursFeries.value() : []).map((jour) => jour.date),
    );
    const absents = joursAbsents(this.lesAbsences(), this.selection.jours());
    return new Set([
      ...this.selection.jours().filter((jour) => estWeekEnd(jour) || feries.has(jour)),
      ...absents,
    ]);
  });

  /** RG-32 : les jours où une demi-journée d'absence ne laisse à saisir qu'une demi-journée. */
  protected readonly joursEnDemiAbsence = computed(
    () => new Set(joursEnDemiAbsence(this.lesAbsences(), this.selection.jours())),
  );

  /** RG-34 et RG-35 : seule une feuille en brouillon se modifie. */
  protected readonly lectureSeule = computed(
    () => !this.feuille.hasValue() || this.feuille.value().statut !== 'Brouillon',
  );

  protected readonly libelleStatut = computed(() =>
    this.feuille.hasValue() ? LIBELLES_STATUT[this.feuille.value().statut] : '',
  );

  /** RG-31 et RG-32 : les jours dont le total dépasse ce qui reste à saisir (une journée, moins les absences). */
  protected readonly joursEnDepassement = computed(() =>
    this.selection
      .jours()
      .filter(
        (jour) =>
          totalDuJour(this.grille(), this.lignes(), jour) > disponible(this.lesAbsences(), jour),
      ),
  );

  protected readonly envoiEnCours = signal(false);
  protected readonly peutEnvoyer = computed(
    () => !this.lectureSeule() && this.joursEnDepassement().length === 0 && !this.envoiEnCours(),
  );

  protected enregistrer(): void {
    if (!this.peutEnvoyer() || !this.feuille.hasValue()) return;
    const id = this.feuille.value().id;
    this.envoyer(this.api.enregistrer(id, versSaisies(this.grille())), 'Brouillon enregistré.');
  }

  /** Enregistre d'abord, soumet ensuite : la feuille soumise est celle qu'on a sous les yeux. */
  protected soumettre(): void {
    if (!this.peutEnvoyer() || !this.feuille.hasValue()) return;
    const id = this.feuille.value().id;
    this.envoyer(
      this.api
        .enregistrer(id, versSaisies(this.grille()))
        .pipe(concatMap(() => this.api.soumettre(id))),
      'Feuille de temps soumise.',
      true,
    );
  }

  private lesAbsences() {
    return this.absences.hasValue() ? this.absences.value() : [];
  }

  /**
   * `annonce` est lue par les lecteurs d'écran : l'écran change peu, et ils ne le voient pas.
   * `focusSurLeTitre` : la feuille soumise perd ses boutons, le focus revient au titre plutôt que de se perdre.
   */
  private envoyer(requete: Observable<unknown>, annonce: string, focusSurLeTitre = false): void {
    this.envoiEnCours.set(true);
    requete.pipe(finalize(() => this.envoiEnCours.set(false))).subscribe({
      next: () => {
        this.feuille.reload();
        void this.annonceur.announce(annonce, 'polite');
        if (focusSurLeTitre) this.titre().nativeElement.focus();
      },
      error: () => {
        // déjà notifiée par l'intercepteur ; la saisie reste à l'écran
      },
    });
  }
}

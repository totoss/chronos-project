import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable } from 'rxjs';
import { CollaborateursApi } from '../../collaborateurs/collaborateurs-api';
import { ajouterJours, estWeekEnd, libelleJour } from '../../commun/dates';
import { DialogueDeRejet } from '../dialogue-de-rejet/dialogue-de-rejet';
import {
  FeuilleDeTemps,
  LIBELLES_STATUT,
  lignesDeLaFeuille,
  versGrille,
} from '../feuille-de-temps';
import { FeuillesDeTempsApi } from '../feuilles-de-temps-api';
import { GrilleDeSaisie } from '../grille-de-saisie/grille-de-saisie';

/** Écran du manager : les feuilles soumises qu'il peut valider, relues dans la grille en lecture seule. */
@Component({
  imports: [GrilleDeSaisie, MatButtonModule, MatProgressBarModule],
  selector: 'app-feuilles-a-valider',
  styleUrl: './feuilles-a-valider.scss',
  templateUrl: './feuilles-a-valider.html',
})
export class FeuillesAValider {
  private readonly api = inject(FeuillesDeTempsApi);
  private readonly dialog = inject(MatDialog);
  private readonly annonceur = inject(LiveAnnouncer);
  private readonly titre = viewChild.required<ElementRef<HTMLElement>>('titre');

  protected readonly libelleJour = libelleJour;
  protected readonly libelleStatut = LIBELLES_STATUT;

  protected readonly soumises = this.api.soumises();
  private readonly collaborateurs = inject(CollaborateursApi).liste(() => ({
    page: 1,
    taille: 100,
    recherche: '',
    avecSortis: true,
    tri: 'nom',
  }));
  private readonly projets = this.api.projets();
  private readonly joursFeries = this.api.joursFeries();

  private readonly feuilleChoisie = signal<number | null>(null);

  /** La feuille ouverte : celle que le manager a choisie, tant qu'elle est encore à valider. */
  protected readonly feuille = computed<FeuilleDeTemps | undefined>(() =>
    (this.soumises.hasValue() ? this.soumises.value() : []).find(
      (f) => f.id === this.feuilleChoisie(),
    ),
  );

  protected readonly jours = computed(() => {
    const lundi = this.feuille()?.semaine;
    return lundi === undefined ? [] : [0, 1, 2, 3, 4, 5, 6].map((n) => ajouterJours(lundi, n));
  });

  protected readonly lignes = computed(() =>
    lignesDeLaFeuille(
      this.feuille()?.saisies ?? [],
      this.projets.hasValue() ? this.projets.value().elements : [],
      this.jours(),
    ),
  );

  protected readonly grille = computed(() => versGrille(this.feuille()?.saisies ?? []));

  protected readonly joursFermes = computed(() => {
    const feries = new Set(
      (this.joursFeries.hasValue() ? this.joursFeries.value() : []).map((jour) => jour.date),
    );
    return new Set(this.jours().filter((jour) => estWeekEnd(jour) || feries.has(jour)));
  });

  protected readonly envoiEnCours = signal(false);

  protected nomDuCollaborateur(id: number): string {
    const collaborateur = this.collaborateurs.hasValue()
      ? this.collaborateurs.value().elements.find((c) => c.id === id)
      : undefined;
    return collaborateur
      ? `${collaborateur.prenom} ${collaborateur.nom}`
      : `Collaborateur n° ${id}`;
  }

  protected ouvrir(feuille: FeuilleDeTemps): void {
    this.feuilleChoisie.set(feuille.id);
  }

  protected valider(feuille: FeuilleDeTemps): void {
    this.envoyer(this.api.valider(feuille.id), 'Feuille de temps validée.');
  }

  protected rejeter(feuille: FeuilleDeTemps): void {
    this.dialog
      .open<DialogueDeRejet, void, string>(DialogueDeRejet)
      .afterClosed()
      .subscribe((motif) => {
        if (motif) this.envoyer(this.api.rejeter(feuille.id, motif), 'Feuille de temps rejetée.');
      });
  }

  /**
   * Valider ou rejeter retire la feuille de la liste des feuilles à valider : on la redemande.
   * Les boutons disparaissent avec la feuille : le focus revient au titre, et l'annonce dit ce qui s'est passé.
   */
  private envoyer(requete: Observable<unknown>, annonce: string): void {
    this.envoiEnCours.set(true);
    requete.subscribe({
      next: () => {
        this.feuilleChoisie.set(null);
        this.envoiEnCours.set(false);
        this.soumises.reload();
        void this.annonceur.announce(annonce, 'polite');
        this.titre().nativeElement.focus();
      },
      error: () => this.envoiEnCours.set(false), // déjà notifiée par l'intercepteur
    });
  }
}

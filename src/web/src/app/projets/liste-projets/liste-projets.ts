import { Component, computed, signal } from '@angular/core';
import { Projet } from '../projet';

@Component({
  imports: [],
  selector: 'app-liste-projets',
  styleUrl: './liste-projets.scss',
  templateUrl: './liste-projets.html',
})
export class ListeProjets {
  protected readonly projets = signal<Projet[]>([
    { code: 'BDL-REFONTE', libelle: 'Refonte du portail client', nature: 'Facturable', statut: 'EnCours', client: 'Banque du Lac' },
    { code: 'ASM-MIGRATION', libelle: 'Migration vers le cloud', nature: 'Facturable', statut: 'EnPreparation', client: 'Assurances du Midi' },
    { code: 'BDL-AUDIT', libelle: 'Audit de sécurité', nature: 'Facturable', statut: 'Termine', client: 'Banque du Lac' },
    { code: 'INT-FORMATION', libelle: 'Plan de formation interne', nature: 'Interne', statut: 'EnCours', client: null },
    { code: 'INT-OUTILS', libelle: 'Outillage de la société', nature: 'Interne', statut: 'Annule', client: null },
  ]);

  protected readonly statutFiltre = signal('');

  protected readonly projetsFiltres = computed(() => {
    const statut = this.statutFiltre();

    return this.projets().filter((p) => statut === '' || p.statut === statut);
  });
}

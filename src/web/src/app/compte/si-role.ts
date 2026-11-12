import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Session } from './session';
import { Role } from './utilisateur';

/** `*appSiRole="'GestionnaireRh'"` n'affiche l'élément qu'aux utilisateurs qui ont l'un des rôles. */
@Directive({ selector: '[appSiRole]' })
export class SiRole {
  readonly appSiRole = input.required<Role | Role[]>();

  private readonly session = inject(Session);
  private readonly gabarit = inject(TemplateRef);
  private readonly conteneur = inject(ViewContainerRef);

  constructor() {
    effect(() => {
      const roles = this.appSiRole();
      const autorise = this.session.aLeRole(...(Array.isArray(roles) ? roles : [roles]));
      this.conteneur.clear();
      if (autorise) {
        this.conteneur.createEmbeddedView(this.gabarit);
      }
    });
  }
}

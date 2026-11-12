import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormulaireCollaborateur } from './formulaire-collaborateur';

describe('FormulaireCollaborateur', () => {
  let component: FormulaireCollaborateur;
  let fixture: ComponentFixture<FormulaireCollaborateur>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormulaireCollaborateur],
    }).compileComponents();

    fixture = TestBed.createComponent(FormulaireCollaborateur);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

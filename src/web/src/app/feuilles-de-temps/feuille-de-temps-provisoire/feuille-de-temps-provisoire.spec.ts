import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeuilleDeTempsProvisoire } from './feuille-de-temps-provisoire';

describe('FeuilleDeTempsProvisoire', () => {
  let component: FeuilleDeTempsProvisoire;
  let fixture: ComponentFixture<FeuilleDeTempsProvisoire>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeuilleDeTempsProvisoire],
    }).compileComponents();

    fixture = TestBed.createComponent(FeuilleDeTempsProvisoire);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

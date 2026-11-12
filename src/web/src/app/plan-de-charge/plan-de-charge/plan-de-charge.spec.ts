import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlanDeCharge } from './plan-de-charge';

describe('PlanDeCharge', () => {
  let component: PlanDeCharge;
  let fixture: ComponentFixture<PlanDeCharge>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlanDeCharge],
    }).compileComponents();

    fixture = TestBed.createComponent(PlanDeCharge);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

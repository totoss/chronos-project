import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ListeAbsences } from './liste-absences';

describe('ListeAbsences', () => {
  let component: ListeAbsences;
  let fixture: ComponentFixture<ListeAbsences>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListeAbsences],
    }).compileComponents();

    fixture = TestBed.createComponent(ListeAbsences);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

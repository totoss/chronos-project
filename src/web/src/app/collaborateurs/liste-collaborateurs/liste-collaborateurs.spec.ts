import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ListeCollaborateurs } from './liste-collaborateurs';

describe('ListeCollaborateurs', () => {
  let component: ListeCollaborateurs;
  let fixture: ComponentFixture<ListeCollaborateurs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListeCollaborateurs],
    }).compileComponents();

    fixture = TestBed.createComponent(ListeCollaborateurs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

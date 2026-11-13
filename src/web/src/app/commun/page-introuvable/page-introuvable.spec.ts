import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PageIntrouvable } from './page-introuvable';

describe('PageIntrouvable', () => {
  let component: PageIntrouvable;
  let fixture: ComponentFixture<PageIntrouvable>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageIntrouvable],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(PageIntrouvable);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

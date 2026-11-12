import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConnexionProvisoire } from './connexion-provisoire';

describe('ConnexionProvisoire', () => {
  let component: ConnexionProvisoire;
  let fixture: ComponentFixture<ConnexionProvisoire>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConnexionProvisoire],
    }).compileComponents();

    fixture = TestBed.createComponent(ConnexionProvisoire);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

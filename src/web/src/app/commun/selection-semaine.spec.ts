import { TestBed } from '@angular/core/testing';
import { SelectionSemaine } from './selection-semaine';

describe('SelectionSemaine', () => {
  let service: SelectionSemaine;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SelectionSemaine);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

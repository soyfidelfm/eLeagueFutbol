import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DraftPlayerFilters } from './draft-player-filters';

describe('DraftPlayerFilters', () => {
  let component: DraftPlayerFilters;
  let fixture: ComponentFixture<DraftPlayerFilters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DraftPlayerFilters]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DraftPlayerFilters);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

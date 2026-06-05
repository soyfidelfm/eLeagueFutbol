import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Seasons } from './seasons';

describe('Seasons', () => {
  let component: Seasons;
  let fixture: ComponentFixture<Seasons>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Seasons]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Seasons);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SharedUI } from './shared-ui';

describe('SharedUI', () => {
  let component: SharedUI;
  let fixture: ComponentFixture<SharedUI>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedUI]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SharedUI);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

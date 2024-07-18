import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActiveItemTasksTableComponent } from './active-item-tasks-table.component';

describe('ActiveItemTasksTableComponent', () => {
  let component: ActiveItemTasksTableComponent;
  let fixture: ComponentFixture<ActiveItemTasksTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActiveItemTasksTableComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ActiveItemTasksTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

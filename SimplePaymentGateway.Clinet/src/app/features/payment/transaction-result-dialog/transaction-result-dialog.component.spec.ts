import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionResultDialogComponent } from './transaction-result-dialog.component';

describe('TransactionResultDialogComponent', () => {
  let component: TransactionResultDialogComponent;
  let fixture: ComponentFixture<TransactionResultDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionResultDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionResultDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

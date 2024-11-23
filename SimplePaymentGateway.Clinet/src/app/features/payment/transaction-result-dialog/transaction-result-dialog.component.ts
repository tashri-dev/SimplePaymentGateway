import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TransactionResponse } from '../../../core/models/transaction.model';

@Component({
  selector: 'app-transaction-result-dialog',
  templateUrl: './transaction-result-dialog.component.html',
  styleUrls: ['./transaction-result-dialog.component.scss']
})
export class TransactionResultDialog {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: TransactionResponse,
    public dialogRef: MatDialogRef<TransactionResultDialog>
  ) {}

  onClose(): void {
    this.dialogRef.close();
  }

  getStatusClass(): string {
    return this.data.responseCode === '00' ? 'success-status' : 'error-status';
  }
}

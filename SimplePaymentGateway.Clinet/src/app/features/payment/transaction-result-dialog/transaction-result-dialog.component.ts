import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { TransactionResponse } from '../../../core/models/transaction.model';

@Component({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
  ],
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

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { CardNumberDirective } from '../../../shared/directives/card-number.directive';
import { ExpiryDateDirective } from '../../../shared/directives/expiry-date.directive';
import { CvvDirective } from '../../../shared/directives/cvv.directive';
import { AmountDirective } from '../../../shared/directives/amount.directive';
import { Currency } from '../../../core/models/currency.model';
import { FunctionCode, Transaction, TransactionResponse } from '../../../core/models/transaction.model';
import { CurrencyService } from '../../../core/services/currency.service';
import { LoggerService } from '../../../core/services/logger.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-payment-form',
  standalone: true,
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
    CardNumberDirective,
    ExpiryDateDirective,
    CvvDirective,
    AmountDirective
  ],
  templateUrl: './payment-form.component.html',
  styleUrls: ['./payment-form.component.scss']
})
export class PaymentFormComponent implements OnInit {
  paymentForm!: FormGroup;
  loading = false;
  currencies: Currency[] = [];
  functionCodes = [
    { code: FunctionCode.Purchase, name: 'Purchase' },
    { code: FunctionCode.Refund, name: 'Refund' },
    { code: FunctionCode.Void, name: 'Void' }
  ];

  constructor(
    private fb: FormBuilder,
    private transactionService: TransactionService,
    private currencyService: CurrencyService,
    private logger: LoggerService,
    private authService:AuthService
  ) {
    this.createForm();
  }

  ngOnInit(): void {
    this.currencies = this.currencyService.getCurrencies();
  }

  private createForm(): void {
    this.paymentForm = this.fb.group({
      cardNo: ['', [
        Validators.required,
        Validators.pattern('^[0-9]{16}$')
      ]],
      cardHolder: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.pattern('^[a-zA-Z ]*$')
      ]],
      expiryDate: ['', [
        Validators.required,
        Validators.pattern('^(0[1-9]|1[0-2])\/([0-9]{2})$'),
        this.expiryDateValidator()
      ]],
      cvv: ['', [
        Validators.required,
        Validators.pattern('^[0-9]{3,4}$')
      ]],
      amountTrxn: ['', [
        Validators.required,
        Validators.min(0.01)
      ]],
      currencyCode: ['840', Validators.required],
      functionCode: [FunctionCode.Purchase, Validators.required]
    });
  }

  private expiryDateValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const [month, year] = control.value.split('/');
      const expiry = new Date(2000 + parseInt(year), parseInt(month) - 1);
      const today = new Date();
      today.setDate(1);
      today.setHours(0, 0, 0, 0);

      return expiry >= today ? null : { expired: true };
    };
  }

  async onSubmit(): Promise<void> {
    if (this.paymentForm.invalid) {
      this.markFormGroupTouched(this.paymentForm);
      return;
    }

    this.loading = true;

    try {
      const formValue = this.paymentForm.value;

      const transaction: Transaction = {
        processingCode: '999000',
        systemTraceNr: Math.floor(Math.random() * 1000000).toString().padStart(6, '0'),
        functionCode: formValue.functionCode,
        cardNo: formValue.cardNo.replace(/\s/g, ''),
        cardHolder: formValue.cardHolder,
        amountTrxn: parseFloat(formValue.amountTrxn),
        currencyCode: formValue.currencyCode,
        expiryDate: formValue.expiryDate.replace('/', ''),
        cvv: formValue.cvv
      };

      const response = await this.transactionService.processTransaction(transaction);

      if (response.responseCode === '00') {
        this.showSuccessDialog(response);
        this.paymentForm.reset({
          currencyCode: '840',
          functionCode: FunctionCode.Purchase
        });
      } else {
        this.showError(response.message);
      }
    } catch (error) {
      this.logger.error('Payment processing failed', error);
      this.showError('Failed to process payment. Please try again.');
    } finally {
      this.loading = false;
    }
  }

  private showSuccessDialog(response: TransactionResponse): void {
    alert(`Transaction Successful! Approval Code: ${response.approvalCode}`);
  }

  private showError(message: string): void {
    alert(message);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getErrorMessage(controlName: string): string {
    const control = this.paymentForm.get(controlName);
    if (!control?.errors || !control.touched) return '';

    const errors = control.errors;

    if (errors['required']) return `${this.formatControlName(controlName)} is required`;
    if (errors['pattern']) {
      switch (controlName) {
        case 'cardNo': return 'Invalid card number';
        case 'cardHolder': return 'Only letters and spaces allowed';
        case 'expiryDate': return 'Invalid expiry date format (MM/YY)';
        case 'cvv': return 'Invalid CVV';
        default: return 'Invalid format';
      }
    }

    if (errors['min']) return `Amount must be greater than 0`;
    if (errors['minlength']) return `${this.formatControlName(controlName)} is too short`;
    if (errors['expired']) return 'Card has expired';
    return 'Invalid value';
  }

  private formatControlName(name: string): string {
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  getCurrencySymbol(): string {
    const code = this.paymentForm.get('currencyCode')?.value;
    return this.currencyService.getCurrencyByCode(code)?.symbol ?? '';
  }

  resetForm(): void {
    // Reset form with default values
    this.paymentForm.reset({
      currencyCode: '840',  // Default to USD
      functionCode: FunctionCode.Purchase
    });

    // Clear all form errors
    Object.keys(this.paymentForm.controls).forEach(key => {
      const control = this.paymentForm.get(key);
      control?.setErrors(null);
      control?.markAsUntouched();
    });
  }

  logout(): void {
    this.authService.logout();
  }
}

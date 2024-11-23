import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from "@angular/forms";
import { SharedModule } from '../../shared/shared.module';
import { PaymentFormComponent } from './payment-form/payment-form.component';
import { TransactionResultDialog } from './transaction-result-dialog/transaction-result-dialog.component';
import { AmountDirective } from '../../shared/directives/amount.directive';
import { CardNumberDirective } from '../../shared/directives/card-number.directive';
import { CvvDirective } from '../../shared/directives/cvv.directive';
import { ExpiryDateDirective } from '../../shared/directives/expiry-date.directive';

@NgModule({
  declarations: [
    // Declare components that aren't standalone
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SharedModule,
    // Your standalone components
    PaymentFormComponent,
    TransactionResultDialog,
    // Directives
    AmountDirective,
    CardNumberDirective,
    ExpiryDateDirective,
    CvvDirective
  ],
  exports: [
    PaymentFormComponent
  ]
})
export class PaymentModule { }

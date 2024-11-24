import { Directive, ElementRef, HostListener, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CurrencyService } from '../../core/services/currency.service';
import { FormGroup, NgControl } from '@angular/forms';

@Directive({
  selector: '[appAmount]'
})
export class AmountDirective implements OnInit, OnDestroy {
  @Input() currencyCode: string = '840'; // Default to USD
  private destroy$ = new Subject<void>();
  private currentDecimals = 2;

  constructor(
    private currencyService: CurrencyService,
    private el: ElementRef<HTMLInputElement>,
    private ngControl: NgControl
  ) {}

  ngOnInit() {
    if (this.ngControl && this.ngControl.control) {
      // Listen for currency changes if the control is part of a form group
      const form = this.ngControl.control.parent as FormGroup;
      if (form && form.get('currencyCode')) {
        form.get('currencyCode')!.valueChanges
          .pipe(takeUntil(this.destroy$))
          .subscribe(code => {
            this.currencyCode = code;
            this.currentDecimals = this.currencyService.getDecimalPlaces(code);
            this.formatValue();
          });
      }
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('input', ['$event'])
  onInput(event: InputEvent): void {
    const input = event.target as HTMLInputElement;
    let value = input.value;

    // Remove all non-digits/decimal points
    value = value.replace(/[^\d.]/g, '');

    // Ensure only one decimal point
    const parts = value.split('.');
    if (parts.length > 2) {
      value = parts[0] + '.' + parts.slice(1).join('');
    }

    // Apply decimal places limit based on currency
    if (parts.length === 2) {
      value = parts[0] + '.' + parts[1].substring(0, this.currentDecimals);
    }

    // Update the input value
    input.value = value;

    // Update the form control value
    if (this.ngControl && this.ngControl.control) {
      this.ngControl.control.setValue(value, { emitEvent: false });
    }
  }

  private formatValue(): void {
    if (this.ngControl && this.ngControl.value) {
      const value = this.ngControl.value;
      const formatted = Number(value).toFixed(this.currentDecimals);
      this.ngControl.control?.setValue(formatted, { emitEvent: false });
    }
  }
}

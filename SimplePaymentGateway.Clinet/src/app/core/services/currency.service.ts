import { Injectable } from '@angular/core';
import { Currency } from '../models/currency.model';

@Injectable({
  providedIn: 'root'
})
export class CurrencyService {
  private currencies = [
    { code: '840', name: 'USD', symbol: '$', decimals: 2 },
    { code: '978', name: 'EUR', symbol: '€', decimals: 2 },
    { code: '826', name: 'GBP', symbol: '£', decimals: 2 },
    { code: '392', name: 'JPY', symbol: '¥', decimals: 0 },
    { code: '756', name: 'CHF', symbol: 'Fr', decimals: 2 },
    { code: '036', name: 'AUD', symbol: 'A$', decimals: 2 }
  ];

  getCurrencies(): Currency[] {
    return this.currencies;
  }

  getCurrencyByCode(code: string): Currency | undefined {
    return this.currencies.find(c => c.code === code);
  }

  getDecimalPlaces(currencyCode: string): number {
    return this.getCurrencyByCode(currencyCode)?.decimals ?? 2;
  }
}

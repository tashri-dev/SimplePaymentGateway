import { Injectable } from "@angular/core";

@Injectable({
  providedIn: 'root'
})
export class TransactionGeneratorService {
  private readonly processingCodes = [
    '000000', // Purchase
    '200000', // Refund
    '020000', // Account inquiry
    '400000', // Transfer
    '900000', // Balance inquiry
    '999000'  // Generic transaction
  ];

  generateSystemTraceNr(): string {
    return Math.floor(100000 + Math.random() * 900000).toString();
  }

  generateProcessingCode(): string {
    const index = Math.floor(Math.random() * this.processingCodes.length);
    return this.processingCodes[index];
  }

  generateReferenceNumber(): string {
    const timestamp = new Date().getTime().toString().slice(-6);
    const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0');
    return `${timestamp}${random}`;
  }
}

import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { Transaction, TransactionResponse, Result } from "../models/transaction.model";
import { BcdConverter } from "../utils/bcd-converter";
import { EncryptionService } from "./encryption.service";
import { LoggerService } from "./logger.service";

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private readonly apiUrl ='/api/transaction'
  constructor(
    private http: HttpClient,
    private encryptionService: EncryptionService,
    private logger: LoggerService
  ) {}

  async processTransaction(transaction: Transaction): Promise<TransactionResponse> {
    try {
      this.logger.info('Starting transaction process', { transaction });

      // Get new encryption key
      const keySuccess = await this.encryptionService.getNewKey();
      if (!keySuccess) {
        throw new Error('Failed to get encryption key');
      }

      // Compact and encrypt transaction data
      const compactData = BcdConverter.compactTransaction(transaction);
      this.logger.debug('Compacted transaction data', { size: compactData.length });

      const encryptedData = await this.encryptionService.encrypt(compactData);
      this.logger.debug('Encrypted transaction data');

      // Send request
      const keyIdentifier = this.encryptionService.getCurrentKeyIdentifier();
      if (!keyIdentifier) {
        throw new Error('No key identifier available');
      }

       // Send the encrypted string directly
       const response = await firstValueFrom(
         this.http.post<Result<string>>(
           `${this.apiUrl}/process`,
           JSON.stringify(encryptedData), // Send encrypted string directly
           {
             headers: {
               'Content-Type': 'application/json',
               'X-Key-Identifier': this.encryptionService.getCurrentKeyIdentifier()!
             }
           }
         )
       );

      if (!response.isSuccess) {
        throw new Error(response.error || 'Transaction failed');
      }

      // Ensure response.value is not undefined
      const responseValue = response.value ?? '';
      if (responseValue === '') {
        throw new Error('Response value is undefined');
      }

      // Decrypt and parse response
      const decryptedData = await this.encryptionService.decrypt(responseValue);
      const transactionResponse = JSON.parse(new TextDecoder().decode(decryptedData));

      this.logger.info('Transaction completed', {
        responseCode: transactionResponse.responseCode,
        message: transactionResponse.message
      });

      return transactionResponse;
    } catch (error) {
      this.logger.error('Transaction processing failed', error);
      throw error;
    } finally {
      this.encryptionService.clearCurrentKey();
    }
  }
}

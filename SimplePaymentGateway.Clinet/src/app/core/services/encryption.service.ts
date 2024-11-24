import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { Result } from "../models/transaction.model";
import { EncryptionKeyResponse } from "../models/encryption.model";
import { LoggerService } from "./logger.service";

@Injectable({
  providedIn: 'root'
})
export class EncryptionService {
  private readonly apiUrl = '/api/encryption'
  private currentKey: string | null = null;
  private currentKeyIdentifier: string | null = null;
  private currentIv: string | null = null;

  constructor(
    private http: HttpClient,
    private logger: LoggerService
  ) {}

  async getNewKey(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.get<Result<EncryptionKeyResponse>>(`${this.apiUrl}/key`)
      );

      if (!response?.isSuccess || !response.value) {
        throw new Error(response?.error);
      }

      this.currentKey = response.value.key ?? null;
      this.currentKeyIdentifier = response.value.keyIdentifier ?? null;
      this.currentIv = response.value.iv ?? null;

      return true;
    } catch (error) {
      this.logger.error('Failed to get encryption key', error);
      return false;
    }
  }

  async encrypt(data: Uint8Array): Promise<string> {
    if (!this.currentKey) throw new Error('No encryption key available');

    try {
      const key = await this.importKey(this.currentKey);
      const iv = this.base64ToArrayBuffer(this.currentIv!);

      const encrypted = await window.crypto.subtle.encrypt(
        {
          name: 'AES-CBC',
          iv
        },
        key,
        data
      );

      // Create array with IV + encrypted data (matching backend implementation)
      const resultArray = new Uint8Array(iv.byteLength + encrypted.byteLength);
      resultArray.set(new Uint8Array(iv), 0);
      resultArray.set(new Uint8Array(encrypted), iv.byteLength);

      // Convert to Base64 to match backend's output
      return btoa(String.fromCharCode.apply(null, Array.from(resultArray)));
    } catch (error) {
      this.logger.error('Encryption error', error);
      throw new Error('Failed to encrypt data');
    }
  }

  async decrypt(encryptedData: string): Promise<Uint8Array> {
    if (!this.currentKey) throw new Error('No encryption key available');

    try {
      // Convert base64 to array buffer
      const fullCipher = this.base64ToArrayBuffer(encryptedData);

      // Split IV and cipher text (matching backend implementation)
      const iv = fullCipher.slice(0, 16);
      const cipherText = fullCipher.slice(16);

      const key = await this.importKey(this.currentKey);

      const decrypted = await window.crypto.subtle.decrypt(
        {
          name: 'AES-CBC',
          iv
        },
        key,
        cipherText
      );

      return new Uint8Array(decrypted);
    } catch (error) {
      this.logger.error('Decryption error', error);
      throw new Error('Failed to decrypt data');
    }
  }

  getCurrentKeyIdentifier(): string | null {
    return this.currentKeyIdentifier;
  }

  clearCurrentKey(): void {
    this.currentKey = null;
    this.currentKeyIdentifier = null;
    this.currentIv = null;
  }

  private async importKey(keyBase64: string): Promise<CryptoKey> {
    try {
      const keyData = this.base64ToArrayBuffer(keyBase64);

      // Validate key length to match backend
      if (keyData.byteLength !== 32) { // 256 bits = 32 bytes
        throw new Error('Invalid key length');
      }

      return await window.crypto.subtle.importKey(
        'raw',
        keyData,
        {
          name: 'AES-CBC',
          length: 256 // Matching backend's AES-256
        },
        false,
        ['encrypt', 'decrypt']
      );
    } catch (error) {
      this.logger.error('Key import error', error);
      throw new Error('Failed to import key');
    }
  }

  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    try {
      const binaryString = atob(base64);
      const bytes = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
      }
      return bytes.buffer;
    } catch (error) {
      this.logger.error('Base64 conversion error', error);
      throw new Error('Failed to convert base64 string');
    }
  }
}

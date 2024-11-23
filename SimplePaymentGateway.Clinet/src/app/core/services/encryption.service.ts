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
  private readonly apiUrl = '/api/encryption/'
  private currentKey: string | null = null;
  private currentKeyIdentifier: string | null = null;
  private currentIv: string | null = null;

  constructor(
    private http: HttpClient,
    private logger : LoggerService
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

    return btoa(String.fromCharCode(...new Uint8Array(encrypted)));
  }

  async decrypt(data: string): Promise<Uint8Array> {
    if (!this.currentKey) throw new Error('No encryption key available');

    const key = await this.importKey(this.currentKey);
    const iv = this.base64ToArrayBuffer(this.currentIv!);
    const encryptedData = this.base64ToArrayBuffer(data);

    const decrypted = await window.crypto.subtle.decrypt(
      {
        name: 'AES-CBC',
        iv
      },
      key,
      encryptedData
    );

    return new Uint8Array(decrypted);
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
    const keyData = this.base64ToArrayBuffer(keyBase64);
    return window.crypto.subtle.importKey(
      'raw',
      keyData,
      { name: 'AES-CBC' },
      false,
      ['encrypt', 'decrypt']
    );
  }

  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  }
}

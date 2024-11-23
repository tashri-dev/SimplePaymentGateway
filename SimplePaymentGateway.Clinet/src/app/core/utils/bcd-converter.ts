import { Transaction, FunctionCode } from "../models/transaction.model";

export class BcdConverter {
  static stringToBcd(value: string): Uint8Array {
    const paddedValue = value.length % 2 ? '0' + value : value;
    const result = new Uint8Array(paddedValue.length / 2);

    for (let i = 0; i < paddedValue.length; i += 2) {
      const highNibble = parseInt(paddedValue[i], 16);
      const lowNibble = parseInt(paddedValue[i + 1], 16);
      result[i / 2] = (highNibble << 4) | lowNibble;
    }

    return result;
  }

  static bcdToString(bcd: Uint8Array): string {
    let result = '';
    for (const byte of bcd) {
      result += ((byte >> 4) & 0x0F).toString(16);
      result += (byte & 0x0F).toString(16);
    }
    return result.toUpperCase();
  }

  static compactTransaction(transaction: Transaction): Uint8Array {
    const buffer = new ArrayBuffer(28); // Total compact size
    const view = new DataView(buffer);
    let offset = 0;

    // Processing Code (999000 -> 999)
    const procCode = this.stringToBcd(transaction.processingCode.substring(0, 3));
    for (const byte of procCode) view.setUint8(offset++, byte);

    // System Trace (6 digits -> 3 bytes)
    const sysTrace = this.stringToBcd(transaction.systemTraceNr.padStart(6, '0'));
    for (const byte of sysTrace) view.setUint8(offset++, byte);

    // Function Code (4 digits -> 2 bytes)
    const funcCode = this.stringToBcd(transaction.functionCode.toString().padStart(4, '0'));
    for (const byte of funcCode) view.setUint8(offset++, byte);

    // Card Number (16 digits -> 8 bytes)
    const cardNo = this.stringToBcd(transaction.cardNo);
    for (const byte of cardNo) view.setUint8(offset++, byte);

    // Amount (12 digits -> 6 bytes)
    const amount = this.stringToBcd(Math.round(transaction.amountTrxn * 100).toString().padStart(12, '0'));
    for (const byte of amount) view.setUint8(offset++, byte);

    // Currency Code (3 digits -> 2 bytes)
    const currency = this.stringToBcd(transaction.currencyCode.padStart(3, '0'));
    for (const byte of currency) view.setUint8(offset++, byte);

    // Expiry Date (YYMM -> 2 bytes)
    const expiry = this.stringToBcd(transaction.expiryDate);
    for (const byte of expiry) view.setUint8(offset++, byte);

    // CVV (3/4 digits -> 2 bytes)
    const cvv = this.stringToBcd(transaction.cvv.padStart(4, '0'));
    for (const byte of cvv) view.setUint8(offset++, byte);

    return new Uint8Array(buffer);
  }

  static decompactTransaction(data: Uint8Array): Transaction {
    const view = new DataView(data.buffer);
    let offset = 0;

    const readBcdString = (length: number): string => {
      const bytes = new Uint8Array(data.buffer, offset, length);
      offset += length;
      return this.bcdToString(bytes);
    };

    return {
      processingCode: readBcdString(3) + '000',
      systemTraceNr: readBcdString(3),
      functionCode: parseInt(readBcdString(2)) as unknown as FunctionCode,
      cardNo: readBcdString(8),
      cardHolder: '', // Not included in compact format
      amountTrxn: parseInt(readBcdString(6)) / 100,
      currencyCode: readBcdString(2),
      expiryDate: readBcdString(2),
      cvv: readBcdString(2).replace(/^0+/, '')
    };
  }
}

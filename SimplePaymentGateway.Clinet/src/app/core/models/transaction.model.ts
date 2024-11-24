export interface Transaction {
  processingCode: string;
  systemTraceNr: string;
  functionCode: string;
  cardNo: string;
  cardHolder: string;
  amountTrxn: number;
  currencyCode: string;
  expiryDate: string;
  cvv: string;
}

export interface TransactionResponse {
  responseCode: string;
  message: string;
  approvalCode: string;
  dateTime: string;
}

export interface Result<T> {
  isSuccess: boolean;
  value?: T;
  error?: string;
  errorCode?: string;
}

export enum FunctionCode {
  Purchase = '1234',
  Refund = '234',
  Void = '34'
}

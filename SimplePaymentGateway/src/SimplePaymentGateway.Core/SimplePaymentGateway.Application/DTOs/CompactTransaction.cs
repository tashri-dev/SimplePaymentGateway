using SimplePaymentGateway.Application.Common;
using SimplePaymentGateway.Domain.Entities;
using SimplePaymentGateway.Domain.Enums;

namespace SimplePaymentGateway.Application.DTOs;


public class CompactTransaction
{
    public const int PROCESSING_CODE_LENGTH = 3; // 999000 -> 999
    public const int SYSTEM_TRACE_LENGTH = 3;    // 6 digits -> 3 bytes
    public const int FUNCTION_CODE_LENGTH = 2;   // 4 digits -> 2 bytes
    public const int CARD_NO_LENGTH = 8;         // 16 digits -> 8 bytes
    public const int AMOUNT_LENGTH = 6;          // 12 digits -> 6 bytes
    public const int CURRENCY_LENGTH = 2;        // 3 digits -> 2 bytes
    public const int EXPIRY_LENGTH = 2;          // YYMM -> 2 bytes
    public const int CVV_LENGTH = 2;             // 3/4 digits -> 2 bytes

    public byte[] ToCompactFormat(Transaction transaction)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Processing Code (999000 -> 999)
        writer.Write(BcdConverter.StringToBcd(transaction.ProcessingCode.Substring(0, 3)));

        // System Trace (6 digits -> 3 bytes)
        writer.Write(BcdConverter.StringToBcd(transaction.SystemTraceNr.PadLeft(6, '0')));

        // Function Code (4 digits -> 2 bytes)
        writer.Write(BcdConverter.StringToBcd(((int)transaction.FunctionCode).ToString().PadLeft(4, '0')));

        // Card Number (16 digits -> 8 bytes)
        writer.Write(BcdConverter.StringToBcd(transaction.CardNo));

        // Amount (12 digits -> 6 bytes, includes 2 decimal places)
        var amount = ((long)(transaction.AmountTrxn * 100)).ToString().PadLeft(12, '0');
        writer.Write(BcdConverter.StringToBcd(amount));

        // Currency Code (3 digits -> 2 bytes)
        writer.Write(BcdConverter.StringToBcd(transaction.CurrencyCode.PadLeft(3, '0')));

        // Expiry Date (YYMM -> 2 bytes)
        writer.Write(BcdConverter.StringToBcd(transaction.ExpiryDate));

        // CVV (3/4 digits -> 2 bytes)
        writer.Write(BcdConverter.StringToBcd(transaction.CVV.PadLeft(4, '0')));

        return ms.ToArray();
    }

    public Transaction FromCompactFormat(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var processingCode = BcdConverter.BcdToString(reader.ReadBytes(PROCESSING_CODE_LENGTH)) + "000";
        var systemTrace = BcdConverter.BcdToString(reader.ReadBytes(SYSTEM_TRACE_LENGTH));
        var functionCode = int.Parse(BcdConverter.BcdToString(reader.ReadBytes(FUNCTION_CODE_LENGTH)));
        var cardNo = BcdConverter.BcdToString(reader.ReadBytes(CARD_NO_LENGTH));
        var amount = decimal.Parse(BcdConverter.BcdToString(reader.ReadBytes(AMOUNT_LENGTH))) / 100M;
        var currency = BcdConverter.BcdToString(reader.ReadBytes(CURRENCY_LENGTH));
        var expiry = BcdConverter.BcdToString(reader.ReadBytes(EXPIRY_LENGTH));
        var cvv = BcdConverter.BcdToString(reader.ReadBytes(CVV_LENGTH)).TrimStart('0');

        return new Transaction
        {
            ProcessingCode = processingCode,
            SystemTraceNr = systemTrace,
            FunctionCode = (FunctionCode)functionCode,
            CardNo = cardNo,
            AmountTrxn = amount,
            CurrencyCode = currency,
            ExpiryDate = expiry,
            CVV = cvv
        };
    }

    public int GetCompactSize()
    {
        return PROCESSING_CODE_LENGTH + SYSTEM_TRACE_LENGTH + FUNCTION_CODE_LENGTH +
               CARD_NO_LENGTH + AMOUNT_LENGTH + CURRENCY_LENGTH + EXPIRY_LENGTH + CVV_LENGTH;
    }
}
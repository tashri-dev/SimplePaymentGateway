using SimplePaymentGateway.Domain.Entities;
using SimplePaymentGateway.Domain.Enums;

namespace SimplePaymentGateway.Application.DTOs;

public class TransactionRequest
{
    public string ProcessingCode { get; set; } = "999000";
    public string SystemTraceNr { get; set; }
    public string FunctionCode { get; set; }
    public string CardNo { get; set; }
    public string CardHolder { get; set; }
    public decimal AmountTrxn { get; set; }
    public string CurrencyCode { get; set; }
    public string ExpiryDate { get; set; }
    public string CVV { get; set; }

    public Transaction ToEntity()
    {
        return new Transaction
        {
            ProcessingCode = ProcessingCode,
            SystemTraceNr = SystemTraceNr,
            FunctionCode = Enum.Parse<FunctionCode>(FunctionCode),
            CardNo = CardNo,
            CardHolder = CardHolder,
            AmountTrxn = AmountTrxn,
            CurrencyCode = CurrencyCode,
            ExpiryDate = ExpiryDate,
            CVV = CVV
        };
    }
}

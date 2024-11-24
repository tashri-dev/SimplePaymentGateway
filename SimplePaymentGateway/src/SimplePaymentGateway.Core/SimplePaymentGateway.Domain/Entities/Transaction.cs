using SimplePaymentGateway.Domain.Enums;
using SimplePaymentGateway.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace SimplePaymentGateway.Domain.Entities;

public class Transaction
{
    public string ProcessingCode { get; set; } = "999000";
    public string SystemTraceNr { get; set; }
    public FunctionCode FunctionCode { get; set; }
    public string CardNo { get; set; }
    public string CardHolder { get; set; }
    public decimal AmountTrxn { get; set; }
    public string CurrencyCode { get; set; }
    public string ExpiryDate { get; set; }  // YYMM format
    public string CVV { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(CardNo) || CardNo.Length != 16)
            errors.Add("Invalid card number");

        if (string.IsNullOrEmpty(CardHolder))
            errors.Add("Cardholder name is required");

        if (AmountTrxn <= 0)
            errors.Add("Amount must be greater than zero");

        if (string.IsNullOrEmpty(ExpiryDate) || !Regex.IsMatch(ExpiryDate, @"^(0[1-9]|1[0-2])/(2[0-9])$"))
            errors.Add("Invalid expiry date format (MM/YY)");

        if (string.IsNullOrEmpty(CVV) || !Regex.IsMatch(CVV, @"^\d{3,4}$"))
            errors.Add("Invalid CVV");

        if (errors.Any())
            throw new PaymentGatewayException($"Validation failed: {string.Join(", ", errors)}", "VALIDATION_ERROR");
    }
}

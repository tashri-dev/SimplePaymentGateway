using Microsoft.Extensions.Logging;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Application.DTOs;
using SimplePaymentGateway.Domain.Common;
using SimplePaymentGateway.Domain.Entities;
using SimplePaymentGateway.Domain.Enums;
using System.Text.RegularExpressions;

namespace SimplePaymentGateway.Infrastructure.Services;

public class TransactionProcessor : ITransactionProcessor
{
    private readonly ILogger<TransactionProcessor> _logger;
    private readonly Random _random;
    private const int ApprovalCodeLength = 6;
    private static readonly HashSet<string> _processedTransactions = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public TransactionProcessor(ILogger<TransactionProcessor> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public async Task<Result<TransactionResponse>> ProcessTransaction(Transaction transaction)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transaction.SystemTraceNr,
            ["FunctionCode"] = transaction.FunctionCode,
            ["Amount"] = transaction.AmountTrxn,
            ["Currency"] = transaction.CurrencyCode
        });

        try
        {
            _logger.LogInformation("Starting transaction processing: {TransactionId}",
                transaction.SystemTraceNr);

            // Validate transaction
            if (!await ValidateTransaction(transaction))
            {
                _logger.LogWarning("Transaction validation failed: {TransactionId}",
                    transaction.SystemTraceNr);
                return Result<TransactionResponse>.Failure(
                    "Transaction validation failed",
                    "VALIDATION_ERROR");
            }

            // Check for duplicate transaction
            if (!await CheckDuplicateTransaction(transaction.SystemTraceNr))
            {
                _logger.LogWarning("Duplicate transaction detected: {TransactionId}",
                    transaction.SystemTraceNr);
                return Result<TransactionResponse>.Failure(
                    "Duplicate transaction",
                    "DUPLICATE_ERROR");
            }

            // Process based on function code
            var response = await ProcessByFunctionCode(transaction);

            _logger.LogInformation(
                "Transaction completed: {TransactionId}, Response: {ResponseCode}",
                transaction.SystemTraceNr,
                response.ResponseCode);

            return Result<TransactionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing transaction: {TransactionId}",
                transaction.SystemTraceNr);

            return Result<TransactionResponse>.Failure(
                "Internal processing error",
                "SYSTEM_ERROR");
        }
    }

    public async Task<bool> ValidateTransaction(Transaction transaction)
    {
        try
        {
            var validationErrors = new List<string>();

            // Required fields validation
            if (string.IsNullOrEmpty(transaction.CardNo))
                validationErrors.Add("Card number is required");

            if (string.IsNullOrEmpty(transaction.CardHolder))
                validationErrors.Add("Cardholder name is required");

            if (transaction.AmountTrxn <= 0)
                validationErrors.Add("Amount must be greater than zero");

            // Card number validation (using Luhn algorithm)
            if (!await ValidateCardNumber(transaction.CardNo))
                validationErrors.Add("Invalid card number");

            // Expiry date validation
            if (!ValidateExpiryDate(transaction.ExpiryDate))
                validationErrors.Add("Invalid or expired card");

            // CVV validation
            if (!ValidateCVV(transaction.CVV, transaction.CardNo))
                validationErrors.Add("Invalid CVV");

            // Currency code validation
            if (!ValidateCurrencyCode(transaction.CurrencyCode))
                validationErrors.Add("Invalid currency code");

            // Function code validation
            if (!Enum.IsDefined(typeof(FunctionCode), transaction.FunctionCode))
                validationErrors.Add("Invalid function code");

            if (validationErrors.Any())
            {
                _logger.LogWarning(
                    "Validation failed for transaction {TransactionId}: {Errors}",
                    transaction.SystemTraceNr,
                    string.Join(", ", validationErrors));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating transaction: {TransactionId}",
                transaction.SystemTraceNr);
            return false;
        }
    }

    private async Task<bool> CheckDuplicateTransaction(string systemTraceNr)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_processedTransactions.Contains(systemTraceNr))
                return false;

            _processedTransactions.Add(systemTraceNr);

            // Keep the set size manageable
            if (_processedTransactions.Count > 10000)
            {
                _processedTransactions.Clear();
            }

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<TransactionResponse> ProcessByFunctionCode(Transaction transaction)
    {
        return transaction.FunctionCode switch
        {
            FunctionCode.Purchase => await ProcessPurchase(transaction),
            FunctionCode.Refund => await ProcessRefund(transaction),
            FunctionCode.Void => await ProcessVoid(transaction),
            _ => throw new ArgumentException("Invalid function code")
        };
    }

    private async Task<TransactionResponse> ProcessPurchase(Transaction transaction)
    {
        // Simulate processing delay
        await Task.Delay(100);

        // Basic business rules
        if (transaction.AmountTrxn > 10000)
        {
            return new TransactionResponse
            {
                ResponseCode = "01",
                Message = "Amount exceeds limit",
                ApprovalCode = string.Empty,
                DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };
        }

        return new TransactionResponse
        {
            ResponseCode = "00",
            Message = "Approved",
            ApprovalCode = GenerateApprovalCode(),
            DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        };
    }

    private async Task<TransactionResponse> ProcessRefund(Transaction transaction)
    {
        // Simulate processing delay
        await Task.Delay(100);

        // Basic refund validation
        if (transaction.AmountTrxn > 5000)
        {
            return new TransactionResponse
            {
                ResponseCode = "10",
                Message = "Refund amount exceeds limit",
                ApprovalCode = string.Empty,
                DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };
        }

        return new TransactionResponse
        {
            ResponseCode = "00",
            Message = "Refund approved",
            ApprovalCode = GenerateApprovalCode(),
            DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        };
    }

    private async Task<TransactionResponse> ProcessVoid(Transaction transaction)
    {
        // Simulate processing delay
        await Task.Delay(100);

        return new TransactionResponse
        {
            ResponseCode = "00",
            Message = "Void approved",
            ApprovalCode = GenerateApprovalCode(),
            DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        };
    }

    private string GenerateApprovalCode()
    {
        return new string(Enumerable.Range(0, ApprovalCodeLength)
            .Select(_ => _random.Next(10).ToString()[0])
            .ToArray());
    }

    private async Task<bool> ValidateCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) return false;

        // Luhn algorithm implementation
        return await Task.Run(() =>
        {
            if (!cardNumber.All(char.IsDigit)) return false;

            var sum = 0;
            var isEven = false;

            // Loop through values starting from the rightmost digit
            for (var i = cardNumber.Length - 1; i >= 0; i--)
            {
                var digit = cardNumber[i] - '0';

                if (isEven)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                isEven = !isEven;
            }

            return sum % 10 == 0;
        });
    }

    private bool ValidateExpiryDate(string expiryDate)
    {
        if (!Regex.IsMatch(expiryDate, @"^(0[1-9]|1[0-2])/?([0-9]{2})$"))
            return false;

        var month = int.Parse(expiryDate.Substring(0, 2));
        var year = int.Parse(expiryDate.Substring(2, 2));
        var expiryDateTime = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1);

        return expiryDateTime > DateTime.UtcNow;
    }

    private bool ValidateCVV(string cvv, string cardNumber)
    {
        if (string.IsNullOrEmpty(cvv) || !cvv.All(char.IsDigit))
            return false;

        // American Express cards have 4-digit CVV
        if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
            return cvv.Length == 4;

        // All other cards have 3-digit CVV
        return cvv.Length == 3;
    }

    private bool ValidateCurrencyCode(string currencyCode)
    {
        // Basic ISO 4217 currency code validation
        return !string.IsNullOrEmpty(currencyCode) &&
               currencyCode.Length == 3 &&
               currencyCode.All(char.IsDigit);
    }

}

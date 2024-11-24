using Microsoft.Extensions.Logging;
using SimplePaymentGateway.Application.Common;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Application.DTOs;
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

    private ResponseCode GenerateRandomResponse()
    {
        var allResponses = Enum.GetValues<ResponseCode>();
        return allResponses[_random.Next(allResponses.Length)];
    }

    private string GetResponseCodeString(ResponseCode responseCode)
    {
        return ((int)responseCode).ToString("D2");
    }

    public async Task<Result<TransactionResponse>> ProcessTransaction(Transaction transaction)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transaction.SystemTraceNr,
            ["ProcessingCode"] = transaction.ProcessingCode,
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
                    ResultType.ValidationError);
            }

            // Check for duplicate transaction
            if (!await CheckDuplicateTransaction(transaction.SystemTraceNr))
            {
                _logger.LogWarning("Duplicate transaction detected: {TransactionId}",
                    transaction.SystemTraceNr);
                return Result<TransactionResponse>.Failure(
                    "Duplicate transaction",
                    ResultType.BadRequest);
            }

            // Process transaction and get response
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
                ResultType.Error);
        }
    }

    public async Task<bool> ValidateTransaction(Transaction transaction)
    {
        try
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrEmpty(transaction.CardNo))
                validationErrors.Add("Card number is required");

            if (string.IsNullOrEmpty(transaction.CardHolder))
                validationErrors.Add("Cardholder name is required");

            if (transaction.AmountTrxn <= 0)
                validationErrors.Add("Amount must be greater than zero");

            if (!await ValidateCardNumber(transaction.CardNo))
                validationErrors.Add("Invalid card number");

            if (!ValidateExpiryDate(transaction.ExpiryDate))
                validationErrors.Add("Invalid or expired card");

            if (!ValidateCVV(transaction.CVV, transaction.CardNo))
                validationErrors.Add("Invalid CVV");

            if (!ValidateCurrencyCode(transaction.CurrencyCode))
                validationErrors.Add("Invalid currency code");

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
        // Simulate processing delay
        await Task.Delay(_random.Next(100, 500));

        // Generate random response
        var responseCode = GenerateRandomResponse();

        // Create response
        var response = new TransactionResponse
        {
            ResponseCode = GetResponseCodeString(responseCode),
            Message = responseCode == ResponseCode.Approved ? "Success" : "Rejected",
            ApprovalCode = responseCode == ResponseCode.Approved ? GenerateApprovalCode() : null,
            DateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
        };

        return response;
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

        return await Task.Run(() =>
        {
            if (!cardNumber.All(char.IsDigit)) return false;

            var sum = 0;
            var isEven = false;

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
        return !string.IsNullOrEmpty(currencyCode) &&
               currencyCode.Length == 3 &&
               currencyCode.All(char.IsDigit);
    }

    private string GenerateResponseCode()
    {
        // Generate random response based on probability
        var probability = _random.NextDouble();

        return probability switch
        {
            // 70% chance of approval
            < 0.7 => ((int)ResponseCode.Approved).ToString("D2"),

            // 10% chance of insufficient funds
            < 0.8 => ((int)ResponseCode.InsufficientFunds).ToString("D2"),

            // 5% chance of rejection
            < 0.85 => ((int)ResponseCode.Rejected).ToString("D2"),

            // 5% chance of reversal
            < 0.9 => ((int)ResponseCode.Reversed).ToString("D2"),

            // 5% chance of do not honor
            < 0.95 => ((int)ResponseCode.DoNotHonor).ToString("D2"),

            // 5% chance of system error
            _ => ((int)ResponseCode.SystemError).ToString("D2")
        };
    }

}

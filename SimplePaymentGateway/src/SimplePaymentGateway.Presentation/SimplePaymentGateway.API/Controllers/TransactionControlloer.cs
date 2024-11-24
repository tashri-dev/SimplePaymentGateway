using Microsoft.AspNetCore.Mvc;
using SimplePaymentGateway.Application.Common;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Application.DTOs;
using System.Text.Json;

namespace SimplePaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]

public class TransactionController : ControllerBase
{
    private readonly ITransactionProcessor _processor;
    private readonly IEncryptionManager _encryptionManager;
    private readonly IKeyManager _keyManager;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        ITransactionProcessor processor,
        IEncryptionManager encryptionManager,
        IKeyManager keyManager,
        ILogger<TransactionController> logger)
    {
        _processor = processor;
        _encryptionManager = encryptionManager;
        _keyManager = keyManager;
        _logger = logger;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessTransaction([FromBody] string encryptedData)
    {
        var transactionId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transactionId,
            ["Timestamp"] = DateTime.UtcNow
        });

        try
        {
            _logger.LogInformation("Received encrypted transaction data: {TransactionId}", transactionId);

            // Get key identifier from header
            if (!Request.Headers.TryGetValue("X-Key-Identifier", out var keyIdentifier))
            {
                _logger.LogWarning("Missing key identifier: {TransactionId}", transactionId);
                return Ok(Result<string>.Failure(
                    "Key identifier is required",
                    ResultType.ValidationError));
            }

            // Retrieve the key using the identifier
            var key = await _keyManager.GetKey(keyIdentifier.ToString());
            if (key == null)
            {
                _logger.LogWarning("Invalid key identifier: {TransactionId}", transactionId);
                return Ok(Result<string>.Failure(
                    "Invalid or expired key",
                    ResultType.ValidationError));
            }

            // Decrypt the request data
            var decryptedData = _encryptionManager.Decrypt(encryptedData, key);
            var transaction = JsonSerializer.Deserialize<TransactionRequest>(decryptedData)?.ToEntity();

            if (transaction == null)
            {
                _logger.LogWarning("Invalid transaction data format: {TransactionId}", transactionId);
                return Ok(Result<string>.Failure(
                    "Invalid transaction data",
                    ResultType.ValidationError));
            }

            // Process the transaction
            var result = await _processor.ProcessTransaction(transaction);

            // Encrypt the response
            var responseJson = JsonSerializer.Serialize(result);
            var encryptedResponse = _encryptionManager.Encrypt(responseJson, key);

            // Invalidate the key after successful processing
            await _keyManager.RemoveKey(keyIdentifier.ToString());

            _logger.LogInformation(
                "Transaction processed and key invalidated: {TransactionId}, Success: {Success}",
                transactionId,
                result.IsSuccess);

            return Ok(Result<string>.Success(encryptedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction: {TransactionId}", transactionId);
            return Ok(Result<string>.Failure(
                "Error processing transaction",
                ResultType.Error));
        }
    }
}

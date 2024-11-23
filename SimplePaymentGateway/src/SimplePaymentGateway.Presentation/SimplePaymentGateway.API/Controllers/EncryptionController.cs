using Microsoft.AspNetCore.Mvc;
using SimplePaymentGateway.Application.Common;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Application.DTOs;

namespace SimplePaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]

public class EncryptionController : ControllerBase
{
    private readonly IEncryptionManager _encryptionManager;
    private readonly IKeyManager _keyManager;
    private readonly ILogger<EncryptionController> _logger;

    public EncryptionController(
        IEncryptionManager encryptionManager,
        IKeyManager keyManager,
        ILogger<EncryptionController> logger)
    {
        _encryptionManager = encryptionManager;
        _keyManager = keyManager;
        _logger = logger;
    }

    [HttpGet("key")]
    [ProducesResponseType(typeof(Result<EncryptionKeyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<EncryptionKeyResponse>>> GetEncryptionKey()
    {
        try
        {
            var key = _encryptionManager.GenerateKey();
            var keyIdentifier = Guid.NewGuid().ToString();
            var iv = _encryptionManager.GenerateIV();

            // Store both key and IV
            await _keyManager.StoreKey(keyIdentifier, key);

            _logger.LogInformation("Generated new encryption key and IV: {KeyIdentifier}", keyIdentifier);

            var response = new EncryptionKeyResponse(
                key: key,
                keyIdentifier: keyIdentifier,
                iv: iv
            );

            return Ok(Result<EncryptionKeyResponse>.Success(response));  // Return directly, ActionResult<T> will handle the Ok() wrapping
        }
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "Error generating encryption key");
        //    return Problem(
        //        detail: "Error generating encryption key",
        //        statusCode: StatusCodes.Status500InternalServerError);
        //}
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key");
            return Ok(Result<EncryptionKeyResponse>.Failure(
                "Error generating encryption key",
                ResultType.Error));
        }
    }
}
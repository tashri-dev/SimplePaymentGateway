using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePaymentGateway.Application.DTOs;

public class EncryptionKeyResponse
{
    // Properties must have public getters and setters for serialization
    public string Key { get; set; }
    public string KeyIdentifier { get; set; }
    public string IV { get; set; }

    public EncryptionKeyResponse(string key, string keyIdentifier, string iv)
    {
        Key = key;
        KeyIdentifier = keyIdentifier;
        IV = iv;
    }

    // Parameterless constructor for serialization
    public EncryptionKeyResponse() { }
}

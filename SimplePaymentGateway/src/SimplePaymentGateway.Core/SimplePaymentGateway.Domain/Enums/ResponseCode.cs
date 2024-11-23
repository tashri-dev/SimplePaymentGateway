namespace SimplePaymentGateway.Domain.Enums;

public enum ResponseCode
{
    Approved = 0,        // "00"
    InsufficientFunds = 1, // "01"
    Rejected = 2,        // "02"
    Reversed = 3,        // "03"
    DoNotHonor = 10,     // "10"
    SystemError = 99     // "99"
}

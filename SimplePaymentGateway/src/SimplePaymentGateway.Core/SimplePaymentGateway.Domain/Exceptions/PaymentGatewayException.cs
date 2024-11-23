namespace SimplePaymentGateway.Domain.Exceptions;

public class PaymentGatewayException : Exception
{
    public string ErrorCode { get; }

    public PaymentGatewayException(string message, string errorCode = "PAYMENT_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public PaymentGatewayException(string message, Exception innerException, string errorCode = "PAYMENT_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

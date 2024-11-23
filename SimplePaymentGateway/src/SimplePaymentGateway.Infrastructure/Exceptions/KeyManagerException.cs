namespace SimplePaymentGateway.Infrastructure.Exceptions;

public class KeyManagerException : Exception
{
    public KeyManagerException(string message) : base(message)
    {
    }

    public KeyManagerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

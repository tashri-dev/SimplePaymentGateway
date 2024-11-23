using SimplePaymentGateway.Domain.Enums;

namespace SimplePaymentGateway.Application.DTOs;

public class TransactionResponse
{
    public string ResponseCode { get; set; }
    public string Message { get; set; }
    public string ApprovalCode { get; set; }
    public string DateTime { get; set; }

    public static TransactionResponse CreateSuccess(string approvalCode)
    {
        return new TransactionResponse
        {
            ResponseCode = "00",
            Message = "Success",
            ApprovalCode = approvalCode,
            DateTime = System.DateTime.Now.ToString("yyyyMMddHHmm")
        };
    }

    public static TransactionResponse CreateError(ResponseCode code)
    {
        return new TransactionResponse
        {
            ResponseCode = ((int)code).ToString().PadLeft(2, '0'),
            Message = GetResponseMessage(code),
            ApprovalCode = string.Empty,
            DateTime = System.DateTime.Now.ToString("yyyyMMddHHmm")
        };
    }

    private static string GetResponseMessage(ResponseCode code) => code switch
    {
        Domain.Enums.ResponseCode.Approved => "Success",
        Domain.Enums.ResponseCode.InsufficientFunds => "Insufficient funds",
        Domain.Enums.ResponseCode.Rejected => "Transaction rejected",
        Domain.Enums.ResponseCode.Reversed => "Transaction reversed",
        Domain.Enums.ResponseCode.DoNotHonor => "Do not honor",
        Domain.Enums.ResponseCode.SystemError => "System error",
        _ => "Unknown error"
    };
}

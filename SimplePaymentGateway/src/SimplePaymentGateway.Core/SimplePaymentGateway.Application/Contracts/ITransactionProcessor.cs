using SimplePaymentGateway.Application.Common;
using SimplePaymentGateway.Application.DTOs;
using SimplePaymentGateway.Domain.Entities;

namespace SimplePaymentGateway.Application.Contracts;

public interface ITransactionProcessor
{
    Task<Result<TransactionResponse>> ProcessTransaction(Transaction transaction);
    Task<bool> ValidateTransaction(Transaction transaction);
}


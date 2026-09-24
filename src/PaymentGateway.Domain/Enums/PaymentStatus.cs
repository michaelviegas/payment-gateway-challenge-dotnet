namespace PaymentGateway.Domain.Enums;

public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected,

    /// <summary>
    /// Recorded before the acquiring bank is called. A payment stuck here means the bank's
    /// outcome was never recorded and must be reconciled.
    /// </summary>
    Pending
}

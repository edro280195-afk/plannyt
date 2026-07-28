namespace Plannyt.Api.Modules.Payments.Domain;

public enum PaymentPlanStatus
{
    Draft,
    Active,
    Completed,
    Cancelled
}

public enum InstallmentType
{
    Deposit,
    ScheduledPayment,
    FinalPayment,
    AdditionalCharge
}

public enum PaymentInstallmentStatus
{
    Pending,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Deposit,
    CardExternal,
    Check,
    Other
}

public enum PaymentRecordStatus
{
    PendingReview,
    Approved,
    Rejected,
    Cancelled,
    Refunded
}

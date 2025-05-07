namespace BillProcessorAPI.Enums
{
    public enum ETransactionStatus
    {
        Pending = 1,
        Successful,
        Created,
        Failed,
        Unsuccessful,
        RECONCILIATION_NEEDED
    }
}

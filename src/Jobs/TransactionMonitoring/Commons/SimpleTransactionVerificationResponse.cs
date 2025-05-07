namespace TransactionMonitoring.Service
{
    public record SimpleTransactionVerificationResponse
    {
        public int TransactionId { get; set; }
        public string StatusMessage { get; set; }
        public string Status { get; set; }
    }
}

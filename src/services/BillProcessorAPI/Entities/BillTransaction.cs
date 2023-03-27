﻿using BillProcessorAPI.Enums;

namespace BillProcessorAPI.Entities
{
    public class BillTransaction : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid? BillPayerInfoId { get; set; }
        public string PayerName { get; set; }
        public string BillNumber { get; set; }
        public string Pid { get; set; }
        public string RevName { get; set; }
        public string PhoneNumber { get; set; }
        public string DueDate { get; set; }
        public EGatewayType GatewayType { get; set; }
        public string Status { get; set; }
        public string TransactionReference { get; set; }
        public string GatewayTransactionReference { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; } // Include charges
        public decimal BillAmount { get; set; }
        public decimal PrinciPalAmount { get; set; } // Amount without charge
        public decimal TransactionCharge { get; set; }
        public decimal GatewayTransactionCharge { get; set; }
        public string Narration { get; set; }
        public string Channel { get; set; }
        public string ResourcePIN { get; set; } // PropertyPIN
        public string PaymentUrl { get; set; }
        public string ReceiptUrl { get; set; }
        public string Receipt { get; set; }
        public string StatusMessage { get; set; }
        public string SuccessIndicator { get; set; }
        public string PaymentInfoRequestData { get; set; }
		public string PaymentInfoResponseData { get; set; }
		public BillPayerInfo BillPayerInfo { get; set; }
    }
}

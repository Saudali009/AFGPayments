using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.Workflows
{
    public class NACHAEntry
    {
        public NACHAEntry(string paymentName, string accountName,string customerId, int paymentType, decimal amount, string reasonOfPayment, string note, string bank, string routingNumber, string accountNumber, string bankBranch)
        {
            PaymentName = paymentName;
            AccountName = accountName;
            PaymentType = paymentType;
            Amount = amount;
            ReasonOfPayment = reasonOfPayment;
            Note = note;
            Bank = bank;
            RoutingNumber = routingNumber;
            AccountNumber = accountNumber;
            BankBranch = bankBranch;
            CustomerId = customerId;
        }
        public string PaymentName { get; set; }
        public string AccountName { get; set; }
        public string CustomerId { get; set; }
        public int PaymentType { get; set; }
        public decimal Amount { get; set; }
        public string ReasonOfPayment { get; set; }
        public string Note { get; set; }
        public string Bank { get; set; }
        public string RoutingNumber { get; set; }
        public string AccountNumber { get; set; }
        public string BankBranch { get; set; }        
    }
}
